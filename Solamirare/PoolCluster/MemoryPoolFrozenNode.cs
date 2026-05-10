namespace Solamirare;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

/// <summary>
/// 单个内存池节点，管理一批等长内存块的分配与回收。
/// 内部以空闲索引栈（int[]）跟踪可用块，以位图（ulong[]）记录分配状态。
/// <para>
/// 线程安全：Alloc / Free 均在 SpinLock 保护的临界区内完成，
/// 消除了"原子计数器 + 非原子数组访问"模式下计数器更新与数组读写之间的竞态窗口。
/// </para>
/// </summary>
[SkipLocalsInit]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Size = 64)]
public unsafe ref struct MemoryPoolFrozenNode
{
    /// <summary>所有内存块的起始地址。</summary>
    byte* innerMemory;

    /// <summary>位图数组，每个 bit 对应一个块的分配状态（1 = 已分配，0 = 空闲）。</summary>
    ulong* nodesAllocStaus;

    /// <summary>所有内存块的总字节数（boxSize × boxiesCount）。</summary>
    ulong memorySize;

    /// <summary>空闲块索引栈的起始地址；栈顶游标为 _stackIndex。</summary>
    int* _stackBase;

    /// <summary>当前栈顶（即下一个可用槽位的上方），初始值等于 _stackCapacity。</summary>
    int _stackIndex;

    /// <summary>栈的最大容量，等于块总数。</summary>
    int _stackCapacity;

    /// <summary>每个块的字节长度。</summary>
    uint boxSize;

    /// <summary>boxSize 的二进制位移量（boxSize 为 2 的幂时使用），否则为 -1。</summary>
    int _pow2Shift;

    bool _isDisposed;

    /// <summary>保护 Alloc / Free 临界区的自旋锁。</summary>
    SpinLock _lock;

    // ──────────────────────────────────────────────────────────────────────────
    // 只读属性
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>内存块总数。</summary>
    public ulong Capacity => (ulong)_stackCapacity;

    /// <summary>
    /// 当前空闲块数量。
    /// 使用 Volatile.Read 确保读取到最新值，避免编译器/CPU 缓存旧值。
    /// </summary>
    public uint FreeNodesCount => (uint)Volatile.Read(ref _stackIndex);

    /// <summary>每个内存块的字节长度。</summary>
    public uint MemoryBlocksBytesSize => boxSize;

    // ──────────────────────────────────────────────────────────────────────────
    // 静态工具
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 计算给定配置下内存池所需的总字节数。
    /// 内部布局（低地址 → 高地址）：位图 | 空闲索引栈 | 内存块数组。
    /// </summary>
    public static ulong CalulateTotalMemorySize(ulong boxSize, uint boxiesCount)
    {
        ulong nodesBytesSize = boxSize * boxiesCount;
        ulong UmapUnits = (boxiesCount + 64 - 1) / 64;
        ulong bitmapBytesSize = UmapUnits * sizeof(ulong);
        ulong freeStackInnerMemorySize = (ulong)boxiesCount * sizeof(int);
        return bitmapBytesSize + freeStackInnerMemorySize + nodesBytesSize;
    }

    static bool validateTotalMemorySize(ulong externalMemorySize, ulong boxSize, uint boxiesCount)
        => externalMemorySize >= CalulateTotalMemorySize(boxSize, boxiesCount);

    // ──────────────────────────────────────────────────────────────────────────
    // 构造函数
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 在调用方提供的外部内存块上初始化内存池节点。
    /// </summary>
    /// <param name="externalMemory">外部内存起始地址。</param>
    /// <param name="externalMemorySize">外部内存字节数。</param>
    /// <param name="boxSize">每个内存块的字节长度。</param>
    /// <param name="boxiesCount">内存块总数。</param>
    public MemoryPoolFrozenNode(byte* externalMemory, ulong externalMemorySize, uint boxSize, uint boxiesCount)
    {
        if (boxSize > ulong.MaxValue / boxiesCount)
            throw new ArgumentException("boxSize * boxiesCount overflows ulong.");

        memorySize = boxSize * boxiesCount;

        if (externalMemory is null || memorySize == 0)
            return;

        if (!validateTotalMemorySize(externalMemorySize, boxSize, boxiesCount))
            throw new ArgumentException("External memory size is insufficient for the configuration.");

        this.boxSize = boxSize;

        _pow2Shift = ((boxSize & (boxSize - 1)) == 0)
            ? BitOperations.TrailingZeroCount(boxSize)
            : -1;

        ulong UmapUnits = ((ulong)boxiesCount + 63) >> 6;
        ulong bitmapBytesSize = UmapUnits * sizeof(ulong);

        nodesAllocStaus = (ulong*)externalMemory;
        NativeMemory.Clear(nodesAllocStaus, (nuint)bitmapBytesSize);

        _stackBase = (int*)((byte*)nodesAllocStaus + bitmapBytesSize);
        _stackCapacity = (int)boxiesCount;
        _stackIndex = (int)boxiesCount;

        innerMemory = (byte*)(_stackBase + boxiesCount);

        for (int i = 0; i < (int)boxiesCount; i++)
            _stackBase[i] = i;

        _lock = new SpinLock(enableThreadOwnerTracking: false);
        _isDisposed = false;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 内部工具
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 将指针换算回块索引；指针不合法时返回 ulong.MaxValue。
    /// </summary>
    internal ulong CalculateBlockIndex(void* P)
    {
        if (P is null) return ulong.MaxValue;

        nuint startAddress = (nuint)innerMemory;
        nuint pointerAddress = (nuint)P;

        if (pointerAddress < startAddress)
            return ulong.MaxValue;

        nuint byteOffset = pointerAddress - startAddress;

        if (_pow2Shift >= 0)
        {
            if ((byteOffset & ((nuint)boxSize - 1)) != 0)
                return ulong.MaxValue;
            return byteOffset >> _pow2Shift;
        }
        else
        {
            if (byteOffset % boxSize != 0)
                return ulong.MaxValue;
            return byteOffset / boxSize;
        }
    }

    /// <summary>
    /// 验证指针是否属于本池（范围 + 对齐）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool AddressFromPool(void* p)
    {
        if (p is null) return false;
        nuint ptrValue = (nuint)p;
        nuint start = (nuint)innerMemory;
        nuint end = start + (nuint)memorySize;
        return ptrValue >= start
            && ptrValue < end
            && ((ptrValue - start) % (nuint)boxSize) == 0;
    }

    
    // ──────────────────────────────────────────────────────────────────────────
    // 分配 / 回收
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 从池中分配一个块，返回其起始地址；池耗尽时返回 null。
    /// <para>
    /// 整个 Pop 操作（栈顶递减 + 读取索引 + 位图置位）在 SpinLock 内完成，
    /// 消除计数器更新与数组读取之间的竞态窗口。
    /// </para>
    /// </summary>
    public byte* Alloc(bool clear = true, bool @lock = true)
    {
        bool taken = false;
        if(@lock) _lock.Enter(ref taken);

        byte* p;
        try
        {
            if (_stackIndex <= 0)
                return null;

            int idx = --_stackIndex;
            int nodeIndexInt = _stackBase[idx];
            ulong nodeIndex = (ulong)nodeIndexInt;

            p = innerMemory + nodeIndex * boxSize;

            // 位图置位（已在锁内，无需 Interlocked）
            ulong UnitIndex = nodeIndex >> 6;
            ulong ShiftAmount = nodeIndex & 63;
            nodesAllocStaus[UnitIndex] |= 1UL << (int)ShiftAmount;
        }
        finally
        {
            if (taken && @lock) _lock.Exit(useMemoryBarrier: false);
        }

        if (clear) NativeMemory.Clear(p, (nuint)boxSize);

        return p;
    }

    /// <summary>
    /// 将块归还到池；重复释放或地址非法时返回 false。
    /// <para>
    /// 整个 Push 操作（位图清位 + 写入索引 + 栈顶递增）在 SpinLock 内完成，
    /// 消除位图清位与栈写入之间的竞态窗口，以及栈索引递增与数组写入之间的窗口。
    /// </para>
    /// </summary>
    public bool Free(void* address, ulong blockSize, bool clear = false)
    {
        if (address == null) return false;

        nuint ptrValue = (nuint)address;
        nuint start = (nuint)innerMemory;
        nuint byteOffset = ptrValue - start;

        if (byteOffset >= (nuint)memorySize)
            return false;

        ulong nodeIndex;

        if (_pow2Shift >= 0)
        {
            if ((byteOffset & ((nuint)boxSize - 1)) != 0) return false;
            nodeIndex = byteOffset >> _pow2Shift;
        }
        else
        {
            if (byteOffset % boxSize != 0) return false;
            nodeIndex = byteOffset / boxSize;
        }

        ulong UnitIndex = nodeIndex >> 6;
        ulong ShiftAmount = nodeIndex & 63;
        ulong BitMask = 1UL << (int)ShiftAmount;

        bool taken = false;
        _lock.Enter(ref taken);
        try
        {
            // 位图检查：若对应位为 0，说明块未分配，属重复释放
            if ((nodesAllocStaus[UnitIndex] & BitMask) == 0)
                return false;

            // 位图清位
            nodesAllocStaus[UnitIndex] &= ~BitMask;

            // 栈越界检查（防御 double-free 导致的溢出）
            if (_stackIndex >= _stackCapacity)
                return false;

            // Push：写入索引后递增栈顶
            _stackBase[_stackIndex++] = (int)nodeIndex;
        }
        finally
        {
            if (taken) _lock.Exit(useMemoryBarrier: false);
        }

        if (clear) NativeMemory.Clear(address, (nuint)boxSize);

        return true;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 生命周期
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 清零所有内存块内容并重置栈索引，使节点可被重新使用。
    /// </summary>
    public void Dispose()
    {
        if (innerMemory is not null)
            NativeMemory.Clear(innerMemory, (nuint)memorySize);

        _stackIndex = 0;
    }
}