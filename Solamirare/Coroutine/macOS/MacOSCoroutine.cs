namespace Solamirare;

/// <summary>
/// Represents a macOS coroutine backed by a manually managed stack slot.
/// </summary>
public unsafe struct MacOSCoroutine
{
    internal MacOSCoroutineContext Context;

    /// <summary>所属调度器指针，供 Coroutine.Resume / Yield 使用。</summary>
    internal MacOSCoroutineScheduler* Scheduler;

    /// <summary>
    /// Gets a value indicating whether the coroutine has been resumed at least once.
    /// </summary>
    public bool IsStarted  { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether the coroutine entry function has completed.
    /// </summary>
    public bool IsFinished { get; internal set; }
    internal int StackSlotIndex;

#if COROUTINE_DEBUG
    public CoroutineDebugInfo DebugInfo;
#endif

    /// <summary>
    /// Creates a new macOS coroutine bound to the specified scheduler.
    /// </summary>
    /// <param name="scheduler">Owning scheduler.</param>
    /// <param name="entry">Coroutine entry function.</param>
    /// <param name="param">User parameter passed to the entry function.</param>
    /// <returns>The created coroutine pointer, or <see langword="null"/> when creation fails.</returns>
    public static MacOSCoroutine* Create(
        MacOSCoroutineScheduler* scheduler,
        delegate* unmanaged<void*, void> entry,
        void* param)
    {
        if (scheduler == null || entry == null) return null;

        var self = (MacOSCoroutine*)NativeMemory.AlignedAlloc(
            (nuint)sizeof(MacOSCoroutine), 16);
        if (self == null) return null;

        *self = default;

        // 新增：存入 scheduler 指针
        self->Scheduler = scheduler;

        void* stackTop = MacOSCoroutineStack.Alloc(out int slotIndex);
        if (stackTop == null)
        {
            NativeMemory.AlignedFree(self);
            return null;
        }

        self->StackSlotIndex = slotIndex;

        MacOSPlatformSwitch.InitContext(
            &self->Context,
            stackTop,
            entry,
            param,
            &MacOSCoroutineScheduler.CoroutineFinish,
            scheduler);

        return self;
    }

    /// <summary>
    /// Destroys a coroutine that has already finished execution.
    /// </summary>
    /// <param name="coroutine">Coroutine pointer to destroy.</param>
    /// <exception cref="InvalidOperationException">Thrown when the coroutine is still running.</exception>
    public static void Destroy(MacOSCoroutine* coroutine)
    {
        if (coroutine == null) return;
        if (coroutine->IsStarted && !coroutine->IsFinished)
            throw new InvalidOperationException("不能销毁尚未完成的协程");
        if (coroutine->StackSlotIndex >= 0)
            MacOSCoroutineStack.Free(coroutine->StackSlotIndex);
        NativeMemory.AlignedFree(coroutine);
    }
}
