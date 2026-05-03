/*
 * EPoolServer — 基于 Linux epoll 的零 GC 单线程 HTTP 服务器（Linux 专用）
 *
 * ┌─────────────────────────────────────────────────────────────────────────────┐
 * │  整体工作流程 / Overall Workflow                                              │
 * ├─────────────────────────────────────────────────────────────────────────────┤
 * │  1. 构造阶段 Constructor                                                     │
 * │     · 校验 ServerConfig 参数合法性                                            │
 * │     · 预分配三段固定内存：                                                     │
 * │       – http_context_pool_pointer : MAX_CONNECTIONS 个 UHttpContext，按 fd 下标寻址     │
 * │       – response_buffer_pool_pointer: MAX_CONNECTIONS 个响应缓冲区，每个 RESPONSE_BUFFER_CAPACITY │
 * │       – request_buffer_pool_pointer : MAX_CONNECTIONS 个接收缓冲区，每个 READ_BUFFER_CAPACITY    │
 * │     · 创建 TCP 监听 socket，绑定端口，开始监听                                 │
 * │                                                                             │
 * │  2. 启动阶段 Start                                                           │
 * │     · 创建 epoll 实例 + eventfd，注册 listenFd，进入事件循环                   │
 * │                                                                             │
 * │  3. 事件循环                                                                 │
 * │     · listenFd 可读  → HandleAccept（循环 accept 直到 EAGAIN）               │
 * │     · eventFd  可读  → Stop 信号，退出                                       │
 * │     · EPOLLIN        → HandleRead（单次 read，写入固定接收缓冲区）             │
 * │     · EPOLLOUT       → HandleWrite（循环 send 直到 EAGAIN 或发完）           │
 * │     · EPOLLERR/HUP   → CleanupConnection                                   │
 * │  4. HandleAccept：循环 accept，每个 clientFd 设非阻塞，注册 EPOLLIN|ET       │
 * │  5. HandleRead：单次 read 写入固定接收缓冲区，读满容量则关闭连接，               │
 * │     否则处理业务，切换 EPOLLOUT                                               │
 * │  6. HandleWrite：循环 send，EAGAIN 保持 EPOLLOUT，发完重置为 EPOLLIN 或关闭   │
 * │                                                                             │
 * │  所有 native 调用均通过 EPoolLibcs                                           │
 * └─────────────────────────────────────────────────────────────────────────────┘
 */

using System.Runtime.CompilerServices;

namespace Solamirare;

/// <summary>
/// 基于 Linux epoll 机制的零 GC 单线程 HTTP 服务器。
/// 所有 I/O 均在单一事件循环线程内完成，无锁、无线程切换开销。
/// <para>
/// A zero-GC single-threaded HTTP server based on the Linux epoll mechanism.
/// All I/O is performed within a single event loop thread, with no locks or thread-switching overhead.
/// </para>
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public unsafe ref struct EPoolServer
{
    // ──────────────────────────────────────────────────────────────────────────
    // 字段 / Fields
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>以 fd 为下标寻址的 UHttpContext 数组。</summary>
    private UHttpContext* http_context_pool_pointer;

    /// <summary>预分配响应缓冲区，每个连接独占 RESPONSE_BUFFER_CAPACITY 字节。</summary>
    private byte* response_buffer_pool_pointer;

    /// <summary>预分配接收缓冲区，每个连接独占 READ_BUFFER_CAPACITY 字节。</summary>
    private byte* request_buffer_pool_pointer;

    /// <summary>用户业务逻辑函数指针。</summary>
    private delegate*<UHttpContext*, bool> user_logic_callback;

    /// <summary>监听 socket fd，初始化为 -1。</summary>
    private int listening_socket_file_descriptor;

    /// <summary>epoll 实例 fd，初始化为 -1。</summary>
    private int epoll_file_descriptor;

    /// <summary>Stop() 用于唤醒 epoll_wait 的 eventfd，初始化为 -1。</summary>
    private int event_file_descriptor;

    /// <summary>停止标志。</summary>
    private volatile bool stopping_requested;

    /// <summary>服务器配置。</summary>
    public HTTPSeverConfig* ServerConfig;

    /// <summary>Dispose 幂等保护。</summary>
    private bool is_disposed;

    // ──────────────────────────────────────────────────────────────────────────
    // 构造函数 / Constructor
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 初始化服务器：校验配置、一次性分配所有运行时内存、创建监听套接字。
    /// </summary>
    public void Init(HTTPSeverConfig* config)
    {
        ServerConfig = config;
        listening_socket_file_descriptor = -1;
        epoll_file_descriptor = -1;
        event_file_descriptor = -1;
        is_disposed = false;

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

        int max_conn = config->MAX_CONNECTIONS;

        uint context_size = (uint)(sizeof(UHttpContext) * max_conn);
        uint response_size = (uint)config->RESPONSE_BUFFER_CAPACITY * (uint)max_conn;
        uint request_size = (uint)config->READ_BUFFER_CAPACITY * (uint)max_conn;

        http_context_pool_pointer = (UHttpContext*)NativeMemory.AlignedAlloc(context_size, SolamirareEnvironment.ALIGNMENT);
        response_buffer_pool_pointer = (byte*)NativeMemory.AlignedAlloc(response_size, SolamirareEnvironment.ALIGNMENT);
        request_buffer_pool_pointer = (byte*)NativeMemory.AlignedAlloc(request_size, SolamirareEnvironment.ALIGNMENT);

        NativeMemory.Clear(http_context_pool_pointer, context_size);
        NativeMemory.Clear(response_buffer_pool_pointer, response_size);
        NativeMemory.Clear(request_buffer_pool_pointer, request_size);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 启动 / Start
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 启动服务器并进入事件循环。
    /// </summary>
    /// <param name="user_logic">处理请求的用户回调。</param>
    public void Start(delegate*<UHttpContext*, bool> user_logic)
    {
        user_logic_callback = user_logic;

        listening_socket_file_descriptor = LinuxAPI.socket(EPoolConsts.AF_INET, EPoolConsts.SOCK_STREAM, 0);
        if (listening_socket_file_descriptor < 0) Fail("socket() failed");

        int opt = 1;
        LinuxAPI.setsockopt(listening_socket_file_descriptor, EPoolConsts.SOL_SOCKET, EPoolConsts.SO_REUSEADDR, &opt, sizeof(int));
        LinuxAPI.setsockopt(listening_socket_file_descriptor, EPoolConsts.SOL_SOCKET, EPoolConsts.SO_REUSEPORT, &opt, sizeof(int));

        // listenFd 设为非阻塞，配合 ET 模式循环 accept
        SetNonBlocking(listening_socket_file_descriptor);

        sockaddr_in address = new sockaddr_in
        {
            sin_family = EPoolConsts.AF_INET,
            sin_port = LinuxAPI.htons(ServerConfig->Port),
            sin_addr = 0x00000000
        };

        if (LinuxAPI.bind(listening_socket_file_descriptor, &address, 16) < 0) Fail("bind() failed");
        if (LinuxAPI.listen(listening_socket_file_descriptor, 1024) < 0) Fail("listen() failed");

        epoll_file_descriptor = LinuxAPI.epoll_create1(0);
        if (epoll_file_descriptor < 0) Fail("epoll_create1() failed");

        epoll_event listen_event = new epoll_event
        {
            events = EPoolConsts.EPOLLIN | EPoolConsts.EPOLLET,
            data_u64 = (ulong)listening_socket_file_descriptor
        };
        if (LinuxAPI.epoll_ctl(epoll_file_descriptor, EPoolConsts.EPOLL_CTL_ADD, listening_socket_file_descriptor, &listen_event) < 0)
            Fail("epoll_ctl() failed for listenFd");

        // 创建 eventfd 注册到 epoll，Stop() 写入即可唤醒 epoll_wait
        event_file_descriptor = LinuxAPI.eventfd(0, 0);
        if (event_file_descriptor < 0) Fail("eventfd() failed");

        epoll_event stop_event = new epoll_event
        {
            events = EPoolConsts.EPOLLIN,
            data_u64 = (ulong)event_file_descriptor
        };
        LinuxAPI.epoll_ctl(epoll_file_descriptor, EPoolConsts.EPOLL_CTL_ADD, event_file_descriptor, &stop_event);

        ServerFunctions.ConsoleStartedStatus("EPool HTTP Server", ServerConfig);

        epoll_event* events = stackalloc epoll_event[64];

        // Execution Order:
        // 1. Wait for events (epoll_wait).
        // 2. Iterate through ready events.
        // 3. Dispatch to handler (Stop, Accept, Read/Write, Error).
        while (!stopping_requested)
        {
            int num_events = LinuxAPI.epoll_wait(epoll_file_descriptor, events, 64, -1);
            if (num_events < 0) continue;

            for (int i = 0; i < num_events; i++)
            {
                int file_descriptor = (int)events[i].data_u64;
                uint event_mask = events[i].events;

                if (file_descriptor == event_file_descriptor)
                {
                    stopping_requested = true;
                    break;
                }

                if (file_descriptor == listening_socket_file_descriptor)
                {
                    HandleAccept();
                    continue;
                }

                if ((event_mask & (EPoolConsts.EPOLLERR | EPoolConsts.EPOLLHUP | EPoolConsts.EPOLLRDHUP)) != 0)
                {
                    CleanupConnection(file_descriptor);
                    continue;
                }

                if ((event_mask & EPoolConsts.EPOLLIN) != 0)
                {
                    HandleRead(file_descriptor);
                    continue;
                }

                if ((event_mask & EPoolConsts.EPOLLOUT) != 0)
                {
                    HandleWrite(file_descriptor);
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 接受连接 / HandleAccept
    // ET 模式只通知一次，必须循环 accept 直到 EAGAIN 排空 backlog
    // ──────────────────────────────────────────────────────────────────────────

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void HandleAccept()
    {
        while (true)
        {
            int client_file_descriptor = LinuxAPI.accept(listening_socket_file_descriptor, null, null);

            if (client_file_descriptor < 0)
                break; // EAGAIN：backlog 已排空

            if (client_file_descriptor >= ServerConfig->MAX_CONNECTIONS)
            {
                LinuxAPI.close(client_file_descriptor);
                continue;
            }

            SetNonBlocking(client_file_descriptor);

            epoll_event client_event = new epoll_event
            {
                events = EPoolConsts.EPOLLIN | EPoolConsts.EPOLLET | EPoolConsts.EPOLLRDHUP,
                data_u64 = (ulong)client_file_descriptor
            };
            LinuxAPI.epoll_ctl(epoll_file_descriptor, EPoolConsts.EPOLL_CTL_ADD, client_file_descriptor, &client_event);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 读取请求 / HandleRead
    // ──────────────────────────────────────────────────────────────────────────

    void HandleRead(int file_descriptor)
    {
        if (file_descriptor < 0 || file_descriptor >= ServerConfig->MAX_CONNECTIONS) return;
        UHttpContext* context = http_context_pool_pointer + file_descriptor;

        // 首次进入（或 keep-alive Clear() 之后）时将固定接收缓冲区包装为视图。
        // Initialize a fixed-buffer view on first entry or after keep-alive Clear().
        if (!context->RequestHeader.Activated)
        {
            byte* request_buffer = request_buffer_pool_pointer + (long)file_descriptor * ServerConfig->READ_BUFFER_CAPACITY;
            context->RequestHeader.Init(request_buffer, ServerConfig->READ_BUFFER_CAPACITY, 0, MemoryTypeDefined.Heap);
        }

        byte* write_pointer = context->RequestHeader.Pointer + context->RequestHeader.UsageSize;
        // 替换后
        if (context->RequestHeader.UsageSize >= (uint)ServerConfig->READ_BUFFER_CAPACITY)
        {
            CleanupConnection(file_descriptor);
            return;
        }
        int available_bytes = (int)ServerConfig->READ_BUFFER_CAPACITY - (int)context->RequestHeader.UsageSize;

        long bytes_read = LinuxAPI.read(file_descriptor, write_pointer, (uint)available_bytes);

        if (bytes_read > 0)
        {
            context->RequestHeader.ReLength((uint)(context->RequestHeader.UsageSize + bytes_read));

            // 读满容量说明请求超出固定缓冲区限制，关闭连接
            if (context->RequestHeader.UsageSize >= (uint)ServerConfig->READ_BUFFER_CAPACITY)
            {
                CleanupConnection(file_descriptor);
                return;
            }
        }
        else if (bytes_read == 0)
        {
            // 对端关闭连接（FIN）
            CleanupConnection(file_descriptor);
            return;
        }
        else
        {
            int err = *LinuxAPI.__errno_location();
            if (err == EPoolConsts.EAGAIN) return; // 内核缓冲区已排空，等待下次通知
            CleanupConnection(file_descriptor);
            return;
        }

        if (context->RequestHeader.UsageSize == 0) return;

        // 检查请求是否完整（Headers 结束且 Body 已达 Content-Length）
        // POST 请求 Body 可能分多次到达，未完整则继续等待下次 EPOLLIN
        // 替换后
        if (!ServerFunctions.IsRequestComplete(context))
            return;

        // 快速预检，拒绝明显非 HTTP 的数据
        if (!ServerFunctions.IsLikelyHttpRequest(context->RequestHeader.Pointer, context->RequestHeader.UsageSize))
        {
            CleanupConnection(file_descriptor);
            return;
        }

        byte* response_buffer = response_buffer_pool_pointer + (long)file_descriptor * ServerConfig->RESPONSE_BUFFER_CAPACITY;

        bool process_result = ServerFunctions.ProcessUserLogic(
            ServerConfig, user_logic_callback, context, response_buffer, &file_descriptor);

        if (!process_result)
        {
            CleanupConnection(file_descriptor);
            return;
        }

        // 切换到发送阶段
        context->State = 1;
        context->ReadBytes = 0; // 复用为已发送字节偏移

        epoll_event write_event = new epoll_event
        {
            events = EPoolConsts.EPOLLOUT | EPoolConsts.EPOLLET | EPoolConsts.EPOLLRDHUP,
            data_u64 = (ulong)file_descriptor
        };
        LinuxAPI.epoll_ctl(epoll_file_descriptor, EPoolConsts.EPOLL_CTL_MOD, file_descriptor, &write_event);

        // 立即尝试发送（多数情况下一次发完，减少 epoll_wait 往返）
        HandleWrite(file_descriptor);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 发送响应 / HandleWrite
    // 循环 send，处理部分发送；EAGAIN 保持 EPOLLOUT 等下次通知
    // ──────────────────────────────────────────────────────────────────────────

    void HandleWrite(int file_descriptor)
    {
        if (file_descriptor < 0 || file_descriptor >= ServerConfig->MAX_CONNECTIONS) return;
        UHttpContext* context = http_context_pool_pointer + file_descriptor;

        while (true)
        {

            uint bytes_sent = context->ReadBytes;
            uint total_length = context->TotalResponseLength;

            // 防止下溢：bytes_sent 不应超过 total_length
            if (bytes_sent > total_length)
            {
                context->Clear();
                context->State = 0;
                CleanupConnection(file_descriptor);
                return;
            }

            uint remaining = total_length - bytes_sent;

            if (remaining == 0)
            {
                // 全部发送完毕
                bool keep_alive = ServerFunctions.IsKeepAliveRequest(context);
                context->Clear();
                context->State = 0;

                if (keep_alive)
                {
                    epoll_event read_event = new epoll_event
                    {
                        events = EPoolConsts.EPOLLIN | EPoolConsts.EPOLLET | EPoolConsts.EPOLLRDHUP,
                        data_u64 = (ulong)file_descriptor
                    };
                    LinuxAPI.epoll_ctl(epoll_file_descriptor, EPoolConsts.EPOLL_CTL_MOD, file_descriptor, &read_event);
                }
                else
                {
                    CleanupConnection(file_descriptor);
                }
                return;
            }

            byte* buffer_pointer = context->Response.ResponseBuffer + bytes_sent;

            int n = LinuxAPI.send(file_descriptor, buffer_pointer, remaining, (int)EPoolConsts.MSG_NOSIGNAL);

            if (n > 0)
            {
                context->ReadBytes += (uint)n;
                continue;
            }

            if (n < 0)
            {
                int err = *LinuxAPI.__errno_location();
                if (err == EPoolConsts.EAGAIN)
                {
                    // 发送缓冲区满，等待 EPOLLOUT 通知继续
                    return;
                }

                // 其他错误（对端重置等）
                context->Clear();
                context->State = 0;
                CleanupConnection(file_descriptor);
                return;
            }

            // n == 0：对端关闭
            context->Clear();
            context->State = 0;
            CleanupConnection(file_descriptor);
            return;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 清理连接 / CleanupConnection
    // epoll_ctl DEL 统一在此处执行，是唯一清理入口
    // ──────────────────────────────────────────────────────────────────────────

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void CleanupConnection(int file_descriptor)
    {
        UHttpContext* context = http_context_pool_pointer + file_descriptor;

        context->Clear();
        context->State = 0;
        context->ReadBytes = 0;

        LinuxAPI.epoll_ctl(epoll_file_descriptor, EPoolConsts.EPOLL_CTL_DEL, file_descriptor, null);
        LinuxAPI.close(file_descriptor);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 辅助方法 / Helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 设置 fd 为非阻塞模式（O_NONBLOCK）。
    /// 使用 EPoolLibcs.fcntl 和 EPoolLibcs.O_NONBLOCK（Linux = 0x800）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void SetNonBlocking(int file_descriptor)
    {
        int flags = LinuxAPI.fcntl(file_descriptor, EPoolConsts.F_GETFL, 0);
        LinuxAPI.fcntl(file_descriptor, EPoolConsts.F_SETFL, flags | EPoolConsts.O_NONBLOCK);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 停止 / Stop
    // 向 eventfd 写入唤醒 epoll_wait
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 请求停止服务器事件循环。
    /// </summary>
    public void Stop()
    {
        stopping_requested = true;

        if (event_file_descriptor >= 0)
        {
            ulong val = 1;
            LinuxAPI.eventfd_write(event_file_descriptor, val);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 释放资源 / Dispose
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 释放所有非托管资源。完全幂等，多次调用安全。
    /// </summary>
    public void Dispose()
    {
        if (is_disposed) return;
        is_disposed = true;

        if (epoll_file_descriptor >= 0)
        {
            LinuxAPI.close(epoll_file_descriptor);
            epoll_file_descriptor = -1;
        }

        if (event_file_descriptor >= 0)
        {
            LinuxAPI.close(event_file_descriptor);
            event_file_descriptor = -1;
        }

        if (listening_socket_file_descriptor >= 0)
        {
            LinuxAPI.close(listening_socket_file_descriptor);
            listening_socket_file_descriptor = -1;
        }

        if (http_context_pool_pointer != null)
        {
            NativeMemory.AlignedFree(http_context_pool_pointer);
            http_context_pool_pointer = null;
        }

        if (response_buffer_pool_pointer != null)
        {
            NativeMemory.AlignedFree(response_buffer_pool_pointer);
            response_buffer_pool_pointer = null;
        }

        if (request_buffer_pool_pointer != null)
        {
            NativeMemory.AlignedFree(request_buffer_pool_pointer);
            request_buffer_pool_pointer = null;
        }
    }

    /// <summary>
    /// 构造阶段致命错误处理：释放已分配资源并终止进程。
    /// </summary>
    private void Fail(ReadOnlySpan<char> msg)
    {
        Console.Error.WriteLine($"[Fatal] EPoolServer: {msg}");
        Dispose();
        Environment.Exit(1);
    }
}
