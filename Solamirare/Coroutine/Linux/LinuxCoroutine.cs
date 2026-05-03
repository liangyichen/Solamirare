namespace Solamirare;

/// <summary>
/// Linux 平台协程，基于 POSIX ucontext 实现。
/// <para>
/// 所有实例通过 <see cref="Create"/> 创建，通过 <see cref="Destroy"/> 释放。
/// 实例本身分配在非托管堆，不受 GC 管理。
/// </para>
/// <para>
/// 约束：
///   同一个协程的 Create / Resume / Destroy 必须在同一个线程上执行。
///   不能在协程内部调用 Resume，只能调用 Yield。
///   协程入口函数必须标注 [UnmanagedCallersOnly]。
/// </para>
/// </summary>
public unsafe struct LinuxCoroutine
{
    /// <summary>协程自己的 ucontext，保存执行状态。</summary>
    internal LinuxCoroutineContext FiberContext;

    /// <summary>所属调度器指针。</summary>
    internal LinuxCoroutineScheduler* Scheduler;

    /// <summary>协程是否已经开始执行。</summary>
    public bool IsStarted { get; internal set; }

    /// <summary>协程入口函数是否已执行完毕。</summary>
    public bool IsFinished { get; internal set; }

    /// <summary>在栈内存池中占用的槽编号，Destroy 时用于归还。</summary>
    internal int StackSlotIndex;

    /// <summary>
    /// trampoline 参数结构体，分配在非托管堆，生命周期与协程相同。
    /// makecontext 只能传 int，把所有参数打包在此结构体里，
    /// 通过指针拆分后传入 trampoline 函数。
    /// </summary>
    internal LinuxCoroutineTrampolineArgs* TrampolineArgs;

    #if COROUTINE_DEBUG
    public CoroutineDebugInfo DebugInfo;
    #endif

    // ────────────────────────────────────────────────────────────────────────────
    //  生命周期
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 创建一个 Linux 协程。
    /// </summary>
    /// <param name="scheduler">所属调度器。</param>
    /// <param name="entry">
    /// 协程入口函数，必须标注 [UnmanagedCallersOnly]。
    /// 签名：void Entry(void* param)。
    /// </param>
    /// <param name="param">透传给入口函数的参数指针。</param>
    /// <returns>
    /// 成功返回协程指针。
    /// 栈内存池已满时返回 null。
    /// </returns>
    public static LinuxCoroutine* Create(
        LinuxCoroutineScheduler* scheduler,
        delegate* unmanaged<void*, void> entry,
        void* param)
    {
        if (scheduler == null || entry == null) return null;

        // 分配栈槽
        void* stackTop = LinuxCoroutineStack.Alloc(out int slotIndex);
        if (stackTop == null) return null;

        // 分配协程结构体
        var self = (LinuxCoroutine*)NativeMemory.AlignedAlloc(
            (nuint)sizeof(LinuxCoroutine), 16);
        if (self == null)
        {
            LinuxCoroutineStack.Free(slotIndex);
            return null;
        }

        *self = default;
        self->Scheduler = scheduler;
        self->StackSlotIndex = slotIndex;

        // 分配 trampoline 参数结构体
        self->TrampolineArgs = (LinuxCoroutineTrampolineArgs*)NativeMemory.AlignedAlloc(
            (nuint)sizeof(LinuxCoroutineTrampolineArgs), 16);
        if (self->TrampolineArgs == null)
        {
            NativeMemory.AlignedFree(self);
            LinuxCoroutineStack.Free(slotIndex);
            return null;
        }

        // 填充 trampoline 参数
        self->TrampolineArgs->Entry = entry;
        self->TrampolineArgs->Param = param;
        self->TrampolineArgs->FinishCallback = &LinuxCoroutineScheduler.CoroutineFinish;
        self->TrampolineArgs->Scheduler = scheduler;

        // 初始化 ucontext
        void* stackBase = (byte*)stackTop - LinuxCoroutineStack.SlotSize;

        // getcontext 填充基础字段
        // 注意：getcontext 在这里是为了获取当前线程的上下文模板，
        // 我们会修改其中的栈指针和入口点。
        LinuxAPI.GetContext(&self->FiberContext);

        // 设置栈
        // 关键修复：直接修改 uc_stack 结构体内部的 ss_sp 和 ss_size
        self->FiberContext.StackPointer = stackBase;    // 栈底指针
        self->FiberContext.StackSize = LinuxCoroutineStack.SlotSize; // 栈大小

        // uc_link = null，入口函数返回后由 trampoline 手动处理
        self->FiberContext.Link = null;

        // 纠正：必须传递 TrampolineArgs 的指针，而不是 self 的指针
        // 否则跳板机重组后会访问错误的内存布局
        ulong argsPtr = (ulong)self->TrampolineArgs;
        int argsHi = (int)(argsPtr >> 32);
        int argsLo = (int)(argsPtr & 0xFFFFFFFF);

        // 设置 trampoline 为入口函数，argc=2 传两个 int
        LinuxAPI.MakeContext(
            &self->FiberContext,
            (nint)(delegate* unmanaged<int, int, void>)&Trampoline, // Trampoline 现在接收 coroutine*
            2,
            argsHi,
            argsLo);

        return self;
    }

    /// <summary>
    /// 释放协程占用的所有资源。
    /// </summary>
    /// <param name="coroutine">由 <see cref="Create"/> 返回的协程指针。</param>
    public static void Destroy(LinuxCoroutine* coroutine)
    {
        if (coroutine == null) return;
        if (coroutine->IsStarted && !coroutine->IsFinished)
            throw new InvalidOperationException("不能销毁尚未完成的协程");

        if (coroutine->TrampolineArgs != null)
        {
            NativeMemory.AlignedFree(coroutine->TrampolineArgs);
            coroutine->TrampolineArgs = null;
        }

        if (coroutine->StackSlotIndex >= 0)
            LinuxCoroutineStack.Free(coroutine->StackSlotIndex);

        NativeMemory.AlignedFree(coroutine);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  内部 trampoline
    //  解决 makecontext 只能传 int 的限制
    //  接收拆分的两个 int，重组为 LinuxCoroutineTrampolineArgs*，
    //  调用真正的入口函数，入口函数返回后调用 FinishCallback
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// makecontext 的跳板入口函数。
    /// <para>
    /// 将两个 int 重组为 LinuxCoroutineTrampolineArgs*，
    /// 从中取出入口函数、参数、完成回调，
    /// 调用真正的入口函数后自动调用完成回调，对用户完全透明。
    /// </para>
    /// </summary>
    [UnmanagedCallersOnly]
    private static void Trampoline(int argsHi, int argsLo)
    {
        // 重组 64 位指针
        var args = (LinuxCoroutineTrampolineArgs*)
            (void*)(((ulong)(uint)argsHi << 32) | (uint)argsLo);

        // 调用用户定义的真正入口函数
        args->Entry(args->Param);

        // 入口函数返回后调用完成回调，通知调度器
        args->FinishCallback(args->Scheduler);

        // FinishCallback 不会返回（它会切换回调用方）
        // 以防万一进入死循环
        while (true) { }
    }
}
