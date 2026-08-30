namespace Solamirare;

// --- 内部结构定义 (严格对齐内核) ---
/// <summary>
/// 表示 Linux io_uring 提交队列中的一个提交项。
/// <para>
/// 该结构体与内核中的 <c>struct io_uring_sqe</c> 布局一致，用于描述一次 I/O 提交请求的操作类型和参数。
/// </para>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct io_uring_sqe
{
    /// <summary>
    /// I/O 操作码。
    /// </summary>
    public byte opcode;

    /// <summary>
    /// 提交项附加标志位。
    /// </summary>
    public byte flags;

    /// <summary>
    /// I/O 优先级。
    /// </summary>
    public ushort ioprio;

    /// <summary>
    /// 目标文件描述符。
    /// </summary>
    public int fd;

    /// <summary>
    /// 偏移量。
    /// </summary>
    public ulong off;

    /// <summary>
    /// 目标缓冲区或地址。
    /// </summary>
    public ulong addr;

    /// <summary>
    /// 传输长度。
    /// </summary>
    public uint len;

    /// <summary>
    /// 读写附加标志位。
    /// </summary>
    public uint rw_flags;

    /// <summary>
    /// 提交时的用户上下文数据。
    /// </summary>
    public ulong user_data;

    /// <summary>
    /// 关联缓冲区索引。
    /// </summary>
    public ushort buf_index;

    /// <summary>
    /// 线程人格信息。
    /// </summary>
    public ushort personality;

    /// <summary>
    /// 文件索引。
    /// </summary>
    public ushort file_index;

    /// <summary>
    /// 内核保留字段。
    /// </summary>
    public ulong __pad2_1;

    /// <summary>
    /// 内核保留字段。
    /// </summary>
    public ulong __pad2_2;
}