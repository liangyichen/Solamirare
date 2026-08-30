namespace Solamirare;

/// <summary>
/// 提供 Windows 平台 HTTP 客户端使用的 WinSock 常量、结构体与薄封装方法。
/// </summary>
public static unsafe class WindowsHttpApi
{
    /// <summary>IPv4 地址族常量。</summary>
    public const int AF_INET = 2;

    /// <summary>流式套接字类型常量。</summary>
    public const int SOCK_STREAM = 1;

    /// <summary>TCP 协议常量。</summary>
    public const int IPPROTO_TCP = 6;

    /// <summary>Socket 通用选项层级常量。</summary>
    public const int SOL_SOCKET = 0xffff;

    /// <summary>接收超时套接字选项常量。</summary>
    public const int SO_RCVTIMEO = 0x1006;

    /// <summary>发送超时套接字选项常量。</summary>
    public const int SO_SNDTIMEO = 0x1005;

    /// <summary>
    /// 表示时间间隔结构。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct timeval
    {
        /// <summary>秒数部分。</summary>
        public long tv_sec;

        /// <summary>微秒部分。</summary>
        public long tv_usec;
    }


    /// <summary>
    /// 表示 WinSock 初始化信息。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct WSAData
    {
        /// <summary>请求的 WinSock 版本。</summary>
        public ushort wVersion;

        /// <summary>系统支持的最高版本。</summary>
        public ushort wHighVersion;

        /// <summary>实现描述字符串缓冲区。</summary>
        public fixed byte szDescription[257];

        /// <summary>系统状态字符串缓冲区。</summary>
        public fixed byte szSystemStatus[129];

        /// <summary>最大套接字数量。</summary>
        public ushort iMaxSockets;

        /// <summary>最大 UDP 数据报大小。</summary>
        public ushort iMaxUdpDg;

        /// <summary>厂商信息指针。</summary>
        public IntPtr lpVendorInfo;
    }




    /// <summary>
    /// 解析主机名并返回主机信息结构。
    /// </summary>
    /// <param name="name">主机名字符串指针。</param>
    public static hostent* gethostbyname(byte* name)
    {
        IntPtr p = WindowsAPI.gethostbyname_raw(name);
        return (hostent*)p;
    }

    /// <summary>
    /// 与远端地址建立连接。
    /// </summary>
    /// <param name="sockfd">套接字句柄。</param>
    /// <param name="addr">目标地址结构。</param>
    /// <param name="addrlen">地址结构长度。</param>
    public static int connect(int sockfd, sockaddr_in* addr, uint addrlen)
    {
        return WindowsAPI.connect_raw(new IntPtr(sockfd), new IntPtr(addr), (int)addrlen);
    }

    /// <summary>
    /// 创建套接字。
    /// </summary>
    /// <param name="domain">地址族。</param>
    /// <param name="type">套接字类型。</param>
    /// <param name="protocol">协议号。</param>
    public static int socket(int domain, int type, int protocol)
    {
        IntPtr s = WindowsAPI.socket_raw(domain, type, protocol);
        if (s == IntPtr.Zero || s == new IntPtr(-1)) return -1;
        return s.ToInt32();
    }

    /// <summary>
    /// 发送数据到套接字。
    /// </summary>
    public static int send(int sockfd, byte* buf, UIntPtr len, int flags) => WindowsAPI.send_raw(new IntPtr(sockfd), buf, len, flags);

    /// <summary>
    /// 关闭套接字。
    /// </summary>
    public static int closesocket(int sockfd) => WindowsAPI.closesocket_raw(new IntPtr(sockfd));

    /// <summary>
    /// 从套接字接收数据。
    /// </summary>
    public static int recv(int sockfd, byte* buf, UIntPtr len, int flags) => WindowsAPI.recv_raw(new IntPtr(sockfd), buf, len, flags);


    /// <summary>
    /// 设置整型套接字选项。
    /// </summary>
    public static int setsockopt(int sockfd, int level, int optname, int* optval, uint optlen)
        => WindowsAPI.setsockopt_raw(new IntPtr(sockfd), level, optname, new IntPtr(optval), (int)optlen);

}
