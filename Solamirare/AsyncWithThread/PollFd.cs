namespace Solamirare;

/// <summary>
/// 用于 poll 系统调用的文件描述符结构。
/// <br/>
/// Structure for file descriptors used in the poll system call.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct PollFd
{
    /// <summary>
    /// 文件描述符。
    /// <br/>
    /// The file descriptor.
    /// </summary>
    public int Fd;

    /// <summary>
    /// 请求的事件掩码。
    /// <br/>
    /// Requested events bitmask.
    /// </summary>
    public short Events;

    /// <summary>
    /// 返回的事件掩码。
    /// <br/>
    /// Returned events bitmask.
    /// </summary>
    public short Revents;
}
