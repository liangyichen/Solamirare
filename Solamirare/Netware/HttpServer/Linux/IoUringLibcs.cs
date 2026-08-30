using System.Runtime.InteropServices;

namespace Solamirare;

/// <summary>
/// 封装了 Linux io_uring 核心系统调用、操作码及相关底层常量。
/// </summary>
/// <remarks>
/// <para><b>注意：</b>此库专为 Linux 内核设计（建议版本 5.10+）。通过直接系统调用 (syscall) 实现极致性能。</para>
/// </remarks>
public static unsafe class IoUringLibcs
{
    // --- 标准网络常量 ---

    /// <summary>IPv4 互联网协议地址族。</summary>
    internal const int AF_INET = 2;

    /// <summary>提供面向连接的可靠字节流 (TCP)。</summary>
    internal const int SOCK_STREAM = 1;

    /// <summary>在创建套接字时直接设置为非阻塞模式 (O_NONBLOCK)。</summary>
    internal const int SOCK_NONBLOCK = 0x800;

    /// <summary>通用套接字选项层级。</summary>
    internal const int SOL_SOCKET = 1;

    /// <summary>允许套接字强制绑定到正在被使用的本地地址。</summary>
    internal const int SO_REUSEADDR = 2;

    /// <summary>允许对同一个端口进行多次负载均衡式绑定。</summary>
    internal const int SO_REUSEPORT = 15;

    /// <summary>监听套接字的最大连接队列上限。</summary>
    internal const int SOMAXCONN = 4096;

    /// <summary>关闭套接字的发送端（禁止进一步发送数据）。</summary>
    internal const int SHUT_WR = 1;

    /// <summary>发送数据时禁止产生 SIGPIPE 信号，防止连接断开导致进程异常退出。</summary>
    internal const int MSG_NOSIGNAL = 0x4000;

    // --- io_uring 操作码 (Opcode) ---

    /// <summary>io_uring 操作码：异步接收新的连接请求 (accept)。</summary>
    internal const byte IORING_OP_ACCEPT = 13;

    /// <summary>io_uring 操作码：异步发起连接 (connect)。</summary>
    internal const byte IORING_OP_CONNECT = 16;

    /// <summary>io_uring 操作码：异步关闭文件描述符 (close)。</summary>
    internal const byte IORING_OP_CLOSE = 19;

    /// <summary>io_uring 操作码：异步从文件描述符读取数据 (read)。</summary>
    internal const byte IORING_OP_READ = 22;

    /// <summary>io_uring 操作码：异步向套接字发送数据 (send)。</summary>
    internal const byte IORING_OP_SEND = 26;

    /// <summary>io_uring 操作码：异步从套接字接收数据 (recv)。</summary>
    internal const byte IORING_OP_RECV = 27;

    /// <summary>io_uring 操作码：异步零拷贝发送 (send_zc)。</summary>
    internal const byte IORING_OP_SEND_ZC = 47;

    // --- io_uring 运行标志 (Flags) ---

    /// <summary>io_uring_enter 标志：等待并获取完成事件 (GETEVENTS)。</summary>
    internal const uint IORING_ENTER_GETEVENTS = 0x001;

    /// <summary>io_uring_enter 标志：唤醒正在睡眠的内核 SQ 线程 (SQ_WAKEUP)。</summary>
    internal const uint IORING_ENTER_SQ_WAKEUP = 0x002;

    /// <summary>io_uring setup 标志：启用内核提交队列轮询线程 (SQPOLL)。</summary>
    internal const uint IORING_SETUP_SQPOLL = 0x0002;

    /// <summary>SQ ring 标志：内核指示 SQ 线程已进入睡眠，需要应用手动唤醒 (NEED_WAKEUP)。</summary>
    internal const uint IORING_SQ_NEED_WAKEUP = 0x0001;

    /// <summary>io_uring register 操作：注册一组固定缓冲区 (buffers)。</summary>
    internal const uint IORING_REGISTER_BUFFERS = 0;

    /// <summary>io_uring register 操作：注销已注册的固定缓冲区 (unregister buffers)。</summary>
    internal const uint IORING_UNREGISTER_BUFFERS = 1;

    /// <summary>指示 send/recv 操作应使用已注册的固定缓冲区索引 (fixed buffer)。</summary>
    internal const ushort IORING_RECVSEND_FIXED_BUF = 1;

    /// <summary>CQE 标志：指示这是一个通知性结果 (notification)，通常用于零拷贝发送。</summary>
    internal const uint IORING_CQE_F_NOTIF = 1u << 3;

    /// <summary>CQE 标志：指示后续仍有更多事件产生 (more)，用于多重接收模式。</summary>
    internal const uint IORING_CQE_F_MORE = 1u << 1;

    /// <summary>Accept 标志：启用多重接收模式 (multishot)。</summary>
    internal const ushort IORING_ACCEPT_MULTISHOT = 1;

    // --- Linux 系统调用号 (x64) ---

    /// <summary>Linux x64 下 io_uring_setup 的系统调用号 (425)。</summary>
    const long SYS_io_uring_setup = 425;

    /// <summary>Linux x64 下 io_uring_enter 的系统调用号 (426)。</summary>
    const long SYS_io_uring_enter = 426;

    /// <summary>Linux x64 下 io_uring_register 的系统调用号 (427)。</summary>
    const long SYS_io_uring_register = 427;

    // --- 内存映射相关常量 (mmap) ---

    /// <summary>映射区域可读 (PROT_READ)。</summary>
    internal const int PROT_READ = 1;

    /// <summary>映射区域可写 (PROT_WRITE)。</summary>
    internal const int PROT_WRITE = 2;

    /// <summary>建立共享映射 (MAP_SHARED)。</summary>
    internal const int MAP_SHARED = 1;

    /// <summary>预先填充页表以减少缺页中断 (MAP_POPULATE)。</summary>
    internal const int MAP_POPULATE = 0x8000;

    /// <summary>表示 mmap 调用失败的返回地址 (MAP_FAILED)。</summary>
    internal static readonly void* MAP_FAILED = (void*)-1L;

    // --- io_uring 偏移量 (Offsets) ---

    /// <summary>映射 SQ Ring 结构时的内存偏移量 (IORING_OFF_SQ_RING)。</summary>
    internal const long IORING_OFF_SQ_RING = 0;

    /// <summary>映射 CQ Ring 结构时的内存偏移量 (IORING_OFF_CQ_RING)。</summary>
    internal const long IORING_OFF_CQ_RING = 0x8000000;

    /// <summary>映射 SQE 数组时的内存偏移量 (IORING_OFF_SQES)。</summary>
    internal const long IORING_OFF_SQES = 0x10000000;

    // --- 系统调用封装方法 ---

    /// <summary>
    /// 初始化一个 io_uring 实例。
    /// </summary>
    internal static int io_uring_setup(uint entries, io_uring_params* p)
        => (int)LinuxAPI.syscall(SYS_io_uring_setup, entries, (long)p);

    /// <summary>
    /// 提交挂起的请求并/或等待完成事件。
    /// </summary>
    internal static int io_uring_enter(int fd, uint s, uint c, uint flags)
        => (int)LinuxAPI.syscall(SYS_io_uring_enter, fd, s, c, flags);

    /// <summary>
    /// 向内核注册或管理特定资源（如缓冲区、文件集等）。
    /// </summary>
    internal static int io_uring_register(int fd, uint opcode, void* arg, uint nrArgs)
        => (int)LinuxAPI.syscall(SYS_io_uring_register, fd, opcode, (long)arg, nrArgs);
}