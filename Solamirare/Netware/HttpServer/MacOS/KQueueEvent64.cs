namespace Solamirare;

/// <summary>
/// 表示 macOS/BSD 系统中的 64 位内核事件结构 (kevent64_s)。
/// 用于 <c>kqueue</c> 机制中的事件注册、修改以及获取待处理的内核事件。
/// </summary>
/// <remarks>
/// 该结构采用显式布局（Explicit Layout），总大小为 48 字节。
/// 相比于传统的 <c>kevent</c>，它提供了额外的扩展字段 (<see cref="ext0"/>, <see cref="ext1"/>) 用于更复杂的内核交互。
/// </remarks>
[StructLayout(LayoutKind.Explicit, Size = 48)]
internal unsafe struct KQueueEvent64
{
    /// <summary>
    /// 事件的标识符。
    /// 根据过滤器 (<see cref="filter"/>) 的不同，通常为文件描述符 (fd)、进程 ID (pid) 或定时器 ID。
    /// </summary>
    [FieldOffset(0)] public ulong ident;

    /// <summary>
    /// 事件过滤器。
    /// 指定内核监控的事件类型，例如 <c>EVFILT_READ</c> (-1), <c>EVFILT_WRITE</c> (-2), <c>EVFILT_VNODE</c> (-4) 等。
    /// </summary>
    [FieldOffset(8)] public short filter;

    /// <summary>
    /// 操作标志位。
    /// 指示如何处理该事件请求，如 <c>EV_ADD</c> (添加), <c>EV_DELETE</c> (删除), <c>EV_ENABLE</c> (启用) 等。
    /// </summary>
    [FieldOffset(10)] public ushort flags;

    /// <summary>
    /// 过滤器特定的标志位。
    /// 用于进一步微调过滤器的行为（例如在 VNODE 过滤中指定监控文件重命名或属性修改）。
    /// </summary>
    [FieldOffset(12)] public uint fflags;

    /// <summary>
    /// 过滤器特定的数据值。
    /// 存储与事件相关的附加信息，例如读缓冲区中可用的字节数或定时器的超时值。
    /// </summary>
    [FieldOffset(16)] public long data;

    /// <summary>
    /// 用户自定义数据。
    /// 内核不会修改此值，通常用于存放托管对象的句柄、指针或上下文 ID，以便在事件触发时识别来源。
    /// </summary>
    [FieldOffset(24)] public ulong udata;

    /// <summary>
    /// 扩展字段 0。
    /// 预留给特定过滤器或内核子系统使用的 64 位扩展参数。
    /// </summary>
    [FieldOffset(32)] public ulong ext0;

    /// <summary>
    /// 扩展字段 1。
    /// 预留给特定过滤器或内核子系统使用的 64 位扩展参数。
    /// </summary>
    [FieldOffset(40)] public ulong ext1;
}