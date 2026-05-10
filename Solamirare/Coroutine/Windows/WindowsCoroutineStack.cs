namespace Solamirare;

/// <summary>
/// Windows 协程栈配置与并发上限管理。
/// <para>
/// Windows 平台的栈内存由 Win32 CreateFiber 按需分配，无需预分配整块内存。
/// 此类只负责记录单个栈大小和最大并发数，
/// 超出并发上限后所有公共方法立即返回，停止一切执行。
/// </para>
/// </summary>
public static unsafe class WindowsCoroutineStack
{
    /// <summary>单个协程的栈大小（字节），传递给 CreateFiber 的 dwStackSize。</summary>
    private static nuint _slotSize;

    /// <summary>最大允许同时存在的协程数量，由 totalSize / slotSize 计算得出。</summary>
    private static int _maxCount;

    /// <summary>当前活跃的协程数量。</summary>
    private static int _activeCount;

    /// <summary>是否已初始化。</summary>
    private static bool _initialized;

    /// <summary>保护计数器的自旋锁。</summary>
    private static SpinLock _lock = new SpinLock(false);

    /// <summary>
    /// 初始化协程栈配置，整个进程只需调用一次。
    /// </summary>
    /// <param name="totalSize">
    /// 总内存预算（字节）。
    /// 不会实际分配此内存，仅用于计算最大并发数：maxCount = totalSize / slotSize。
    /// </param>
    /// <param name="slotSize">
    /// 单个协程的栈大小（字节），建议 512KB 以上。
    /// 传递给 Win32 CreateFiber 的 dwStackSize 参数。
    /// </param>
    /// <exception cref="InvalidOperationException">重复初始化时抛出。</exception>
    /// <exception cref="ArgumentException">参数不合法时抛出。</exception>
    public static void Initialize(nuint totalSize, nuint slotSize)
    {
        if (_initialized) return; // 已初始化后再次调用 Initialize 不会抛出异常，而是直接返回，保持幂等性。

        if (slotSize == 0)
            throw new ArgumentException("slotSize 不能为 0", nameof(slotSize));

        if (totalSize < slotSize)
            throw new ArgumentException("totalSize 不能小于 slotSize", nameof(totalSize));

        _slotSize    = slotSize;
        _maxCount    = (int)(totalSize / slotSize);
        _activeCount = 0;
        _initialized = true;
    }

    /// <summary>单个协程栈大小，传给 CreateFiber。</summary>
    public static nuint SlotSize => _slotSize;

    /// <summary>最大并发协程数。</summary>
    public static int MaxCount => _maxCount;

    /// <summary>
    /// 尝试占用一个槽位。
    /// </summary>
    /// <returns>成功返回 true，已达上限返回 false。</returns>
    public static bool TryAcquire()
    {
        if (!_initialized) return false;

        bool lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);
            if (_activeCount >= _maxCount) return false;
            _activeCount++;
            return true;
        }
        finally
        {
            if (lockTaken) _lock.Exit();
        }
    }

    /// <summary>
    /// 归还一个槽位，在协程销毁时调用。
    /// </summary>
    public static void Release()
    {
        bool lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);
            if (_activeCount > 0) _activeCount--;
        }
        finally
        {
            if (lockTaken) _lock.Exit();
        }
    }

    /// <summary>当前活跃协程数量。</summary>
    /// <summary>Gets the number of currently active coroutine slots.</summary>
    public static int ActiveCount => _activeCount;

    /// <summary>是否已初始化。</summary>
    public static bool IsInitialized => _initialized;
}
