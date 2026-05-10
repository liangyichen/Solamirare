namespace Solamirare;


// =============================================================================
// IOCPLibrary — 集中管理所有 Win32 P/Invoke 声明
// =============================================================================
internal static unsafe class IOCPLibrary
{
    // ── 常量 ──────────────────────────────────────────────────────────────────
    internal const int AF_INET = 2;
    internal const int SOCK_STREAM = 1;
    internal const int IPPROTO_TCP = 6;
    internal const int SOL_SOCKET = 0xFFFF;
    internal const int SO_REUSEADDR = 4;
    internal const int SO_UPDATE_ACCEPT_CONTEXT = 0x700B;
    internal const int SOMAXCONN = 0x7fffffff;
    internal const nint INVALID_SOCKET = -1;
    internal const int SOCKET_ERROR = -1;
    internal const nint INVALID_HANDLE_VALUE = -1;
    internal const uint WSA_IO_PENDING = 997;
    internal const uint SIO_GET_EXTENSION_FUNCTION_POINTER = 0xC8000006;

    // WSAID_ACCEPTEX = {B5367DF1-CBAC-11CF-95CA-00805F48A192}
    internal static readonly Guid WSAID_ACCEPTEX =
        new Guid(0xb5367df1, 0xcbac, 0x11cf, 0x95, 0xca, 0x00, 0x80, 0x5f, 0x48, 0xa1, 0x92);

}


[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WSADATA { public fixed byte data[408]; }

/// <summary>
/// 表示 Windows 套接字缓冲区 (WSABUF) 的内存布局。
/// 在 Linux/macOS 环境下，其二进制结构通常与 <c>iovec</c> 兼容。
/// </summary>
/// <remarks>
/// <para>
/// <b>对齐说明：</b>在 64 位 (x64) 架构中，由于原生指针 <see cref="buf"/> 要求 8 字节对齐，
/// 编译器会在 4 字节的 <see cref="len"/> 之后隐式插入 4 字节填充。
/// 此处显式定义 <see cref="_pad"/> 字段是为了确保内存布局在序列化、内存拷贝及跨平台 P/Invoke 调用时具备确定性。
/// </para>
/// <para>
/// 使用此结构时，通常配合 <c>stackalloc</c> 分配或固定 (fixed) 托管缓冲区。
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WSABUF
{
    /// <summary>
    /// 缓冲区的长度（以字节为单位）。
    /// </summary>
    public uint len;

    /// <summary>
    /// 显式对齐填充字段。
    /// 用于补偿 x64 平台下 uint 与指针之间的 4 字节空隙，确保结构体总长度为 16 字节。
    /// </summary>
    public uint _pad;

    /// <summary>
    /// 指向实际数据缓冲区的原始指针。
    /// 在 P/Invoke 调用期间，该地址必须指向有效的非托管内存或已锁定的托管内存。
    /// </summary>
    public byte* buf;
}
