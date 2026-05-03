namespace Solamirare;

/// <summary>
/// 提供 macOS 平台 HTTP 客户端使用的 POSIX Socket 常量与结构体定义。
/// </summary>
public unsafe static class MacOSHttpPosixApi
{
    /// <summary>IPv4 地址族常量。</summary>
    public const int AF_INET = 2;

    /// <summary>流式套接字类型常量。</summary>
    public const int SOCK_STREAM = 1;

    /// <summary>TCP 协议常量。</summary>
    public const int IPPROTO_TCP = 6;

    /// <summary>Socket 通用选项层级常量。</summary>
    public const int SOL_SOCKET = 0xffff;

    /// <summary>Socket 错误状态选项常量。</summary>
    public const int SO_ERROR = 0x1007;

    /// <summary>接收超时套接字选项常量。</summary>
    public const int SO_RCVTIMEO = 0x1006;

    /// <summary>发送超时套接字选项常量。</summary>
    public const int SO_SNDTIMEO = 0x1005;

    /// <summary>
    /// 表示 IPv4 套接字地址结构。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct sockaddr_in
    {
        /// <summary>结构体长度。</summary>
        public byte sin_len;

        /// <summary>地址族。</summary>
        public byte sin_family;

        /// <summary>端口号，网络字节序。</summary>
        public ushort sin_port;

        /// <summary>IPv4 地址。</summary>
        public uint sin_addr;

        /// <summary>保留填充字节。</summary>
        public fixed byte sin_zero[8];
    }

    /// <summary>
    /// 表示 POSIX 时间间隔结构。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct timeval
    {
        /// <summary>秒数部分。</summary>
        public IntPtr tv_sec;

        /// <summary>微秒部分。</summary>
        public IntPtr tv_usec;
    }

    /// <summary>
    /// 表示主机解析结果结构。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct hostent
    {
        /// <summary>主机名称指针。</summary>
        public byte* h_name;

        /// <summary>主机别名列表指针。</summary>
        public byte** h_aliases;

        /// <summary>地址类型。</summary>
        public int h_addrtype;

        /// <summary>地址长度。</summary>
        public int h_length;

        /// <summary>地址列表指针。</summary>
        public byte** h_addr_list;
    }

    /// <summary>
    /// Socket 长度类型。
    /// </summary>
    public struct socklen_t
    {
        private uint value;
        public static implicit operator uint(socklen_t s) => s.value;
        public static implicit operator socklen_t(uint u) => new socklen_t { value = u };
    }
}
