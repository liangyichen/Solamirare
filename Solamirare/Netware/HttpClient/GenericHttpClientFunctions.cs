using System.Text;

namespace Solamirare;



/// <summary>
/// 提供跨平台 HTTP 客户端内部使用的辅助函数。
/// </summary>
public static unsafe class GenericHttpClientFunctions
{
    // URL 限制
    private const int MAX_URL_LENGTH = 2048;
    private const int DEFAULT_HOST_LENGTH = 255;
    private const int DEFAULT_PATH_LENGTH = 1023;

    private const int MIN_BUFFER_SIZE = 512;

    // HTTP 状态码范围
    private const int MIN_HTTP_STATUS = 100;
    private const int MAX_HTTP_STATUS = 599;

    private const int MAX_RESPONSE_HEADER_SIZE = 1024 * 1024; // 1MB 响应头限制

    const int MAX_RESPONSE_SIZE = 50 * 1024 * 1024; // 50MB 响应限制

    /// <summary>IPv4 地址族常量。</summary>
    public const int AF_INET = 2;
    /// <summary>流式套接字类型常量。</summary>
    public const int SOCK_STREAM = 1;
    /// <summary>TCP 协议常量。</summary>
    public const int IPPROTO_TCP = 6;
    /// <summary>Socket 通用选项层级常量。</summary>
    public const int SOL_SOCKET = 1;
    // Linux socket option values - correct for all Linux architectures including ARM64
    /// <summary>接收超时套接字选项常量。</summary>
    public const int SO_RCVTIMEO = 20;
    /// <summary>发送超时套接字选项常量。</summary>
    public const int SO_SNDTIMEO = 21;

    // DNS Cache
    private struct DnsCacheEntry
    {
        public uint IpAddress;
        public long ExpirationTicks;
    }


    private static ValueDictionary<UnManagedString, DnsCacheEntry> _dnsCache = new ValueDictionary<UnManagedString, DnsCacheEntry>(16, false);


    private const long DNS_TTL_TICKS = 60 * 10000000; // 60 seconds

    // 全局 SSL 上下文缓存 (单线程模型下无需锁，多线程需加锁或使用 ThreadStatic)
    private static IntPtr _globalSslCtx = IntPtr.Zero;



    /// <summary>
    /// 创建空响应对象
    /// </summary>
    internal static ValueHttpResponse CreateEmptyResponse()
    {
        ValueHttpResponse response = default;
        response.StatusCode = 0;
        response.Body = UnManagedCollection<byte>.Empty;
        return response;
    }


    /// <summary>
    /// 检查缓冲区容量是否足够
    /// </summary>
    internal static bool CheckBufferCapacity(UnManagedMemory<byte>* buffer, uint requiredSize)
    {
        if (buffer->UsageSize + requiredSize > buffer->Capacity)
            return false;
        return true;
    }

    /// <summary>
    /// 向缓冲区追加编码后的字符串
    /// </summary>
    internal static uint AppendEncodedString(UnManagedMemory<byte>* buffer, UnManagedString* charData)
    {
        int bytesCount = Encoding.UTF8.GetByteCount(charData->Pointer, (int)charData->UsageSize);

        CheckBufferCapacity(buffer, (uint)(bytesCount + 2));

        int bytesWritten = Encoding.UTF8.GetBytes(charData->Pointer, (int)charData->UsageSize, buffer->Pointer + buffer->UsageSize, (int)(buffer->Capacity - buffer->UsageSize));

        buffer->ReLength(buffer->UsageSize + (uint)bytesWritten);

        return (uint)bytesWritten;
    }


    /// <summary>
    /// 从 ReadOnlySpan 复制到 UnManaged 缓冲区
    /// </summary>
    internal static void CopySpanToBuffer(ReadOnlySpan<char> source, int maxLength, byte* targetBuffer, ref int targetLength)
    {
        targetLength = Math.Min(source.Length, maxLength);
        UnManagedMemory<byte> temp = source.CopyToBytes();
        temp.CopyTo(targetBuffer, (uint)targetLength);
        temp.Dispose();
    }

    /// <summary>
    /// 验证路径有效性（防止路径遍历攻击）
    /// </summary>
    internal static bool ValidatePath(ReadOnlySpan<char> path)
    {
        if (path.Length == 0)
            return true;

        // 检查是否包含 ../ 或 ..\
        int i = 0;
        while (i < path.Length - 2)
        {
            if (path[i] == '.' && path[i + 1] == '.' && (path[i + 2] == '/' || path[i + 2] == '\\'))
                return false;
            i++;
        }

        return true;
    }


    /// <summary>
    /// 验证主机名有效性
    /// </summary>
    internal static bool ValidateHostname(ReadOnlySpan<char> hostname)
    {
        if (hostname.Length == 0)
            return false;

        // 允许字母、数字、点、连字符
        foreach (char c in hostname)
        {
            if (!char.IsLetterOrDigit(c) && c != '.' && c != '-' && c != '_')
                return false;
        }

        // 不能以点或连字符开头
        if (hostname[0] == '.' || hostname[0] == '-')
            return false;

        return true;
    }


    /// <summary>
    /// 解析 URL
    /// </summary>
    internal static bool ParseUrl(UnManagedCollection<char> url, UrlParts* parts)
    {
        parts->HostLength = 0;
        parts->PathLength = 0;
        parts->Port = 0;

        // 验证 URL 长度
        if (url.size == 0 || url.size > MAX_URL_LENGTH)
            return false;

        // 检查协议
        if (url.StartsWith("https://"))
        {
            parts->IsHttps = true;
            url = url.Slice(8);
            parts->Port = 443;
        }
        else if (url.StartsWith("http://"))
        {
            parts->IsHttps = false;
            url = url.Slice(7);
            parts->Port = 80;
        }
        else
        {
            return false;
        }

        // 查找路径分隔符
        int slashPos = url.IndexOf('/');
        ReadOnlySpan<char> hostPart = slashPos >= 0 ? url.Slice(0, (uint)slashPos) : url;
        ReadOnlySpan<char> pathPart = slashPos >= 0 ? url.Slice((uint)slashPos) : "/";

        // 检查端口
        int colonPos = hostPart.IndexOf(':');
        if (colonPos >= 0)
        {
            ReadOnlySpan<char> portStr = hostPart.Slice(colonPos + 1);
            if (!int.TryParse(portStr, out int parsedPort) || parsedPort <= 0 || parsedPort > 65535)
            {
                return false;
            }
            parts->Port = parsedPort;
            hostPart = hostPart.Slice(0, colonPos);
        }

        // 验证主机名
        if (hostPart.IsEmpty || hostPart.Length > DEFAULT_HOST_LENGTH)
        {
            return false;
        }

        // 验证主机名中没有非法字符
        if (!GenericHttpClientFunctions.ValidateHostname(hostPart))
        {
            return false;
        }

        // 复制主机名
        parts->HostLength = hostPart.Length;
        GenericHttpClientFunctions.CopySpanToBuffer(hostPart, DEFAULT_HOST_LENGTH, parts->Host, ref parts->HostLength);
        parts->Host[parts->HostLength] = 0; // null terminator

        // 路径为空时的处理，确保至少为 "/"
        if (pathPart.IsEmpty)
            pathPart = "/";

        // 检查路径不包含非法模式（防止路径遍历）
        if (!ValidatePath(pathPart))
        {
            return false;
        }

        // 复制路径
        parts->PathLength = Math.Min(pathPart.Length, DEFAULT_PATH_LENGTH);
        CopySpanToBuffer(pathPart, DEFAULT_PATH_LENGTH, parts->Path, ref parts->PathLength);

        return true;
    }

    static bool setHeadersForeach(int index, UnManagedString* key, UnManagedString* value, void* caller)
    {
        UnManagedMemory<byte>* buffer = (UnManagedMemory<byte>*)caller;

        if (key is not null && value is not null && key is not null && buffer is not null)
        {

            // 检查缓冲区容量
            int totalSize = (int)(key->UsageSize + 2 + value->UsageSize + 2);
            CheckBufferCapacity(buffer, (uint)totalSize);

            // 追加 header name
            AppendEncodedString(buffer, key);
            buffer->AddRange(": "u8);

            // 追加 header value
            AppendEncodedString(buffer, value);
            buffer->AddRange("\r\n"u8);
        }

        return true;
    }



    internal static uint BuildRequest(UrlParts* parts, UnManagedMemory<byte>* buffer, int bufferSize, ValueDictionary<UnManagedString, UnManagedString>* headers, HttpMethod method, UnManagedString* body, HttpContentType contentType)
    {
        if(parts is null || buffer is null || headers is null || body is null)
            return 0;
            
        buffer->Zero();

        // 检查缓冲区大小
        if (buffer->Capacity < MIN_BUFFER_SIZE)
            //throw new HttpRequestException("Buffer too small");
            return 0;

        // 验证 HTTP 方法
        if (method != HttpMethod.Get && method != HttpMethod.Post)
            //throw new HttpRequestException($"Unsupported HTTP method: {method}");
            return 0;

        // 验证 Content-Type
        if (method == HttpMethod.Post && (int)contentType < 0)
            //throw new HttpRequestException("Invalid Content-Type");
            return 0;

        if (method == HttpMethod.Post)
            buffer->AddRange("POST "u8);
        else
            buffer->AddRange("GET "u8);

        // Path
        byte* pPath = parts->Path;
        GenericHttpClientFunctions.CheckBufferCapacity(buffer, (uint)parts->PathLength);
        buffer->Add(pPath, (uint)parts->PathLength);

        // HTTP/1.1\r\n
        buffer->AddRange(" HTTP/1.1\r\n"u8);

        // Host: 
        buffer->AddRange("Host: "u8);

        // hostname
        byte* pHost = parts->Host;
        GenericHttpClientFunctions.CheckBufferCapacity(buffer, (uint)parts->HostLength);
        buffer->Add(pHost, (uint)parts->HostLength);

        // \r\n
        buffer->AddRange("\r\n"u8);

        headers->ForEach(&setHeadersForeach, buffer);

        if (method == HttpMethod.Post && body is not null && !body->IsEmpty)
        {
            // Content-Type
            buffer->AddRange("Content-Type: "u8);

            switch (contentType)
            {
                case HttpContentType.FormUrlEncoded:
                    buffer->AddRange("application/x-www-form-urlencoded\r\n"u8);
                    break;
                case HttpContentType.Json:
                    buffer->AddRange("application/json\r\n"u8);
                    break;
                default:
                    buffer->AddRange("text/plain\r\n"u8);
                    break;
            }

            // Content-Length
            int bodyBytesCount = Encoding.UTF8.GetByteCount(body->Pointer, (int)body->UsageSize);

            // 检查数值溢出
            if (bodyBytesCount < 0)
                //throw new HttpRequestException("Body size calculation overflow");
                return 0;

            buffer->AddRange("Content-Length: "u8);

            char* lengthStrBuffer = stackalloc char[32];
            UnManagedString lengthChars = bodyBytesCount.IntToUnmanagedString(lengthStrBuffer, 32);
            UnManagedMemory<byte> lengthBytes = lengthChars.CopyToBytes();

            buffer->AddRange(&lengthBytes);
            lengthBytes.Dispose();

            buffer->AddRange("\r\n\r\n"u8);

            // 优化：不再将 Body 写入栈缓冲区，而是只写入 Header。
            // Body 将在 SendBody 中流式发送，从而支持大文件并避免栈溢出。
            return buffer->UsageSize;
        }
        else
        {
            // Connection: close
            buffer->AddRange("Connection: close\r\n\r\n"u8);
            return buffer->UsageSize;
        }
    }

    /// <summary>
    /// 流式发送 Body 数据
    /// </summary>
    internal static void SendBody(int sockfd, UnManagedString* body, bool isSsl, IntPtr ssl)
    {
        if (body == null || body->IsEmpty || sockfd < 0) return;

        // 使用 8KB 的栈缓冲区进行分块编码发送
        const int CHUNK_SIZE = 8192;
        byte* chunkBuffer = stackalloc byte[CHUNK_SIZE];

        long charsProcessed = 0;
        long totalChars = body->UsageSize;
        char* pBody = body->Pointer;

        while (charsProcessed < totalChars)
        {
            // 计算本次处理的字符数 (UTF8 最多 4 字节/字符，2048 chars * 4 = 8192 bytes，安全)
            int charsToProcess = (int)Math.Min(totalChars - charsProcessed, 2048);

            // 防止切分 Surrogate Pair
            if (charsProcessed + charsToProcess < totalChars)
            {
                char lastChar = pBody[charsProcessed + charsToProcess - 1];
                if (char.IsHighSurrogate(lastChar))
                {
                    charsToProcess--;
                }
            }

            if (charsToProcess <= 0) break;

            // 直接使用 GetBytes 避免 Encoder 分配
            int bytesUsed = Encoding.UTF8.GetBytes(pBody + charsProcessed, charsToProcess, chunkBuffer, CHUNK_SIZE);

            if (bytesUsed > 0)
            {
                Send(sockfd, chunkBuffer, (UIntPtr)bytesUsed, isSsl, ssl);
            }

            charsProcessed += charsToProcess;
        }
    }

    /// <summary>
    /// 查找 HTTP 响应头的结束位置
    /// </summary>
    internal static long FindHeaderEndPosition(byte* ptr, long totalReceived)
    {
        if (totalReceived < 4)
            return -1;

        // 安全的边界检查
        for (long i = 0; i <= totalReceived - 4; i++)
        {
            if (ptr[i] == '\r' && ptr[i + 1] == '\n' && ptr[i + 2] == '\r' && ptr[i + 3] == '\n')
                return i + 4;
        }
        return -1;
    }


    internal static bool headersDisposeEach(int index, UnManagedString* key, UnManagedString* value, void* caller)
    {

        if (key is not null) key->Dispose();

        if (value is not null) value->Dispose();

        return true;
    }


    /// <summary>
    /// 从 HTTP 响应中提取状态码
    /// </summary>
    internal static int ExtractStatusCode(byte* ptr, long totalReceived)
    {
        // 格式：HTTP/1.1 200 OK
        // 状态码从位置 9 开始（"HTTP/1.1 " 是 9 个字符）
        if (totalReceived <= 12)
            return 0;

        // 验证 HTTP 版本前缀
        if (ptr[0] != 'H' || ptr[1] != 'T' || ptr[2] != 'T' || ptr[3] != 'P' || ptr[4] != '/' || ptr[6] != '.' || ptr[8] != ' ')
        {
            return -1;
        }

        byte* statusPtr = ptr + 9;

        // 检查三个状态码数字
        if (!char.IsDigit((char)statusPtr[0]) || !char.IsDigit((char)statusPtr[1]) || !char.IsDigit((char)statusPtr[2]))
            return 0;

        // 解析状态码
        int statusCode = (statusPtr[0] - '0') * 100 + (statusPtr[1] - '0') * 10 + (statusPtr[2] - '0');

        // 检查状态码之后应该是空格或 CR
        if (statusPtr[3] != ' ' && statusPtr[3] != '\r')
            return -1;

        return statusCode;
    }

    /// <summary>
    /// 尝试从响应头中提取 Content-Length
    /// </summary>
    internal static long ExtractContentLength(byte* ptr, long headerLength)
    {
        ReadOnlySpan<byte> headers = new ReadOnlySpan<byte>(ptr, (int)headerLength);
        ReadOnlySpan<byte> clKey = "Content-Length: "u8;

        int idx = headers.IndexOf(clKey);
        if (idx == -1)
        {
            // 尝试小写
            clKey = "content-length: "u8;
            idx = headers.IndexOf(clKey);
        }

        if (idx != -1)
        {
            int start = idx + clKey.Length;
            int end = start;
            while (end < headers.Length && (char)headers[end] != '\r' && (char)headers[end] != '\n')
            {
                end++;
            }

            if (end > start)
            {
                ReadOnlySpan<byte> valSpan = headers.Slice(start, end - start);

                long len = 0;

                for (int i = 0; i < valSpan.Length; i++)
                {
                    char b = (char)valSpan[i];
                    if (b >= '0' && b <= '9') len = len * 10 + (b - '0');
                    else break;
                }

                return len;
            }
        }
        return -1;
    }


    /// <summary>
    /// 解析 HTTP 响应，提取状态码和响应体
    /// </summary>
    internal static ValueHttpResponse ParseHttpResponse(UnManagedMemory<byte>* responseBuffer, long totalReceived)
    {
        ValueHttpResponse response = CreateEmptyResponse();

        if (responseBuffer is null || totalReceived <= 0)
            return response;

        // 检查最小 HTTP 响应大小
        if (totalReceived < 12)
            return response;

        byte* ptr = responseBuffer->Pointer;

        // 查找 \r\n\r\n 分隔符（HTTP 头和体的分界线）
        long headerEndPos = FindHeaderEndPosition(ptr, totalReceived);

        if (headerEndPos == -1)
        {
            // 没找到完整响应体分隔符，但可能是流式响应
            headerEndPos = totalReceived;
        }
        else if (headerEndPos > MAX_RESPONSE_HEADER_SIZE)
        {
            // HTTP 头炸弹防护
            return response;
        }

        // 提取状态码
        response.StatusCode = ExtractStatusCode(ptr, totalReceived);

        // 验证状态码有效性
        if (response.StatusCode < MIN_HTTP_STATUS || response.StatusCode > MAX_HTTP_STATUS)
        {
            return CreateEmptyResponse();
        }

        // 提取响应体
        if (headerEndPos < totalReceived)
        {
            checked
            {
                // 防止整数溢出
                uint bodySize = (uint)(totalReceived - headerEndPos);
                response.Body = responseBuffer->Slice((uint)headerEndPos, bodySize).AsUnManagedCollection();
            }
        }

        return response;
    }

    /// <summary>
    /// 统一发送数据（自动处理 SSL 和 平台差异）
    /// </summary>
    internal static void Send(int sockfd, byte* buffer, UIntPtr length, bool isSsl, IntPtr ssl)
    {
        if (sockfd < 0 || buffer is null || length <= 0) return;

        if (isSsl)
        {
            int sent = 0;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                sent = WindowsOpenSSLWrapper.SSL_write(ssl, buffer, length);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                sent = MacOSOpenSSLWrapper.SSL_write(ssl, buffer, length);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                sent = LinuxOpenSSLWrapper.SSL_write(ssl, buffer, length);

            if (sent <= 0)
                return;
        }
        else
        {
            long sent = 0;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                sent = WindowsHttpApi.send(sockfd, buffer, length, 0);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                sent = MacOSAPI.send(sockfd, buffer, length, 0).ToInt64();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                sent = LinuxAPI.send(sockfd, buffer, length, 0);

            if (sent < 0)
                return;
        }
    }

    internal static ValueHttpResponse ReceiveResponse(int sockfd, UnManagedMemory<byte>* responseBuffer)
    {
        long totalReceived = 0;
        long headerEndPos = -1;
        long contentLength = -1;

        if (responseBuffer is null || sockfd < 0)
            return CreateEmptyResponse();

        while (totalReceived < responseBuffer->Capacity && totalReceived < MAX_RESPONSE_SIZE)
        {
            uint remainingCapacity = (uint)Math.Min((long)responseBuffer->Capacity - totalReceived, (long)MAX_RESPONSE_SIZE - totalReceived);

            if (remainingCapacity == 0)
                return CreateEmptyResponse();

            long receivedBytes = 0;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                int received = WindowsHttpApi.recv(sockfd, responseBuffer->Pointer + totalReceived,
                                               remainingCapacity, 0);

                receivedBytes = received;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                IntPtr received = MacOSAPI.recv(sockfd, responseBuffer->Pointer + totalReceived,
                                           remainingCapacity, 0);

                receivedBytes = received.ToInt64();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                int received = LinuxAPI.recv(sockfd, responseBuffer->Pointer + totalReceived, remainingCapacity, 0);

                receivedBytes = received;
            }

            if (receivedBytes < 0)
                return CreateEmptyResponse();

            if (receivedBytes == 0)
                break;

            totalReceived += receivedBytes;

            // 检查是否已接收完所有数据 (基于 Content-Length)
            if (headerEndPos == -1)
            {
                headerEndPos = FindHeaderEndPosition(responseBuffer->Pointer, totalReceived);

                if (headerEndPos == -1 && totalReceived > MAX_RESPONSE_HEADER_SIZE)
                    return CreateEmptyResponse();

                if (headerEndPos != -1)
                {
                    contentLength = ExtractContentLength(responseBuffer->Pointer, headerEndPos);
                }
            }

            if (headerEndPos != -1 && contentLength != -1)
            {
                if (totalReceived >= headerEndPos + contentLength)
                    break; // 数据接收完毕，无需等待 Socket 关闭
            }
        }

        return ParseHttpResponse(responseBuffer, totalReceived);
    }




    internal static ValueHttpResponse ReceiveSSLResponse(IntPtr ssl, UnManagedMemory<byte>* responseBuffer)
    {
        long totalReceived = 0;
        long headerEndPos = -1;
        long contentLength = -1;

        if (responseBuffer is null)
            return GenericHttpClientFunctions.CreateEmptyResponse();

        while (totalReceived < responseBuffer->Capacity && totalReceived < MAX_RESPONSE_SIZE)
        {
            uint remainingCapacity = (uint)Math.Min((long)responseBuffer->Capacity - totalReceived, (long)MAX_RESPONSE_SIZE - totalReceived);

            if (remainingCapacity == 0)
                return CreateEmptyResponse();

            int received = 0;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                received = WindowsOpenSSLWrapper.SSL_read(ssl, responseBuffer->Pointer + totalReceived,
                                             (int)remainingCapacity);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                received = MacOSOpenSSLWrapper.SSL_read(ssl, responseBuffer->Pointer + totalReceived,
                                             (int)remainingCapacity);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                received = LinuxOpenSSLWrapper.SSL_read(ssl, responseBuffer->Pointer + totalReceived, (int)remainingCapacity);
            }

            if (received < 0)
                return CreateEmptyResponse();

            if (received == 0)
                break;

            totalReceived += received;

            // 优化：检查是否已接收完所有数据 (基于 Content-Length)
            if (headerEndPos == -1)
            {
                headerEndPos = FindHeaderEndPosition(responseBuffer->Pointer, totalReceived);

                if (headerEndPos == -1 && totalReceived > MAX_RESPONSE_HEADER_SIZE)
                    return CreateEmptyResponse();

                if (headerEndPos != -1)
                {
                    contentLength = ExtractContentLength(responseBuffer->Pointer, headerEndPos);
                }
            }

            if (headerEndPos != -1 && contentLength != -1)
            {
                if (totalReceived >= headerEndPos + contentLength)
                    break; // 数据接收完毕
            }
        }

        return GenericHttpClientFunctions.ParseHttpResponse(responseBuffer, totalReceived);
    }

    // ==========================================================================================
    // 跨平台统一连接与 SSL 辅助函数
    // ==========================================================================================

    /// <summary>
    /// 检测当前系统是否支持Https网络协议（Windows平台必须安装 OpenSSL）
    /// </summary>
    internal static bool IsHttpsSupported
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return WindowsOpenSSLWrapper.SupportsHttps;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return MacOSOpenSSLWrapper.SupportsHttps;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return LinuxOpenSSLWrapper.SupportsHttps;
            return false;
        }
    }

    internal static void CloseSocket(int sockfd)
    {
        if (sockfd < 0) return;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) WindowsHttpApi.closesocket(sockfd);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) MacOSAPI.close(sockfd);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) LinuxAPI.close(sockfd);
    }

    private static uint ResolveHost(byte* hostname)
    {
        if (hostname == null) return 0;

        // 1. 检查 DNS 缓存
        int len = 0;
        while (hostname[len] != 0) len++;
        if (len == 0) return 0;

        char* hostChars = stackalloc char[len];

        for (int i = 0; i < len; i++)
        {
            hostChars[i] = (char)hostname[i];
        }

        ReadOnlySpan<char> lookupKey = new ReadOnlySpan<char>(hostChars, len);

        long now = DateTime.UtcNow.Ticks;

        DnsCacheEntry* entry = _dnsCache.Index(lookupKey.MapToUnManagedMemory());

        if (entry is not null && entry->ExpirationTicks > now)
            return entry->IpAddress;

        void* hostPtr = null;
        byte** addrList = null;

        // 各平台 gethostbyname 返回的结构体布局一致，可以直接转换指针读取
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            hostent* h = WindowsHttpApi.gethostbyname(hostname);
            if (h != null) addrList = h->h_addr_list;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            MacOSHttpPosixApi.hostent* h = MacOSAPI.gethostbyname(hostname);
            if (h != null) addrList = h->h_addr_list;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            hostent* h = LinuxAPI.gethostbyname(hostname);
            if (h != null) addrList = h->h_addr_list;
        }

        if (addrList == null || addrList[0] == null)
            return 0;

        uint ip = *(uint*)addrList[0];

        // 2. 更新 DNS 缓存
        DnsCacheEntry newEntry;
        newEntry.IpAddress = ip;
        newEntry.ExpirationTicks = now + DNS_TTL_TICKS;

        if (entry is not null)
        {
            _dnsCache.AddOrUpdate(lookupKey, newEntry);
        }
        else
        {
            UnManagedString cacheKey = new UnManagedString((uint)len, (uint)len);

            for (int i = 0; i < len; i++)
            {
                cacheKey.Pointer[i] = hostChars[i];
            }

            _dnsCache.AddOrUpdate(cacheKey, newEntry);
        }

        return ip;
    }

    private static void ConfigureSocket(int sockfd, int timeoutSeconds)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            int ms = timeoutSeconds * 1000;
            WindowsHttpApi.setsockopt(sockfd, WindowsHttpApi.SOL_SOCKET, WindowsHttpApi.SO_RCVTIMEO, &ms, 4);
            WindowsHttpApi.setsockopt(sockfd, WindowsHttpApi.SOL_SOCKET, WindowsHttpApi.SO_SNDTIMEO, &ms, 4);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var tv = new MacOSHttpPosixApi.timeval { tv_sec = (IntPtr)timeoutSeconds, tv_usec = IntPtr.Zero };
            MacOSAPI.setsockopt(sockfd, MacOSHttpPosixApi.SOL_SOCKET, MacOSHttpPosixApi.SO_RCVTIMEO, &tv, (uint)sizeof(MacOSHttpPosixApi.timeval));
            MacOSAPI.setsockopt(sockfd, MacOSHttpPosixApi.SOL_SOCKET, MacOSHttpPosixApi.SO_SNDTIMEO, &tv, (uint)sizeof(MacOSHttpPosixApi.timeval));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var tv = new timeval { tv_sec = timeoutSeconds, tv_usec = 0 };
            LinuxAPI.setsockopt(sockfd, SOL_SOCKET, SO_RCVTIMEO, &tv, (uint)sizeof(timeval));
            LinuxAPI.setsockopt(sockfd, SOL_SOCKET, SO_SNDTIMEO, &tv, (uint)sizeof(timeval));
        }
    }

    internal static int Connect(UrlParts* parts, int timeoutSeconds)
    {
        int sockfd = -1;
        uint ipAddr = ResolveHost(parts->Host);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            sockfd = WindowsHttpApi.socket(WindowsHttpApi.AF_INET, WindowsHttpApi.SOCK_STREAM, WindowsHttpApi.IPPROTO_TCP);
            if (sockfd < 0) return -1;

            ConfigureSocket(sockfd, timeoutSeconds);

            sockaddr_in addr = new sockaddr_in();
            addr.sin_family = WindowsHttpApi.AF_INET;
            addr.sin_port = WindowsAPI.htons((ushort)parts->Port);
            addr.sin_addr = ipAddr;

            if (WindowsHttpApi.connect(sockfd, &addr, (uint)sizeof(sockaddr_in)) < 0)
            {
                WindowsHttpApi.closesocket(sockfd);
                return -1;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            sockfd = MacOSAPI.socket(MacOSHttpPosixApi.AF_INET, MacOSHttpPosixApi.SOCK_STREAM, MacOSHttpPosixApi.IPPROTO_TCP);

            if (sockfd < 0) return -1;

            ConfigureSocket(sockfd, timeoutSeconds);

            MacOSHttpPosixApi.sockaddr_in addr = new MacOSHttpPosixApi.sockaddr_in();
            addr.sin_len = (byte)sizeof(MacOSHttpPosixApi.sockaddr_in);
            addr.sin_family = MacOSHttpPosixApi.AF_INET;
            addr.sin_port = MacOSAPI.htons((ushort)parts->Port);
            addr.sin_addr = ipAddr;

            if (MacOSAPI.connect(sockfd, &addr, (uint)sizeof(MacOSHttpPosixApi.sockaddr_in)) < 0)
            {
                MacOSAPI.close(sockfd);
                return -1;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            sockfd = LinuxAPI.socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);

            if (sockfd < 0) return -1;

            ConfigureSocket(sockfd, timeoutSeconds);

            sockaddr_in addr = new sockaddr_in();
            addr.sin_family = AF_INET;
            addr.sin_port = LinuxAPI.htons((ushort)parts->Port);
            addr.sin_addr = ipAddr;

            if (LinuxAPI.connect(sockfd, &addr, (uint)sizeof(sockaddr_in)) < 0)
            {
                LinuxAPI.close(sockfd);
                return -1;
            }
        }
        else
        {
            return -1;
        }

        return sockfd;
    }

    internal static void SetupSSL(int sockfd, out IntPtr sslCtx, out IntPtr ssl)
    {
        sslCtx = IntPtr.Zero;
        ssl = IntPtr.Zero;

        try
        {
            // 优化：复用全局 SSL_CTX，避免每次请求都重新加载证书和算法
            if (_globalSslCtx == IntPtr.Zero)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    _globalSslCtx = WindowsOpenSSLWrapper.SSL_CTX_new(WindowsOpenSSLWrapper.TLS_client_method());
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    _globalSslCtx = MacOSOpenSSLWrapper.SSL_CTX_new(MacOSOpenSSLWrapper.TLS_client_method());
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    _globalSslCtx = LinuxOpenSSLWrapper.SSL_CTX_new(LinuxOpenSSLWrapper.TLS_client_method());

                if (_globalSslCtx == IntPtr.Zero) return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ssl = WindowsOpenSSLWrapper.SSL_new(_globalSslCtx);
                if (ssl == IntPtr.Zero) return;
                if (WindowsOpenSSLWrapper.SSL_set_fd(ssl, sockfd) != 1) return;
                if (WindowsOpenSSLWrapper.SSL_connect(ssl) != 1) return;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ssl = MacOSOpenSSLWrapper.SSL_new(_globalSslCtx);
                if (ssl == IntPtr.Zero) return;
                if (MacOSOpenSSLWrapper.SSL_set_fd(ssl, sockfd) != 1) return;
                if (MacOSOpenSSLWrapper.SSL_connect(ssl) != 1) return;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ssl = LinuxOpenSSLWrapper.SSL_new(_globalSslCtx);
                if (ssl == IntPtr.Zero) return;
                if (LinuxOpenSSLWrapper.SSL_set_fd(ssl, sockfd) != 1) return;
                if (LinuxOpenSSLWrapper.SSL_connect(ssl) != 1) return;
            }
        }
        catch
        {
            CleanupSSL(ssl, sslCtx);
            throw;
        }
    }

    internal static void CleanupSSL(IntPtr ssl, IntPtr sslCtx)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (ssl != IntPtr.Zero) { WindowsOpenSSLWrapper.SSL_shutdown(ssl); WindowsOpenSSLWrapper.SSL_free(ssl); }
            if (sslCtx != IntPtr.Zero) WindowsOpenSSLWrapper.SSL_CTX_free(sslCtx);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (ssl != IntPtr.Zero) { MacOSOpenSSLWrapper.SSL_shutdown(ssl); MacOSOpenSSLWrapper.SSL_free(ssl); }
            if (sslCtx != IntPtr.Zero) MacOSOpenSSLWrapper.SSL_CTX_free(sslCtx);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (ssl != IntPtr.Zero) { LinuxOpenSSLWrapper.SSL_shutdown(ssl); LinuxOpenSSLWrapper.SSL_free(ssl); }
            if (sslCtx != IntPtr.Zero) LinuxOpenSSLWrapper.SSL_CTX_free(sslCtx);
        }
    }
}
