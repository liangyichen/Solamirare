namespace Solamirare;

/// <summary>
/// Linux 平台协程调度器，基于 POSIX <c>ucontext</c> 实现。
/// 调度器保存调用方上下文，<see cref="Resume"/> 时切入协程，<see cref="Yield"/> 时切回调用方。
/// </summary>
public unsafe struct LinuxCoroutineScheduler
{
    internal ulong OwnerThreadId;
    internal LinuxCoroutineContext CallerContext;
    internal LinuxCoroutine* Current;

    /// <summary>
    /// 为当前线程创建一个 Linux 协程调度器。
    /// </summary>
    /// <returns>创建成功时返回调度器指针；失败时返回 <see langword="null"/>。</returns>
    public static LinuxCoroutineScheduler* Create()
    {
        if (!LinuxCoroutineStack.IsInitialized) return null;

        var self = (LinuxCoroutineScheduler*)NativeMemory.AlignedAlloc(
            (nuint)sizeof(LinuxCoroutineScheduler), 16);
        if (self == null) return null;

        *self = default;
        self->OwnerThreadId = NativeThread.GetCurrentThreadId();
        return self;
    }

    /// <summary>
    /// 销毁调度器并释放其占用的非托管资源。
    /// </summary>
    /// <param name="scheduler">要销毁的调度器指针。</param>
    public static void Destroy(LinuxCoroutineScheduler* scheduler)
    {
        if (scheduler == null) return;
        scheduler->EnsureThreadAffinity();
        NativeMemory.AlignedFree(scheduler);
    }

    /// <summary>
    /// 切入指定协程，直到其让出执行权或执行完成。
    /// </summary>
    /// <param name="coroutine">要恢复执行的协程。</param>
    public void Resume(LinuxCoroutine* coroutine)
    {
        EnsureThreadAffinity();

        if (coroutine == null || coroutine->IsFinished) return;
        if (Current != null) return;

        Current = coroutine;
        coroutine->IsStarted = true;

        fixed (LinuxCoroutineContext* callerCtx = &CallerContext)
            LinuxAPI.SwapContext(callerCtx, &coroutine->FiberContext);

        Current = null;
    }

    /// <summary>
    /// 由当前协程主动让出执行权，切回调度器调用方。
    /// </summary>
    public void Yield()
    {
        EnsureThreadAffinity();

        if (Current == null) return;

        LinuxCoroutine* current = Current;

        fixed (LinuxCoroutineContext* callerCtx = &CallerContext)
            LinuxAPI.SwapContext(&current->FiberContext, callerCtx);
    }

    [UnmanagedCallersOnly]
    internal static void CoroutineFinish(void* schedulerPtr)
    {
        var scheduler = (LinuxCoroutineScheduler*)schedulerPtr;
        scheduler->EnsureThreadAffinity();

        LinuxCoroutine* current = scheduler->Current;
        current->IsFinished = true;

        LinuxCoroutineContext* callerCtx = &scheduler->CallerContext;
        LinuxAPI.SwapContext(&current->FiberContext, callerCtx);
    }

    private void EnsureThreadAffinity()
    {
        if (OwnerThreadId != 0 &&
            OwnerThreadId != NativeThread.GetCurrentThreadId())
        {
            throw new InvalidOperationException("跨线程访问协程调度器");
        }
    }
}
