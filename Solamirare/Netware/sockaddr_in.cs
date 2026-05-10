namespace Solamirare;


/// <summary>
/// 表示 IPv4 地址信息的结构体，用于 bind/accept/getpeername 等调用。
/// <para>Linux 与 Windows 共用的 sockaddr_in。（ macOS应该结构不一致，必须使用 sockaddr_bsd ）</para>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct sockaddr_in
{
    /// <summary>Address family / 地址族。</summary>
    public ushort sin_family;
    /// <summary>Port (network order) / 端口（网络序）。</summary>
    public ushort sin_port;
    /// <summary>IPv4 address / IPv4 地址。</summary>
    public uint sin_addr;
    /// <summary>Padding bytes / 填充字节。</summary>
    public fixed byte sin_zero[8];
}



