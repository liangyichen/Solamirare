namespace Solamirare;

/// <summary>
/// Windows 协程上下文，封装 Win32 Fiber 句柄。
/// <para>
/// macOS 版本需要手动保存寄存器快照，Windows 版本由 Win32 内部管理，
/// 此处只需保存 Fiber 句柄即可完成所有切换操作。
/// </para>
/// </summary>
internal unsafe struct WindowsCoroutineContext
{
    /// <summary>
    /// Win32 Fiber 句柄，由 CreateFiber 返回。
    /// 调度器通过此句柄调用 SwitchToFiber 完成上下文切换。
    /// </summary>
    public nint FiberHandle;
}