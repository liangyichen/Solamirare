using System.Runtime.CompilerServices;

namespace Solamirare;


/// <summary>
/// Connection对象
/// </summary>
public unsafe struct UHttpConnection
{

    /// <summary>
    /// 客户端IP地址
    /// </summary>
    public UnManagedCollection<char> IpAddress;

    // 客户端 IP 地址 (网络字节序)
    fixed char IpAddressMemory[15];

    /// <summary>
    /// 客户端端口
    /// </summary>
    public ushort ClientPort;

    /// <summary>
    /// 清空数据
    /// </summary>
    public void Clear()
    {
        IpAddress.Clear();
    }

    /// <summary>
    /// MacOS 与 Linux 上进行对象初始化
    /// </summary>
    /// <param name="fd"></param>
    internal void InitFromPosix(int fd)
    {
        // 可以取 sockaddr_in_bsd 或 sockaddr_in_linux 中的任意一个，因为当前需要使用的字段其所在偏移量都是一样的
        sockaddr_bsd clientAddr = new sockaddr_bsd();

        int addrLen = sizeof(sockaddr_bsd); // = 16

        uint clientIpHost = 0;

        int peername;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            peername = LinuxAPI.getpeername(fd, &clientAddr, &addrLen);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            peername = MacOSAPI.getpeername(fd, &clientAddr, &addrLen);
        else
            peername = 0;


        if (peername == 0)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // --- 获取端口 ---
                ClientPort = LinuxAPI.ntohs(clientAddr.sin_port);

                // --- 获取 IP (uint) ---
                clientIpHost = LinuxAPI.ntohl(clientAddr.sin_addr);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // --- 获取端口 ---
                ClientPort = MacOSAPI.ntohs(clientAddr.sin_port);

                // --- 获取 IP (uint) ---
                clientIpHost = MacOSAPI.ntohl(clientAddr.sin_addr);
            }


            fixed (char* p_IpAddressMemory = IpAddressMemory)
            {
                int ipWritten = ServerFunctions.IpToAscii(clientIpHost, p_IpAddressMemory);
                IpAddress = new UnManagedCollection<char>(p_IpAddressMemory, (uint)ipWritten);
            }
        }
    }

    /// <summary>
    /// Windows 上进行对象初始化
    /// </summary>
    /// <param name="sock"></param>
    internal void InitFromWindows(nint sock)
    {
        // Windows 的 sockaddr_in 布局与 POSIX sockaddr_in_bsd 完全一致：
        //   sin_family  (2 bytes) + sin_port (2 bytes) + sin_addr (4 bytes) + sin_zero (8 bytes)
        // 因此可直接复用 IOCPLibrary.sockaddr_in 结构体。
        sockaddr_in clientAddr = default;
        int addrLen = sizeof(sockaddr_in); // = 16

        uint clientIpHost = 0;

        if (WindowsAPI.getpeername(sock, &clientAddr, &addrLen) == 0)
        {
            // --- 获取端口（网络字节序 → 主机字节序）---
            ClientPort = WindowsAPI.ntohs(clientAddr.sin_port);

            // --- 获取 IP（网络字节序 → 主机字节序）---
            clientIpHost = WindowsAPI.ntohl(clientAddr.sin_addr);

            fixed (char* p_IpAddressMemory = IpAddressMemory)
            {
                int ipWritten = ServerFunctions.IpToAscii(clientIpHost, p_IpAddressMemory);
                IpAddress = new UnManagedCollection<char>(p_IpAddressMemory, (uint)ipWritten);
            }
        }
    }

    /// <summary>
    /// 描述当前对象状态的哈希码。
    /// </summary>
    public ulong StatusCode
    {
        get
        {
            fixed(UHttpConnection* p = &this)
            {
                ulong result = Fingerprint.MemoryFingerprint(p);

                return result;
            }
        }
    }

}
