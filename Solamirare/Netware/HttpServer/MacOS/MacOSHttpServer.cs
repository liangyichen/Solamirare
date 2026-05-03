
using System.Runtime.CompilerServices;

namespace Solamirare;

/// <summary>
/// 基于 macOS kqueue 机制的零 GC 单线程 HTTP 服务器。
/// 所有 I/O 均在单一事件循环线程内完成，无锁、无线程切换开销
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public unsafe ref struct MacOSHttpServer
{
    // ──────────────────────────────────────────────────────────────────────────
    // 字段 / Fields
    // ──────────────────────────────────────────────────────────────────────────


    /// <summary>
    /// 指向所有连接响应缓冲区数组的起始指针，每个连接占 RESPONSE_BUFFER_CAPACITY 字节。
    /// </summary>
    private byte* _responseBuffers;

    byte* _requestBuffers;

    HTTPSeverConfig* ServerConfig;

    UHttpContext* _fdContexts;

    /// <summary>
    /// 用户提供的业务逻辑函数指针，在 HTTP 请求解析完成后调用。
    /// 返回 true 表示继续发送响应，返回 false 表示立即关闭连接。
    /// <para>
    /// </para>
    /// </summary>
    public delegate*<UHttpContext*, bool> userLogic;

    /// <summary>
    /// kqueue 实例的文件描述符。初始化为 -1 表示"未创建"。
    /// </summary>
    private int _kQueueFd;

    /// <summary>
    /// TCP 监听套接字的文件描述符。初始化为 -1 表示"未创建"。
    /// </summary>
    private int _listenFd;

    /// <summary>停止标志。</summary>
    private volatile bool _stopping;

    // Dispose 幂等保护 [G6]
    private bool _disposed;


    //是否可以释放当前对象了，主要用于解决在事件循环中调用 Stop() 后，主线程可能正在访问对象的字段导致的访问冲突问题
    bool disposeReafy;

    // ──────────────────────────────────────────────────────────────────────────
    // 私有常量 / Private Constants
    // ──────────────────────────────────────────────────────────────────────────

    private const ushort EV_ADD_ENABLE = KQueueLibcs.EV_ADD | 0x0004; // EV_ADD | EV_ENABLE

    private const ulong FIONBIO = 0x8004667E;

    /// <summary>
    /// 是否可以安全地释放当前对象了，主要用于解决在事件循环中调用 Stop() 后，主线程可能正在访问对象的字段导致的访问冲突问题。
    /// </summary>
    public bool DisposeReady => disposeReafy;


    // ──────────────────────────────────────────────────────────────────────────
    // 构造函数 / Constructor
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 初始化服务器：校验配置、一次性分配所有运行时内存、创建监听套接字。
    /// </summary>
    public MacOSHttpServer(HTTPSeverConfig* config)
    {
        // 初始化为 -1，Dispose() 检查 >= 0 才关闭，避免误关 fd=0（标准输入）
        _kQueueFd = -1;
        _listenFd = -1;

        ServerConfig = config;

        // 只有第一个实例才创建 listenFd
        bool isAcceptThread = (ServerConfig->Instances.UsageSize == 0);

        if (!isAcceptThread)
        {
            // worker线程：直接返回（不创建listenFd）
            return;
        }

        // ServerConfig 参数合法性校验
        if (config == null)
            Fail("ServerConfig is null");
        if (config->MAX_CONNECTIONS <= 0)
            Fail($"MAX_CONNECTIONS must be > 0, got {config->MAX_CONNECTIONS}");
        if (config->READ_BUFFER_CAPACITY <= 0)
            Fail($"READ_BUFFER_CAPACITY must be > 0, got {config->READ_BUFFER_CAPACITY}");
        if (config->RESPONSE_BUFFER_CAPACITY <= 0)
            Fail($"RESPONSE_BUFFER_CAPACITY must be > 0, got {config->RESPONSE_BUFFER_CAPACITY}");
        if (config->Port is <= 0 or > 65535)
            Fail($"Port must be 1-65535, got {config->Port}");



        uint respBufsSize = (uint)ServerConfig->MAX_CONNECTIONS * (uint)ServerConfig->RESPONSE_BUFFER_CAPACITY;

        _responseBuffers = (byte*)NativeMemory.AlignedAlloc(respBufsSize, SolamirareEnvironment.ALIGNMENT);
        NativeMemory.Clear(_responseBuffers, respBufsSize); //必须做，防止脏数据影响逻辑



        uint requestBufferSize = (uint)ServerConfig->MAX_CONNECTIONS * (uint)ServerConfig->READ_BUFFER_CAPACITY;

        _requestBuffers = (byte*)NativeMemory.AlignedAlloc(requestBufferSize, SolamirareEnvironment.ALIGNMENT);
        NativeMemory.Clear(_requestBuffers, requestBufferSize);

        uint contextsSize = (uint)(sizeof(UHttpContext) * ServerConfig->MAX_CONNECTIONS);
        _fdContexts = (UHttpContext*)NativeMemory.AlignedAlloc(contextsSize, SolamirareEnvironment.ALIGNMENT);
        NativeMemory.Clear(_fdContexts, contextsSize);



        // 创建 TCP 监听套接字
        _listenFd = MacOSAPI.socket(KQueueLibcs.AF_INET, KQueueLibcs.SOCK_STREAM, 0);
        if (_listenFd < 0) Fail("socket");

        int opt = 1;
        MacOSAPI.setsockopt(_listenFd, 0xFFFF, KQueueLibcs.SO_NOSIGPIPE, &opt, sizeof(int));


        // ── SO_REUSEADDR + SO_REUSEPORT，必须在 bind() 之前设置 ──────────
        // SO_REUSEADDR：允许服务器重启时立即重新绑定同一端口，避免 TIME_WAIT 导致的 bind 失败
        // SO_REUSEPORT：允许多个进程/线程各自独立 bind+listen 同一端口，
        //               内核将新连接均衡分发到各个监听队列，每个线程独占自己的 accept 队列，无竞争
        int reuse = 1;
        MacOSAPI.setsockopt(_listenFd, 0xFFFF, KQueueLibcs.SO_REUSEADDR, &reuse, sizeof(int));
        MacOSAPI.setsockopt(_listenFd, 0xFFFF, KQueueLibcs.SO_REUSEPORT, &reuse, sizeof(int));
        // ─────────────────────────────────────────────────────────────────────────



        int nbOne = 1;
        MacOSAPI.ioctl(_listenFd, (nuint)FIONBIO, &nbOne);

        sockaddr_bsd addr = new sockaddr_bsd
        {
            sin_len = 16,
            sin_family = KQueueLibcs.AF_INET,
            sin_port = ServerFunctions.Htons(config->Port),
            sin_addr = 0x00000000
        };

        if (MacOSAPI.bind(_listenFd, &addr, 16) < 0) Fail("bind");
        if (MacOSAPI.listen(_listenFd, 1024) < 0) Fail("listen");

    }

    // ──────────────────────────────────────────────────────────────────────────
    // kqueue 封装辅助方法 / kqueue Wrapper Helpers
    // ──────────────────────────────────────────────────────────────────────────

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static KQueueEvent64 CreateKEvent(int fd, short filter, ushort flags)
        => new KQueueEvent64 { ident = (ulong)fd, filter = filter, flags = flags };

    static void MuxRegisterRead(int kqFd, int fd)
    {
        KQueueEvent64 change = CreateKEvent(fd, KQueueLibcs.EVFILT_READ, EV_ADD_ENABLE);
        MacOSAPI.kevent64(kqFd, &change, 1, null, 0, 0u, null);
    }

    static void MuxRegisterWrite(int kqFd, int fd)
    {
        ushort flags = KQueueLibcs.EV_ADD | KQueueLibcs.EV_ONESHOT;
        KQueueEvent64 change = CreateKEvent(fd, KQueueLibcs.EVFILT_WRITE, flags);
        MacOSAPI.kevent64(kqFd, &change, 1, null, 0, 0u, null);
    }

    static KQueueEvent64 GetDeleteReadEvent(int fd)
        => CreateKEvent(fd, KQueueLibcs.EVFILT_READ, KQueueLibcs.EV_DELETE);

    static KQueueEvent64 GetAddOneshotWriteEvent(int fd)
        => CreateKEvent(fd, KQueueLibcs.EVFILT_WRITE, KQueueLibcs.EV_ADD | KQueueLibcs.EV_ONESHOT);

    // ──────────────────────────────────────────────────────────────────────────
    // 启动 / Start
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 启动服务器：创建 kqueue 实例，注册监听套接字，进入阻塞式事件循环。
    /// 不返回，直到 Stop() 将 _stopping 置为 true。
    /// </summary>
    public void Start(delegate*<UHttpContext*, bool> userLogic)
    {
        this.userLogic = userLogic;

        _kQueueFd = MacOSAPI.kqueue();
        if (_kQueueFd < 0) Fail("kqueue");

        MuxRegisterRead(_kQueueFd, _listenFd);


        ServerFunctions.ConsoleStartedStatus("KQueue HTTP Server", ServerConfig);



        EventLoop(_fdContexts);
    }


    // ──────────────────────────────────────────────────────────────────────────
    // 事件循环 / Event Loop
    // ──────────────────────────────────────────────────────────────────────────


    private void EventLoop(UHttpContext* _fdContexts)
    {
        KQueueEvent64* events = stackalloc KQueueEvent64[64];


        disposeReafy = false;

        while (!_stopping)
        {
            int numEvents = MacOSAPI.kevent64(_kQueueFd, null, 0, events, 64, 0u, null);

            if (numEvents < 0)
            {
                if (*MacOSAPI.__error() == 4) continue; // EINTR，重试
                break;
            }

            if (numEvents == 0) continue;


            for (int i = 0; i < numEvents; i++)
            {
                KQueueEvent64 currentEvent = events[i];
                int fd = (int)currentEvent.ident;

                if ((currentEvent.flags & KQueueLibcs.EV_ERROR) != 0)
                {
                    CleanupConnection(_fdContexts, fd);
                    continue;
                }

                if (fd == _listenFd)
                {
                    if ((currentEvent.flags & KQueueLibcs.EV_EOF) != 0)
                    {
                        _stopping = true;
                        continue;
                    }
                    HandleAccept(1); // 批量监听改为：currentEvent.data
                }
                else if (currentEvent.filter == KQueueLibcs.EVFILT_READ)
                {
                    if ((currentEvent.flags & KQueueLibcs.EV_EOF) != 0 && currentEvent.data == 0)
                    {
                        CleanupConnection(_fdContexts, fd);
                        continue;
                    }
                    HandleRead(_fdContexts, _kQueueFd, fd);
                }
                else if (currentEvent.filter == KQueueLibcs.EVFILT_WRITE)
                {
                    UHttpContext* context = _fdContexts + fd;
                    HandleWrite(_fdContexts, _kQueueFd, fd);
                }
            }
        }

        disposeReafy = true;

    }

    private void HandleAccept(int pendingCount)
    {
        int maxConn = ServerConfig->MAX_CONNECTIONS;
        int kqFd = _kQueueFd;
        int one = 1;
        int nbOne = 1;

        for (int i = 0; i < pendingCount; i++)
        {
            int clientFd = MacOSAPI.accept(_listenFd, null, null);

            if (clientFd < 0) return;

            if (clientFd >= maxConn)
            {
                MacOSAPI.close(clientFd);
                continue;
            }

            MacOSAPI.setsockopt(clientFd, 0xFFFF, KQueueLibcs.SO_NOSIGPIPE, &one, sizeof(int));
            MacOSAPI.ioctl(clientFd, (nuint)FIONBIO, &nbOne);


            MuxRegisterRead(_kQueueFd, clientFd);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 读取请求 / Handle Read
    // ──────────────────────────────────────────────────────────────────────────

    void HandleRead(UHttpContext* _fdContexts, int kqFd, int fd)
    {
        if (fd < 0 || fd >= ServerConfig->MAX_CONNECTIONS) return;

        UHttpContext* context = _fdContexts + fd;

        if (context->State == 1) return; // 已在发送阶段，忽略

        int singleMemSize = sizeof(UnManagedMemory<byte>);

        if (!context->RequestHeader.Activated)
        {
            // 寻址修复：fd * 单个连接的容量
            byte* requestBufferCurrent = _requestBuffers + (long)fd * ServerConfig->READ_BUFFER_CAPACITY;

            context->RequestHeader.Init(requestBufferCurrent, ServerConfig->READ_BUFFER_CAPACITY, 0, MemoryTypeDefined.Heap);
        }

        // 循环从 socket 读取数据，直到内核缓冲区排空（EAGAIN）或数据全部读完。
        // 每次迭代直接写入 HeaderBuffer 内部内存，零中转，零额外 memcpy。
        while (true)
        {

            byte* writePtr = context->RequestHeader.Pointer + context->RequestHeader.UsageSize;

            // 读取长度限制修复
            // 替换后
            if (context->RequestHeader.UsageSize >= (uint)ServerConfig->READ_BUFFER_CAPACITY)
            {
                CleanupConnection(_fdContexts, fd);
                return;
            }

            int available = (int)ServerConfig->READ_BUFFER_CAPACITY - (int)context->RequestHeader.UsageSize;

            int n = MacOSAPI.read(fd, writePtr, available);

            if (n < 0)
            {
                if (*MacOSAPI.__error() == KQueueLibcs.EAGAIN_MAC)
                {
                    // 内核缓冲区已排空

                    return;
                }
                // 真正的 I/O 错误，关闭连接
                // Real I/O error — close the connection.
                CleanupConnection(_fdContexts, fd);
                return;
            }

            if (n == 0)
            {
                // 对端关闭连接（TCP FIN）
                CleanupConnection(_fdContexts, fd);
                return;
            }

            // 通知 HeaderBuffer 内核已写入 n 字节，更新 UsageSize。
            context->RequestHeader.ReLength((uint)(context->RequestHeader.UsageSize + n));

            if (n < ServerConfig->READ_BUFFER_CAPACITY) break; // 本次内核缓冲区已排空，进入处理阶段
        }


        // POST 请求 Body 可能分多次到达，未完整则等待下次 EVFILT_READ 触发继续追加
        // 替换后
        if (!ServerFunctions.IsRequestComplete(context))
            return;

        // 快速预检，拒绝明显非 HTTP 的数据
        if (!ServerFunctions.IsLikelyHttpRequest(context->RequestHeader.Pointer, context->RequestHeader.UsageSize))
        {
            CleanupConnection(_fdContexts, fd);
            return;
        }

        byte* responseBuffer = _responseBuffers + (fd * ServerConfig->RESPONSE_BUFFER_CAPACITY);

        bool processResult;

        processResult = ServerFunctions.ProcessUserLogic(ServerConfig, userLogic, context, responseBuffer, &fd);

        if (!processResult)
        {
            CleanupConnection(_fdContexts, fd);
            return;
        }

        // 切换到发送阶段
        context->State = 1;
        context->ReadBytes = 0; // 复用为已发送字节数

        KQueueEvent64* changes = stackalloc KQueueEvent64[2];
        changes[0] = GetDeleteReadEvent(fd);
        changes[1] = GetAddOneshotWriteEvent(fd);
        MacOSAPI.kevent64(kqFd, changes, 2, null, 0, 0u, null);

        HandleWrite(_fdContexts, kqFd, fd);
    }



    // ──────────────────────────────────────────────────────────────────────────
    // 发送响应 / Handle Write
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 向客户端发送 HTTP 响应数据，支持多次部分发送。
    /// </summary>
    void HandleWrite(UHttpContext* _fdContexts, int kqFd, int fd)
    {
        if (fd < 0 || fd >= ServerConfig->MAX_CONNECTIONS) return;
        UHttpContext* context = _fdContexts + fd;

        //虽然目前已知需要发送的数据大小，但是这个while循环不能取消，
        // 当外部因素影响整个网络的时候，PosixApiNetwareLib.send 有可能发送的数据量并不能达到我们指定的大小
        // 所以必须通过while来保证非正常情况下可以再次发送剩余数据
        while (true)
        {
            // 替换后
            uint bytesSent = context->ReadBytes;
            uint totalResponseLength = context->TotalResponseLength;

            // 防止下溢：bytesSent 不应超过 totalResponseLength
            if (bytesSent > totalResponseLength)
            {
                context->Clear();
                CleanupConnection(_fdContexts, fd);
                return;
            }

            uint remaining = totalResponseLength - bytesSent;

            if (remaining == 0)
            {
                // 全部发送完毕，判断连接复用策略
                bool keepAlive = ServerFunctions.IsKeepAliveRequest(context);

                context->Clear();

                if (keepAlive)
                    MuxRegisterRead(kqFd, fd);
                else
                    CleanupConnection(_fdContexts, fd);

                return;
            }

            byte* bufferPtr = context->Response.ResponseBuffer + bytesSent;
            nint n = MacOSAPI.send(fd, bufferPtr, remaining, 0);

            if (n < 0)
            {
                int err = *MacOSAPI.__error();
                if (err == KQueueLibcs.EAGAIN_MAC)
                {
                    // 内核发送缓冲区满，等待 EVFILT_WRITE 通知
                    MuxRegisterWrite(kqFd, fd);
                    return;
                }
                // 其他发送错误（如 EPIPE）

                context->Clear();


                CleanupConnection(_fdContexts, fd);
                return;
            }

            bytesSent += (uint)n;
            context->ReadBytes = bytesSent;

            // 若已全部发送，下一次循环迭代进入 remaining==0 分支
            // 若仍有剩余（部分发送），注册 EVFILT_WRITE 等待通知
            if (bytesSent < totalResponseLength)
            {
                MuxRegisterWrite(kqFd, fd);
                return;
            }

        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 清理连接 / Cleanup Connection
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 清理并关闭一个客户端连接。
    /// M1 fix: 先调用 context->Clear() 释放 UHttpContext 内部持有的非托管资源，
    /// 再关闭 fd。防止 UHttpContext 中的字典/header 等子结构造成非托管内存泄漏。
    /// </summary>
    private void CleanupConnection(UHttpContext* _fdContexts, int fd)
    {
        if (fd < 0 || fd >= ServerConfig->MAX_CONNECTIONS) return;


        UHttpContext* context = _fdContexts + fd;

        context->Clear();

        MacOSAPI.close(fd);
        // 关闭 fd 后内核自动将其从 kqueue 中移除，无需手动 EV_DELETE
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 停止 / 释放 / 错误处理
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>停止服务器。事件循环在当前批次处理完毕后的下一轮退出。</summary>
    public void Stop()
    {
        _stopping = true;
    }

    /// <summary>
    /// 释放所有非托管资源。完全幂等，多次调用安全。
    /// </summary>
    public bool Dispose()
    {
        if (_disposed) return false;

        bool closeFD = false, closeKQueue = false;

        //必须先关闭连接，才清理缓存，否则可能出现正在处理请求时缓存被清理掉导致的访问无效内存的情况

        int fd = _listenFd;
        int kfd = _kQueueFd;

        // 用 >= 0 判断（不用 > 0），避免 fd=0 被误跳过
        if (_listenFd >= 0)
        {
            closeFD = MacOSAPI.close(_listenFd) == 0;
            _listenFd = -1;
        }
        if (_kQueueFd >= 0)
        {
            closeKQueue = MacOSAPI.close(_kQueueFd) == 0;
            _kQueueFd = -1;
        }

        if (_fdContexts != null)
        {
            NativeMemory.AlignedFree(_fdContexts);
            _fdContexts = null;
        }

        if (closeFD && closeKQueue)
        {
            if (_responseBuffers is not null)
                NativeMemory.AlignedFree(_responseBuffers);

            if (_requestBuffers is not null)
                NativeMemory.AlignedFree(_requestBuffers);

            _responseBuffers = null;
            _requestBuffers = null;

            _disposed = true;

            Console.WriteLine($"fd: {fd}, kfd:{kfd} server stoped. Resources released.");

            return true;
        }

        return false;
    }

    /// <summary>
    /// 构造阶段致命错误处理：释放已分配资源并终止进程。
    /// </summary>
    private void Fail(ReadOnlySpan<char> msg)
    {
        Console.Error.WriteLine($"[Fatal] MacOSHttpServer: {msg}");
        Dispose();
        Environment.Exit(1);
    }
}