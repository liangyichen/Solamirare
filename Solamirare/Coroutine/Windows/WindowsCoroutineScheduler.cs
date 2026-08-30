using System.Runtime.InteropServices;

namespace Solamirare;

/// <summary>
/// Windows 平台协程调度器，基于 Win32 Fiber API 实现。
/// </summary>
public unsafe struct WindowsCoroutineScheduler
{
    private nint _callerFiberHandle;
    private bool _convertedCurrentThreadToFiber;
    internal ulong OwnerThreadId;
    internal WindowsCoroutine* Current;

    /// <summary>
    /// 为当前线程创建一个协程调度器。
    /// </summary>
    /// <returns>创建成功时返回调度器指针；栈配置未初始化或分配失败时返回 <see langword="null"/>。</returns>
    public static WindowsCoroutineScheduler* Create()
    {
        if (!WindowsCoroutineStack.IsInitialized) return null;

        var self = (WindowsCoroutineScheduler*)NativeMemory.AlignedAlloc(
            (nuint)sizeof(WindowsCoroutineScheduler), 16);
        if (self == null) return null;

        *self = default;

        self->_callerFiberHandle = WindowsAPI.ConvertThreadToFiber(nint.Zero);
        if (self->_callerFiberHandle == nint.Zero)
        {
            NativeMemory.AlignedFree(self);
            return null;
        }

        self->_convertedCurrentThreadToFiber = true;
        self->OwnerThreadId = NativeThread.GetCurrentThreadId();
        return self;
    }

    /// <summary>
    /// 销毁调度器并释放其占用的非托管资源。
    /// </summary>
    /// <param name="scheduler">要销毁的调度器指针。</param>
    public static void Destroy(WindowsCoroutineScheduler* scheduler)
    {
        if (scheduler == null) return;

        scheduler->EnsureThreadAffinity();

        if (scheduler->_convertedCurrentThreadToFiber &&
            scheduler->OwnerThreadId == NativeThread.GetCurrentThreadId())
        {
            WindowsAPI.ConvertFiberToThread();
        }

        NativeMemory.AlignedFree(scheduler);
    }

    /// <summary>
    /// 切入指定协程，直到其让出执行权或自然结束。
    /// </summary>
    /// <param name="coroutine">要恢复执行的协程。</param>
    public void Resume(WindowsCoroutine* coroutine)
    {
        EnsureThreadAffinity();

        if (coroutine == null || coroutine->IsFinished) return;
        if (Current != null) return;

        Current = coroutine;
        coroutine->IsStarted = true;

        WindowsAPI.SwitchToFiber(coroutine->Context.FiberHandle);

        Current = null;
    }

    /// <summary>
    /// 由当前协程主动让出执行权，切回调度器调用方。
    /// </summary>
    public void Yield()
    {
        EnsureThreadAffinity();

        if (Current == null) return;

        WindowsAPI.SwitchToFiber(_callerFiberHandle);
    }

    internal static void CoroutineFinish(WindowsCoroutineScheduler* scheduler)
    {
        scheduler->EnsureThreadAffinity();

        WindowsCoroutine* current = scheduler->Current;
        current->IsFinished = true;

        WindowsAPI.SwitchToFiber(scheduler->_callerFiberHandle);
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
