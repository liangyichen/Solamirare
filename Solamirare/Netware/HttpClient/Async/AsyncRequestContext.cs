namespace Solamirare;


using System.Runtime.InteropServices;

/// <summary>
/// 请求上下文
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct AsyncRequestContext
{
    //维护提示： 禁止修改字段顺序，禁止手动设置结构的容量

    /// <summary>指向主机名字符串的指针。</summary>
    public char* HostPtr;
    /// <summary>主机名字符串的长度。</summary>
    public int HostLen;
    /// <summary>目标端口号。</summary>
    public ushort Port;
    /// <summary>指向请求路径字符串的指针。</summary>
    public char* PathPtr;
    /// <summary>请求路径字符串的长度。</summary>
    public int PathLen;
    /// <summary>解析后的远程 IPv4 地址。</summary>
    public uint RemoteAddr;
    /// <summary>用于存储 HTTP 响应数据的非托管缓冲区。</summary>
    public UnManagedMemory<byte>* ResponseBuffer;
    /// <summary>请求完成后的回调函数指针。</summary>
    public nint Callback;
    /// <summary>当前 HTTP 请求的状态。</summary>
    public AsyncHttpClientRequestState State;
    /// <summary>套接字文件描述符。</summary>
    public int SocketFd;
    /// <summary>已接收的响应字节总数。</summary>
    public uint ReceivedBytes;

    // POST 与 HTTPS 支持扩展
    /// <summary>HTTP 请求方法（GET, POST 等）。</summary>
    public HttpMethod Method;
    /// <summary>指向请求体数据的非托管内存指针。</summary>
    public UnManagedString* Body;
    /// <summary>请求体的内容类型。</summary>
    public HttpContentType ContentType;
    /// <summary>已处理（发送）的请求体字符数。</summary>
    public long BodyProcessedChars;

    /// <summary>是否使用 HTTPS 协议。</summary>
    public bool IsHttps;
    /// <summary>SSL/TLS 句柄指针（由 OpenSSL 使用）。</summary>
    public IntPtr SslHandle;
    /// <summary>内部使用的请求缓冲区指针，用于堆分配的请求头及 Body 分块中转。</summary>
    public byte* InternalRequestBuffer;
    /// <summary>当前发送的数据偏移量（用于兼容性处理）。</summary>
    public uint CurrentSendOffset;
}
