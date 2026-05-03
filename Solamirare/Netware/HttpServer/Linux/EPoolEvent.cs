namespace Solamirare;


/// <summary>
/// <c>epoll_event</c> 结构体用于在调用 <c>epoll_ctl</c> 时指定感兴趣的事件，
/// 并在调用 <c>epoll_wait</c> 时报告发生的事件。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public ref struct epoll_event
{
    /// <summary>
    /// 事件掩码（位字段）。在 <c>epoll_ctl</c> 中设置感兴趣的事件（如 <c>EPOLLIN</c>），
    /// 在 <c>epoll_wait</c> 中报告已发生的事件。
    /// </summary>
    public uint events;

    /// <summary>
    /// 用户数据。一个 64 位的值，通常用于存储与文件描述符关联的指针、文件描述符本身或一个自定义 ID。
    /// 当事件发生时，该数据会被内核返回给用户空间。
    /// </summary>
    public ulong data_u64;
}