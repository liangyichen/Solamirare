namespace Solamirare;


/// <summary>
/// 表示 macOS/Darwin 系统中的异步 I/O 控制块 (struct aiocb)。
/// 用于 <c>aio_read</c>、<c>aio_write</c> 等 POSIX AIO 系统调用。
/// </summary>
/// <remarks>
/// <para><b>内存布局说明：</b>macOS 上的 <c>aiocb</c> 结构与 Linux 存在显著差异（字段顺序、sigevent 大小及填充空间均不同）。</para>
/// <para><b>维护提示：</b>严禁修改 <see cref="sigevent"/> 及其之前字段的顺序，否则会导致内核在解析 I/O 请求时发生内存错位或崩溃。</para>
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 128)]
public unsafe struct macos_aiocb
{
    // 维护提示：严禁修改 sigevent 以及之前的字段顺序

    /// <summary>
    /// 文件描述符 (aio_fildes)。
    /// 异步 I/O 操作的目标文件或套接字。
    /// </summary>
    public int aio_fildes;

    /// <summary>
    /// 文件偏移量 (aio_offset)。
    /// 异步操作开始执行的起始位置。注意：在 macOS 中此字段位于布局的前端。
    /// </summary>
    public long aio_offset;

    /// <summary>
    /// 缓冲区指针 (aio_buf)。
    /// 指向存放待写入数据或接收读取数据内存区域的原始指针。
    /// </summary>
    public byte* aio_buf;

    /// <summary>
    /// 传输的字节数 (aio_nbytes)。
    /// 指定本次异步操作要读取或写入的数据长度。
    /// </summary>
    public nuint aio_nbytes;

    /// <summary>
    /// 请求的优先级 (aio_reqprio)。
    /// 用于对异步请求进行排队优先级的微调。
    /// </summary>
    public int aio_reqprio;

    /// <summary>
    /// 信号事件块 (sigevent)。
    /// 定义 I/O 完成后的通知行为。在 macOS 上该结构固定为 32 字节（Linux 通常为 64 字节）。
    /// </summary>
    public fixed byte sigevent[32];

    /// <summary>
    /// 列表 I/O 操作码 (aio_lio_opcode)。
    /// 仅在批量调用 <c>lio_listio</c> 时使用。
    /// </summary>
    public int aio_lio_opcode;

    /// <summary>
    /// 内部保留空间。
    /// 用于内核状态维护及 ABI 对齐，确保结构体总大小符合 macOS 系统规范。
    /// </summary>
    private fixed byte __reserved[56]; //<--- 这个长度从前是72，会导致内存占用达到144，为了内存对齐，最后必须设置到256，过于浪费，现在裁剪到56，可以让整个结构保持在128长度
}