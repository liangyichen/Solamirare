namespace Solamirare;



/// <summary>
/// 表示 IPv4 地址信息的结构体，用于 bind/accept/getpeername 等调用。
/// <para>与 macOS 的 struct sockaddr_in 内存布局一致。（ Linux 与 Windows 因为内存布局不一致，必须使用 sockaddr_in ）</para>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe ref struct sockaddr_bsd
{
    //(禁止更改字段排列顺序)

    /// <summary>
    /// BSD 风格 Socket API 的传统特征，用于存储结构体的总长度（16 字节）
    /// </summary>
    public byte sin_len;

    /// <summary>地址族，通常为 AF_INET (2)。</summary>
    public byte sin_family;

    /// <summary>端口号，以网络字节序（大端序）存储。</summary>
    public ushort sin_port;

    /// <summary>IPv4 地址，以网络字节序存储的 32 位整数。</summary>
    public uint sin_addr;

    /// <summary>填充位，确保结构体总大小为 16 字节。</summary>
    public fixed byte sin_zero[8];
}