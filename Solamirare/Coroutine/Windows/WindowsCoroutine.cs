namespace Solamirare;

/// <summary>
/// Represents a Windows fiber-backed coroutine instance.
/// </summary>
public unsafe struct WindowsCoroutine
{
    internal WindowsCoroutineContext Context;

    /// <summary>所属调度器指针，供 Coroutine.Resume / Yield 使用。</summary>
    internal WindowsCoroutineScheduler* Scheduler;

    /// <summary>
    /// Gets a value indicating whether the coroutine has been resumed at least once.
    /// </summary>
    public bool IsStarted  { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether the coroutine entry function has completed.
    /// </summary>
    public bool IsFinished { get; internal set; }
    internal bool StackSlotReleased;
    internal delegate* unmanaged<void*, void> Entry;
    internal void* Param;

#if COROUTINE_DEBUG
    public CoroutineDebugInfo DebugInfo;
#endif

    /// <summary>
    /// Creates a new Windows coroutine bound to the specified scheduler.
    /// </summary>
    /// <param name="scheduler">Owning scheduler.</param>
    /// <param name="entry">Coroutine entry function.</param>
    /// <param name="param">User parameter passed to the entry function.</param>
    /// <returns>The created coroutine pointer, or <see langword="null"/> when creation fails.</returns>
    public static WindowsCoroutine* Create(
        WindowsCoroutineScheduler* scheduler,
        delegate* unmanaged<void*, void> entry,
        void* param)
    {
        if (scheduler == null || entry == null) return null;
        if (!WindowsCoroutineStack.TryAcquire()) return null;

        var self = (WindowsCoroutine*)NativeMemory.AlignedAlloc(
            (nuint)sizeof(WindowsCoroutine), 16);
        if (self == null)
        {
            WindowsCoroutineStack.Release();
            return null;
        }

        *self = default;

        // 新增：存入 scheduler 指针
        self->Scheduler = scheduler;
        self->Entry     = entry;
        self->Param     = param;

        self->Context.FiberHandle = WindowsAPI.CreateFiber(
            WindowsCoroutineStack.SlotSize,
            &FiberProc,
            self);

        if (self->Context.FiberHandle == nint.Zero)
        {
            NativeMemory.AlignedFree(self);
            WindowsCoroutineStack.Release();
            return null;
        }

        return self;
    }

    /// <summary>
    /// Destroys a coroutine that has already finished execution.
    /// </summary>
    /// <param name="coroutine">Coroutine pointer to destroy.</param>
    /// <exception cref="InvalidOperationException">Thrown when the coroutine is still running.</exception>
    public static void Destroy(WindowsCoroutine* coroutine)
    {
        if (coroutine == null) return;
        if (coroutine->IsStarted && !coroutine->IsFinished)
            throw new InvalidOperationException("不能销毁尚未完成的协程");
        if (coroutine->Context.FiberHandle != nint.Zero)
        {
            WindowsAPI.DeleteFiber(coroutine->Context.FiberHandle);
            coroutine->Context.FiberHandle = nint.Zero;
        }

        if (!coroutine->StackSlotReleased)
        {
            WindowsCoroutineStack.Release();
            coroutine->StackSlotReleased = true;
        }

        NativeMemory.AlignedFree(coroutine);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    private static void FiberProc(void* param)
    {
        WindowsCoroutine* self = (WindowsCoroutine*)param;
        self->Entry(self->Param);
        WindowsCoroutineScheduler.CoroutineFinish(self->Scheduler);
        WindowsAPI.ExitThread(1);
    }
}
