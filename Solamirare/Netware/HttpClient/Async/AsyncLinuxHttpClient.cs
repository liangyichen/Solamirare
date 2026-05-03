namespace Solamirare;



/// <summary>
/// io_uring + Socket 异步 HTTP 客户端（零 GC、回调）。
/// <para>
/// 使用 io_uring 的 IORING_OP_CONNECT / IORING_OP_SEND / IORING_OP_RECV
/// 实现全异步网络操作，不阻塞调用线程。
/// </para>
/// </summary>
public unsafe struct AsyncLinuxHttpClient
{
    // ────────────────────────────────────────────────────────────────────────────
    //  io_uring 共享内存指针
    // ────────────────────────────────────────────────────────────────────────────

    private static int _ringFd = -1;

    private static io_uring_sqe* _sqes;
    private static uint* _sq_head;
    private static uint* _sq_tail;
    private static uint* _sq_mask;
    private static uint* _sq_array;
    private static io_uring_cqe* _cqes;
    private static uint* _cq_head;
    private static uint* _cq_tail;
    private static uint* _cq_mask;

    private static SpinLock _sqLock = new SpinLock(false);

    // ────────────────────────────────────────────────────────────────────────────
    //  后台收割线程
    // ────────────────────────────────────────────────────────────────────────────

    private static void* _reaperThreadHandle;


    private static int _pendingRequests;



    private static IntPtr _globalSslCtx = IntPtr.Zero;


    /// <summary>
    /// 初始化 io_uring 实例和后台收割线程。
    /// 整个进程只需调用一次。
    /// </summary>
    public void Initialize()
    {
        //守护条件，保证所有的实例都只能实际执行一次Initialize()方法
        if (_ringFd != -1) return;

        // 初始化 io_uring，请求 64 个槽位
        io_uring_params p = default;
        _ringFd = (int)LinuxAPI.syscall(
            IO_URingConsts.SYS_io_uring_setup, 64, (long)&p, 0, 0, 0);
        if (_ringFd < 0)
            throw new Exception("io_uring setup failed");

        // 映射提交队列共享内存
        nuint sqRingSize = p.sq_off.array + p.sq_entries * sizeof(uint);
        byte* sqPtr = (byte*)LinuxAPI.mmap(
            null, sqRingSize, 3, 1, _ringFd, IO_URingConsts.IORING_OFF_SQ_RING);
        _sq_head = (uint*)(sqPtr + p.sq_off.head);
        _sq_tail = (uint*)(sqPtr + p.sq_off.tail);
        _sq_mask = (uint*)(sqPtr + p.sq_off.ring_mask);
        _sq_array = (uint*)(sqPtr + p.sq_off.array);

        // 映射完成队列共享内存
        nuint cqRingSize = p.cq_off.cqes + p.cq_entries * (nuint)sizeof(io_uring_cqe);
        byte* cqPtr = (byte*)LinuxAPI.mmap(
            null, cqRingSize, 3, 1, _ringFd, IO_URingConsts.IORING_OFF_CQ_RING);
        _cq_head = (uint*)(cqPtr + p.cq_off.head);
        _cq_tail = (uint*)(cqPtr + p.cq_off.tail);
        _cq_mask = (uint*)(cqPtr + p.cq_off.ring_mask);
        _cqes = (io_uring_cqe*)(cqPtr + p.cq_off.cqes);

        // 映射 SQE 数组
        _sqes = (io_uring_sqe*)LinuxAPI.mmap(
            null, p.sq_entries * (nuint)sizeof(io_uring_sqe),
            3, 1, _ringFd, IO_URingConsts.IORING_OFF_SQES);

        _pendingRequests = 0;

        if (LinuxOpenSSLWrapper.SupportsHttps)
        {
            _globalSslCtx = LinuxOpenSSLWrapper.SSL_CTX_new(LinuxOpenSSLWrapper.TLS_client_method());
        }

        // 启动后台收割线程
        ThreadStartInfo info = new ThreadStartInfo();
        info.Worker = &ReaperLoop;
        info.Arg = null;
        NativeThread.Create(out _reaperThreadHandle, &info);

        Thread.Sleep(10);
    }


    /// <summary>
    /// 发起一个异步 GET 请求，立即返回不阻塞调用线程。
    /// 请求完成后在后台收割线程调用 <paramref name="callback"/>。
    /// </summary>
    /// <param name="url">请求 URL，格式：http://host[:port]/path。</param>
    /// <param name="responseBuffer">接收响应数据的缓冲区。</param>
    /// <param name="callback">请求完成后的回调，参数为 AsyncRequestContext*。</param>
    public void RequestGETAsync(
        UnManagedCollection<char> url,
        UnManagedMemory<byte>* responseBuffer,
        delegate* unmanaged<void*, void> callback)
    {
        RequestInternalAsync(url, responseBuffer, HttpMethod.Get, null, HttpContentType.PlainText, callback);
    }

    public void RequestPOSTAsync(
    UnManagedCollection<char> url,
    UnManagedMemory<byte>* responseBuffer,
    UnManagedString* body,
    HttpContentType contentType,
    delegate* unmanaged<void*, void> callback)
    {
        RequestInternalAsync(url,responseBuffer, HttpMethod.Post,body, contentType, callback);
    }

    private static void RequestInternalAsync(UnManagedCollection<char> url, UnManagedMemory<byte>* responseBuffer, HttpMethod method, UnManagedString* body, HttpContentType contentType, delegate* unmanaged<void*, void> callback)
    {
        if (url.IsEmpty || url.InternalPointer == null || responseBuffer == null || callback == null)
            return;

        if (!HttpClientHelper.TryParseHttpUrl(url,
            out char* hostPtr, out int hostLen,
            out ushort port,
            out char* pathPtr, out int pathLen))
            return;

        if (!HttpClientHelper.ResolveHostLinux(hostPtr, hostLen, out uint addr))
            return;

        // 分配请求上下文
        IO_URingRequestContext* ctx = (IO_URingRequestContext*)NativeMemory.AllocZeroed((nuint)sizeof(IO_URingRequestContext));
        ctx->AsyncRequestContext.HostPtr = hostPtr;
        ctx->AsyncRequestContext.HostLen = hostLen;
        ctx->AsyncRequestContext.Port = port;
        ctx->AsyncRequestContext.PathPtr = pathPtr;
        ctx->AsyncRequestContext.PathLen = pathLen;
        ctx->AsyncRequestContext.RemoteAddr = addr;
        ctx->AsyncRequestContext.ResponseBuffer = responseBuffer;
        ctx->AsyncRequestContext.Callback = (nint)callback;

        ctx->AsyncRequestContext.IsHttps = url.StartsWith("https://");
        ctx->AsyncRequestContext.SslHandle = IntPtr.Zero;
        ctx->AsyncRequestContext.Method = HttpMethod.Post;
        ctx->AsyncRequestContext.Body = body;
        ctx->AsyncRequestContext.ContentType = contentType;
        ctx->AsyncRequestContext.State = AsyncHttpClientRequestState.Connecting;
        ctx->AsyncRequestContext.SocketFd = -1;
        ctx->AsyncRequestContext.ReceivedBytes = 0;
        ctx->SendLen = 0;

        Interlocked.Increment(ref _pendingRequests);

        StartConnect(ctx);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  连接阶段
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 创建非阻塞套接字，填充 sockaddr_in，提交 IORING_OP_CONNECT。
    /// </summary>
    private static void StartConnect(IO_URingRequestContext* ctx)
    {
        // AF_INET=2, SOCK_STREAM=1
        int sock = LinuxAPI.socket(2, 1, 0);
        if (sock < 0) { CompleteRequest(ctx); return; }

        ctx->AsyncRequestContext.SocketFd = sock;

        // 填充地址结构体（存在 ctx 里，生命周期覆盖整个异步操作）
        ctx->Addr.sin_family = 2;  // AF_INET
        ctx->Addr.sin_port = LinuxAPI.htons(ctx->AsyncRequestContext.Port);
        ctx->Addr.sin_addr = ctx->AsyncRequestContext.RemoteAddr;

        ctx->AsyncRequestContext.State = AsyncHttpClientRequestState.Connecting;

        // 提交 IORING_OP_CONNECT
        SubmitSQE_Connect(sock, &ctx->Addr, (ulong)ctx);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  发送阶段
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 构造 HTTP GET 请求报文，存入 ctx->SendBuf，提交 IORING_OP_SEND。
    /// </summary>
    private static void StartSend(IO_URingRequestContext* ctx)
    {
        if (ctx->AsyncRequestContext.SocketFd < 0) { CompleteRequest(ctx); return; }

        const int maxLen = 8192;
        if (ctx->AsyncRequestContext.InternalRequestBuffer == null)
            ctx->AsyncRequestContext.InternalRequestBuffer = (byte*)NativeMemory.Alloc(maxLen);

        byte* buf = ctx->AsyncRequestContext.InternalRequestBuffer;
        int len = 0;
        int res;

        // 1. 写入请求行 (使用 HttpClientHelper 避开 1024 限制)
        res = HttpClientHelper.WriteAscii(buf, len, maxLen, ctx->AsyncRequestContext.Method == HttpMethod.Post ? "POST " : "GET ");
        if (res < 0) { CompleteRequest(ctx); return; }
        len += res;

        res = HttpClientHelper.WriteAscii(buf, len, maxLen, ctx->AsyncRequestContext.PathPtr, ctx->AsyncRequestContext.PathLen);
        if (res < 0) { CompleteRequest(ctx); return; }
        len += res;

        res = HttpClientHelper.WriteAscii(buf, len, maxLen, " HTTP/1.1\r\nHost: ");
        if (res < 0) { CompleteRequest(ctx); return; }
        len += res;

        // 2. 写入 Host 头部
        res = HttpClientHelper.WriteAscii(buf, len, maxLen, ctx->AsyncRequestContext.HostPtr, ctx->AsyncRequestContext.HostLen);
        if (res < 0) { CompleteRequest(ctx); return; }
        len += res;

        res = HttpClientHelper.WriteAscii(buf, len, maxLen, "\r\nUser-Agent: SolamirareAsyncLinuxClient\r\nConnection: close\r\n");
        if (res < 0) { CompleteRequest(ctx); return; }
        len += res;

        // 3. 处理 POST 专有头部
        if (ctx->AsyncRequestContext.Method == HttpMethod.Post && ctx->AsyncRequestContext.Body != null && !ctx->AsyncRequestContext.Body->IsEmpty)
        {
            res = HttpClientHelper.WriteAscii(buf, len, maxLen, "Content-Type: ");
            if (res < 0) { CompleteRequest(ctx); return; }
            len += res;

            string cType = ctx->AsyncRequestContext.ContentType switch
            {
                HttpContentType.FormUrlEncoded => "application/x-www-form-urlencoded",
                HttpContentType.Json => "application/json",
                _ => "text/plain"
            };
            res = HttpClientHelper.WriteAscii(buf, len, maxLen, cType);
            if (res < 0) { CompleteRequest(ctx); return; }
            len += res;

            res = HttpClientHelper.WriteAscii(buf, len, maxLen, "; charset=UTF-8\r\nContent-Length: ");
            if (res < 0) { CompleteRequest(ctx); return; }
            len += res;

            int bodyBytesCount = System.Text.Encoding.UTF8.GetByteCount(ctx->AsyncRequestContext.Body->Pointer, (int)ctx->AsyncRequestContext.Body->UsageSize);
            len += AsciiConverter.IntToAscii(bodyBytesCount, buf + len);
            res = HttpClientHelper.WriteAscii(buf, len, maxLen, "\r\n\r\n");
            if (res < 0) { CompleteRequest(ctx); return; }
            len += res;

            int bodyBytesEncoded = System.Text.Encoding.UTF8.GetBytes(ctx->AsyncRequestContext.Body->Pointer, (int)ctx->AsyncRequestContext.Body->UsageSize, buf + len, maxLen - len);
            if (bodyBytesEncoded < 0) { CompleteRequest(ctx); return; }
            len += bodyBytesEncoded;
        }
        else
        {
            res = HttpClientHelper.WriteAscii(buf, len, maxLen, "\r\n");
            if (res < 0) { CompleteRequest(ctx); return; }
            len += res;
        }

        uint totalSendLen = (uint)len;
        ctx->SendLen = (int)totalSendLen;


#if DEBUG
        // --- Debug: 发送前打印完整构造数据 ---
        Console.WriteLine("[DEBUG] AsyncLinuxHttpClient: Constructing Request...");
        Console.WriteLine(System.Text.Encoding.UTF8.GetString(new ReadOnlySpan<byte>(buf, (int)totalSendLen)).Replace("\r", "\\r").Replace("\n", "\\n"));
        Console.WriteLine("-------------------------------------------------------");
#endif

        if (ctx->AsyncRequestContext.IsHttps)
        {
            ctx->AsyncRequestContext.State = AsyncHttpClientRequestState.Sending;
            ProcessSslSend(ctx);
            return;
        }

        ctx->AsyncRequestContext.State = AsyncHttpClientRequestState.Sending;
        SubmitSQE_Send(ctx->AsyncRequestContext.SocketFd, buf, totalSendLen, (ulong)ctx);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  接收阶段
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 提交 IORING_OP_RECV，将数据直接写入 responseBuffer。
    /// </summary>
    private static void StartRecv(IO_URingRequestContext* ctx)
    {
        if (ctx->AsyncRequestContext.SocketFd < 0 || ctx->AsyncRequestContext.ResponseBuffer == null)
        {
            CompleteRequest(ctx);
            return;
        }

        uint capacity = ctx->AsyncRequestContext.ResponseBuffer->Capacity;
        uint received = ctx->AsyncRequestContext.ReceivedBytes;

        if (received >= capacity)
        {
            ctx->AsyncRequestContext.ResponseBuffer->ReLength(capacity);
            CompleteRequest(ctx);
            return;
        }

        byte* buf = ctx->AsyncRequestContext.ResponseBuffer->Pointer + received;
        uint size = capacity - received;

        if (ctx->AsyncRequestContext.IsHttps)
        {
            ctx->AsyncRequestContext.State = AsyncHttpClientRequestState.Receiving;
            ProcessSslRecv(ctx);
            return;
        }

        ctx->AsyncRequestContext.State = AsyncHttpClientRequestState.Receiving;

        SubmitSQE_Recv(ctx->AsyncRequestContext.SocketFd, buf, size, (ulong)ctx);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  完成处理
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 关闭套接字，调用回调，释放上下文。
    /// </summary>
    private static void CompleteRequest(IO_URingRequestContext* ctx)
    {
        if (ctx == null) return;

        ctx->AsyncRequestContext.State = AsyncHttpClientRequestState.Completed;

        if (ctx->AsyncRequestContext.Callback != 0)
            ((delegate* unmanaged<void*, void>)ctx->AsyncRequestContext.Callback)(ctx);

        if (ctx->AsyncRequestContext.IsHttps && ctx->AsyncRequestContext.SslHandle != IntPtr.Zero)
        {
            LinuxOpenSSLWrapper.SSL_shutdown(ctx->AsyncRequestContext.SslHandle);
            LinuxOpenSSLWrapper.SSL_free(ctx->AsyncRequestContext.SslHandle);
            ctx->AsyncRequestContext.SslHandle = IntPtr.Zero;
        }

        if (ctx->AsyncRequestContext.InternalRequestBuffer != null)
        {
            NativeMemory.Free(ctx->AsyncRequestContext.InternalRequestBuffer);
            ctx->AsyncRequestContext.InternalRequestBuffer = null;
        }

        if (ctx->AsyncRequestContext.SocketFd >= 0)
        {
            LinuxAPI.close(ctx->AsyncRequestContext.SocketFd);
            ctx->AsyncRequestContext.SocketFd = -1;
        }

        Interlocked.Decrement(ref _pendingRequests);
        FreeContext(ctx);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  后台收割线程
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 后台收割线程主循环。
    /// 阻塞等待 io_uring 完成事件，根据上下文状态推进请求状态机。
    /// 阻塞的是后台线程，调用线程完全不受影响。
    /// </summary>
    [UnmanagedCallersOnly]
    private static uint ReaperLoop(void* arg)
    {
        while (true)
        {
            uint head = Volatile.Read(ref *_cq_head);
            uint tail = Volatile.Read(ref *_cq_tail);

            if (head == tail)
            {
                // 队列为空，进入内核阻塞等待
                int res = (int)LinuxAPI.syscall(
                    IO_URingConsts.SYS_io_uring_enter,
                    _ringFd, 0, 1,
                    IO_URingConsts.IORING_ENTER_GETEVENTS, 0);

                if (res < 0)
                {
                    // 处理中断或错误
                    Thread.Sleep(1);
                }
                continue;
            }

            uint mask = *_cq_mask;

            // 限制单次收割数量，防止极端情况下后台线程饿死其他任务
            int quota = 64;
            while (head != tail && quota-- > 0)
            {
                io_uring_cqe* cqe = &_cqes[head & mask];
                IO_URingRequestContext* ctx = (IO_URingRequestContext*)cqe->user_data;

                if (ctx != null)
                    HandleCompletion(ctx, cqe->res); //这里传入-1,导致立即就返回了

                head++;
                Volatile.Write(ref *_cq_head, head);
            }
        }

        return 0;
    }

    /// <summary>
    /// 根据当前请求状态和 io_uring 操作结果推进状态机。
    /// </summary>
    private static void HandleCompletion(IO_URingRequestContext* ctx, int result)
    {
        switch (ctx->AsyncRequestContext.State)
        {
            case AsyncHttpClientRequestState.Connecting:
                // result == 0 表示连接成功，负数表示失败
                if (result < 0)
                {
                    CompleteRequest(ctx);
                    return;
                }
                if (ctx->AsyncRequestContext.IsHttps)
                {
                    ctx->AsyncRequestContext.SslHandle = LinuxOpenSSLWrapper.SSL_new(_globalSslCtx);
                    LinuxOpenSSLWrapper.SSL_set_fd(ctx->AsyncRequestContext.SslHandle, ctx->AsyncRequestContext.SocketFd);
                    ctx->AsyncRequestContext.State = AsyncHttpClientRequestState.Handshaking;
                    ProcessSslHandshake(ctx);
                }
                else
                {
                    StartSend(ctx);
                }
                break;

            case AsyncHttpClientRequestState.Sending:
                if (result <= 0)
                {
                    CompleteRequest(ctx);
                    return;
                }
                // 检查是否发生短写 (Short Write)
                if (result < ctx->SendLen)
                {
                    // 逻辑警告：此处简化处理，但在生产环境应支持偏移重发
                    CompleteRequest(ctx);
                    return;
                }
                StartRecv(ctx);
                break;

            case AsyncHttpClientRequestState.Handshaking:
            case AsyncHttpClientRequestState.WaitingForPoll:
                HandlePollCompletion(ctx, result);
                break;

            case AsyncHttpClientRequestState.Receiving:
                // result == 0 表示对端关闭连接，负数表示失败
                if (result < 0)
                {
                    CompleteRequest(ctx);
                    return;
                }

                if (result == 0)
                {
                    // 对端关闭，接收完成
                    ctx->AsyncRequestContext.ResponseBuffer->ReLength(ctx->AsyncRequestContext.ReceivedBytes);
                    CompleteRequest(ctx);
                    return;
                }

                // 累积已接收字节数，继续接收
                ctx->AsyncRequestContext.ReceivedBytes += (uint)result;
                ctx->AsyncRequestContext.ResponseBuffer->ReLength(ctx->AsyncRequestContext.ReceivedBytes);

                // 主动识别协议结束：解析 Content-Length 并判断是否收齐
                byte* ptr = ctx->AsyncRequestContext.ResponseBuffer->Pointer;
                long headerEndPos = GenericHttpClientFunctions.FindHeaderEndPosition(ptr, ctx->AsyncRequestContext.ReceivedBytes);
                if (headerEndPos != -1)
                {
                    long contentLength = GenericHttpClientFunctions.ExtractContentLength(ptr, headerEndPos);
                    if (contentLength >= 0 && ctx->AsyncRequestContext.ReceivedBytes >= (headerEndPos + contentLength))
                    {
                        CompleteRequest(ctx);
                        return;
                    }
                }

                StartRecv(ctx);
                break;

            default:
                CompleteRequest(ctx);
                break;
        }
    }

    private static void ProcessSslHandshake(IO_URingRequestContext* ctx)
    {
        int ret = LinuxOpenSSLWrapper.SSL_connect(ctx->AsyncRequestContext.SslHandle);
        if (ret == 1)
        {
            StartSend(ctx);
            return;
        }
        int err = LinuxOpenSSLWrapper.SSL_get_error(ctx->AsyncRequestContext.SslHandle, ret);
        if (err == IO_URingConsts.SSL_ERROR_WANT_READ)
        {
            ctx->PostPollState = AsyncHttpClientRequestState.Handshaking;
            SubmitSQE_Poll(ctx->AsyncRequestContext.SocketFd, IO_URingConsts.POLLIN, (ulong)ctx);
        }
        else if (err == IO_URingConsts.SSL_ERROR_WANT_WRITE)
        {
            ctx->PostPollState = AsyncHttpClientRequestState.Handshaking;
            SubmitSQE_Poll(ctx->AsyncRequestContext.SocketFd, IO_URingConsts.POLLOUT, (ulong)ctx);
        }
        else { CompleteRequest(ctx); }
    }

    private static void ProcessSslSend(IO_URingRequestContext* ctx)
    {
        int ret = LinuxOpenSSLWrapper.SSL_write(ctx->AsyncRequestContext.SslHandle, ctx->AsyncRequestContext.InternalRequestBuffer, (UIntPtr)ctx->SendLen);
        if (ret > 0)
        {
            StartRecv(ctx);
        }
        else
        {
            int err = LinuxOpenSSLWrapper.SSL_get_error(ctx->AsyncRequestContext.SslHandle, ret);
            if (err == IO_URingConsts.SSL_ERROR_WANT_WRITE)
            {
                ctx->PostPollState = AsyncHttpClientRequestState.Sending;
                SubmitSQE_Poll(ctx->AsyncRequestContext.SocketFd, IO_URingConsts.POLLOUT, (ulong)ctx);
            }
            else { CompleteRequest(ctx); }
        }
    }

    private static void ProcessSslRecv(IO_URingRequestContext* ctx)
    {
        uint remain = ctx->AsyncRequestContext.ResponseBuffer->Capacity - ctx->AsyncRequestContext.ReceivedBytes;
        if (remain == 0) { CompleteRequest(ctx); return; }

        int ret = LinuxOpenSSLWrapper.SSL_read(ctx->AsyncRequestContext.SslHandle, ctx->AsyncRequestContext.ResponseBuffer->Pointer + ctx->AsyncRequestContext.ReceivedBytes, (int)remain);
        if (ret > 0)
        {
            ctx->AsyncRequestContext.ReceivedBytes += (uint)ret;
            ctx->AsyncRequestContext.ResponseBuffer->ReLength(ctx->AsyncRequestContext.ReceivedBytes);

            // 主动识别协议结束：解析 Content-Length 并判断是否收齐
            byte* ptr = ctx->AsyncRequestContext.ResponseBuffer->Pointer;
            long headerEndPos = GenericHttpClientFunctions.FindHeaderEndPosition(ptr, ctx->AsyncRequestContext.ReceivedBytes);
            if (headerEndPos != -1)
            {
                long contentLength = GenericHttpClientFunctions.ExtractContentLength(ptr, headerEndPos);
                if (contentLength >= 0 && ctx->AsyncRequestContext.ReceivedBytes >= (headerEndPos + contentLength))
                {
                    CompleteRequest(ctx);
                    return;
                }
            }

            ProcessSslRecv(ctx); // 递归尝试读取更多
        }
        else if (ret == 0)
        {
            CompleteRequest(ctx);
        }
        else
        {
            int err = LinuxOpenSSLWrapper.SSL_get_error(ctx->AsyncRequestContext.SslHandle, ret);
            if (err == IO_URingConsts.SSL_ERROR_WANT_READ)
            {
                ctx->PostPollState = AsyncHttpClientRequestState.Receiving;
                SubmitSQE_Poll(ctx->AsyncRequestContext.SocketFd, IO_URingConsts.POLLIN, (ulong)ctx);
            }
            else { CompleteRequest(ctx); }
        }
    }

    private static void HandlePollCompletion(IO_URingRequestContext* ctx, int result)
    {
        if (result < 0) { CompleteRequest(ctx); return; }

        switch (ctx->PostPollState)
        {
            case AsyncHttpClientRequestState.Handshaking:
                ProcessSslHandshake(ctx);
                break;
            case AsyncHttpClientRequestState.Sending:
                ProcessSslSend(ctx);
                break;
            case AsyncHttpClientRequestState.Receiving:
                ProcessSslRecv(ctx);
                break;
        }
    }

    private static void SubmitSQE_Poll(int fd, uint events, ulong udata)
    {
        bool lockTaken = false;
        try
        {
            _sqLock.Enter(ref lockTaken);
            io_uring_sqe* sqe = AcquireSQE();
            if (sqe == null) return;
            System.Runtime.CompilerServices.Unsafe.InitBlock(sqe, 0, (uint)sizeof(io_uring_sqe));
            sqe->opcode = IO_URingConsts.IORING_OP_POLL_ADD;
            sqe->fd = fd;
            sqe->len = events; // poll_events goes here
            sqe->user_data = udata;
            FlushSQE();
        }
        finally
        {
            if (lockTaken) _sqLock.Exit();
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  SQE 提交辅助方法
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 提交 IORING_OP_CONNECT 到 io_uring 提交队列。
    /// </summary>
    private static void SubmitSQE_Connect(int fd, sockaddr_in* addr, ulong udata)
    {
        bool lockTaken = false;
        try
        {
            _sqLock.Enter(ref lockTaken);
            io_uring_sqe* sqe = AcquireSQE();
            if (sqe == null) return;

            System.Runtime.CompilerServices.Unsafe.InitBlock(sqe, 0, (uint)sizeof(io_uring_sqe));
            sqe->opcode = IO_URingConsts.IORING_OP_CONNECT;
            sqe->fd = fd;
            sqe->addr = (ulong)addr;
            sqe->off = (ulong)sizeof(sockaddr_in); // CONNECT 必须使用 off 传递长度
            sqe->user_data = udata;

            FlushSQE();
        }
        finally
        {
            if (lockTaken) _sqLock.Exit();
        }
    }

    /// <summary>
    /// 提交 IORING_OP_SEND 到 io_uring 提交队列。
    /// </summary>
    private static void SubmitSQE_Send(int fd, byte* buf, uint len, ulong udata)
    {
        bool lockTaken = false;
        try
        {
            _sqLock.Enter(ref lockTaken);
            io_uring_sqe* sqe = AcquireSQE();
            if (sqe == null) return;

            System.Runtime.CompilerServices.Unsafe.InitBlock(sqe, 0, (uint)sizeof(io_uring_sqe));
            sqe->opcode = IO_URingConsts.IORING_OP_SEND;
            sqe->fd = fd;
            sqe->addr = (ulong)buf;
            sqe->len = len;
            sqe->user_data = udata;

            FlushSQE();
        }
        finally
        {
            if (lockTaken) _sqLock.Exit();
        }
    }

    /// <summary>
    /// 提交 IORING_OP_RECV 到 io_uring 提交队列。
    /// </summary>
    private static void SubmitSQE_Recv(int fd, byte* buf, uint len, ulong udata)
    {
        bool lockTaken = false;
        try
        {
            _sqLock.Enter(ref lockTaken);
            io_uring_sqe* sqe = AcquireSQE();
            if (sqe == null) return;

            System.Runtime.CompilerServices.Unsafe.InitBlock(sqe, 0, (uint)sizeof(io_uring_sqe));
            sqe->opcode = IO_URingConsts.IORING_OP_RECV;
            sqe->fd = fd;
            sqe->addr = (ulong)buf;
            sqe->len = len;
            sqe->user_data = udata;

            FlushSQE();
        }
        finally
        {
            if (lockTaken) _sqLock.Exit();
        }
    }

    /// <summary>
    /// 从提交队列取一个空闲 SQE 槽位。
    /// </summary>
    private static io_uring_sqe* AcquireSQE()
    {
        uint tail = *_sq_tail;
        uint index = tail & (*_sq_mask);
        _sq_array[index] = index;
        return &_sqes[index];
    }

    /// <summary>
    /// 推进提交队列尾部并通知内核。
    /// </summary>
    private static void FlushSQE()
    {
        Interlocked.MemoryBarrier();
        *_sq_tail += 1;
        Interlocked.MemoryBarrier();

        LinuxAPI.syscall(
            IO_URingConsts.SYS_io_uring_enter,
            _ringFd, 1, 0, 0, 0);
    }




    /// <summary>
    /// 释放请求上下文。
    /// </summary>
    private static void FreeContext(IO_URingRequestContext* ctx)
    {
        if (ctx != null)
            NativeMemory.AlignedFree(ctx);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  释放
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 关闭 io_uring 实例，释放资源。
    /// </summary>
    public void Dispose()
    {
        if (_ringFd >= 0)
        {
            LinuxAPI.close(_ringFd);
            _ringFd = -1;
        }
    }
}