/*
 * Windows IOCP Web Server 处理流程说明
 * ================================================================================================
 *
 * 架构：一个 WindowsHttpServer 实例持有一个共享 IOCP。
 *   - ListenThread（StartListening）：负责初始化资源、bind、listen、PostAccept，完成后退出。
 *   - N 个 WorkerThread（WorkerLoop）：各自阻塞在 GetQueuedCompletionStatus，共享同一个 IOCP，
 *     内核按需唤醒其中一个线程消费完成事件，实现多核并行处理。
 *
 * 停止流程：
 *   - Stop() 设置 stopping_requested，并向 IOCP 投递与工作线程数等量的哨兵包，
 *     确保每个 WorkerThread 都能收到退出信号自然退出。
 *   - 最后一个退出的 WorkerThread 将 stoped 置为 true，
 *     外部通过 Stopped 属性得知所有工作线程已安全退出，此时才可调用 Dispose。
 *
 * 线程安全：
 *   - AcquireFromStack / ReleaseToStack 用 SpinLock 保护，多 WorkerThread 并发安全。
 *
 * ================================================================================================
 */

using Solamirare;
using System.Runtime.CompilerServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal unsafe ref struct WindowsHttpServer
{
    const int OP_ACCEPT = 1;
    const int OP_RECV   = 2;
    const int OP_SEND   = 3;

    const int ACCEPT_ADDR_LEN = 32;

    // ── IOCP Context 非托管内存布局 ────────────────────────────────────────────
    //   0       40      OVERLAPPED              ← 必须在偏移 0
    //   40       4      operation_type    (int)
    //   44       4      keepAlive         (int)
    //   48       8      socket            (nint)
    //   56      16      WSABUF
    //   72       4      respSlot          (int)
    //   76       4      (pad)
    //   80       4      respLen           (uint)
    //   84       4      context_slot      (int)
    //   88       4      wsa_received      (uint)
    //   92       4      wsa_flags         (uint)
    //   96      64      accept_addr_buffer
    //   160  varies     UHttpContext       ← 32 字节对齐

    const int OFFSET_OVERLAPPED            = 0;
    const int OFFSET_OPERATION_TYPE        = 40;
    const int OFFSET_KEEPALIVE             = 44;
    const int OFFSET_SOCKET                = 48;
    const int OFFSET_WSABUF                = 56;
    const int OFFSET_RESPONSE_BUFFER_INDEX = 72;
    const int OFFSET_RESPONSE_LENGTH       = 80;
    const int OFFSET_CONTEXT_SLOT          = 84;
    const int OFFSET_WSA_RECEIVED          = 88;
    const int OFFSET_WSA_FLAGS             = 92;
    const int OFFSET_ACCEPT_ADDR_BUFFER    = 96;
    const int OFFSET_HTTP_CONTEXT          = 160;

    static readonly int CONTEXT_SIZE;

    static WindowsHttpServer()
    {
        int ctx_size = (sizeof(UHttpContext) + 31) & ~31;
        CONTEXT_SIZE = OFFSET_HTTP_CONTEXT + ctx_size;
    }

    internal HTTPSeverConfig* ServerConfig;

    delegate* unmanaged[Stdcall]<
        nint, nint, void*, uint, uint, uint, uint*, OVERLAPPED*, bool>
        accept_ex_delegate;

    byte* response_buffer_pool_pointer;
    byte* request_buffer_pool_pointer;
    int*  response_buffer_slot_stack_pointer;
    byte* iocp_context_pool_pointer;
    int*  iocp_context_slot_stack_pointer;

    SpinLock response_buffer_slot_lock;
    SpinLock iocp_context_slot_lock;

    delegate*<UHttpContext*, bool> user_logic_callback;

    nint io_completion_port_handle;
    nint listening_socket_handle;

    int  iocp_context_total_size;

    volatile bool stopping_requested;
    bool is_disposed;

    // 还在运行的工作线程数量，用原子操作维护
    // 最后一个退出的线程将 stoped 置为 true
    int  running_worker_count;
    bool stoped;

    /// <summary>所有工作线程均已退出，可以安全调用 Dispose。</summary>
    internal bool Stopped => stoped;

    /// <summary>
    /// 初始化服务器
    /// </summary>
    /// <param name="config"></param>
    internal void Init(HTTPSeverConfig* config)
    {
        ServerConfig              = config;
        listening_socket_handle   = IOCPLibrary.INVALID_SOCKET;
        io_completion_port_handle = 0;
        stoped                    = false;
        running_worker_count      = 0;
    }

    // =========================================================================
    // 监听线程入口：初始化资源、bind、listen、PostAccept，完成即退出
    // =========================================================================

    internal void StartListening(delegate*<UHttpContext*, bool> user_logic)
    {
        if (ServerConfig == null) Fail("ServerConfig is null");

        user_logic_callback     = user_logic;
        iocp_context_total_size = CONTEXT_SIZE;

        int max_conn = ServerConfig->MAX_CONNECTIONS;

        request_buffer_pool_pointer = (byte*)NativeMemory.AllocZeroed(
            (nuint)max_conn * (nuint)ServerConfig->READ_BUFFER_CAPACITY);

        response_buffer_pool_pointer = (byte*)NativeMemory.AllocZeroed(
            (nuint)max_conn * (nuint)ServerConfig->RESPONSE_BUFFER_CAPACITY);

        response_buffer_slot_stack_pointer = (int*)NativeMemory.AllocZeroed(
            (nuint)((max_conn + 1) * sizeof(int)));
        response_buffer_slot_stack_pointer[0] = max_conn;
        for (int s = 0; s < max_conn; s++)
            response_buffer_slot_stack_pointer[s + 1] = s;

        iocp_context_pool_pointer = (byte*)NativeMemory.AllocZeroed(
            (nuint)max_conn * (nuint)iocp_context_total_size);

        iocp_context_slot_stack_pointer = (int*)NativeMemory.AllocZeroed(
            (nuint)((max_conn + 1) * sizeof(int)));
        iocp_context_slot_stack_pointer[0] = max_conn;
        for (int s = 0; s < max_conn; s++)
            iocp_context_slot_stack_pointer[s + 1] = s;

        WSADATA wd;
        if (WindowsAPI.WSAStartup(0x0202, &wd) != 0) Fail("WSAStartup");

        io_completion_port_handle = WindowsAPI.CreateIoCompletionPort(
            IOCPLibrary.INVALID_HANDLE_VALUE, 0, 0, (uint)Environment.ProcessorCount * 2);
        if (io_completion_port_handle == 0) Fail("CreateIoCompletionPort");

        listening_socket_handle = WindowsAPI.socket(
            IOCPLibrary.AF_INET, IOCPLibrary.SOCK_STREAM, IOCPLibrary.IPPROTO_TCP);
        if (listening_socket_handle == IOCPLibrary.INVALID_SOCKET) Fail("socket");

        int one = 1;
        WindowsAPI.setsockopt(listening_socket_handle,
            IOCPLibrary.SOL_SOCKET, IOCPLibrary.SO_REUSEADDR, &one, sizeof(int));

        sockaddr_in addr = default;
        addr.sin_family = IOCPLibrary.AF_INET;
        addr.sin_port   = ServerFunctions.Htons(ServerConfig->Port);

        if (WindowsAPI.bind(listening_socket_handle, &addr, sizeof(sockaddr_in)) == IOCPLibrary.SOCKET_ERROR)
            Fail("bind");
        if (WindowsAPI.listen(listening_socket_handle, IOCPLibrary.SOMAXCONN) == IOCPLibrary.SOCKET_ERROR)
            Fail("listen");
        if (WindowsAPI.CreateIoCompletionPort(listening_socket_handle, io_completion_port_handle, 0,
                (uint)Environment.ProcessorCount * 2) == 0)
            Fail("Associate listenSock→IOCP");

        accept_ex_delegate = ResolveAcceptEx(listening_socket_handle);

        for (int i = 0; i < Environment.ProcessorCount; i++)
            PostAccept();

        ServerFunctions.ConsoleStartedStatus("IOCP Server", ServerConfig);

        // StartListening 到此结束，监听线程退出，WorkerThread 负责后续处理
    }

    // =========================================================================
    // 工作线程入口：阻塞消费 IOCP 完成事件，多个线程共享同一个 IOCP
    // =========================================================================

    internal void WorkerLoop()
    {
        // 注册自己为一个活跃的工作线程
        Interlocked.Increment(ref running_worker_count);

        while (true)
        {
            uint       transferred_bytes;
            nuint      completion_key;
            OVERLAPPED* overlapped_pointer;

            WindowsAPI.GetQueuedCompletionStatus(
                io_completion_port_handle.ToPointer(),
                &transferred_bytes, &completion_key, &overlapped_pointer, 0xFFFFFFFF);

            // 收到哨兵包，退出循环
            if (completion_key == nuint.MaxValue) break;

            if (overlapped_pointer == null) continue;

            byte* iocp_context_pointer = (byte*)overlapped_pointer;
            int   operation_type       = *(int*)(iocp_context_pointer + OFFSET_OPERATION_TYPE);
            nint  socket_handle        = *(nint*)(iocp_context_pointer + OFFSET_SOCKET);

            switch (operation_type)
            {
                case OP_ACCEPT: OnAccept(iocp_context_pointer, socket_handle); break;
                case OP_RECV:   OnRecv(iocp_context_pointer, socket_handle, transferred_bytes); break;
                case OP_SEND:   OnSend(iocp_context_pointer, socket_handle); break;
            }
        }

        // 最后一个退出的工作线程将 stoped 置为 true
        if (Interlocked.Decrement(ref running_worker_count) == 0)
            stoped = true;
    }

    // =========================================================================
    // 停止：投递与工作线程数等量的哨兵，确保每个 WorkerThread 都能退出
    // =========================================================================

    internal void Stop(int worker_thread_count)
    {
        stopping_requested = true;

        // 每个工作线程需要一个哨兵包才能从 GetQueuedCompletionStatus 返回并退出
        for (int i = 0; i < worker_thread_count; i++)
            WindowsAPI.PostQueuedCompletionStatus(io_completion_port_handle, 0, nuint.MaxValue, null);
    }

    // =========================================================================
    // AcceptEx 完成
    // =========================================================================

    void OnAccept(byte* iocp_context_pointer, nint accept_socket)
    {
        nint ls = listening_socket_handle;
        WindowsAPI.setsockopt(accept_socket, IOCPLibrary.SOL_SOCKET,
            IOCPLibrary.SO_UPDATE_ACCEPT_CONTEXT, &ls, sizeof(nint));
        WindowsAPI.CreateIoCompletionPort(accept_socket, io_completion_port_handle,
            (nuint)accept_socket, (uint)Environment.ProcessorCount * 2);

        if (!stopping_requested) PostAccept();

        *(nint*)(iocp_context_pointer + OFFSET_SOCKET) = accept_socket;

        int slot_index = AcquireSlot();
        if (slot_index < 0)
        {
            WindowsAPI.closesocket(accept_socket);
            ReleaseCtxSlot(*(int*)(iocp_context_pointer + OFFSET_CONTEXT_SLOT));
            return;
        }
        *(int*)(iocp_context_pointer + OFFSET_RESPONSE_BUFFER_INDEX) = slot_index;

        PostRecv(accept_socket, iocp_context_pointer);
    }

    // =========================================================================
    // WSARecv 完成
    // =========================================================================

    void OnRecv(byte* iocp_context_pointer, nint socket_handle, uint transferred_bytes)
    {
        if (transferred_bytes == 0) { CloseConnection(iocp_context_pointer, socket_handle); return; }

        UHttpContext* context = (UHttpContext*)(iocp_context_pointer + OFFSET_HTTP_CONTEXT);

        context->RequestHeader.ReLength(context->RequestHeader.UsageSize + transferred_bytes);

        if (!ServerFunctions.IsRequestComplete(context))
        {
            PostRecv(socket_handle, iocp_context_pointer);
            return;
        }

        if (!ServerFunctions.IsLikelyHttpRequest(
            context->RequestHeader.Pointer, context->RequestHeader.UsageSize))
        {
            CloseConnection(iocp_context_pointer, socket_handle);
            return;
        }

        int slot_index = *(int*)(iocp_context_pointer + OFFSET_RESPONSE_BUFFER_INDEX);
        if (slot_index < 0) { CloseConnection(iocp_context_pointer, socket_handle); return; }

        byte* response_buffer = response_buffer_pool_pointer
            + (nuint)slot_index * (nuint)ServerConfig->RESPONSE_BUFFER_CAPACITY;

        bool proceed = ServerFunctions.ProcessUserLogic(
            ServerConfig, user_logic_callback, context, response_buffer, &socket_handle);

        if (!proceed)
        {
            CloseConnection(iocp_context_pointer, socket_handle);
            return;
        }

        *(int*) (iocp_context_pointer + OFFSET_KEEPALIVE)       = ServerFunctions.IsKeepAliveRequest(context) ? 1 : 0;
        *(uint*)(iocp_context_pointer + OFFSET_RESPONSE_LENGTH) = context->TotalResponseLength;

        context->Clear();
        PostSend(socket_handle, iocp_context_pointer);
    }

    // =========================================================================
    // WSASend 完成
    // =========================================================================

    void OnSend(byte* iocp_context_pointer, nint socket_handle)
    {
        if (*(int*)(iocp_context_pointer + OFFSET_KEEPALIVE) == 1)
            PostRecv(socket_handle, iocp_context_pointer);
        else
            CloseConnection(iocp_context_pointer, socket_handle);
    }

    // =========================================================================
    // 投递 AcceptEx
    // =========================================================================

    void PostAccept()
    {
        nint accept_socket = WindowsAPI.socket(
            IOCPLibrary.AF_INET, IOCPLibrary.SOCK_STREAM, IOCPLibrary.IPPROTO_TCP);
        if (accept_socket == IOCPLibrary.INVALID_SOCKET) return;

        byte* iocp_context_pointer = AllocCtx(OP_ACCEPT, accept_socket);
        if (iocp_context_pointer == null)
        {
            WindowsAPI.closesocket(accept_socket);
            return;
        }

        bool ok = accept_ex_delegate(
            listening_socket_handle, accept_socket,
            iocp_context_pointer + OFFSET_ACCEPT_ADDR_BUFFER,
            0, ACCEPT_ADDR_LEN, ACCEPT_ADDR_LEN,
            (uint*)(iocp_context_pointer + OFFSET_WSA_RECEIVED),
            (OVERLAPPED*)(iocp_context_pointer + OFFSET_OVERLAPPED));

        if (!ok && WindowsAPI.GetLastError() != IOCPLibrary.WSA_IO_PENDING)
        {
            WindowsAPI.closesocket(accept_socket);
            ReleaseCtxSlot(*(int*)(iocp_context_pointer + OFFSET_CONTEXT_SLOT));
        }
    }

    // =========================================================================
    // 投递 WSARecv
    // =========================================================================

    void PostRecv(nint socket_handle, byte* iocp_context_pointer)
    {
        *(int*)(iocp_context_pointer + OFFSET_OPERATION_TYPE) = OP_RECV;

        UHttpContext* context      = (UHttpContext*)(iocp_context_pointer + OFFSET_HTTP_CONTEXT);
        int           context_slot = *(int*)(iocp_context_pointer + OFFSET_CONTEXT_SLOT);

        if (!context->RequestHeader.Activated)
        {
            byte* request_buffer = request_buffer_pool_pointer
                + (nuint)context_slot * (nuint)ServerConfig->READ_BUFFER_CAPACITY;
            context->RequestHeader.Init(request_buffer,
                ServerConfig->READ_BUFFER_CAPACITY, 0, MemoryTypeDefined.Heap);
        }

        if (context->RequestHeader.UsageSize >= (uint)ServerConfig->READ_BUFFER_CAPACITY)
        {
            CloseConnection(iocp_context_pointer, socket_handle);
            return;
        }

        WSABUF* wsa_buffer = (WSABUF*)(iocp_context_pointer + OFFSET_WSABUF);
        wsa_buffer->buf    = context->RequestHeader.Pointer + context->RequestHeader.UsageSize;
        wsa_buffer->len    = (uint)ServerConfig->READ_BUFFER_CAPACITY - context->RequestHeader.UsageSize;

        *(uint*)(iocp_context_pointer + OFFSET_WSA_FLAGS) = 0;
        OVERLAPPED* overlapped_pointer = (OVERLAPPED*)(iocp_context_pointer + OFFSET_OVERLAPPED);
        *overlapped_pointer = default;

        int r = WindowsAPI.WSARecv(socket_handle, wsa_buffer, 1,
            (uint*)(iocp_context_pointer + OFFSET_WSA_RECEIVED),
            (uint*)(iocp_context_pointer + OFFSET_WSA_FLAGS),
            overlapped_pointer, 0);
        if (r == IOCPLibrary.SOCKET_ERROR && WindowsAPI.GetLastError() != IOCPLibrary.WSA_IO_PENDING)
            CloseConnection(iocp_context_pointer, socket_handle);
    }

    // =========================================================================
    // 投递 WSASend
    // =========================================================================

    void PostSend(nint socket_handle, byte* iocp_context_pointer)
    {
        *(int*)(iocp_context_pointer + OFFSET_OPERATION_TYPE) = OP_SEND;

        int     slot_index = *(int*)(iocp_context_pointer + OFFSET_RESPONSE_BUFFER_INDEX);
        WSABUF* wsa_buffer = (WSABUF*)(iocp_context_pointer + OFFSET_WSABUF);
        wsa_buffer->buf    = SlotToPtr(slot_index);
        wsa_buffer->len    = *(uint*)(iocp_context_pointer + OFFSET_RESPONSE_LENGTH);

        OVERLAPPED* overlapped_pointer = (OVERLAPPED*)(iocp_context_pointer + OFFSET_OVERLAPPED);
        *overlapped_pointer = default;

        int r = WindowsAPI.WSASend(socket_handle, wsa_buffer, 1,
            (uint*)(iocp_context_pointer + OFFSET_WSA_RECEIVED),
            0, overlapped_pointer, 0);
        if (r == IOCPLibrary.SOCKET_ERROR && WindowsAPI.GetLastError() != IOCPLibrary.WSA_IO_PENDING)
            CloseConnection(iocp_context_pointer, socket_handle);
    }

    // =========================================================================
    // 关闭连接
    // =========================================================================

    void CloseConnection(byte* iocp_context_pointer, nint socket_handle)
    {
        ((UHttpContext*)(iocp_context_pointer + OFFSET_HTTP_CONTEXT))->Clear();
        WindowsAPI.closesocket(socket_handle);

        int response_slot = *(int*)(iocp_context_pointer + OFFSET_RESPONSE_BUFFER_INDEX);
        ReleaseSlot(response_slot);
        *(int*)(iocp_context_pointer + OFFSET_RESPONSE_BUFFER_INDEX) = -1;

        ReleaseCtxSlot(*(int*)(iocp_context_pointer + OFFSET_CONTEXT_SLOT));
    }

    // =========================================================================
    // iocp_context 内存管理
    // =========================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    byte* AllocCtx(int operation_type, nint socket_handle)
    {
        int slot_index = AcquireCtxSlot();
        if (slot_index < 0) return null;

        byte* p = iocp_context_pool_pointer + (nuint)slot_index * (nuint)iocp_context_total_size;
        NativeMemory.Clear(p, (nuint)iocp_context_total_size);

        *(int*) (p + OFFSET_OPERATION_TYPE)        = operation_type;
        *(nint*)(p + OFFSET_SOCKET)                = socket_handle;
        *(int*) (p + OFFSET_RESPONSE_BUFFER_INDEX) = -1;
        *(int*) (p + OFFSET_CONTEXT_SLOT)          = slot_index;
        return p;
    }

    // =========================================================================
    // 槽位管理（SpinLock 保护，多 WorkerThread 并发安全）
    // =========================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int AcquireFromStack(int* stack, ref SpinLock spinLock)
    {
        bool taken = false;
        spinLock.Enter(ref taken);
        try
        {
            int top = stack[0];
            if (top <= 0) return -1;
            stack[0] = top - 1;
            return stack[top];
        }
        finally { if (taken) spinLock.Exit(useMemoryBarrier: false); }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void ReleaseToStack(int* stack, ref SpinLock spinLock, int slot_index)
    {
        if (slot_index < 0) return;
        bool taken = false;
        spinLock.Enter(ref taken);
        try
        {
            int top  = stack[0] + 1;
            stack[0] = top;
            stack[top] = slot_index;
        }
        finally { if (taken) spinLock.Exit(useMemoryBarrier: false); }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int  AcquireSlot()                  => AcquireFromStack(response_buffer_slot_stack_pointer, ref response_buffer_slot_lock);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ReleaseSlot(int slot_index)    => ReleaseToStack(response_buffer_slot_stack_pointer, ref response_buffer_slot_lock, slot_index);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int  AcquireCtxSlot()               => AcquireFromStack(iocp_context_slot_stack_pointer, ref iocp_context_slot_lock);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ReleaseCtxSlot(int slot_index) => ReleaseToStack(iocp_context_slot_stack_pointer, ref iocp_context_slot_lock, slot_index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    byte* SlotToPtr(int slot_index)
        => response_buffer_pool_pointer + (nuint)slot_index * (nuint)ServerConfig->RESPONSE_BUFFER_CAPACITY;

    // =========================================================================
    // 动态获取 AcceptEx 函数指针
    // =========================================================================

    static delegate* unmanaged[Stdcall]<nint, nint, void*, uint, uint, uint, uint*, OVERLAPPED*, bool>
        ResolveAcceptEx(nint socket_handle)
    {
        Guid  wsaId            = IOCPLibrary.WSAID_ACCEPTEX;
        nint  function_pointer = 0;
        uint  returned_bytes   = 0;

        int r = WindowsAPI.WSAIoctl(
            socket_handle,
            IOCPLibrary.SIO_GET_EXTENSION_FUNCTION_POINTER,
            &wsaId, (uint)sizeof(Guid),
            &function_pointer, (uint)sizeof(nint),
            &returned_bytes, null, 0);

        if (r == IOCPLibrary.SOCKET_ERROR) Fail("WSAIoctl(AcceptEx)");

        return (delegate* unmanaged[Stdcall]<nint, nint, void*, uint, uint, uint, uint*, OVERLAPPED*, bool>)
            function_pointer;
    }

 

    // =========================================================================
    // 释放资源
    // =========================================================================

    internal bool Dispose()
    {
        if (is_disposed) return false;
        is_disposed = true;

        if (listening_socket_handle != IOCPLibrary.INVALID_SOCKET)
        {
            WindowsAPI.closesocket(listening_socket_handle);
            listening_socket_handle = IOCPLibrary.INVALID_SOCKET;
        }

        if (io_completion_port_handle != 0)
        {
            WindowsAPI.CloseHandle((char*)io_completion_port_handle);
            io_completion_port_handle = 0;
        }

        WindowsAPI.WSACleanup();

        if (response_buffer_pool_pointer != null)
        {
            NativeMemory.Free(response_buffer_pool_pointer);
            response_buffer_pool_pointer = null;
        }
        if (request_buffer_pool_pointer != null)
        {
            NativeMemory.Free(request_buffer_pool_pointer);
            request_buffer_pool_pointer = null;
        }
        if (response_buffer_slot_stack_pointer != null)
        {
            NativeMemory.Free(response_buffer_slot_stack_pointer);
            response_buffer_slot_stack_pointer = null;
        }
        if (iocp_context_pool_pointer != null)
        {
            NativeMemory.Free(iocp_context_pool_pointer);
            iocp_context_pool_pointer = null;
        }
        if (iocp_context_slot_stack_pointer != null)
        {
            NativeMemory.Free(iocp_context_slot_stack_pointer);
            iocp_context_slot_stack_pointer = null;
        }

        return true;
    }

    static void Fail(ReadOnlySpan<char> msg)
    {
        Console.Error.WriteLine($"[Fatal] {msg}  LastError={WindowsAPI.GetLastError()}");
        Environment.Exit(1);
    }
}