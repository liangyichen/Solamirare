namespace Solamirare;

/// <summary>
/// makecontext 参数传递用的 trampoline 结构体。
/// <para>
/// Linux makecontext 只支持传递 int 类型参数，无法直接传递 64 位指针。
/// 将所有需要传递的指针打包进此结构体，
/// makecontext 只传此结构体的指针（拆成两个 int），
/// trampoline 函数内部重组指针后调用真正的入口函数。
/// </para>
/// </summary>
internal unsafe struct LinuxCoroutineTrampolineArgs
{
    /// <summary>用户定义的真正入口函数指针。</summary>
    public delegate* unmanaged<void*, void> Entry;

    /// <summary>透传给入口函数的参数指针。</summary>
    public void* Param;

    /// <summary>协程完成后的通知回调函数指针。</summary>
    public delegate* unmanaged<void*, void> FinishCallback;

    /// <summary>调度器指针，透传给 FinishCallback。</summary>
    public void* Scheduler;
}