namespace Solamirare;

/// <summary>
/// macOS 平台协程调度器，负责在调用方与协程上下文之间切换执行权。
/// </summary>
public unsafe struct MacOSCoroutineScheduler
{
    internal ulong OwnerThreadId;
    internal MacOSCoroutineContext CallerContext;
    internal MacOSCoroutine* Current;

    private static readonly delegate* unmanaged<void*, void> _finishCallback
        = &CoroutineFinish;

    /// <summary>
    /// 为当前线程创建一个 macOS 协程调度器。
    /// </summary>
    /// <returns>创建成功时返回调度器指针。</returns>
    public static MacOSCoroutineScheduler* Create()
    {
        var self = (MacOSCoroutineScheduler*)NativeMemory.AlignedAlloc(
            (nuint)sizeof(MacOSCoroutineScheduler), 16);
        if (self == null)
            throw new OutOfMemoryException("调度器内存分配失败");

        *self = default;
        self->OwnerThreadId = NativeThread.GetCurrentThreadId();
        return self;
    }

    /// <summary>
    /// 销毁调度器并释放其占用的非托管资源。
    /// </summary>
    /// <param name="scheduler">要销毁的调度器指针。</param>
    public static void Destroy(MacOSCoroutineScheduler* scheduler)
    {
        if (scheduler == null) return;
        scheduler->EnsureThreadAffinity();
        NativeMemory.AlignedFree(scheduler);
    }

    /// <summary>
    /// 切入指定协程，直到其让出执行权或执行完成。
    /// </summary>
    /// <param name="coroutine">要恢复执行的协程。</param>
    public void Resume(MacOSCoroutine* coroutine)
    {
        EnsureThreadAffinity();

        if (coroutine->IsFinished)
            throw new InvalidOperationException("不能 Resume 已完成的协程");

        if (Current != null)
            throw new InvalidOperationException("不能在协程内部调用 Resume");

        Current = coroutine;
        coroutine->IsStarted = true;

        fixed (MacOSCoroutineContext* callerCtx = &CallerContext)
            MacOSPlatformSwitch.Switch(callerCtx, &coroutine->Context);

        Current = null;
    }

    /// <summary>
    /// 由当前协程主动让出执行权，切回调度器调用方。
    /// </summary>
    public void Yield()
    {
        EnsureThreadAffinity();

        if (Current == null)
            throw new InvalidOperationException("Yield 只能在协程内部调用");

        MacOSCoroutine* current = Current;

        fixed (MacOSCoroutineContext* callerCtx = &CallerContext)
            MacOSPlatformSwitch.Switch(&current->Context, callerCtx);
    }

    [UnmanagedCallersOnly]
    internal static void CoroutineFinish(void* schedulerPtr)
    {
        MacOSCoroutineScheduler* scheduler = (MacOSCoroutineScheduler*)schedulerPtr;
        scheduler->EnsureThreadAffinity();

        MacOSCoroutine* current = scheduler->Current;
        current->IsFinished = true;

        MacOSCoroutineContext* callerCtx = &scheduler->CallerContext;
        MacOSPlatformSwitch.Switch(&current->Context, callerCtx);
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
