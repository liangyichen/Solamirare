using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Solamirare;




/// <summary>
/// 服务器通用逻辑
/// </summary>
public static unsafe class ServerFunctions
{

    const byte CR = (byte)'\r'; // 0x0D

    const byte LF = (byte)'\n'; // 0x0A

    static ReadOnlySpan<byte> CRLF => "\r\n"u8;

    static ReadOnlySpan<byte> CRLFCRLF => "\r\n\r\n"u8;

    /// <summary>
    /// 启动状态
    /// </summary>
    /// <param name="name"></param>
    /// <param name="ServerConfig"></param>
    public static void ConsoleStartedStatus(ReadOnlySpan<char> name, HTTPSeverConfig* ServerConfig)
    {
        Console.WriteLine($"{name} 启动成功, 线程ID: {Thread.CurrentThread.ManagedThreadId},监听端口: {ServerConfig->Port}, 最大连接数: {ServerConfig->MAX_CONNECTIONS}, 读缓冲区大小: {ServerConfig->READ_BUFFER_CAPACITY}, 响应缓冲区大小: {ServerConfig->RESPONSE_BUFFER_CAPACITY}");
    }


    /// <summary>
    /// 初始化 Context
    /// </summary>
    /// <param name="config"></param>
    /// <param name="context"></param>
    /// <param name="responseBuffer"></param>
    /// <returns></returns>
    public static bool InitConnectionContext(HTTPSeverConfig* config, UHttpContext* context, byte* responseBuffer)
    {

        if (context is null || context->RequestHeader.IsEmpty || responseBuffer is null)
            return false;


        context->Response.Init(config, context, responseBuffer);

        context->ReadBytes = context->RequestHeader.UsageSize;


        //==============================================


        // 1. 分离请求行 (Request Line)

        int requestLineEnd = context->RequestHeader.IndexOf(CRLF);

        if (requestLineEnd < 0) return false;

        // 在请求行之后的剩余内容里查找 \r\n\r\n
        int searchStart = requestLineEnd + 2;

        int relativeHeaderEnd = context->RequestHeader.Slice((uint)searchStart).IndexOf(CRLFCRLF);

        if (relativeHeaderEnd < 0) return false;

        // 转回绝对偏移
        int headerEndIndex = searchStart + relativeHeaderEnd;

        UnManagedCollection<byte> requestLineBytes = context->RequestHeader.Slice(0, (uint)requestLineEnd);

        // 2. 分离请求头块 (Headers)

        int headerBlockStart = requestLineEnd + 2; // 2 是 \r\n 的长度

        int headerBlockLength = headerEndIndex - headerBlockStart;

        if (headerBlockLength < 0)  /*  GET / HTTP/1.1\r\n\r\n 是合法的最简 HTTP 请求，没有任何 Header */
        {
            return false;
        }

        UnManagedCollection<byte> headerBlockBytes = context->RequestHeader.Slice((uint)headerBlockStart, (uint)headerBlockLength);

        // 3. 分离请求体 (Body)
        const int terminatorLength = 4; // \r\n\r\n

        int bodyStart = headerEndIndex + terminatorLength;

        int bodyLength = (int)context->RequestHeader.UsageSize - bodyStart;

        UnManagedCollection<byte> bodyBytes = UnManagedCollection<byte>.Empty;
        if (bodyLength > 0)
            bodyBytes = context->RequestHeader.Slice((uint)bodyStart);

        //======================

        context->Request.Init(&requestLineBytes, &headerBlockBytes, &bodyBytes);

        return true;
    }

    /// <summary>
    /// 检查 HTTP 请求是否接收完整（Headers 结束且 Body 已达到 Content-Length 长度）。
    /// </summary>
    public static bool IsRequestComplete(UHttpContext* context)
    {
        if (context == null || context->RequestHeader.IsEmpty) return false;

        int headerEndIndex = context->RequestHeader.IndexOf(CRLFCRLF);
        
        if (headerEndIndex < 0) return false;

        int headerLength = headerEndIndex + 4;
        long contentLength = GenericHttpClientFunctions.ExtractContentLength(context->RequestHeader.Pointer, headerLength);

        if (contentLength <= 0) return true;


        // 先做边界检查，防止 uint 减法下溢
        if ((uint)headerLength > context->RequestHeader.UsageSize) return false;

        return (context->RequestHeader.UsageSize - (uint)headerLength) >= (ulong)contentLength;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="config"></param>
    /// <param name="userLogic"></param>
    /// <param name="context"></param>
    /// <param name="responseBuffer"></param>
    /// <param name="fd_or_sock">windows 传入sock的地址，linux 和 macos 传入 fd 的地址</param>
    /// <returns></returns>
    public static bool ProcessUserLogic(
        HTTPSeverConfig* config,
        delegate*<UHttpContext*, bool> userLogic,
        UHttpContext* context,
        byte* responseBuffer,
        void* fd_or_sock
    )
    {


        bool result = false;

        if (context is null || context->RequestHeader.IsEmpty || responseBuffer is null || fd_or_sock is null) return result;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            nint* p_sock = (nint*)fd_or_sock;

            context->Connection.InitFromWindows(*p_sock);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            int* p_fd = (int*)fd_or_sock;

            context->Connection.InitFromPosix(*p_fd);
        }

        bool contextInit = InitConnectionContext(config, context, responseBuffer);

        if (!contextInit) return result;

        bool proceed;

        if (userLogic is not null)
            proceed = userLogic(context);
        else
            proceed = DefaultUserLogic(context);


        context->RequestHeader.Clear(); //已经完成用户逻辑，请求数据立即释放，尽快让别的线程重用这段内存


        if (!proceed)
        {
            return result;
        }
        else
        {
            // 序列化响应头，将状态码和 Content-Length 回填到响应缓冲区的预留位置，
            // 并将最终响应总长度写入 context->TotalResponseLength
            // Serialize response headers, backfill status code and Content-Length into reserved positions
            // in the response buffer, and write the final total length to context->TotalResponseLength
            context->Response.FinalizeResponse();


            result = true;


            return result;
        }

    }







    /// <summary>
    /// Keep-Alive 检测
    /// <para>HTTP/1.1 默认 keep-alive；HTTP/1.0 默认 close</para>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static bool IsKeepAliveRequest(UHttpContext* context)
    {
        bool keepAlive = true;
        UnManagedCollection<byte> connValue = context->Request.ConnectionHeader;

        // 客户端明确要求 Connection: close，则关闭连接
        // Client explicitly requested Connection: close; close the connection
        if (!connValue.IsEmpty && connValue.Equals("close"u8))
            keepAlive = false;

        return keepAlive;
    }


    /// <summary>
    /// 将 uint IP 地址（主机字节序）转换为 ASCII 字符。
    /// 直接写入非托管/栈分配的 char 缓冲区，零 GC。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int IpToAscii(uint ip, char* buffer)
    {
        char* ptr = buffer;
        int written = 0;

        // 辅助函数：写入一个八位字节（0-255）
        void WriteOctet(uint octet)
        {
            char* currentPtr = ptr;

            if (octet >= 100)
            {
                *currentPtr++ = (char)('0' + (octet / 100));
                octet %= 100;
            }
            if (octet >= 10 || currentPtr != ptr)
            {
                *currentPtr++ = (char)('0' + (octet / 10));
                octet %= 10;
            }
            *currentPtr++ = (char)('0' + octet);

            written += (int)(currentPtr - ptr);
            ptr = currentPtr;
        }

        // IP地址的四个段。主机字节序下，按网络序（b1.b2.b3.b4）的顺序提取。
        uint b1 = (ip >> 24) & 0xFF;
        uint b2 = (ip >> 16) & 0xFF;
        uint b3 = (ip >> 8) & 0xFF;
        uint b4 = ip & 0xFF;

        // 1. Write Octet 1 (b1)
        WriteOctet(b1);
        *ptr++ = '.'; written++;

        // 2. Write Octet 2 (b2)
        WriteOctet(b2);
        *ptr++ = '.'; written++;

        // 3. Write Octet 3 (b3)
        WriteOctet(b3);
        *ptr++ = '.'; written++;

        // 4. Write Octet 4 (b4)
        WriteOctet(b4);

        return written;
    }


    /// <summary>
    /// Fast pre-check to reject obviously non-HTTP payloads before full parse.
    /// 在完整解析前快速拒绝明显非 HTTP 请求。
    /// </summary>
    public static bool IsLikelyHttpRequest(byte* buffer, uint len)
    {
        if (buffer == null || len < 14) // e.g. "GET / HTTP/1.1"
            return false;
        if (buffer[0] < (byte)'A' || buffer[0] > (byte)'Z')
            return false;

        bool hasSpace = false;
        uint scan = len < 64 ? len : 64;
        for (uint i = 0; i < scan; i++)
        {
            if (buffer[i] == (byte)' ')
            {
                hasSpace = true;
                break;
            }
        }
        return hasSpace;
    }

    /// <summary>
    /// 端口字节序转换
    /// </summary>
    /// <param name="port"></param>
    /// <returns></returns>
    public static ushort Htons(ushort port) =>
        (ushort)((port >> 8) | ((port & 0xFF) << 8));


    /// <summary>Default response logic when callback is null / 回调为空时的默认响应逻辑。</summary>
    public static bool DefaultUserLogic(UHttpContext* context)
    {
        if (context == null) return false;
        context->Response.ResponseContentType = HttpMimeTypes.TextPlain;
        context->Response.Write("ok"u8);
        return true;
    }


}