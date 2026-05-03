namespace Solamirare;


/// <summary>
/// 表示主机数据库条目的结构。
/// 用于存储主机的名称、别名、地址类型及地址列表等信息。
/// </summary>
/// <remarks>
/// 该结构在不同平台（Windows 的 ws2_32.dll 与 Linux/macOS 的 libc）的二进制布局基本一致，
/// 但其内部指针指向的内存通常由系统库管理，调用者需注意生命周期。
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct hostent
{
    /// <summary>
    /// 指向主机的正式名称（Canonical Name）的指针。
    /// 以空字符 (null-terminated) 结尾的字节数组。
    /// </summary>
    public byte* h_name;

    /// <summary>
    /// 指向主机别名列表的指针数组。
    /// 该数组以 null 指针结束。
    /// </summary>
    public byte** h_aliases;

    /// <summary>
    /// 返回地址的类型。
    /// 对于 IPv4，该值通常为 <c>AF_INET</c> (通常为 2)。
    /// </summary>
    public int h_addrtype;

    /// <summary>
    /// 每个地址的长度（以字节为单位）。
    /// 对于 IPv4 地址（struct in_addr），该值为 4。
    /// </summary>
    public int h_length;

    /// <summary>
    /// 指向主机网络地址列表的指针数组。
    /// 数组以 null 指针结束，地址以网络字节序存储。
    /// 习惯上可通过 <c>h_addr_list[0]</c> 获取主地址。
    /// </summary>
    public byte** h_addr_list;
}