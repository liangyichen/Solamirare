namespace Solamirare;

/// <summary>
/// 表示一个时间间隔，用于指定超时时间（例如在 <c>kevent</c> 调用中）。
/// </summary>
/// <remarks>
/// <c>IntPtr</c> 用于确保在 32 位和 64 位系统上字段大小与原生 C 结构体对齐。
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct timeval
{
    /// <summary>
    /// 时间间隔的秒数部分。
    /// </summary>
    public nint tv_sec;

    /// <summary>
    /// 时间间隔的纳秒部分（0 到 999,999,999）。
    /// </summary>
    public nint tv_usec;
}