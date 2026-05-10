using System.Runtime.InteropServices;

namespace Solamirare;

/// <summary>
/// 全局静态协程栈内存池。
/// <para>
/// 在程序启动时通过 <see cref="Initialize"/> 一次性分配整块内存，
/// 内部按固定槽大小切割，每个协程占用一个槽。
/// 协程结束后归还槽，供后续协程复用。
/// </para>
/// <para>
/// 线程安全：槽的分配和归还通过 SpinLock 保护，支持多线程并发创建协程。
/// </para>
/// </summary>
public static unsafe class MacOSCoroutineStack
{
    /// <summary>整块栈内存的起始地址。</summary>
    private static byte*  _memory;

    /// <summary>单个栈槽的大小（字节），必须是页大小（4096）的整数倍。</summary>
    private static nuint  _slotSize;

    /// <summary>总槽数量，由总内存大小除以槽大小得出。</summary>
    private static int    _slotCount;

    /// <summary>
    /// 空闲槽位图，每个 bit 对应一个槽。
    /// 1 表示空闲，0 表示已占用。
    /// 使用 long 数组，每个 long 管理 64 个槽。
    /// </summary>
    private static long*  _freeBitmap;

    /// <summary>位图数组的长度（long 的个数）。</summary>
    private static int    _bitmapLength;
    private static int    _activeCount;

    /// <summary>保护位图分配和归还操作的自旋锁。</summary>
    private static SpinLock _lock = new SpinLock(false);

    /// <summary>是否已初始化。</summary>
    private static bool _initialized;

    /// <summary>
    /// 初始化全局栈内存池。必须在任何协程创建之前调用一次。
    /// </summary>
    /// <param name="totalSize">
    /// 总内存大小（字节）。
    /// 实际可用槽数 = totalSize / slotSize。
    /// 建议根据最大并发协程数估算：并发数 × slotSize。
    /// </param>
    /// <param name="slotSize">
    /// 单个协程栈槽的大小（字节），必须是 4096 的整数倍。
    /// 建议值：普通协程 512KB（524288），有深层递归的协程 2MB（2097152）。
    /// </param>
    /// <exception cref="InvalidOperationException">重复初始化时抛出。</exception>
    /// <exception cref="ArgumentException">参数不合法时抛出。</exception>
    /// <exception cref="OutOfMemoryException">内存分配失败时抛出。</exception>
    public static void Initialize(nuint totalSize, nuint slotSize)
    {
        if (_initialized) return; // 已初始化后再次调用 Initialize 不会抛出异常，而是直接返回，保持幂等性。

        if (slotSize < 4096 || slotSize % 4096 != 0)
            throw new ArgumentException("slotSize 必须是 4096 的整数倍且不小于 4096", nameof(slotSize));

        if (totalSize < slotSize)
            throw new ArgumentException("totalSize 不能小于 slotSize", nameof(totalSize));

        _slotSize   = slotSize;
        _slotCount  = (int)(totalSize / slotSize);
        _bitmapLength = (_slotCount + 63) / 64;
        _activeCount = 0;

        // 分配主栈内存，按页对齐
        _memory = (byte*)NativeMemory.AlignedAlloc(totalSize, 4096);
        if (_memory == null)
            throw new OutOfMemoryException($"协程栈内存分配失败，请求大小：{totalSize} 字节");

        // 分配位图内存
        _freeBitmap = (long*)NativeMemory.AlignedAlloc(
            (nuint)(_bitmapLength * sizeof(long)), 8);
        if (_freeBitmap == null)
        {
            NativeMemory.AlignedFree(_memory);
            throw new OutOfMemoryException("协程栈位图内存分配失败");
        }

        // 所有槽初始化为空闲（bit = 1）
        for (int i = 0; i < _bitmapLength; i++)
            _freeBitmap[i] = -1L; // 0xFFFFFFFFFFFFFFFF，全部 bit 置 1

        // 如果 slotCount 不是 64 的整数倍，把最后一个 long 中多余的 bit 清零
        // 防止分配到不存在的槽
        int remainder = _slotCount % 64;
        if (remainder != 0)
        {
            // 只保留低 remainder 个 bit 为 1
            _freeBitmap[_bitmapLength - 1] = (1L << remainder) - 1L;
        }

        // 为每个槽的底部设置 Guard Page，防止栈溢出静默覆盖相邻槽
        SetupGuardPages();

        _initialized = true;
    }

    /// <summary>
    /// 从内存池分配一个栈槽。
    /// </summary>
    /// <param name="slotIndex">输出分配到的槽编号，归还时需要传回。</param>
    /// <returns>
    /// 栈顶地址（高地址端）。
    /// ARM64 栈向低地址增长，协程启动时 sp 应指向此地址。
    /// 分配失败时返回 null（内存池已满）。
    /// </returns>
    public static void* Alloc(out int slotIndex)
    {
        slotIndex = -1;
        if (!_initialized || _memory == null || _freeBitmap == null) return null;

        bool lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);

            // 遍历位图，找到第一个有空闲 bit 的 long
            for (int i = 0; i < _bitmapLength; i++)
            {
                long word = _freeBitmap[i];
                if (word == 0L) continue; // 此 long 所有槽都已占用

                // 找到最低位的 1（即最低编号的空闲槽）
                // TrailingZeroCount 返回从最低位开始连续 0 的个数
                int bit = System.Numerics.BitOperations.TrailingZeroCount((ulong)word);

                // 将该 bit 清零，标记为已占用
                _freeBitmap[i] = word & ~(1L << bit);

                slotIndex = i * 64 + bit;
                _activeCount++;

                // 返回该槽的栈顶地址（槽起始地址 + 槽大小 = 高地址端）
                byte* slotBase = _memory + (nuint)slotIndex * _slotSize;
                return slotBase + _slotSize;
            }

            // 所有槽已满
            return null;
        }
        finally
        {
            if (lockTaken) _lock.Exit();
        }
    }

    /// <summary>
    /// 归还一个栈槽到内存池。
    /// </summary>
    /// <param name="slotIndex">由 <see cref="Alloc"/> 返回的槽编号。</param>
    public static void Free(int slotIndex)
    {
        if (!_initialized || _freeBitmap == null) return;
        if (slotIndex < 0 || slotIndex >= _slotCount) return;

        bool lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);

            int wordIndex = slotIndex / 64;
            int bit       = slotIndex % 64;

            // 将对应 bit 置 1，标记为空闲
            _freeBitmap[wordIndex] |= (1L << bit);
            if (_activeCount > 0) _activeCount--;
        }
        finally
        {
            if (lockTaken) _lock.Exit();
        }
    }

    /// <summary>
    /// 获取指定槽的栈底地址（低地址端，Guard Page 之上）。
    /// 仅供内部诊断使用。
    /// </summary>
    public static void* GetSlotBase(int slotIndex)
    {
        if (!_initialized || _memory == null) return null;
        if (slotIndex < 0 || slotIndex >= _slotCount) return null;
        return _memory + (nuint)slotIndex * _slotSize;
    }

    /// <summary>单个槽大小。</summary>
    public static nuint SlotSize => _slotSize;

    /// <summary>总槽数量。</summary>
    public static int SlotCount => _slotCount;
    /// <summary>Gets the number of stack slots currently in use.</summary>
    public static int ActiveCount => _activeCount;

    /// <summary>
    /// 为每个槽的底部设置 Guard Page。
    /// ARM64 栈向低地址增长，Guard Page 设在槽的低地址端。
    /// 当栈溢出触碰 Guard Page 时触发 SIGSEGV，而不是静默覆盖相邻数据。
    /// </summary>
    private static void SetupGuardPages()
    {
        for (int i = 0; i < _slotCount; i++)
        {
            byte* slotBase = _memory + (nuint)i * _slotSize;
            // PROT_NONE = 0，不可读不可写不可执行
            MacOSAPI.MProtect(slotBase, 4096, 0);
        }
    }

    /// <summary>
    /// 释放整个栈内存池。通常在程序退出时调用。
    /// </summary>
    public static void Dispose()
    {
        if (!_initialized) return;
        if (_activeCount > 0)
            throw new InvalidOperationException("仍有活动协程时不能释放协程栈");
        if (_memory      != null) NativeMemory.AlignedFree(_memory);
        if (_freeBitmap  != null) NativeMemory.AlignedFree(_freeBitmap);
        _memory     = null;
        _freeBitmap = null;
        _slotSize = 0;
        _slotCount = 0;
        _bitmapLength = 0;
        _activeCount = 0;
        _initialized = false;
    }
}
