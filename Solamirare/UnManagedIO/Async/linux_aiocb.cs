namespace Solamirare;


/// <summary>
/// 表示 Linux 异步 I/O 控制块 (struct aiocb)。
/// 用于 <c>aio_read</c>、<c>aio_write</c> 等 POSIX AIO 函数。
/// </summary>
/// <remarks>
/// <para><b>内存布局注意：</b>该结构体的字段顺序及对齐方式必须与目标系统的 glibc 定义严格一致。在 64 位 Linux 下，总大小及偏移量受对齐规则影响。</para>
/// <para>内部字段（以 <c>__</c> 开头）由系统库在异步执行期间管理，用户代码不应直接修改这些字段，以防止内核态或库层的定义冲突。</para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct linux_aiocb
{
    // 维护提示：禁止修改字段顺序

    /// <summary>
    /// 文件描述符 (aio_fildes)。
    /// 异步操作的目标文件、套接字或设备。
    /// </summary>
    public int aio_fildes;

    /// <summary>
    /// 列表 I/O 操作码 (aio_lio_opcode)。
    /// 仅在调用 <c>lio_listio</c> 时有效，例如 <c>LIO_READ</c>、<c>LIO_WRITE</c> 或 <c>LIO_NOP</c>。
    /// </summary>
    public int aio_lio_opcode;

    /// <summary>
    /// 请求的优先级 (aio_reqprio)。
    /// 用于指定异步请求相对于其他请求的优先级降低量。
    /// </summary>
    public int aio_reqprio;

    /// <summary>
    /// 缓冲区指针 (aio_buf)。
    /// 指向用于读取或写入数据的原始内存地址。在 64 位系统上通常要求 8 字节对齐。
    /// </summary>
    public byte* aio_buf;

    /// <summary>
    /// 传输的字节数 (aio_nbytes)。
    /// 操作所涉及的数据长度。
    /// </summary>
    public nuint aio_nbytes;

    /// <summary>
    /// 信号事件块 (sigevent)。
    /// 定义 I/O 完成时的通知机制（如信号或回调线程）。固定长度为 64 字节。
    /// </summary>
    public fixed byte sigevent[64];

    /// <summary>glibc 内部使用的链表优先级指针。</summary>
    private IntPtr __next_prio;
    /// <summary>glibc 内部使用的绝对优先级。</summary>
    private int __abs_prio;
    /// <summary>glibc 内部使用的调度策略。</summary>
    private int __policy;
    /// <summary>glibc 内部使用的错误码存储位。</summary>
    private int __error_code;
    /// <summary>glibc 内部使用的异步返回结果（返回值）。</summary>
    private IntPtr __return_value;

    /// <summary>
    /// 文件偏移量 (aio_offset)。
    /// 异步 I/O 操作开始时的文件起始位置。对于不支持寻址的文件描述符（如套接字），此值将被忽略。
    /// </summary>
    public long aio_offset;

    /// <summary>
    /// 保留空间。
    /// 用于填充结构体以匹配特定平台下的 size 要求或未来的二进制兼容性。
    /// </summary>
    private fixed byte __reserved[32];
}