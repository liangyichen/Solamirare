namespace Solamirare;

/// <summary>
/// 表示 Windows 异步 I/O 操作的核心结构 (OVERLAPPED)。
/// 用于在重叠 I/O 操作（如 <c>ReadFile</c>、<c>WriteFile</c>）及完成端口 (IOCP) 中同步数据和状态。
/// </summary>
/// <remarks>
/// <para><b>内存对齐：</b>该结构体采用显式布局（Explicit），总长度在 64 位系统下通常为 32 字节。</para>
/// <para><b>生命周期管理：</b>在异步操作完成之前（即收到 IOCP 通知或事件触发前），
/// 必须确保该结构体在内存中的位置固定（Pinned）且不被释放，否则会导致内核写入非法内存造成系统崩溃。</para>
/// </remarks>
[StructLayout(LayoutKind.Explicit)]
public unsafe struct OVERLAPPED
{
    /// <summary>
    /// 系统保留字段。
    /// 用于保存与系统相关的状态。在 I/O 操作开始前应初始化为 0。
    /// 异步操作完成后，此字段包含 I/O 处理的具体状态代码（如 <c>STATUS_PENDING</c>）。
    /// </summary>
    [FieldOffset(0)] public ulong Internal;

    /// <summary>
    /// 系统保留字段。
    /// 用于保存已传输的数据字节数。当异步操作完成且不返回错误时，此字段由系统设置。
    /// </summary>
    [FieldOffset(8)] public ulong InternalHigh;

    /// <summary>
    /// 文件偏移量的低 32 位。
    /// 指定开始 I/O 操作的文件位置。对于不支持寻址的设备（如管道或通信设备），此值必须为 0。
    /// </summary>
    [FieldOffset(16)] public uint Offset;

    /// <summary>
    /// 文件偏移量的高 32 位。
    /// 配合 <see cref="Offset"/> 组成 64 位的文件起始偏移量。
    /// </summary>
    [FieldOffset(20)] public uint OffsetHigh;

    /// <summary>
    /// 事件句柄或用户自定义数据。
    /// 如果在关联 IOCP 时使用，此句柄通常设为 NULL。
    /// 也可以存放由 <c>CreateEvent</c> 创建的同步事件，当操作完成时系统会将该事件设为有信号状态。
    /// </summary>
    [FieldOffset(24)] public void* hEvent;
}

