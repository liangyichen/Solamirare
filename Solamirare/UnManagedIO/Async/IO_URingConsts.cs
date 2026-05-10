namespace Solamirare;

/// <summary>
/// IO_URing 常量
/// </summary>
internal static class IO_URingConsts
{
        // --- Linux 系统调用号 (x64) ---

    /// <summary>
    /// 初始化 io_uring 实例的系统调用号 (425)。
    /// 用于设置提交队列和完成队列，并返回一个文件描述符。
    /// </summary>
    public const int SYS_io_uring_setup = 425;

    /// <summary>
    /// 提交并进入 io_uring 环的系统调用号 (426)。
    /// 用于启动 I/O 操作或等待已完成的事件。
    /// </summary>
    public const int SYS_io_uring_enter = 426;

    // --- io_uring 操作码与标志 ---

    /// <summary>
    /// 异步读取操作码 (IORING_OP_READ)。
    /// 相当于异步版本的 <c>preadv2</c>。
    /// </summary>
    public const byte IORING_OP_READ = 22;

    /// <summary>
    /// 异步写入操作码 (IORING_OP_WRITE)。
    /// 相当于异步版本的 <c>pwritev2</c>。
    /// </summary>
    public const byte IORING_OP_WRITE = 23;

    /// <summary>
    /// io_uring_enter 标志：等待并获取完成事件 (GETEVENTS)。
    /// 指示内核在返回前至少等待指定数量的 CQE。
    /// </summary>
    public const uint IORING_ENTER_GETEVENTS = (1U << 0);

    // --- mmap 内存映射偏移量 ---

    /// <summary>
    /// 映射提交队列 (Submission Queue) 环结构时的内存偏移量。
    /// </summary>
    public const long IORING_OFF_SQ_RING = 0;

    /// <summary>
    /// 映射完成队列 (Completion Queue) 环结构时的内存偏移量。
    /// </summary>
    public const long IORING_OFF_CQ_RING = 0x8000000;

    /// <summary>
    /// 映射提交队列条目数组 (SQEs) 时的内存偏移量。
    /// SQEs 数组通常在独立的内存区域进行映射。
    /// </summary>
    public const long IORING_OFF_SQES = 0x10000000;


    /// <summary>异步发起连接 (connect)。</summary>
    internal const byte IORING_OP_CONNECT = 16;

    /// <summary>异步向套接字发送数据 (send)。</summary>
    internal const byte IORING_OP_SEND = 26;

    /// <summary>异步从套接字接收数据 (recv)。</summary>
    internal const byte IORING_OP_RECV = 27;

    /// <summary>io_uring 操作码：异步添加 Poll 事件 (poll_add)。</summary>
    internal const byte IORING_OP_POLL_ADD = 6;

    /// <summary>Poll 事件：可读。</summary>
    internal const uint POLLIN = 0x0001;

    /// <summary>Poll 事件：可写。</summary>
    internal const uint POLLOUT = 0x0004;

    // --- SSL 错误常量 ---
    internal const int SSL_ERROR_WANT_READ = 2;
    internal const int SSL_ERROR_WANT_WRITE = 3;
}