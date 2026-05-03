namespace Solamirare;

/// <summary>
/// 表示一个原生任务项。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeThreadTask
{
    /// <summary>任务执行函数指针。</summary>
    public delegate* unmanaged<void*, void> Function;

    /// <summary>传递给任务函数的上下文指针。</summary>
    public void* Context;
}
