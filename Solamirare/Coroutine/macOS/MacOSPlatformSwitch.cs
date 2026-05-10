namespace Solamirare;



/// <summary>
/// 平台切换层，所有平台相关的操作都通过此类进行。
/// <para>
/// 上层的 Coroutine 和 CoroutineScheduler 只调用此类，
/// 不直接接触任何平台 API 或汇编函数。
/// </para>
/// </summary>
public static unsafe partial class MacOSPlatformSwitch
{


    // 静态字段缓存跳板地址
    // 直接取 coroutine_entry_trampoline 的函数指针转为 ulong
    private static readonly ulong _trampolineAddress =
        (ulong)(delegate*<void>)&MacOSAPI.coroutine_entry_trampoline;

    /// <summary>
    /// 初始化协程上下文，使第一次切入时从 entry 开始执行。
    /// </summary>
    /// <param name="ctx">要初始化的协程上下文。</param>
    /// <param name="stackTop">栈顶地址（高地址端）。</param>
    /// <param name="entry">协程入口函数指针，必须标注 [UnmanagedCallersOnly]。</param>
    /// <param name="param">传递给入口函数的参数指针。</param>
    /// <param name="finishCallback">
    /// 协程入口函数自然返回后由跳板函数调用的完成通知函数。
    /// 必须是 [UnmanagedCallersOnly] 函数指针。
    /// 签名：void FinishCallback(void* scheduler)。
    /// </param>
    /// <param name="scheduler">调度器指针，透传给 finishCallback。</param>
    public static void InitContext(
        MacOSCoroutineContext* ctx,
        void* stackTop,
        delegate* unmanaged<void*, void> entry,
        void* param,
        delegate* unmanaged<void*, void> finishCallback,
        void* scheduler)
    {
        *ctx = default;

        // ARM64 ABI：sp 必须 16 字节对齐，向下对齐后再留出 16 字节帧
        ulong sp = (ulong)stackTop & ~(ulong)0xF;
        sp -= 16;

        ctx->Sp = sp;

        // lr 指向跳板函数，第一次切入时 ret 指令跳入跳板
        // 直接读静态字段，无任何运行时开销
        ctx->Lr = _trampolineAddress;

        // 跳板函数通过 callee-saved 寄存器接收以下四个参数
        ctx->X19 = (ulong)entry;           // 真正的入口函数
        ctx->X20 = (ulong)param;           // 入口函数参数
        ctx->X21 = (ulong)finishCallback;  // 完成回调，入口函数返回后调用
        ctx->X22 = (ulong)scheduler;       // 调度器指针，透传给 finishCallback
    }

    /// <summary>
    /// 保存当前上下文到 from，切换执行到 to。
    /// </summary>
    /// <param name="from">保存当前寄存器状态的目标结构体。</param>
    /// <param name="to">要恢复并跳入的目标上下文。</param>
    public static void Switch(MacOSCoroutineContext* from, MacOSCoroutineContext* to)
    {
        MacOSAPI.coroutine_switch(from, to);
    }
}