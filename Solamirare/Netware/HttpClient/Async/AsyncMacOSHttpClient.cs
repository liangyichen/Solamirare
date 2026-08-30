namespace Solamirare;

using System;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
/// 纯原生 Kqueue + Socket 异步 HTTP 客户端（零 GC、回调）。
/// 参考 MacOSHttpServer 的 Kqueue 设计风格（EVFILT_READ/WRITE、EV_ERROR、FIONBIO、SO_NOSIGPIPE）。
/// </summary>
public unsafe struct AsyncMacOSHttpClient
{
    private static int _kqFd = -1;
    private static int _pendingRequests;
    private static IntPtr _dispatchQueue = IntPtr.Zero;
    private static IntPtr _kqueueDispatchSource = IntPtr.Zero;
    private static IntPtr _dispatchSourceTypeRead = IntPtr.Zero;

    private static IntPtr _globalSslCtx = IntPtr.Zero;

    // Kqueue 常数
    private const ulong FIONBIO = 0x8004667E;

    // OpenSSL 错误常量
    private const int SSL_ERROR_WANT_READ = 2;
    private const int SSL_ERROR_WANT_WRITE = 3;

    public void Initialize(int timeoutSeconds = 10)
    {
        _ = timeoutSeconds;

        if (_kqFd < 0)
        {
            _kqFd = MacOSAPI.kqueue();
            if (_kqFd < 0)
                throw new Exception("kqueue() failed");
        }

        _pendingRequests = 0;

        if (_globalSslCtx == IntPtr.Zero && MacOSOpenSSLWrapper.SupportsHttps)
        {
            _globalSslCtx = MacOSOpenSSLWrapper.SSL_CTX_new(MacOSOpenSSLWrapper.TLS_client_method());
        }

        EnsureDispatchSource();
    }

    private static unsafe void EnsureDispatchSource()
    {
        if (_dispatchQueue == IntPtr.Zero)
        {
            _dispatchQueue = MacOSAPI.dispatch_get_global_queue(0, 0);
        }

        if (_dispatchSourceTypeRead == IntPtr.Zero)
        {
            IntPtr lib = MacOSAPI.OpenDispatchLibrary();
            if (lib == IntPtr.Zero)
                throw new Exception("Failed to open libdispatch");

            _dispatchSourceTypeRead = MacOSAPI.dlsym(lib, "_dispatch_source_type_read");
            if (_dispatchSourceTypeRead == IntPtr.Zero)
                _dispatchSourceTypeRead = MacOSAPI.dlsym(lib, "dispatch_source_type_read");

            if (_dispatchSourceTypeRead == IntPtr.Zero)
                throw new Exception("Failed to resolve dispatch source type read symbol");
        }

        if (_kqueueDispatchSource == IntPtr.Zero && _kqFd >= 0)
        {
            _kqueueDispatchSource = MacOSAPI.dispatch_source_create(_dispatchSourceTypeRead, (ulong)_kqFd, 0, _dispatchQueue);
            if (_kqueueDispatchSource == IntPtr.Zero)
                throw new Exception("Failed to create dispatch source for kqueue");

            MacOSAPI.dispatch_source_set_event_handler_f(_kqueueDispatchSource, &DispatchSourceKqueueHandler);
            MacOSAPI.dispatch_source_set_cancel_handler_f(_kqueueDispatchSource, &DispatchSourceCancelHandler);
            MacOSAPI.dispatch_resume(_kqueueDispatchSource);
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe void DispatchSourceKqueueHandler(void* context)
    {
        ProcessKqueueEvents();
    }

    [UnmanagedCallersOnly]
    private static unsafe void DispatchSourceCancelHandler(void* context)
    {
        // 取消时不需要专门操作，允许 Dispose 管理资源。
    }

    private static unsafe void ProcessKqueueEvents()
    {
        if (_kqFd < 0) return;

        KQueueEvent64* events = stackalloc KQueueEvent64[64];
        while (true)
        {
            int n = MacOSAPI.kevent64(_kqFd, null, 0, events, 64, 0u, null);
            if (n < 0)
            {
                int err = *MacOSAPI.__error();
                if (err == 4) continue; // EINTR
                break;
            }

            if (n == 0)
                break;

            if (n > 64) n = 64; // Prevent overflow

            for (int i = 0; i < n; i++)
            {
                ref KQueueEvent64 ev = ref events[i];
                AsyncRequestContext* ctx = (AsyncRequestContext*)ev.udata;
                if (ctx == null) continue;

                if ((ev.flags & KQueueLibcs.EV_ERROR) != 0)
                {
                    CompleteRequest(ctx);
                    continue;
                }

                switch (ctx->State)
                {
                    case AsyncHttpClientRequestState.Connecting:
                    case AsyncHttpClientRequestState.Handshaking:
                        HandleConnectAndHandshake(ctx);
                        break;
                    case AsyncHttpClientRequestState.Sending:
                        HandleWriteEvent(ctx);
                        break;
                    case AsyncHttpClientRequestState.Receiving:
                        HandleReadEvent(ctx);
                        break;
                }
            }
        }
    }

    public void RequestGETAsync(UnManagedCollection<char> url, UnManagedMemory<byte>* responseBuffer, delegate* unmanaged<void*, void> callback)
    {
        RequestInternalAsync(url, responseBuffer, HttpMethod.Get, null, default, callback);
    }

    public void RequestPOSTAsync(UnManagedCollection<char> url, UnManagedMemory<byte>* responseBuffer, UnManagedString* body, HttpContentType contentType, delegate* unmanaged<void*, void> callback)
    {
        RequestInternalAsync(url, responseBuffer, HttpMethod.Post, body, contentType, callback);
    }

    private static void RequestInternalAsync(UnManagedCollection<char> url, UnManagedMemory<byte>* responseBuffer, HttpMethod method, UnManagedString* body, HttpContentType contentType, delegate* unmanaged<void*, void> callback)
    {
        if (url.IsEmpty || url.InternalPointer == null || responseBuffer == null || callback == null) return;


        if (!HttpClientHelper.TryParseHttpUrl(url, out char* hostPtr, out int hostLen, out ushort port, out char* pathPtr, out int pathLen))
        {
            throw new ArgumentException("URL must be http://host[:port]/path");
        }

        if (!HttpClientHelper.ResolveHostMacOS(hostPtr, hostLen, out uint addr))
        {
            throw new Exception("hostname resolution failed");
        }


        AsyncRequestContext* ctx = (AsyncRequestContext*)NativeMemory.AllocZeroed((nuint)sizeof(AsyncRequestContext));



        ctx->HostPtr = hostPtr;
        ctx->HostLen = hostLen;
        ctx->Port = port;
        ctx->PathPtr = pathPtr;
        ctx->PathLen = pathLen;
        ctx->RemoteAddr = addr;
        ctx->ResponseBuffer = responseBuffer;
        ctx->Callback = (nint)callback;

        ctx->Method = method;
        ctx->Body = body;
        ctx->ContentType = contentType;
        ctx->BodyProcessedChars = 0;

        // HTTPS 检测
        ctx->IsHttps = url.StartsWith("https://");
        ctx->SslHandle = IntPtr.Zero;
        ctx->InternalRequestBuffer = null;

        ctx->State = AsyncHttpClientRequestState.Connecting; // 初始状态
        ctx->SocketFd = -1;

        Interlocked.Increment(ref _pendingRequests);

        StartConnect(ctx);
    }

    private static void StartConnect(AsyncRequestContext* ctx)
    {
        int sock = MacOSAPI.socket(MacOSHttpPosixApi.AF_INET, MacOSHttpPosixApi.SOCK_STREAM, 0);

        if (sock < 0)
        {
            CompleteRequest(ctx);
            return;
        }

        ctx->SocketFd = sock;

        int one = 1;
        MacOSAPI.setsockopt(sock, MacOSHttpPosixApi.SOL_SOCKET, KQueueLibcs.SO_NOSIGPIPE, &one, (uint)sizeof(int));
        MacOSAPI.ioctl(sock, (nuint)FIONBIO, &one);

        MacOSHttpPosixApi.sockaddr_in addr = default;
        addr.sin_len = (byte)sizeof(MacOSHttpPosixApi.sockaddr_in);
        addr.sin_family = MacOSHttpPosixApi.AF_INET;
        addr.sin_port = MacOSAPI.htons(ctx->Port);
        addr.sin_addr = ctx->RemoteAddr;

        int connectResult = MacOSAPI.connect(sock, &addr, (uint)sizeof(MacOSHttpPosixApi.sockaddr_in));

        if (connectResult == 0)
        {
            // 同步连接成功，直接注册写事件进入发送阶段
            RegisterEvent(sock, KQueueLibcs.EVFILT_WRITE, KQueueLibcs.EV_ADD | KQueueLibcs.EV_ONESHOT, ctx);
            return;
        }

        // 只有失败时才读 errno
        int err = *MacOSAPI.__error();
        if (err == KQueueLibcs.EAGAIN_MAC || err == 36 /* EINPROGRESS */)
        {
            // 连接正在进行中，注册写事件等待连接完成
            RegisterEvent(sock, KQueueLibcs.EVFILT_WRITE, KQueueLibcs.EV_ADD | KQueueLibcs.EV_ONESHOT, ctx);
            return;
        }

        CompleteRequest(ctx);
    }

    private static void HandleConnectAndHandshake(AsyncRequestContext* ctx)
    {
        if (ctx->State == AsyncHttpClientRequestState.Connecting)
        {
            int err = 0;
            MacOSHttpPosixApi.socklen_t len = sizeof(int);
            MacOSAPI.getsockopt(ctx->SocketFd, MacOSHttpPosixApi.SOL_SOCKET, MacOSHttpPosixApi.SO_ERROR, &err, &len);
            if (err != 0)
            {
                CompleteRequest(ctx);
                return;
            }

            if (ctx->IsHttps)
            {
                if (_globalSslCtx == IntPtr.Zero) { CompleteRequest(ctx); return; }
                ctx->SslHandle = MacOSOpenSSLWrapper.SSL_new(_globalSslCtx);
                MacOSOpenSSLWrapper.SSL_set_fd(ctx->SslHandle, ctx->SocketFd);
                ctx->State = AsyncHttpClientRequestState.Handshaking;
            }
            else
            {
                ctx->State = AsyncHttpClientRequestState.Sending;
                SendRequest(ctx);
                return;
            }
        }

        if (ctx->State == AsyncHttpClientRequestState.Handshaking)
        {
            int ret = MacOSOpenSSLWrapper.SSL_connect(ctx->SslHandle);
            if (ret == 1)
            {
                ctx->State = AsyncHttpClientRequestState.Sending;
                SendRequest(ctx);
            }
            else
            {
                int sslErr = MacOSOpenSSLWrapper.SSL_get_error(ctx->SslHandle, ret);
                if (sslErr == SSL_ERROR_WANT_READ)
                    RegisterEvent(ctx->SocketFd, KQueueLibcs.EVFILT_READ, KQueueLibcs.EV_ADD | KQueueLibcs.EV_ONESHOT, ctx);
                else if (sslErr == SSL_ERROR_WANT_WRITE)
                    RegisterEvent(ctx->SocketFd, KQueueLibcs.EVFILT_WRITE, KQueueLibcs.EV_ADD | KQueueLibcs.EV_ONESHOT, ctx);
                else
                    CompleteRequest(ctx);
            }
        }
    }

    private static void HandleWriteEvent(AsyncRequestContext* ctx)
    {
        SendRequest(ctx);
    }

    private static void SendRequest(AsyncRequestContext* ctx)
    {
        if (ctx->SocketFd < 0) { CompleteRequest(ctx); return; }

        const int maxLen = 8192; // 足够大的缓冲区容纳 Headers + Body
        if (ctx->InternalRequestBuffer == null)
            ctx->InternalRequestBuffer = (byte*)NativeMemory.Alloc(maxLen);

        byte* buffer = ctx->InternalRequestBuffer;
        int len = 0;
        int res;

        // 1. 写入请求行 (Method + Path + Protocol)
        res = HttpClientHelper.WriteAscii(buffer, len, maxLen, ctx->Method == HttpMethod.Post ? "POST " : "GET ");
        if (res < 0) { CompleteRequest(ctx); return; }
        len += res;

        res = HttpClientHelper.WriteAscii(buffer, len, maxLen, ctx->PathPtr, ctx->PathLen);
        if (res < 0) { CompleteRequest(ctx); return; }
        len += res;

        res = HttpClientHelper.WriteAscii(buffer, len, maxLen, " HTTP/1.1\r\nHost: ");
        if (res < 0) { CompleteRequest(ctx); return; }
        len += res;

        // 2. 写入 Host 头部
        res = HttpClientHelper.WriteAscii(buffer, len, maxLen, ctx->HostPtr, ctx->HostLen);
        if (res < 0) { CompleteRequest(ctx); return; }
        len += res;

        res = HttpClientHelper.WriteAscii(buffer, len, maxLen, "\r\nUser-Agent: SolamirareAsyncMacOSClient\r\nConnection: close\r\n");
        if (res < 0) { CompleteRequest(ctx); return; }
        len += res;

        // 3. 处理 POST 头部与 Body (一次性填充)
        if (ctx->Method == HttpMethod.Post && ctx->Body != null && !ctx->Body->IsEmpty)
        {
            res = HttpClientHelper.WriteAscii(buffer, len, maxLen, "Content-Type: ");
            if (res < 0) { CompleteRequest(ctx); return; }
            len += res;

            string cType = ctx->ContentType switch
            {
                HttpContentType.FormUrlEncoded => "application/x-www-form-urlencoded",
                HttpContentType.Json => "application/json",
                _ => "text/plain"
            };
            res = HttpClientHelper.WriteAscii(buffer, len, maxLen, cType);
            if (res < 0) { CompleteRequest(ctx); return; }
            len += res;

            res = HttpClientHelper.WriteAscii(buffer, len, maxLen, "; charset=UTF-8\r\nContent-Length: ");
            if (res < 0) { CompleteRequest(ctx); return; }
            len += res;

            int bodyBytesCount = System.Text.Encoding.UTF8.GetByteCount(ctx->Body->Pointer, (int)ctx->Body->UsageSize);
            len += AsciiConverter.IntToAscii(bodyBytesCount, buffer + len);

            res = HttpClientHelper.WriteAscii(buffer, len, maxLen, "\r\n\r\n");
            if (res < 0) { CompleteRequest(ctx); return; }
            len += res;

            // 直接在 Header 结束后追加 Body
            int bodyBytesEncoded = System.Text.Encoding.UTF8.GetBytes(ctx->Body->Pointer, (int)ctx->Body->UsageSize, buffer + len, maxLen - len);
            if (bodyBytesEncoded < 0) { CompleteRequest(ctx); return; }
            len += bodyBytesEncoded;
        }
        else
        {
            // 非 POST 请求或无 Body 的结束符
            res = HttpClientHelper.WriteAscii(buffer, len, maxLen, "\r\n");
            if (res < 0) { CompleteRequest(ctx); return; }
            len += res;
        }

        uint totalSendLen = (uint)len;

#if DEBUG
        // --- Debug: 发送前打印完整构造数据，显式展示 \r\n ---
        Console.WriteLine("[DEBUG] AsyncMacOSHttpClient: Constructing Request...");
        Console.WriteLine(System.Text.Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ctx->InternalRequestBuffer, (int)totalSendLen)).Replace("\r", "\\r").Replace("\n", "\\n"));
        Console.WriteLine("-------------------------------------------------------");
#endif

        nint sent;
        if (ctx->IsHttps)
            sent = MacOSOpenSSLWrapper.SSL_write(ctx->SslHandle, buffer, (UIntPtr)totalSendLen);
        else
            sent = (nint)MacOSAPI.send(ctx->SocketFd, buffer, (UIntPtr)totalSendLen, 0);

        if (sent < 0)
        {
            CompleteRequest(ctx);
            return;
        }

        ctx->State = AsyncHttpClientRequestState.Receiving;
        RegisterEvent(ctx->SocketFd, KQueueLibcs.EVFILT_READ, KQueueLibcs.EV_ADD | KQueueLibcs.EV_ONESHOT, ctx);
    }

    private static void HandleReadEvent(AsyncRequestContext* ctx)
    {
        if (ctx->SocketFd < 0) { CompleteRequest(ctx); return; }
        if (ctx->ResponseBuffer == null) { CompleteRequest(ctx); return; }

        byte* buffer = ctx->ResponseBuffer->Pointer + ctx->ReceivedBytes;
        uint capacity = ctx->ResponseBuffer->Capacity;
        if (ctx->ReceivedBytes >= capacity)
        {
            ctx->ResponseBuffer->ReLength(capacity);
            CompleteRequest(ctx);
            return;
        }
        uint remain = capacity - ctx->ReceivedBytes;




        int r;
        if (ctx->IsHttps)
            r = MacOSOpenSSLWrapper.SSL_read(ctx->SslHandle, buffer, (int)remain);
        else
            r = (int)MacOSAPI.recv(ctx->SocketFd, buffer, (UIntPtr)remain, 0);

        if (r > 0)
        {
            ctx->ReceivedBytes += (uint)r;
            ctx->ResponseBuffer->ReLength(ctx->ReceivedBytes);
            RegisterEvent(ctx->SocketFd, KQueueLibcs.EVFILT_READ, KQueueLibcs.EV_ADD | KQueueLibcs.EV_ONESHOT, ctx);
            return;
        }

        if (r == 0)
        {
            ctx->ResponseBuffer->ReLength(ctx->ReceivedBytes);
            CompleteRequest(ctx);
            return;
        }

        if (!ctx->IsHttps && *MacOSAPI.__error() == KQueueLibcs.EAGAIN_MAC)
        {
            RegisterEvent(ctx->SocketFd, KQueueLibcs.EVFILT_READ, KQueueLibcs.EV_ADD | KQueueLibcs.EV_ONESHOT, ctx);
            return;
        }
        if (ctx->IsHttps)
        {
            int sslErr = MacOSOpenSSLWrapper.SSL_get_error(ctx->SslHandle, r);
            if (sslErr == SSL_ERROR_WANT_READ)
            {
                RegisterEvent(ctx->SocketFd, KQueueLibcs.EVFILT_READ, KQueueLibcs.EV_ADD | KQueueLibcs.EV_ONESHOT, ctx);
                return;
            }
        }

        CompleteRequest(ctx);
    }

    private static void CompleteRequest(AsyncRequestContext* ctx)
    {
        if (ctx == null) return;

        ctx->State = AsyncHttpClientRequestState.Completed;

        if (ctx->Callback != 0)
            ((delegate* unmanaged<void*, void>)ctx->Callback)(ctx);

        if (ctx->IsHttps && ctx->SslHandle != IntPtr.Zero)
        {
            MacOSOpenSSLWrapper.SSL_shutdown(ctx->SslHandle);
            MacOSOpenSSLWrapper.SSL_free(ctx->SslHandle);
            ctx->SslHandle = IntPtr.Zero;
        }

        if (ctx->InternalRequestBuffer != null)
        {
            NativeMemory.Free(ctx->InternalRequestBuffer);
            ctx->InternalRequestBuffer = null;
        }

        if (ctx->SocketFd >= 0)
            MacOSAPI.close(ctx->SocketFd);

        Interlocked.Decrement(ref _pendingRequests);
        FreeContext(ctx);
    }

    public void Dispose()
    {
        if (_kqueueDispatchSource != IntPtr.Zero)
        {
            MacOSAPI.dispatch_source_cancel(_kqueueDispatchSource);
            MacOSAPI.dispatch_release(_kqueueDispatchSource);
            _kqueueDispatchSource = IntPtr.Zero;
        }

        if (_kqFd >= 0)
        {
            MacOSAPI.close(_kqFd);
            _kqFd = -1;
        }
    }

    private static void RegisterEvent(int fd, short filter, ushort flags, AsyncRequestContext* ctx)
    {
        KQueueEvent64 change = default;
        change.ident = (ulong)fd;
        change.filter = filter;
        change.flags = flags;
        change.udata = (ulong)ctx;
        MacOSAPI.kevent64(_kqFd, &change, 1, null, 0, 0u, null);
    }


    private static void FreeContext(AsyncRequestContext* ctx)
    {
        if (ctx != null)
            NativeMemory.Free(ctx);
    }
}
