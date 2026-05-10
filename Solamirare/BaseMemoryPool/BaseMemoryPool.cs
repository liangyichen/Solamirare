using System.Runtime.CompilerServices;

namespace Solamirare;


// --- 核心结构定义 ---

[StructLayout(LayoutKind.Sequential)]
public unsafe struct SlabNodeHeader
{
    public BaseMemoryPoolSlab* ParentSlab; // 8 bytes
    public bool IsFree;                   // 1 byte
    private fixed byte _padding[23];       // 维持 32 字节对齐
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public unsafe struct BaseMemoryPoolSlab
{
    // --- 热数据 (Cache Line 1) ---
    public void* NextFree;         // 偏移 0: 分配的核心
    public uint AllocatedCount;    // 偏移 8: 逻辑判断核心
    public uint SlotSize;          // 偏移 12: 计算核心
    public uint Capacity;          // 偏移 16
    public byte* DataStart;        // 偏移 24

    // --- 温数据 ---
    public BaseMemoryPoolSlab* Next; // 偏移 32
    public BaseMemoryPoolSlab* Prev; // 偏移 40
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct BaseMemoryPoolHandleEntry
{
    public byte* Pointer;
    public ulong Length;
    public void* PhysicalAddr;
    public BaseMemoryPoolSlab* Slab;
    public BaseMemoryPoolHandleEntry* NextFree;
    private fixed ulong _padding[3];       // 凑足 64 字节，对齐 Cache Line
}

[StructLayout(LayoutKind.Sequential)]
public unsafe ref struct MemoryPoolStats
{
    public ulong TotalPhysicalUsed;
    public ulong TotalUserRequested;
    public ulong TotalFreeInSlabs;
    public uint ActiveObjects;
    public uint TotalSlabs;
    public uint CachedSlabs;
    private uint _pad0;

    public double Utilization => TotalPhysicalUsed == 0 ? 100 : (TotalUserRequested * 100.0 / TotalPhysicalUsed);

    public override string ToString()
    {
        return $"[内存池统计报表]\n" +
               $"--------------------------------------\n" +
               $"存活对象数: {ActiveObjects:N0}\n" +
               $"活跃 Slabs: {TotalSlabs:N0} (缓存中: {CachedSlabs})\n" +
               $"物理占用内存: {TotalPhysicalUsed / 1024.0 / 1024.0:F2} MB\n" +
               $"用户载荷内存: {TotalUserRequested / 1024.0 / 1024.0:F2} MB\n" +
               $"Slab 内余量 : {TotalFreeInSlabs / 1024.0 / 1024.0:F2} MB\n" +
               $"整体利用率  : {Utilization:F2}%\n" +
               $"--------------------------------------";
    }
}

// --- 内存池主逻辑 ---

public unsafe partial struct BaseMemoryPool
{


    private static readonly uint SlabHeaderSize = (uint)sizeof(BaseMemoryPoolSlab);

    private static readonly uint NodeHeaderSize = (uint)sizeof(SlabNodeHeader);

    private const uint SmallThreshold = 2048;

    private const uint SlabSize = 16384;

    private const uint MaxEmptySlabCache = 4;


    private fixed ulong _slabBuckets[128];

    private BaseMemoryPoolSlab* _emptySlabCache;

    private BaseMemoryPoolHandleEntry* _freeHandles;

    private BaseMemoryPoolHandleEntry** _handlePoolsTrack;

    private ulong _totalPhysical;

    private ulong _totalUser;

    private uint _emptySlabCacheCount;

    private uint _handlePoolCount;

    private uint _activeCount;

    private uint _slabCount;


    public BaseMemoryPool(ulong unused = 0)
    {
        _handlePoolsTrack = (BaseMemoryPoolHandleEntry**)NativeMemory.AllocZeroed(4096 * (nuint)sizeof(void*));
        _freeHandles = CreateHandlePool();

        fixed (ulong* p = _slabBuckets)
        {
            NativeMemory.Clear(p, 128 * sizeof(ulong));
        }

        _emptySlabCache = null;
        _emptySlabCacheCount = 0;
        _activeCount = 0;
        _slabCount = 0;
        _totalPhysical = 0;
        _totalUser = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetBucketIndex(uint slotSize) => (int)((slotSize >> 5) - 1);

    public BaseMemoryPoolHandleEntry* Alloc(ulong size)
    {
        if (size == 0) return null;
        return (size > SmallThreshold) ? AllocLarge(size) : AllocSmall((uint)size);
    }
    private BaseMemoryPoolHandleEntry* AllocSmall(uint size)
    {
        // 1. 快速计算规格与桶索引
        uint slotSize = ((size + 31) & ~31u) + NodeHeaderSize;
        int bucketIdx = (int)((slotSize >> 5) - 1);

        fixed (ulong* buckets = _slabBuckets)
        {
            // 获取桶内第一个 Slab 的指针地址
            BaseMemoryPoolSlab** bucketHead = (BaseMemoryPoolSlab**)&buckets[bucketIdx];
            BaseMemoryPoolSlab* slab = *bucketHead;

            // --- 核心热路径优化 (Fast Path) ---
            // 绝大多数高频分配场景下，桶的第一个 Slab 都有空闲空间
            if (slab != null && slab->NextFree != null)
            {
                return PopHandleFromFreeList(slab, size);
            }

            // --- 次热路径 (Slow Path: 遍历链表) ---
            if (slab != null)
            {
                // 只有第一个 Slab 满了，才进入循环查找后续 Slab
                var curr = slab->Next;
                while (curr != null)
                {
                    if (curr->NextFree != null)
                    {
                        // 提升该 Slab 到桶头部，下次分配直接命中 Fast Path (LRU 思想)
                        // 这一步能大幅优化后续同规格分配的性能
                        curr->Prev->Next = curr->Next;
                        if (curr->Next != null) curr->Next->Prev = curr->Prev;

                        curr->Next = *bucketHead;
                        curr->Prev = null;
                        (*bucketHead)->Prev = curr;
                        *bucketHead = curr;

                        return PopHandleFromFreeList(curr, size);
                    }
                    curr = curr->Next;
                }
            }

            // --- 最终路径 (Cold Path: 创建或从缓存获取) ---
            BaseMemoryPoolSlab* newSlab = GetSlabFromCache(slotSize);
            if (newSlab == null) newSlab = CreateSlab(slotSize);

            // 插入到桶链表头部
            newSlab->Next = *bucketHead;
            if (*bucketHead != null) (*bucketHead)->Prev = newSlab;
            *bucketHead = newSlab;

            return PopHandleFromFreeList(newSlab, size);
        }
    }

    private BaseMemoryPoolHandleEntry* PopHandleFromFreeList(BaseMemoryPoolSlab* slab, uint userSize)
    {
        void* slotAddr = slab->NextFree;
        if (slotAddr == null) return null;

        // 弹出嵌入式链表节点
        slab->NextFree = *(void**)slotAddr;

        byte* p = (byte*)slotAddr;
        
        SlabNodeHeader* node = (SlabNodeHeader*)p;

        node->IsFree = false;

        slab->AllocatedCount++;
        _activeCount++;
        _totalUser += userSize;

        var h = PopHandle();
        h->PhysicalAddr = p;
        h->Pointer = p + NodeHeaderSize;
        h->Length = (ulong)userSize;
        h->Slab = slab;
        return h;
    }

    public void Free(BaseMemoryPoolHandleEntry* handle)
    {
        if (handle == null || handle->PhysicalAddr == null) return;

        var node = (SlabNodeHeader*)handle->PhysicalAddr;
        if (node->IsFree) return;

        node->IsFree = true;
        _activeCount--;
        _totalUser -= handle->Length;

        if (handle->Slab != null)
        {
            var slab = handle->Slab;
            void* p = handle->PhysicalAddr;

            // 归还至嵌入式链表
            *(void**)p = slab->NextFree;
            slab->NextFree = p;

            slab->AllocatedCount--;
            if (slab->AllocatedCount == 0) DeactivateSlab(slab);
        }
        else
        {
            ulong totalFreed = handle->Length + NodeHeaderSize;
            NativeMemory.AlignedFree(handle->PhysicalAddr);
            _totalPhysical -= totalFreed;
        }

        PushHandle(handle);
    }

    private void DeactivateSlab(BaseMemoryPoolSlab* slab)
    {
        int bucketIdx = GetBucketIndex(slab->SlotSize);
        fixed (ulong* buckets = _slabBuckets)
        {
            BaseMemoryPoolSlab** bucketHead = (BaseMemoryPoolSlab**)&buckets[bucketIdx];

            if (slab->Prev != null) slab->Prev->Next = slab->Next;
            if (slab == *bucketHead) *bucketHead = slab->Next;
            if (slab->Next != null) slab->Next->Prev = slab->Prev;
        }

        _slabCount--;

        if (_emptySlabCacheCount < MaxEmptySlabCache)
        {
            slab->Next = _emptySlabCache;
            _emptySlabCache = slab;
            _emptySlabCacheCount++;
        }
        else
        {
            _totalPhysical -= (SlabSize + SlabHeaderSize);
            NativeMemory.AlignedFree(slab);
        }
    }

    private BaseMemoryPoolSlab* CreateSlab(uint slotSize)
    {
        uint total = SlabSize + SlabHeaderSize;
        void* raw = NativeMemory.AlignedAlloc(total, 64);
        NativeMemory.Clear(raw, total);

        var slab = (BaseMemoryPoolSlab*)raw;
        slab->DataStart = (byte*)raw + SlabHeaderSize;
        slab->Capacity = SlabSize;
        slab->SlotSize = slotSize;

        InitSlabChain(slab, slotSize);

        _slabCount++;
        _totalPhysical += total;
        return slab;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InitSlabChain(BaseMemoryPoolSlab* slab, uint slotSize)
    {
        byte* p = slab->DataStart;
        byte* lastValid = slab->DataStart + SlabSize - slotSize;
        slab->NextFree = p;

        while (p <= lastValid)
        {
            ((SlabNodeHeader*)p)->IsFree = true;
            ((SlabNodeHeader*)p)->ParentSlab = slab;

            byte* next = p + slotSize;
            if (next <= lastValid)
                *(void**)p = next;
            else
                *(void**)p = null;
            p = next;
        }
    }

    private BaseMemoryPoolSlab* GetSlabFromCache(uint slotSize)
    {
        if (_emptySlabCache == null) return null;

        BaseMemoryPoolSlab* slab = _emptySlabCache;
        _emptySlabCache = slab->Next;
        _emptySlabCacheCount--;

        slab->SlotSize = slotSize;
        slab->AllocatedCount = 0;
        slab->Next = null;
        slab->Prev = null;

        InitSlabChain(slab, slotSize);

        _slabCount++;
        return slab;
    }

    private BaseMemoryPoolHandleEntry* AllocLarge(ulong size)
    {
        ulong totalAlloc = size + NodeHeaderSize;
        void* raw = NativeMemory.AlignedAlloc((nuint)totalAlloc, 64);
        NativeMemory.Clear(raw, (nuint)totalAlloc);

        ((SlabNodeHeader*)raw)->IsFree = false;
        ((SlabNodeHeader*)raw)->ParentSlab = null;

        _activeCount++;
        _totalPhysical += totalAlloc;
        _totalUser += size;

        var h = PopHandle();
        h->PhysicalAddr = raw;
        h->Pointer = (byte*)raw + NodeHeaderSize;
        h->Length = size;
        h->Slab = null;
        return h;
    }

    public MemoryPoolStats GetStatistics() => new MemoryPoolStats
    {
        ActiveObjects = _activeCount,
        TotalSlabs = _slabCount,
        CachedSlabs = _emptySlabCacheCount,
        TotalPhysicalUsed = _totalPhysical,
        TotalUserRequested = _totalUser,
        TotalFreeInSlabs = CalculateFreeSize()
    };

    private ulong CalculateFreeSize()
    {
        ulong totalFree = 0;
        fixed (ulong* buckets = _slabBuckets)
        {
            for (int i = 0; i < 128; i++)
            {
                BaseMemoryPoolSlab* s = (BaseMemoryPoolSlab*)buckets[i];
                while (s != null)
                {
                    uint maxSlots = s->Capacity / s->SlotSize;
                    uint freeCount = maxSlots - s->AllocatedCount;
                    totalFree += (ulong)freeCount * (s->SlotSize - NodeHeaderSize);
                    s = s->Next;
                }
            }
        }
        return totalFree;
    }

    public void Dispose()
    {
        fixed (ulong* buckets = _slabBuckets)
        {
            for (int i = 0; i < 128; i++)
            {
                BaseMemoryPoolSlab* curr = (BaseMemoryPoolSlab*)buckets[i];
                while (curr != null)
                {
                    var next = curr->Next;
                    NativeMemory.AlignedFree(curr);
                    curr = next;
                }
                buckets[i] = 0;
            }
        }

        var cache = _emptySlabCache;
        while (cache != null)
        {
            var next = cache->Next;
            NativeMemory.AlignedFree(cache);
            cache = next;
        }

        for (uint i = 0; i < _handlePoolCount; i++) NativeMemory.Free(_handlePoolsTrack[i]);
        NativeMemory.Free(_handlePoolsTrack);

        _totalPhysical = 0; _totalUser = 0; _activeCount = 0; _slabCount = 0;
    }

    public uint ChunksCount => _activeCount;
    public uint HandlesCount => _handlePoolCount * 1024;

    private BaseMemoryPoolHandleEntry* PopHandle()
    {
        if (_freeHandles == null) _freeHandles = CreateHandlePool();
        var h = _freeHandles;
        _freeHandles = h->NextFree;
        return h;
    }

    private void PushHandle(BaseMemoryPoolHandleEntry* h)
    {
        h->Pointer = null; h->PhysicalAddr = null; h->Slab = null; h->Length = 0;
        h->NextFree = _freeHandles;
        _freeHandles = h;
    }

    private BaseMemoryPoolHandleEntry* CreateHandlePool()
    {
        var pool = (BaseMemoryPoolHandleEntry*)NativeMemory.AllocZeroed(1024 * (nuint)sizeof(BaseMemoryPoolHandleEntry));
        _handlePoolsTrack[_handlePoolCount++] = pool;
        for (int i = 0; i < 1023; i++) pool[i].NextFree = &pool[i + 1];
        return pool;
    }
}