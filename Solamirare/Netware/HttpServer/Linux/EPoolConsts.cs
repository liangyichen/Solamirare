namespace Solamirare;

/// <summary>
/// 封装了 POSIX 标准及平台特定的底层网络系统调用常量。
/// </summary>
/// <remarks>
/// <para><b>警示：</b>此类中定义的部分常量（如 <see cref="SO_REUSEPORT"/>, <see cref="O_NONBLOCK"/>）在 Linux 与 macOS/BSD 之间存在数值差异。</para>
/// <para>当前硬编码值主要以 Linux (x64) 布局为准。若在 macOS 上运行，需通过运行时检测（RuntimeInformation）进行动态调整或重新映射。</para>
/// </remarks>
internal unsafe partial class EPoolConsts
{
    // --- 地址族与套接字类型 ---

    /// <summary>IPv4 互联网协议地址族。</summary>
    public const int AF_INET = 2;

    /// <summary>提供面向连接的、可靠的字节流（通常为 TCP）。</summary>
    public const int SOCK_STREAM = 1;

    // --- 套接字选项 (setsockopt) ---

    /// <summary>通用套接字选项层级。</summary>
    public const int SOL_SOCKET = 1;

    /// <summary>允许套接字强制绑定到正在被使用的本地地址。</summary>
    public const int SO_REUSEADDR = 2;

    /// <summary>
    /// 允许对同一个端口进行多次绑定。
    /// <para>注意：Linux 为 15，macOS 为 0x0200。</para>
    /// </summary>
    public const int SO_REUSEPORT = 15;

    // --- 文件控制 (fcntl) ---

    /// <summary>获取文件状态标志。</summary>
    public const int F_GETFL = 3;

    /// <summary>设置文件状态标志。</summary>
    public const int F_SETFL = 4;

    /// <summary>
    /// 非阻塞 I/O 标志。
    /// <para>注意：Linux 为 0x800，macOS 为 0x0004。</para>
    /// </summary>
    public const int O_NONBLOCK = 0x800;

    // --- 传输标志 (send/recv flags) ---

    /// <summary>
    /// 禁止在套接字关闭时产生 SIGPIPE 信号。
    /// <para>仅限 Linux；macOS 通常在 <c>setsockopt</c> 中使用 <c>SO_NOSIGPIPE</c>。</para>
    /// </summary>
    public const uint MSG_NOSIGNAL = 0x4000;

    // --- 错误码 (errno) ---

    /// <summary>资源暂时不可用（通常用于非阻塞模式下的重试通知）。</summary>
    public const int EAGAIN = 11;

    /// <summary>管道破裂，通常表示对端已关闭连接。</summary>
    public const int EPIPE = 32;

    // --- epoll 事件标志 (Linux 特定) ---

    /// <summary>表示关联的文件描述符可读。</summary>
    public const uint EPOLLIN = 0x00000001u;

    /// <summary>表示关联的文件描述符可写。</summary>
    public const uint EPOLLOUT = 0x00000004u;

    /// <summary>关联的文件描述符发生错误条件。</summary>
    public const uint EPOLLERR = 0x00000008u;

    /// <summary>关联的文件描述符被挂断（对端关闭）。</summary>
    public const uint EPOLLHUP = 0x00000010u;

    /// <summary>对端关闭连接或关闭写入半连接。</summary>
    public const uint EPOLLRDHUP = 0x00002000u;

    /// <summary>
    /// 设置为边缘触发（Edge Triggered）模式。
    /// <para>默认为水平触发（Level Triggered）。</para>
    /// </summary>
    public const uint EPOLLET = 0x80000000u;

    // --- epoll_ctl 操作类型 ---

    /// <summary>向 epoll 实例中注册新的文件描述符。</summary>
    public const int EPOLL_CTL_ADD = 1;

    /// <summary>从 epoll 实例中删除已注册的文件描述符。</summary>
    public const int EPOLL_CTL_DEL = 2;

    /// <summary>修改 epoll 实例中已注册文件描述符的关联事件。</summary>
    public const int EPOLL_CTL_MOD = 3;
}