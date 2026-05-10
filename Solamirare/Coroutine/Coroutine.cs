namespace Solamirare;

/// <summary>
/// 跨平台协程操作入口。
/// </summary>
public static unsafe class Coroutine
{
    /// <summary>
    /// 创建一个协程实例。
    /// </summary>
    /// <param name="scheduler">所属调度器指针。</param>
    /// <param name="entry">
    /// 协程入口函数指针，必须标注 [UnmanagedCallersOnly]。
    /// 签名：void Entry(void* param)。
    /// 入口函数自然返回即视为协程完成，无需手动调用任何结束函数。
    /// </param>
    /// <param name="param">透传给入口函数的参数指针。</param>
    /// <returns>
    /// 成功返回协程指针。
    /// 并发数已达上限或内存不足时返回 null。
    /// 返回 null 后所有后续操作均安全，立即返回不执行任何操作。
    /// </returns>
    public static void* Create(
        void* scheduler,
        delegate* unmanaged<void*, void> entry,
        void* param)
    {
        if (scheduler == null) return null;
#if PLATFORM_WINDOWS
        return WindowsCoroutine.Create(
            (WindowsCoroutineScheduler*)scheduler, entry, param);
#elif PLATFORM_LINUX
        return LinuxCoroutine.Create(
            (LinuxCoroutineScheduler*)scheduler, entry, param);
#elif PLATFORM_MACOS
        return MacOSCoroutine.Create(
            (MacOSCoroutineScheduler*)scheduler, entry, param);
#else
        return null;
#endif
    }

    /// <summary>
    /// 切入指定协程，直到协程调用 Yield 或执行完毕后返回。
    /// <para>
    /// 从 coroutine 内部自动获取所属调度器，调用方无需传入 scheduler。
    /// 协程完成时自动归还栈槽，调用方只需最终调用 <see cref="Destroy"/> 释放结构体。
    /// </para>
    /// <para>coroutine 为 null 或已完成时立即返回。</para>
    /// </summary>
    /// <param name="coroutine">要切入的协程指针。</param>
    public static void Resume(void* coroutine)
    {
        if (coroutine == null) return;

#if PLATFORM_WINDOWS
        var c = (WindowsCoroutine*)coroutine;

#if COROUTINE_DEBUG
        if (c->IsFinished) { c->DebugInfo.ResumeAfterFinishedCount++; return; }
        c->DebugInfo.ResumeCount++;
#else
        if (c->IsFinished) return;
#endif

        c->Scheduler->Resume(c);

        if (c->IsFinished)
        {
            WindowsCoroutineStack.Release();
            c->StackSlotReleased = true;
        }

#if COROUTINE_DEBUG
        ValidateCounts(&c->DebugInfo, c->IsFinished);
#endif

#elif PLATFORM_LINUX
        var c = (LinuxCoroutine*)coroutine;

#if COROUTINE_DEBUG
        if (c->IsFinished) { c->DebugInfo.ResumeAfterFinishedCount++; return; }
        c->DebugInfo.ResumeCount++;
#else
        if (c->IsFinished) return;
#endif

        c->Scheduler->Resume(c);

        if (c->IsFinished && c->StackSlotIndex >= 0)
        {
            LinuxCoroutineStack.Free(c->StackSlotIndex);
            c->StackSlotIndex = -1;
        }

#if COROUTINE_DEBUG
        ValidateCounts(&c->DebugInfo, c->IsFinished);
#endif

#elif PLATFORM_MACOS
        var c = (MacOSCoroutine*)coroutine;

#if COROUTINE_DEBUG
        if (c->IsFinished) { c->DebugInfo.ResumeAfterFinishedCount++; return; }
        c->DebugInfo.ResumeCount++;
#else
        if (c->IsFinished) return;
#endif

        c->Scheduler->Resume(c);

        if (c->IsFinished && c->StackSlotIndex >= 0)
        {
            MacOSCoroutineStack.Free(c->StackSlotIndex);
            c->StackSlotIndex = -1;
        }

#if COROUTINE_DEBUG
        ValidateCounts(&c->DebugInfo, c->IsFinished);
#endif

#endif
    }

    /// <summary>
    /// 在协程内部调用，将执行权让回调用方。
    /// <para>
    /// 从 coroutine 内部自动获取所属调度器，调用方无需传入 scheduler。
    /// 协程在此处挂起，直到下次 <see cref="Resume"/> 时从此处之后继续执行。
    /// </para>
    /// <para>coroutine 为 null 或在协程外部调用时立即返回。</para>
    /// </summary>
    /// <param name="coroutine">当前协程指针。</param>
    public static void Yield(void* coroutine)
    {
        if (coroutine == null) return;

#if PLATFORM_WINDOWS
        var c = (WindowsCoroutine*)coroutine;
#if COROUTINE_DEBUG
        c->DebugInfo.YieldCount++;
#endif
        c->Scheduler->Yield();

#elif PLATFORM_LINUX
        var c = (LinuxCoroutine*)coroutine;
#if COROUTINE_DEBUG
        c->DebugInfo.YieldCount++;
#endif
        c->Scheduler->Yield();

#elif PLATFORM_MACOS
        var c = (MacOSCoroutine*)coroutine;
#if COROUTINE_DEBUG
        c->DebugInfo.YieldCount++;
#endif
        c->Scheduler->Yield();

#endif
    }

    /// <summary>
    /// 释放协程结构体本身。
    /// <para>
    /// 栈槽已在最后一次 <see cref="Resume"/> 返回时自动释放，
    /// 此方法只释放协程结构体占用的非托管内存。
    /// 建议在 <see cref="IsFinished"/> 返回 true 后调用。
    /// </para>
    /// </summary>
    /// <param name="coroutine">由 <see cref="Create"/> 返回的协程指针。</param>
    public static void Destroy(void* coroutine)
    {
        if (coroutine == null) return;
#if PLATFORM_WINDOWS
        var c = (WindowsCoroutine*)coroutine;
        if (c->Scheduler == null) return;
        if (c->Scheduler != null &&
            c->Scheduler->OwnerThreadId != 0 &&
            c->Scheduler->OwnerThreadId != NativeThread.GetCurrentThreadId())
        {
            throw new InvalidOperationException("跨线程销毁协程");
        }
        WindowsCoroutine.Destroy(c);
#elif PLATFORM_LINUX
        var c = (LinuxCoroutine*)coroutine;
        if (c->Scheduler == null) return;
        if (c->Scheduler != null &&
            c->Scheduler->OwnerThreadId != 0 &&
            c->Scheduler->OwnerThreadId != NativeThread.GetCurrentThreadId())
        {
            throw new InvalidOperationException("跨线程销毁协程");
        }
        LinuxCoroutine.Destroy(c);
#elif PLATFORM_MACOS
        var c = (MacOSCoroutine*)coroutine;
        if (c->Scheduler == null) return;
        if (c->Scheduler != null &&
            c->Scheduler->OwnerThreadId != 0 &&
            c->Scheduler->OwnerThreadId != NativeThread.GetCurrentThreadId())
        {
            throw new InvalidOperationException("跨线程销毁协程");
        }
        MacOSCoroutine.Destroy(c);
#endif
    }

    /// <summary>获取协程是否已经开始执行。</summary>
    public static bool IsStarted(void* coroutine)
    {
        if (coroutine == null) return false;
#if PLATFORM_WINDOWS
        return ((WindowsCoroutine*)coroutine)->IsStarted;
#elif PLATFORM_LINUX
        return ((LinuxCoroutine*)coroutine)->IsStarted;
#elif PLATFORM_MACOS
        return ((MacOSCoroutine*)coroutine)->IsStarted;
#else
        return false;
#endif
    }

    /// <summary>
    /// 获取协程是否已执行完毕。
    /// <para>为 true 后调用 <see cref="Destroy"/> 释放协程结构体。</para>
    /// </summary>
    public static bool IsFinished(void* coroutine)
    {
        if (coroutine == null) return false;
#if PLATFORM_WINDOWS
        return ((WindowsCoroutine*)coroutine)->IsFinished;
#elif PLATFORM_LINUX
        return ((LinuxCoroutine*)coroutine)->IsFinished;
#elif PLATFORM_MACOS
        return ((MacOSCoroutine*)coroutine)->IsFinished;
#else
        return false;
#endif
    }

#if COROUTINE_DEBUG
    /// <summary>
    /// 获取协程的调试信息。
    /// 仅在 COROUTINE_DEBUG 模式下可用。
    /// </summary>
    public static CoroutineDebugInfo* GetDebugInfo(void* coroutine)
    {
        if (coroutine == null) return null;
#if PLATFORM_WINDOWS
        return &((WindowsCoroutine*)coroutine)->DebugInfo;
#elif PLATFORM_LINUX
        return &((LinuxCoroutine*)coroutine)->DebugInfo;
#elif PLATFORM_MACOS
        return &((MacOSCoroutine*)coroutine)->DebugInfo;
#else
        return null;
#endif
    }

    /// <summary>
    /// 验证 Resume / Yield 计数是否匹配，不匹配时记录异常。
    /// </summary>
    private static void ValidateCounts(CoroutineDebugInfo* dbg, bool isFinished)
    {
        if (!isFinished && dbg->ResumeCount > dbg->YieldCount + 1)
            dbg->MismatchCount++;
        if (dbg->YieldCount > dbg->ResumeCount)
            dbg->MismatchCount++;
    }
#endif
}
