namespace Solamirare;

/// <summary>
/// 表示 Linux io_uring 完成队列中的一个事件项。
/// <para>
/// 该结构体与内核中的 <c>struct io_uring_cqe</c> 布局一致，保存用户数据、完成结果以及附加状态标志。
/// </para>
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct io_uring_cqe
{
    /// <summary>
    /// 提交时关联的用户数据。
    /// </summary>
    public ulong user_data;

    /// <summary>
    /// 操作的返回结果或错误码。
    /// </summary>
    public int res;

    /// <summary>
    /// 完成事件的附加标志位。
    /// </summary>
    public uint flags;
}