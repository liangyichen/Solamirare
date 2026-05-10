namespace Solamirare;

using System;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
/// 纯原生 IOCP + Socket 异步 HTTP 客户端（零 GC、回调）。
/// 使用 Windows I/O Completion Ports 实现完全非阻塞的异步操作。
/// </summary>
public unsafe struct AsyncWindowsHttpClient
{
    private static void* _iocpHandle = null;
    private static int _pendingRequests;
    private static bool _isRunning;
    private static void* _workerThread;

    private static IntPtr _globalSslCtx = IntPtr.Zero;

    // OpenSSL 错误常量，用于非阻塞操作
    private const int SSL_ERROR_WANT_READ = 2;
    private const int SSL_ERROR_WANT_WRITE = 3;

    /// <summary>
    /// 内部上下文扩展，确保 OVERLAPPED 结构紧跟其后以便 IOCP 转换。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct InternalContext
    {
        //维护提示： 禁止修改字段顺序，禁止手动设置结构的容量

        public OVERLAPPED Overlapped;


        public AsyncRequestContext UserContext;


        public byte* RequestBuffer; // 用于持有堆分配的发送缓冲区指针，防止异步发送时栈内存失效
    }


    /// <summary>
    /// 初始化上下文
    /// </summary>
    /// <param name="timeoutSeconds"></param>
    /// <param name="retries"></param>
    /// <exception cref="Exception"></exception>
    public void Initialize(int timeoutSeconds = 10, int retries = 3)
    {
        if (_iocpHandle == null)
        {
            // 显式初始化 WinSock，确保 gethostbyname 等网络 API 可用
            WindowsHttpApi.WSAData wsaData;
            WindowsAPI.WSAStartup(0x0202, out wsaData);

            _iocpHandle = WindowsAPI.CreateIoCompletionPort((void*)-1, null, 0, 0);
            if (_iocpHandle == null)
                throw new Exception("CreateIoCompletionPort failed");

            if (WindowsOpenSSLWrapper.SupportsHttps)
            {
                _globalSslCtx = WindowsOpenSSLWrapper.SSL_CTX_new(WindowsOpenSSLWrapper.TLS_client_method());
            }

            _isRunning = true;
            ThreadStartInfo info = new ThreadStartInfo();
            info.Worker = &IocpWorkerThread;
            info.Arg = null;
            NativeThread.Create(out _workerThread, &info);
        }

        _pendingRequests = 0;
    }

    [UnmanagedCallersOnly]
    private static uint IocpWorkerThread(void* arg)
    {
        uint bytesTransferred;
        nuint completionKey;
        OVERLAPPED* lpOverlapped;

        while (_isRunning)
        {
            bool result = WindowsAPI.GetQueuedCompletionStatus(_iocpHandle, &bytesTransferred, &completionKey, &lpOverlapped, 100);

            if (lpOverlapped == null) continue;

            InternalContext* internalCtx = (InternalContext*)lpOverlapped;
            AsyncRequestContext* ctx = &internalCtx->UserContext;

            if (!result)
            {
                CompleteRequest(internalCtx);
                continue;
            }

            switch (ctx->State)
            {
                case AsyncHttpClientRequestState.Connecting:
                    HandleConnectCompletion(internalCtx);
                    break;
                case AsyncHttpClientRequestState.Handshaking:
                    AdvanceHandshake(internalCtx);
                    break;
                case AsyncHttpClientRequestState.Sending: // 当 WSASend 完成时触发
                    HandleReadEvent(internalCtx); // 数据发送完毕，立即开始读取响应
                    break;
                case AsyncHttpClientRequestState.Receiving:
                    if (bytesTransferred > 0)
                    {
                        ctx->ReceivedBytes += bytesTransferred;
                        ctx->ResponseBuffer->ReLength(ctx->ReceivedBytes);
                        HandleReadEvent(internalCtx); // 继续读，直到 bytesTransferred 为 0
                    }
                    else
                    {
                        CompleteRequest(internalCtx);
                    }
                    break;
            }
        }
        return 0;
    }


    /// <summary>
    /// 启动异步 GET 请求
    /// </summary>
    /// <param name="url"></param>
    /// <param name="responseBuffer"></param>
    /// <param name="callback"></param>
    public void RequestGETAsync(UnManagedCollection<char> url, UnManagedMemory<byte>* responseBuffer, delegate* unmanaged<void*, void> callback)
    {
        RequestInternalAsync(url, responseBuffer, HttpMethod.Get, null, default, callback);
    }

    /// <summary>
    /// 启动异步 POST 请求
    /// </summary>
    public void RequestPOSTAsync(
        UnManagedCollection<char> url,
        UnManagedMemory<byte>* responseBuffer,
        UnManagedString* body,
        HttpContentType contentType,
        delegate* unmanaged<void*, void> callback)
    {
        RequestInternalAsync(url, responseBuffer, HttpMethod.Post, body, contentType, callback);
    }

    private static void RequestInternalAsync(
        UnManagedCollection<char> url,
        UnManagedMemory<byte>* responseBuffer,
        HttpMethod method,
        UnManagedString* body,
        HttpContentType contentType,
        delegate* unmanaged<void*, void> callback)
    {
        if (url.IsEmpty || url.InternalPointer == null || responseBuffer == null || callback == null) return;

        if (!HttpClientHelper.TryParseHttpUrl(url, out char* hostPtr, out int hostLen, out ushort port, out char* pathPtr, out int pathLen))
        {
            return;
        }

        if (!HttpClientHelper.ResolveHostWindows(hostPtr, hostLen, out uint addr))
        {
            return;
        }

        InternalContext* internalCtx = (InternalContext*)NativeMemory.AllocZeroed((nuint)sizeof(InternalContext));
        

        AsyncRequestContext* ctx = &internalCtx->UserContext;
        ctx->HostPtr = hostPtr;
        ctx->HostLen = hostLen;
        ctx->Port = port;
        ctx->PathPtr = pathPtr;
        ctx->PathLen = pathLen;
        ctx->RemoteAddr = addr;
        ctx->ResponseBuffer = responseBuffer;
        ctx->Callback = (nint)callback;
        ctx->State = AsyncHttpClientRequestState.Connecting;
        ctx->SocketFd = -1;
        ctx->ReceivedBytes = 0;

        ctx->Method = method;
        ctx->Body = body;
        ctx->ContentType = contentType;
        ctx->BodyProcessedChars = 0;

        ctx->IsHttps = url.StartsWith("https://");
        ctx->SslHandle = IntPtr.Zero;
        ctx->InternalRequestBuffer = null;

        Interlocked.Increment(ref _pendingRequests);

        StartConnect(internalCtx);
    }

    private static void StartConnect(InternalContext* internalCtx)
    {
        AsyncRequestContext* ctx = &internalCtx->UserContext;
        IntPtr sock = WindowsAPI.socket_raw(2 /* AF_INET */, 1 /* SOCK_STREAM */, 6 /* IPPROTO_TCP */);
        if (sock == (IntPtr)(-1))
        {
            CompleteRequest(internalCtx);
            return;
        }

        ctx->SocketFd = (int)sock;

        // 将 Socket 关联到 IOCP
        if (WindowsAPI.CreateIoCompletionPort((void*)sock, _iocpHandle, (nuint)ctx, 0) == null)
        {
            CompleteRequest(internalCtx);
            return;
        }

        sockaddr_in addr = default;
        addr.sin_family = 2; // AF_INET
        addr.sin_port = WindowsAPI.htons(ctx->Port);
        addr.sin_addr = ctx->RemoteAddr;

        // Windows ConnectEx 是真正的异步，但为了兼容现有 WindowsHttpApi 结构，
        // 我们使用非阻塞 connect + WSASend 触发。
        int connectResult = WindowsAPI.connect_raw(sock, (IntPtr)(&addr), sizeof(sockaddr_in));

        // 如果立即成功或正在进行中，我们进入发送状态
        // 注意：在 IOCP 模型中，我们通常手动投递一个完成包来启动状态机
        WindowsAPI.PostQueuedCompletionStatus(_iocpHandle, 0, (nuint)ctx, &internalCtx->Overlapped);
    }

    private static void HandleConnectCompletion(InternalContext* internalCtx)
    {
        AsyncRequestContext* ctx = &internalCtx->UserContext;
        if (ctx->IsHttps)
        {
            if (_globalSslCtx == IntPtr.Zero) { CompleteRequest(internalCtx); return; }
            ctx->SslHandle = WindowsOpenSSLWrapper.SSL_new(_globalSslCtx);
            WindowsOpenSSLWrapper.SSL_set_fd(ctx->SslHandle, ctx->SocketFd);
            ctx->State = AsyncHttpClientRequestState.Handshaking;
            AdvanceHandshake(internalCtx);
        }
        else
        {
            ctx->State = AsyncHttpClientRequestState.Sending;
            SendRequest(internalCtx);
        }
    }

    private static void AdvanceHandshake(InternalContext* internalCtx)
    {
        AsyncRequestContext* ctx = &internalCtx->UserContext;
        int ret = WindowsOpenSSLWrapper.SSL_connect(ctx->SslHandle);
        if (ret == 1)
        {
            ctx->State = AsyncHttpClientRequestState.Sending;
            SendRequest(internalCtx);
            return;
        }

        int sslErr = WindowsOpenSSLWrapper.SSL_get_error(ctx->SslHandle, ret);
        uint bytes;
        uint flags = 0;
        if (sslErr == SSL_ERROR_WANT_READ)
        {
            WindowsAPI.WSARecv(ctx->SocketFd, null, 0, &bytes, &flags, &internalCtx->Overlapped, 0);
        }
        else if (sslErr == SSL_ERROR_WANT_WRITE)
        {
            WindowsAPI.WSASend(ctx->SocketFd, null, 0, &bytes, 0, &internalCtx->Overlapped, 0);
        }
        else
        {
            CompleteRequest(internalCtx);
        }
    }

    private static void SendRequest(InternalContext* internalCtx)
    {
        AsyncRequestContext* ctx = &internalCtx->UserContext;
        if (ctx->SocketFd < 0) { CompleteRequest(internalCtx); return; }

        const int maxLen = 8192; // 扩大缓冲区以容纳 Headers + Body
        if (ctx->InternalRequestBuffer == null)
            ctx->InternalRequestBuffer = (byte*)NativeMemory.Alloc(maxLen);

        byte* buffer = ctx->InternalRequestBuffer;
        internalCtx->RequestBuffer = buffer;

        int len = 0;
        int res;

        // 1. 写入请求行 (Method + Path + Protocol)
        res = HttpClientHelper.WriteAscii(buffer, len, maxLen, ctx->Method == HttpMethod.Post ? "POST " : "GET ");
        if (res < 0) { CompleteRequest(internalCtx); return; }
        len += res;

        res = HttpClientHelper.WriteAscii(buffer, len, maxLen, ctx->PathPtr, ctx->PathLen);
        if (res < 0) { CompleteRequest(internalCtx); return; }
        len += res;

        res = HttpClientHelper.WriteAscii(buffer, len, maxLen, " HTTP/1.1\r\nHost: ");
        if (res < 0) { CompleteRequest(internalCtx); return; }
        len += res;

        // 2. 写入 Host 头部
        res = HttpClientHelper.WriteAscii(buffer, len, maxLen, ctx->HostPtr, ctx->HostLen);
        if (res < 0) { CompleteRequest(internalCtx); return; }
        len += res;

        res = HttpClientHelper.WriteAscii(buffer, len, maxLen, "\r\nUser-Agent: SolamirareAsyncWindowsClient\r\nConnection: close\r\n");
        if (res < 0) { CompleteRequest(internalCtx); return; }
        len += res;

        // 3. 处理 POST 头部与 Body (一次性填充)
        if (ctx->Method == HttpMethod.Post && ctx->Body != null && !ctx->Body->IsEmpty)
        {
            res = HttpClientHelper.WriteAscii(buffer, len, maxLen, "Content-Type: ");
            if (res < 0) { CompleteRequest(internalCtx); return; }
            len += res;

            string cType = ctx->ContentType switch
            {
                HttpContentType.FormUrlEncoded => "application/x-www-form-urlencoded",
                HttpContentType.Json => "application/json",
                _ => "text/plain"
            };
            res = HttpClientHelper.WriteAscii(buffer, len, maxLen, cType);
            if (res < 0) { CompleteRequest(internalCtx); return; }
            len += res;

            res = HttpClientHelper.WriteAscii(buffer, len, maxLen, "; charset=UTF-8\r\nContent-Length: ");
            if (res < 0) { CompleteRequest(internalCtx); return; }
            len += res;

            int bodyBytesCount = System.Text.Encoding.UTF8.GetByteCount(ctx->Body->Pointer, (int)ctx->Body->UsageSize);
            len += AsciiConverter.IntToAscii(bodyBytesCount, buffer + len);

            res = HttpClientHelper.WriteAscii(buffer, len, maxLen, "\r\n\r\n");
            if (res < 0) { CompleteRequest(internalCtx); return; }
            len += res;

            // 直接在 Header 结束后追加 Body 内容
            int bodyBytesEncoded = System.Text.Encoding.UTF8.GetBytes(ctx->Body->Pointer, (int)ctx->Body->UsageSize, buffer + len, maxLen - len);
            if (bodyBytesEncoded < 0) { CompleteRequest(internalCtx); return; }
            len += bodyBytesEncoded;
        }
        else
        {
            res = HttpClientHelper.WriteAscii(buffer, len, maxLen, "\r\n");
            if (res < 0) { CompleteRequest(internalCtx); return; }
            len += res;
        }


#if DEBUG
        // --- Debug: 发送前打印完整构造数据，显式展示 \r\n ---
        Console.WriteLine("[DEBUG] AsyncWindowsHttpClient: Constructing Request...");
        Console.WriteLine(System.Text.Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ctx->InternalRequestBuffer, len)).Replace("\r", "\\r").Replace("\n", "\\n"));
        Console.WriteLine("-------------------------------------------------------");
#endif
        if (ctx->IsHttps)
        {
            ctx->State = AsyncHttpClientRequestState.Sending; // 设置状态，确保 SSL_write 完成后触发 IocpWorkerThread
            int ret = WindowsOpenSSLWrapper.SSL_write(ctx->SslHandle, buffer, (UIntPtr)len);
            {
                int sslErr = WindowsOpenSSLWrapper.SSL_get_error(ctx->SslHandle, ret);
                if (sslErr == SSL_ERROR_WANT_WRITE)
                {
                    uint bytes;
                    WindowsAPI.WSASend(ctx->SocketFd, null, 0, &bytes, 0, &internalCtx->Overlapped, 0);
                }
                else
                {
                    CompleteRequest(internalCtx);
                }
            }
            return;
        }

        ctx->State = AsyncHttpClientRequestState.Sending; // 设置状态，确保 WSASend 完成后触发正确分支
        WSABUF wsabuf;
        wsabuf.len = (uint)len;
        wsabuf.buf = buffer;
        uint bytesSent;

        int result = WindowsAPI.WSASend(ctx->SocketFd, &wsabuf, 1, &bytesSent, 0, &internalCtx->Overlapped, 0);

        if (result == -1 /* SOCKET_ERROR */)
        {
            int err = (int)WindowsAPI.GetLastError();

            if (err != 997 /* WSA_IO_PENDING */)
            {
                CompleteRequest(internalCtx);
            }
        }
    }

    private static void HandleReadEvent(InternalContext* internalCtx)
    {
        AsyncRequestContext* ctx = &internalCtx->UserContext;
        ctx->State = AsyncHttpClientRequestState.Receiving;
        if (ctx->SocketFd < 0 || ctx->ResponseBuffer == null) { CompleteRequest(internalCtx); return; }

        byte* buffer = ctx->ResponseBuffer->Pointer + ctx->ReceivedBytes;
        uint remain = ctx->ResponseBuffer->Capacity - ctx->ReceivedBytes;

        if (remain == 0)
        {
            CompleteRequest(internalCtx);
            return;
        }

        if (ctx->IsHttps)
        {
            int ret = WindowsOpenSSLWrapper.SSL_read(ctx->SslHandle, buffer, (int)remain);
            if (ret > 0)
            {
                ctx->ReceivedBytes += (uint)ret;
                ctx->ResponseBuffer->ReLength(ctx->ReceivedBytes);
                HandleReadEvent(internalCtx);
            }
            else
            {
                int sslErr = WindowsOpenSSLWrapper.SSL_get_error(ctx->SslHandle, ret);
                if (sslErr == SSL_ERROR_WANT_READ)
                {
                    uint bytes;
                    uint flags_0 = 0;
                    WindowsAPI.WSARecv(ctx->SocketFd, null, 0, &bytes, &flags_0, &internalCtx->Overlapped, 0);
                }
                else { CompleteRequest(internalCtx); }
            }
            return;
        }

        WSABUF wsabuf;
        wsabuf.len = remain;
        wsabuf.buf = buffer;


        uint bytesRecv;
        uint flags = 0;
        int result = WindowsAPI.WSARecv(ctx->SocketFd, &wsabuf, 1, &bytesRecv, &flags, &internalCtx->Overlapped, 0);

        if (result == -1 /* SOCKET_ERROR */)
        {
            int err = (int)WindowsAPI.GetLastError();
            if (err != 997 /* WSA_IO_PENDING */)
            {
                CompleteRequest(internalCtx);
            }
        }
    }

    private static void CompleteRequest(InternalContext* internalCtx)
    {
        if (internalCtx == null) return;
        AsyncRequestContext* ctx = &internalCtx->UserContext;
        ctx->State = AsyncHttpClientRequestState.Completed;

        if (ctx->Callback != 0)
            ((delegate* unmanaged<void*, void>)ctx->Callback)(ctx);
        
        if (ctx->IsHttps && ctx->SslHandle != IntPtr.Zero)
        {
            WindowsOpenSSLWrapper.SSL_shutdown(ctx->SslHandle);
            WindowsOpenSSLWrapper.SSL_free(ctx->SslHandle);
        }

        if (ctx->SocketFd >= 0)
            WindowsAPI.closesocket_raw(ctx->SocketFd);

        Interlocked.Decrement(ref _pendingRequests);

        // 释放请求过程中分配的非托管堆内存，确保无内存泄漏
        if (internalCtx->RequestBuffer != null)
        {
            NativeMemory.Free(internalCtx->RequestBuffer);
            internalCtx->RequestBuffer = null;
        }

        NativeMemory.Free(internalCtx);


    }


    /// <summary>
    /// 销毁资源
    /// </summary>
    public void Dispose()
    {
        _isRunning = false;
        if (_iocpHandle != null)
        {
            WindowsAPI.CloseHandle(_iocpHandle);
            _iocpHandle = null;
        }
    }





}