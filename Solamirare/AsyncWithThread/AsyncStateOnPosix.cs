namespace Solamirare;

/// <summary>
/// 表示 macOS 与 Linux 共用的 Posix 异步状态结构。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct AsyncStateOnPosix
{
    /// <summary>异步操作完成时执行的回调函数指针。</summary>
    public delegate* unmanaged<void*, void> Callback;

    /// <summary>传递给回调函数的用户数据。</summary>
    public void* UserData;

    /// <summary>当前状态码。</summary>
    public int State;

    /// <summary>是否应停止当前异步流程。</summary>
    public int ShouldStop;

    /// <summary>读取端文件描述符。</summary>
    public int ReadFd;

    /// <summary>写入端文件描述符。</summary>
    public int WriteFd;

    /// <summary>标记异步任务是否已完成。</summary>
    public int IsFinished;

    // ── 条件变量同步原语 ──────────────────────────────────────────────────────
    // pthread_mutex_t 在 macOS（arm64/x86_64）上为 56 字节
    // pthread_cond_t  在 macOS（arm64/x86_64）上为 40 字节
    // 使用固定字节数组占位，确保结构体内存布局与 libc 完全匹配。

    /// <summary>
    /// 保护 State / IsFinished 的互斥锁，配合 CompletionCond 使用。
    /// Wait() 持锁检查条件，工作线程持锁修改条件后发出 signal。
    /// </summary>
    public PthreadMutex CompletionMutex;

    /// <summary>
    /// 任务完成信号的条件变量。
    /// 工作线程回调执行完毕后调用 pthread_cond_signal，唤醒挂起在 Wait() 中的主线程。
    /// </summary>
    public PthreadCond CompletionCond;
}