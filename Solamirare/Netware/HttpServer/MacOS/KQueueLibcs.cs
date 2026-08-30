namespace Solamirare;

/// <summary>
/// 提供 macOS KQueue 网络编程相关的常量定义。
/// </summary>
public unsafe struct KQueueLibcs
{
    /// <summary>允许地址复用的套接字选项。</summary>
    public const int SO_REUSEADDR = 0x0004;

    /// <summary>错误码：中断的系统调用 (EINTR)。</summary>
    internal const int EINTR_MAC = 4;

    /// <summary>允许端口复用的套接字选项。</summary>
    public const int SO_REUSEPORT = 0x0200;

    /// <summary>IPv4 地址族常量。</summary>
    public const byte AF_INET = 2;

    /// <summary>流式套接字类型常量。</summary>
    public const int SOCK_STREAM = 1;

    /// <summary>设置文件状态标志的 fcntl 命令。</summary>
    public const int F_SETFL = 4;

    /// <summary>非阻塞模式标志。</summary>
    public const int O_NONBLOCK = 0x0800;

    /// <summary>禁止向已断开的套接字写入时触发 SIGPIPE。</summary>
    public const int SO_NOSIGPIPE = 0x1022;

    /// <summary>资源暂时不可用错误码。</summary>
    public const int EAGAIN_MAC = 35;

    /// <summary>管道破裂错误码。</summary>
    public const int EPIPE_MAC = 32;

    /// <summary>可读事件过滤器。</summary>
    public const short EVFILT_READ = -1;

    /// <summary>可写事件过滤器。</summary>
    public const short EVFILT_WRITE = -2;

    /// <summary>添加或更新事件。</summary>
    public const ushort EV_ADD = 0x0001;

    /// <summary>删除事件。</summary>
    public const ushort EV_DELETE = 0x0002;

    /// <summary>单次触发事件。</summary>
    public const ushort EV_ONESHOT = 0x0010;

    /// <summary>文件结束或连接关闭标志。</summary>
    public const ushort EV_EOF = 0x8000;

    /// <summary>事件错误标志。</summary>
    public const ushort EV_ERROR = 0x4000;
}
