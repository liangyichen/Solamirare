using Solamirare;
using System.Runtime.CompilerServices;

/// <summary>
/// 多规格内存池集群，将若干个 <see cref="MemoryPoolFrozenNode"/> 按块大小升序组织，
/// 通过查找表（直接表或跳转表）将分配请求路由到合适的子池。
/// <para>
/// 线程安全：路由逻辑（SelectPool）本身是无状态的只读操作；
/// 实际的分配/回收安全由各子池的内部 SpinLock 保证。
/// </para>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct MemoryPoolCluster
{
    private const int JUMP_TABLE_SIZE = 1024;

    // ── 热路径字段（紧凑排列，提高缓存命中率） ────────────────────────────────
    private uint* _tablePtr;
    private MemoryPoolFrozenNode* _poolsPtr;
    private uint _minLimit;
    private uint _maxLimit;
    private uint _poolsCount;
    private byte _granularityShift;
    private byte _useJump;

    // ── 冷路径字段 ─────────────────────────────────────────────────────────────
    private ulong _totalMemorySize;
    private void* _handle;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Align64(ulong size) => (size + 63) & ~63UL;

    // ──────────────────────────────────────────────────────────────────────────
    // 路由
    // ──────────────────────────────────────────────────────────────────────────


    /// <summary>
    /// 检查是否支持该容量的内存分配
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public bool Support(ulong size)
    {
        return SelectPool(size) != null;
    }


    /// <summary>
    /// 根据请求字节数找到能容纳该大小的最小子池；超出范围时返回 null。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MemoryPoolFrozenNode* SelectPool(ulong size)
    {
        if (size > _maxLimit || size < _minLimit)
            return null;

        uint index;
        if (_useJump == 0)
        {
            // 直接表：size 在 [0, _maxLimit] 内，直接下标
            index = _tablePtr[(uint)size];
        }
        else
        {
            // 跳转表：先用粒度位移定位候选起点，再线性步进到第一个满足容量的池
            uint jIdx = (uint)(size >> _granularityShift);
            if (jIdx >= JUMP_TABLE_SIZE) jIdx = JUMP_TABLE_SIZE - 1;
            index = _tablePtr[jIdx];

            MemoryPoolFrozenNode* pArr = _poolsPtr;
            while (index < _poolsCount && pArr[index].MemoryBlocksBytesSize < (uint)size)
                index++;
        }

        return index < _poolsCount ? &_poolsPtr[index] : null;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 初始化
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 按给定的 Schema 列表初始化集群。
    /// Schema 会在内部按 NodeSize 升序排序；调用方无需预先排序。
    /// </summary>
    public bool Init(Span<MemoryPoolSchema> schemas)
    {
        // 1. 按 NodeSize 升序排序（冒泡，Schema 数量通常极少）
        for (int i = 0; i < schemas.Length - 1; i++)
            for (int j = i + 1; j < schemas.Length; j++)
                if (schemas[i].NodeSize > schemas[j].NodeSize)
                    (schemas[i], schemas[j]) = (schemas[j], schemas[i]);

        _minLimit = schemas[0].NodeSize;
        _maxLimit = schemas[schemas.Length - 1].NodeSize;
        _poolsCount = (uint)schemas.Length;

        // 2. 路由策略：块大小 ≤ 4096 且池数 ≤ 16 时用直接表，否则用跳转表
        _useJump = (byte)(_maxLimit > 4096 || _poolsCount > 16 ? 1 : 0);
        uint tableEntries = _useJump == 1 ? (uint)JUMP_TABLE_SIZE : _maxLimit + 1;

        if (_useJump == 1)
        {
            // 计算粒度：向上取整到最近的 2 的幂
            uint rawGranularity = (_maxLimit + JUMP_TABLE_SIZE - 1) >> 10;
            _granularityShift = 0;
            uint pow2 = 1;
            while (pow2 < rawGranularity) { pow2 <<= 1; _granularityShift++; }
        }

        // 3. 内存布局计算（各段均 64 字节对齐）
        uint tableByteSize = (uint)Align64((ulong)sizeof(uint) * tableEntries);
        uint schemasByteSize = (uint)Align64((ulong)sizeof(MemoryPoolSchema) * _poolsCount);
        uint nodesByteSize = (uint)Align64((ulong)sizeof(MemoryPoolFrozenNode) * _poolsCount);

        _totalMemorySize = tableByteSize + schemasByteSize + nodesByteSize;
        for (int i = 0; i < schemas.Length; i++)
            _totalMemorySize += Align64(MemoryPoolFrozenNode.CalulateTotalMemorySize(schemas[i].NodeSize, schemas[i].Count));


        // 4. 内存申请（暂时屏蔽全局基础池，这样更有利于多个集群同时工作）
        byte* basePtr;


        basePtr = (byte*)NativeMemory.AlignedAlloc((nuint)_totalMemorySize, SolamirareEnvironment.ALIGNMENT);


        NativeMemory.Clear(basePtr, (nuint)_totalMemorySize);

        // 5. 指针切分：查找表 | Schema 临时区 | FrozenNode 数组 | 各子池数据
        _tablePtr = (uint*)basePtr;
        _poolsPtr = (MemoryPoolFrozenNode*)(basePtr + tableByteSize + schemasByteSize);

        // Schema 区仅在 Init 期间用于填充查找表，之后不再访问，
        // 直接从已分配的布局内借用这段空间即可（无需单独 stackalloc）
        MemoryPoolSchema* schemaArray = (MemoryPoolSchema*)(basePtr + tableByteSize);

        byte* dataIter = basePtr + tableByteSize + schemasByteSize + nodesByteSize;

        for (int i = 0; i < (int)_poolsCount; i++)
        {
            schemaArray[i] = schemas[i];
            ulong subSize = MemoryPoolFrozenNode.CalulateTotalMemorySize(schemas[i].NodeSize, schemas[i].Count);
            _poolsPtr[i] = new MemoryPoolFrozenNode(dataIter, subSize, schemas[i].NodeSize, schemas[i].Count);
            dataIter += Align64(subSize);
        }

        InitializeLookup(schemaArray, _tablePtr);
        return true;
    }

    /// <summary>
    /// 填充路由查找表。
    /// 直接表：table[s] = 能容纳 s 字节的最小池索引。
    /// 跳转表：table[i] = 粒度槽 i 对应的候选起始池索引（SelectPool 会线性步进确认）。
    /// </summary>
    private void InitializeLookup(MemoryPoolSchema* schemas, uint* table)
    {
        if (_useJump == 0)
        {
            uint poolIdx = 0;
            for (uint s = 0; s <= _maxLimit; s++)
            {
                while (poolIdx < _poolsCount && schemas[poolIdx].NodeSize < s) poolIdx++;
                table[s] = (poolIdx < _poolsCount) ? poolIdx : uint.MaxValue;
            }
        }
        else
        {
            uint poolIdx = 0;
            uint step = 1U << _granularityShift;
            for (uint i = 0; i < JUMP_TABLE_SIZE; i++)
            {
                uint boundary = i * step + 1;
                while (poolIdx < _poolsCount && schemas[poolIdx].NodeSize < boundary) poolIdx++;
                table[i] = poolIdx;
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 分配 / 回收
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 从集群中分配一个能容纳 <paramref name="allocSize"/> 字节的块，无合适子池或子池耗尽时返回 null。
    /// <para>传入逻辑上的数量即可，比如业务需要 17 byte，那么就传入 17， 不需要管内部是否会向上取整</para>
    /// </summary>
    /// <param name="allocSize">传入逻辑上的数量即可，比如业务需要 17 byte，那么就传入 17， 不需要管内部是否会向上取整</param>
    /// <param name="clear"></param>
    /// <param name="lock"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MemoryPollAllocatedResult Alloc(ulong allocSize, bool clear = true, bool @lock = true)
    {
        int realSize = MemoryAlignmentHelper.Align((int)allocSize);

        MemoryPoolFrozenNode* pool = SelectPool((ulong)realSize);

        if (pool == null) return new MemoryPollAllocatedResult { Address = null, BytesSize = 0 };

        byte* bytes = pool->Alloc(clear, @lock);

        MemoryPollAllocatedResult result = new MemoryPollAllocatedResult
        {
            Address = bytes,

            BytesSize = (uint)realSize
        };

        return result;
    }

    /// <summary>
    /// 将 <paramref name="address"/> 指向的块归还到集群。
    /// 长度参数使用分配时的输入长度（逻辑长度）。
    /// 地址非法或重复释放时返回 false。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Return(void* address, ulong blockSize, bool clear = false)
    {
        if (address == null) return false;

        int realSize = MemoryAlignmentHelper.Align((int)blockSize);

        MemoryPoolFrozenNode* pool = SelectPool((ulong)realSize);

        bool result = pool != null && pool->Free(address, blockSize, clear);

        return result;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 生命周期
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 释放集群占用的全部内存。
    /// </summary>
    public void Dispose()
    {
        if (_tablePtr == null) return;

        for (int i = 0; i < (int)_poolsCount; i++)
            _poolsPtr[i].Dispose();

        NativeMemory.AlignedFree(_tablePtr);

        _tablePtr = null;
        _poolsPtr = null;
    }
}