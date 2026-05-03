/*
 * IoUringServer — 基于 io_uring 的零 GC 异步 HTTP 服务器（Linux 专用）
 *
 * ┌─────────────────────────────────────────────────────────────────────────────┐
 * │  整体工作流程 / Overall Workflow                                              │
 * ├─────────────────────────────────────────────────────────────────────────────┤
 * │  1. 构造阶段 Constructor                                                     │
 * │     · 校验 ServerConfig 参数合法性                                            │
 * │     · 初始化 io_uring ring，映射 SQ/CQ/SQE 内存                              │
 * │     · SliceConnectionPools 统一切片：响应缓冲区、接收缓冲区、发送进度等各池     │
 * │     · 可选：注册 fixed send buffer、启用 SQPOLL、multishot accept            │
 * │                                                                             │
 * │  2. 事件循环 EventLoop                                                       │
 * │     · io_uring_enter 等待 CQE，DrainCQ 批量消费                             │
 * │     · tag_accept_operation  → 建立新连接，提交首次 RECV                      │
 * │     · tag_receive_operation → 数据到达，调用业务逻辑，提交 SEND              │
 * │       · 读满接收缓冲区容量 → 关闭连接（请求超限）                             │
 * │     · tag_send_operation    → 发送完成，keep-alive 或关闭                   │
 * │     · tag_close_operation   → 释放连接资源                                  │
 * │     · tag_stop_operation    → 收到 Stop() 信号，退出循环                     │
 * │                                                                             │
 * │  3. 接收缓冲区                                                               │
 * │     · 每个连接独占 READ_BUFFER_CAPACITY 字节的固定内存段                      │
 * │     · 由 SliceConnectionPools 统一分配，SubmitRecv 以视图方式挂载到           │
 * │       context->RequestHeader，无动态分配，无扩容                              │
 * │                                                                             │
 * │  4. 发送路径                                                                 │
 * │     · 优先 SEND_ZC（零拷贝），不支持时自动回退到 SEND                         │
 * │     · 可选 fixed buffer 注册加速响应缓冲区发送                               │
 * └─────────────────────────────────────────────────────────────────────────────┘
 */



namespace Solamirare;



/// <summary>
/// io_uring-based HTTP server.
/// 基于 io_uring 的 HTTP 服务。
/// </summary>
public unsafe ref struct IoUringServer
{
    /// <summary>默认 io_uring 队列深度。</summary>
    const uint default_queue_depth_entries = 256;

    /// <summary>过载接入控制预留的连接余量。</summary>
    const int accept_overload_headroom_count = 64;
    /// <summary>允许在栈上分配连接池的最大字节数。</summary>
    const nuint maximum_stack_pool_bytes = 1024 * 1024;

    /// <summary>发送路径使用的 fixed buffer 索引。</summary>
    const ushort fixed_send_buffer_index = 0;

    /// <summary>accept 完成事件的 user_data 标签。</summary>
    const ulong tag_accept_operation = 1UL << 56;
    /// <summary>receive 完成事件的 user_data 标签。</summary>
    const ulong tag_receive_operation = 2UL << 56;
    /// <summary>send 完成事件的 user_data 标签。</summary>
    const ulong tag_send_operation = 3UL << 56;
    /// <summary>close 完成事件的 user_data 标签。</summary>
    const ulong tag_close_operation = 4UL << 56;
    /// <summary>stop 事件的 user_data 标签。</summary>
    const ulong tag_stop_operation = 5UL << 56;
    /// <summary>从 user_data 提取标签位的掩码。</summary>
    const ulong tag_mask_value = 0xFFUL << 56;
    /// <summary>从 user_data 提取文件描述符位的掩码。</summary>
    const ulong file_descriptor_mask_value = ~(0xFFUL << 56);

    static ulong MakeUserData(ulong tag, int file_descriptor) => tag | (ulong)(uint)file_descriptor;

    static ulong GetTag(ulong user_data) => user_data & tag_mask_value;

    static int GetFileDescriptor(ulong user_data) => (int)(user_data & file_descriptor_mask_value);

    byte* request_buffer_pool_pointer;

    byte* response_buffer_pool_pointer;

    /// <summary>发送进度池指针。</summary>
    uint* send_progress_pool_pointer;

    /// <summary>响应长度池指针。</summary>
    uint* response_length_pool_pointer;

    /// <summary>keep-alive 标记池指针。</summary>
    byte* keep_alive_pool_pointer;

    /// <summary>HttpContext 池指针。</summary>
    UHttpContext* http_context_pool_pointer;

    /// <summary>持有的堆内存池块指针。</summary>
    byte* pool_block_pointer;


    /// <summary>持有的堆内存池块字节数。</summary>
    nuint pool_block_size_bytes;

    /// <summary>
    /// io_uring ring 文件描述符。
    /// 初始化为 -1，Dispose() 用 >= 0 判断，避免 fd=0 被误关闭（标准输入）。
    /// </summary>
    int ring_file_descriptor;

    /// <summary>映射的提交队列 ring 指针。</summary>
    void* submission_queue_ring_pointer;

    /// <summary>映射的提交队列 ring 字节数。</summary>
    nuint submission_queue_ring_size_bytes;

    /// <summary>映射的完成队列 ring 指针。</summary>
    void* completion_queue_ring_pointer;

    /// <summary>映射的完成队列 ring 字节数。</summary>
    nuint completion_queue_ring_size_bytes;

    /// <summary>映射的 SQE 数组指针。</summary>
    io_uring_sqe* submission_queue_entries_pointer;

    /// <summary>映射的 SQE 数组字节数。</summary>
    nuint submission_queue_entries_size_bytes;

    /// <summary>提交队列 head 指针。</summary>
    uint* submission_queue_head_pointer;

    /// <summary>提交队列 tail 指针。</summary>
    uint* submission_queue_tail_pointer;

    /// <summary>提交队列 mask 指针。</summary>
    uint* submission_queue_mask_pointer;

    /// <summary>提交队列索引数组指针。</summary>
    uint* submission_queue_array_pointer;

    /// <summary>提交队列 flags 指针。</summary>
    uint* submission_queue_flags_pointer;

    /// <summary>提交队列 entries 数量指针。</summary>
    uint* submission_queue_entries_count_pointer;

    /// <summary>完成队列 head 指针。</summary>
    uint* completion_queue_head_pointer;

    /// <summary>完成队列 tail 指针。</summary>
    uint* completion_queue_tail_pointer;

    /// <summary>完成队列 mask 指针。</summary>
    uint* completion_queue_mask_pointer;

    /// <summary>完成队列 CQE 数组指针。</summary>
    io_uring_cqe* completion_queue_entries_pointer;

    /// <summary>事件循环停止标记。</summary>
    volatile bool stopping_requested;

    /// <summary>监听套接字文件描述符。</summary>
    int listening_socket_file_descriptor;

    /// <summary>用于唤醒循环的 eventfd。</summary>
    int stop_event_file_descriptor;

    /// <summary>eventfd 读取缓冲区。</summary>
    ulong stop_event_buffer;

    /// <summary>生效队列深度。</summary>
    uint queue_depth_entries;

    /// <summary>
    /// User business callback pointer / 用户业务回调指针。
    /// 回调函数内部不得调用 context->Clear() 或 context->Dispose()。
    /// </summary>
    delegate*<UHttpContext*, bool> user_logic_callback;

    /// <summary>是否启用提交队列轮询。</summary>
    bool submission_queue_polling_active;

    /// <summary>是否启用 fixed buffer 模式。</summary>
    bool fixed_buffers_active;

    /// <summary>是否启用零拷贝发送。</summary>
    bool zero_copy_send_active;

    /// <summary>是否启用 multishot accept。</summary>
    bool accept_multishot_active;

    /// <summary>待提交请求数量。</summary>
    uint submit_pending_count;

    /// <summary>当前活跃连接数。</summary>
    uint active_connections_count;

    /// <summary>最大活跃连接数。</summary>
    uint max_active_connections_count;

    /// <summary>槽位 owner 表指针。</summary>
    int* slot_owner_pool;

    /// <summary>零拷贝通知数量。</summary>
    ulong zero_copy_notification_count;

    /// <summary>零拷贝通知错误数量。</summary>
    ulong zero_copy_notification_error_count;

    /// <summary>协议错误日志计数器。</summary>
    uint log_protocol_error_counter;

    /// <summary>零拷贝通知错误日志计数器。</summary>
    uint log_zero_copy_notification_error_counter;

    /// <summary>零拷贝瞬时错误日志计数器。</summary>
    uint log_zero_copy_transient_error_counter;

    /// <summary>连接槽位冲突日志计数器。</summary>
    uint log_connection_slot_counter;

    /// <summary>提交队列轮询警告日志计数器。</summary>
    uint log_submission_queue_polling_counter;

    /// <summary>过载日志计数器。</summary>
    uint log_overload_counter;

    /// <summary>诊断 CQE 计数器。</summary>
    ulong diagnostic_cqe_counter;

    // Dispose 幂等保护
    bool is_disposed;

    /// <summary>低负载提交批大小。</summary>
    const uint submit_batch_size_low = 8;

    /// <summary>中负载提交批大小。</summary>
    const uint submit_batch_size_mid = 32;

    /// <summary>高负载提交批大小。</summary>
    const uint submit_batch_size_high = 64;

    /// <summary>日志采样节流掩码。</summary>
    const uint log_sample_mask = 0x3F;

    /// <summary>周期诊断输出的 CQE 间隔。</summary>
    const ulong diagnostic_print_every_cqe = 32768;

    byte* RequestBufferFor(int file_descriptor) => request_buffer_pool_pointer + (file_descriptor % ServerConfig->MAX_CONNECTIONS) * ServerConfig->READ_BUFFER_CAPACITY;

    byte* ResponseBufferFor(int file_descriptor) => response_buffer_pool_pointer + (file_descriptor % ServerConfig->MAX_CONNECTIONS) * ServerConfig->RESPONSE_BUFFER_CAPACITY;

    uint* SendProgressFor(int file_descriptor) => send_progress_pool_pointer + (file_descriptor % ServerConfig->MAX_CONNECTIONS);

    uint* ResponseLengthFor(int file_descriptor) => response_length_pool_pointer + (file_descriptor % ServerConfig->MAX_CONNECTIONS);

    byte* KeepAliveFor(int file_descriptor) => keep_alive_pool_pointer + (file_descriptor % ServerConfig->MAX_CONNECTIONS);

    int* SlotOwnerFor(int file_descriptor) => slot_owner_pool + (file_descriptor % ServerConfig->MAX_CONNECTIONS);

    UHttpContext* ContextFor(int file_descriptor) => http_context_pool_pointer + (file_descriptor % ServerConfig->MAX_CONNECTIONS);

    bool IsSlotOwnedBy(int file_descriptor) => *SlotOwnerFor(file_descriptor) == file_descriptor;

    /// <summary>
    /// 当前服务器实例使用的配置指针。
    /// </summary>
    public HTTPSeverConfig* ServerConfig;

    /// <summary>
    /// 初始化一个基于 io_uring 的 HTTP 服务器实例。
    /// </summary>
    /// <param name="config">服务器配置指针。</param>
    public void Init(HTTPSeverConfig* config, IoUringOptimizationOptions options)
    {
        ServerConfig = config;

        // 初始化为 -1，避免 Dispose() 误判 fd=0 为有效并关闭 stdin
        ring_file_descriptor = -1;
        listening_socket_file_descriptor = -1;
        stop_event_file_descriptor = -1;

                submission_queue_polling_active = options.enable_submission_queue_polling;
        fixed_buffers_active = options.enable_fixed_buffers_registration;
        zero_copy_send_active = options.enable_zero_copy_send;

        queue_depth_entries = options.queue_depth_entries == 0
                                        ? default_queue_depth_entries
                                        : options.queue_depth_entries;

        if (options.queue_depth_entries == 0)
            Console.WriteLine($"[queue_depth_entries] invalid value=0, fallback to default={default_queue_depth_entries}.");

    }

    /// <summary>
    /// 热路径日志采样门控（单线程，无需原子操作）。
    /// </summary>
    static bool ShouldSampleLog(ref uint counter)
    {
        counter++;
        return (counter & log_sample_mask) == 1;
    }


    /// <summary>
    /// 连接池切片偏移的向上对齐工具。
    /// </summary>
    static nuint AlignUp(nuint value, nuint alignment)
        => (value + alignment - 1) & ~(alignment - 1);

    /// <summary>
    /// 统一的连接池切片方法。
    ///
    /// 双模式运行，由 base_pointer 决定：
    ///   · base_pointer == null → dry-run：只走对齐+累加逻辑，返回所需总字节数，不写任何字段。
    ///   · base_pointer != null → 正式切片：将各池指针字段赋值到对应偏移，同样返回消耗字节数。
    ///
    /// 新增或删除连接池字段时只需修改此一处，Calculate 和 Allocate 两条路径自动同步。
    /// </summary>
    nuint SliceConnectionPools(byte* base_pointer)
    {
        nuint align = (nuint)sizeof(nuint);
        nuint offset = 0;

        offset = AlignUp(offset, align);
        if (base_pointer != null) request_buffer_pool_pointer = base_pointer + offset;
        offset += (nuint)ServerConfig->MAX_CONNECTIONS * (nuint)ServerConfig->READ_BUFFER_CAPACITY;

        offset = AlignUp(offset, align);
        if (base_pointer != null) response_buffer_pool_pointer = base_pointer + offset;
        offset += (nuint)(ServerConfig->MAX_CONNECTIONS * ServerConfig->RESPONSE_BUFFER_CAPACITY);

        offset = AlignUp(offset, align);
        if (base_pointer != null) send_progress_pool_pointer = (uint*)(base_pointer + offset);
        offset += (nuint)(ServerConfig->MAX_CONNECTIONS * sizeof(uint));

        offset = AlignUp(offset, align);
        if (base_pointer != null) response_length_pool_pointer = (uint*)(base_pointer + offset);
        offset += (nuint)(ServerConfig->MAX_CONNECTIONS * sizeof(uint));

        offset = AlignUp(offset, align);
        if (base_pointer != null) keep_alive_pool_pointer = base_pointer + offset;
        offset += (nuint)ServerConfig->MAX_CONNECTIONS;

        offset = AlignUp(offset, align);
        if (base_pointer != null) http_context_pool_pointer = (UHttpContext*)(base_pointer + offset);
        offset += (nuint)(ServerConfig->MAX_CONNECTIONS * sizeof(UHttpContext));

        offset = AlignUp(offset, align);
        if (base_pointer != null) slot_owner_pool = (int*)(base_pointer + offset);
        offset += (nuint)(ServerConfig->MAX_CONNECTIONS * sizeof(int));

        return offset;
    }


    /// <summary>
    /// 将调用方传入的大内存块切片到各连接池字段。
    /// </summary>
    void AllocateConnectionPools(byte* base_pointer, nuint total_bytes)
    {
        nuint required_bytes = SliceConnectionPools(null);
        if (total_bytes < required_bytes)
            throw new InvalidOperationException($"Pool block too small. required={required_bytes}, actual={total_bytes}.");

        SliceConnectionPools(base_pointer);
    }

    /// <summary>
    /// 启动服务，可同时指定业务回调和优化开关。
    /// </summary>
    public void Start(delegate*<UHttpContext*, bool> user_logic)
    {
        // ServerConfig 参数合法性校验
        if (ServerConfig == null)
            throw new ArgumentNullException("ServerConfig");
        if (ServerConfig->MAX_CONNECTIONS <= 0)
            throw new ArgumentException($"MAX_CONNECTIONS must be > 0, got {ServerConfig->MAX_CONNECTIONS}");
        if (ServerConfig->READ_BUFFER_CAPACITY <= 0)
            throw new ArgumentException($"READ_BUFFER_CAPACITY must be > 0, got {ServerConfig->READ_BUFFER_CAPACITY}");
        if (ServerConfig->RESPONSE_BUFFER_CAPACITY <= 0)
            throw new ArgumentException($"RESPONSE_BUFFER_CAPACITY must be > 0, got {ServerConfig->RESPONSE_BUFFER_CAPACITY}");
        if (ServerConfig->Port is <= 0 or > 65535)
            throw new ArgumentException($"Port must be 1-65535, got {ServerConfig->Port}");

        // Resolve switches
        user_logic_callback = user_logic;

        accept_multishot_active = true;
        submit_pending_count = 0;
        active_connections_count = 0;

        int max_active = ServerConfig->MAX_CONNECTIONS - accept_overload_headroom_count;
        if (max_active <= 0) max_active = ServerConfig->MAX_CONNECTIONS;
        max_active_connections_count = (uint)max_active;



        nuint pool_bytes = SliceConnectionPools(null);
        byte* pool_pointer;

        if (pool_bytes <= maximum_stack_pool_bytes && pool_bytes <= int.MaxValue)
        {
            // stackalloc 基地址未必是 64 字节对齐。
            // 多分配 63 字节余量，手动将指针对齐到 64 字节缓存行边界。
            
            byte* raw_stack = stackalloc byte[(int)pool_bytes + 63];
            nuint raw_address = (nuint)raw_stack;
            nuint aligned = (raw_address + 63) & ~(nuint)63;
            pool_pointer = (byte*)aligned;
            NativeMemory.Clear(pool_pointer, pool_bytes);
            pool_block_pointer = null;
            pool_block_size_bytes = 0;
            Console.WriteLine($"[Pool] use stackalloc bytes={pool_bytes} (64-byte aligned).");
        }
        else
        {
            // NativeMemory.AllocZeroed 保证至少 64 字节对齐
            pool_block_pointer = (byte*)NativeMemory.AllocZeroed(pool_bytes);
            pool_block_size_bytes = pool_bytes;
            pool_pointer = pool_block_pointer;
            Console.WriteLine($"[Pool] use heap alloc bytes={pool_bytes}.");
        }

        AllocateConnectionPools(pool_pointer, pool_bytes);
        for (int i = 0; i < ServerConfig->MAX_CONNECTIONS; i++)
            slot_owner_pool[i] = -1;


        SetupIoUring();
        TryRegisterFixedBuffers();

        listening_socket_file_descriptor = CreateListenSocket(ServerConfig->Port);

        // Create and register eventfd for stop signaling
        stop_event_file_descriptor = CreateStopEventFd();
        if (!SubmitStopEventWait()) throw new InvalidOperationException("Failed to submit stop event wait.");

        if (!SubmitAccept(listening_socket_file_descriptor))
            throw new InvalidOperationException("Failed to submit initial ACCEPT SQE due to SQ saturation.");
        SubmitPending(force: true);

        ServerFunctions.ConsoleStartedStatus("IO_Uring HTTP Server", ServerConfig);

        Console.WriteLine(
            $"ring_fd={ring_file_descriptor}, queue_depth={queue_depth_entries}," +
            $"sqpoll={submission_queue_polling_active}, fixed_buffers={fixed_buffers_active}, zc={zero_copy_send_active}");

        EventLoop();

    }

    /// <summary>
    /// 请求服务停止；事件循环将在下一次迭代退出。
    /// </summary>
    public void Stop()
    {
        stopping_requested = true;
        if (stop_event_file_descriptor >= 0)
        {
            ulong val = 1;
            LinuxAPI.eventfd_write(stop_event_file_descriptor, val);
        }
    }

    /// <summary>
    /// 每个系统调用均检查返回值，失败时抛出异常而非静默继续。
    /// </summary>
    int CreateListenSocket(int port)
    {
        int file_descriptor = LinuxAPI.socket(IoUringLibcs.AF_INET, IoUringLibcs.SOCK_STREAM, 0);
        if (file_descriptor < 0)
            throw new InvalidOperationException($"socket() failed, errno={-file_descriptor}");

        int one = 1;
        int result;

        result = LinuxAPI.setsockopt(file_descriptor, IoUringLibcs.SOL_SOCKET, IoUringLibcs.SO_REUSEADDR, &one, sizeof(int));
        if (result < 0)
        {
            LinuxAPI.close(file_descriptor);
            throw new InvalidOperationException($"setsockopt(SO_REUSEADDR) failed, errno={-result}");
        }

        result = LinuxAPI.setsockopt(file_descriptor, IoUringLibcs.SOL_SOCKET, IoUringLibcs.SO_REUSEPORT, &one, sizeof(int));
        if (result < 0)
        {
            LinuxAPI.close(file_descriptor);
            throw new InvalidOperationException($"setsockopt(SO_REUSEPORT) failed, errno={-result}");
        }

        sockaddr_in address = default;
        address.sin_family = IoUringLibcs.AF_INET;
        address.sin_port = ServerFunctions.Htons((ushort)port);

        result = LinuxAPI.bind(file_descriptor, &address, (uint)sizeof(sockaddr_in));
        if (result < 0)
        {
            LinuxAPI.close(file_descriptor);
            throw new InvalidOperationException($"bind() failed on port {port}, errno={-result}");
        }

        result = LinuxAPI.listen(file_descriptor, IoUringLibcs.SOMAXCONN);
        if (result < 0)
        {
            LinuxAPI.close(file_descriptor);
            throw new InvalidOperationException($"listen() failed, errno={-result}");
        }

        return file_descriptor;
    }

    /// <summary>创建用于停止信号的 eventfd。</summary>
    int CreateStopEventFd()
    {
        int file_descriptor = LinuxAPI.eventfd(0, 0);
        if (file_descriptor < 0) throw new InvalidOperationException($"eventfd() failed: {file_descriptor}");
        return file_descriptor;
    }

    /// <summary>提交 eventfd 读取请求。</summary>
    bool SubmitStopEventWait()
    {
        if (!TryAcquireSqe(out io_uring_sqe* submission_queue_entry, out uint index))
            return false;
        NativeMemory.Clear(submission_queue_entry, (nuint)sizeof(io_uring_sqe));

        // ref struct fields are on stack, taking address is valid for the duration of Start().
        fixed (ulong* buffer_pointer = &stop_event_buffer)
        {
            submission_queue_entry->opcode = IoUringLibcs.IORING_OP_READ;
            submission_queue_entry->fd = stop_event_file_descriptor;
            submission_queue_entry->addr = (ulong)buffer_pointer;
            submission_queue_entry->len = sizeof(ulong);
            submission_queue_entry->user_data = tag_stop_operation;
        }

        FlushSqe(index);
        return true;
    }

    /// <summary>Submit RECV / 提交 RECV。</summary>
    bool SubmitRecv(int file_descriptor)
    {
        UHttpContext* context = ContextFor(file_descriptor);

        // 首次提交（或 keep-alive Clear() 之后重新进入）时，将固定接收缓冲区包装为视图。
        // On first submit or after keep-alive Clear(), wrap the fixed receive buffer as a view.
        if (!context->RequestHeader.Activated)
        {
            byte* request_buffer = RequestBufferFor(file_descriptor);
            context->RequestHeader.Init(request_buffer, ServerConfig->READ_BUFFER_CAPACITY, 0, MemoryTypeDefined.Heap);
        }

        // 计算当前可写空间；已满则不提交（调用方负责关闭连接）。
        int can_read = (int)(ServerConfig->READ_BUFFER_CAPACITY - context->RequestHeader.UsageSize);
        if (can_read <= 0) return false;

        if (!TryAcquireSqe(out io_uring_sqe* submission_queue_entry, out uint index))
            return false;
        NativeMemory.Clear(submission_queue_entry, (nuint)sizeof(io_uring_sqe));
        submission_queue_entry->opcode = IoUringLibcs.IORING_OP_RECV;
        submission_queue_entry->fd = file_descriptor;
        submission_queue_entry->addr = (ulong)(context->RequestHeader.Pointer + context->RequestHeader.UsageSize);
        submission_queue_entry->len = (uint)can_read;
        submission_queue_entry->user_data = MakeUserData(tag_receive_operation, file_descriptor);
        FlushSqe(index);
        return true;
    }

    /// <summary>
    /// 提交发送（优先 ZC），并支持按连接跟踪发送进度（处理短写）。
    /// remain==0 时直接返回 true，不提交 len=0 的 SQE（行为未定义）。
    /// </summary>
    bool SubmitSend(int file_descriptor)
    {
        uint response_length = *ResponseLengthFor(file_descriptor);
        uint sent = *SendProgressFor(file_descriptor);

        // sent > response_length 说明发送进度溢出，是上游逻辑 bug。
        // 正确做法：记录错误并拒绝发送，由调用方关闭连接。
        if (sent > response_length)
        {
            if (ShouldSampleLog(ref log_protocol_error_counter))
                Console.WriteLine($"[Anomaly] send progress overflow fd={file_descriptor} sent={sent} respLen={response_length}, closing. sampled_count={log_protocol_error_counter}");
            return false;
        }

        uint remain = response_length - sent;

        // 若无数据需要发送，直接告知调用方"成功"（无需 SQE）
        if (remain == 0) return true;

        if (!TryAcquireSqe(out io_uring_sqe* submission_queue_entry, out uint index))
            return false;
        NativeMemory.Clear(submission_queue_entry, (nuint)sizeof(io_uring_sqe));

        if (zero_copy_send_active)
        {
            submission_queue_entry->opcode = IoUringLibcs.IORING_OP_SEND_ZC;
            submission_queue_entry->fd = file_descriptor;
            submission_queue_entry->addr = (ulong)(ResponseBufferFor(file_descriptor) + sent);
            submission_queue_entry->len = remain;
            submission_queue_entry->rw_flags = IoUringLibcs.MSG_NOSIGNAL;
            if (fixed_buffers_active)
            {
                submission_queue_entry->ioprio = IoUringLibcs.IORING_RECVSEND_FIXED_BUF;
                submission_queue_entry->buf_index = fixed_send_buffer_index;
            }
            submission_queue_entry->user_data = MakeUserData(tag_send_operation, file_descriptor);
            FlushSqe(index);
            return true;
        }

        submission_queue_entry->opcode = IoUringLibcs.IORING_OP_SEND;
        submission_queue_entry->fd = file_descriptor;
        submission_queue_entry->addr = (ulong)(ResponseBufferFor(file_descriptor) + sent);
        submission_queue_entry->len = remain;
        submission_queue_entry->rw_flags = IoUringLibcs.MSG_NOSIGNAL;
        if (fixed_buffers_active)
        {
            submission_queue_entry->ioprio = IoUringLibcs.IORING_RECVSEND_FIXED_BUF;
            submission_queue_entry->buf_index = fixed_send_buffer_index;
        }
        submission_queue_entry->user_data = MakeUserData(tag_send_operation, file_descriptor);
        FlushSqe(index);
        return true;
    }

    /// <summary>提交 ACCEPT。</summary>
    bool SubmitAccept(int file_descriptor)
    {
        if (!TryAcquireSqe(out io_uring_sqe* submission_queue_entry, out uint index))
            return false;
        NativeMemory.Clear(submission_queue_entry, (nuint)sizeof(io_uring_sqe));
        submission_queue_entry->opcode = IoUringLibcs.IORING_OP_ACCEPT;
        submission_queue_entry->fd = file_descriptor;
        if (accept_multishot_active)
            submission_queue_entry->ioprio = IoUringLibcs.IORING_ACCEPT_MULTISHOT;
        submission_queue_entry->user_data = MakeUserData(tag_accept_operation, file_descriptor);
        FlushSqe(index);
        return true;
    }

    /// <summary>提交 CLOSE。</summary>
    bool SubmitClose(int file_descriptor)
    {
        if (!TryAcquireSqe(out io_uring_sqe* submission_queue_entry, out uint index))
            return false;
        NativeMemory.Clear(submission_queue_entry, (nuint)sizeof(io_uring_sqe));
        submission_queue_entry->opcode = IoUringLibcs.IORING_OP_CLOSE;
        submission_queue_entry->fd = file_descriptor;
        submission_queue_entry->user_data = MakeUserData(tag_close_operation, file_descriptor);
        FlushSqe(index);
        return true;
    }

    /// <summary>
    /// 过载路径直接执行同步 close。
    /// </summary>
    void CloseClientImmediate(int file_descriptor, string reason)
    {
        int* owner = SlotOwnerFor(file_descriptor);
        if (*owner == file_descriptor)
        {
            *owner = -1;
            // 防止 uint 下溢
            if (active_connections_count > 0)
                active_connections_count--;
            else
                Console.WriteLine($"[Anomaly] active_connections_count underflow avoided, fd={file_descriptor}");
            *SendProgressFor(file_descriptor) = 0;
            *ResponseLengthFor(file_descriptor) = 0;
            *KeepAliveFor(file_descriptor) = 0;
            ContextFor(file_descriptor)->Clear();
        }
        LinuxAPI.shutdown(file_descriptor, IoUringLibcs.SHUT_WR);
        LinuxAPI.close(file_descriptor);
        if (ShouldSampleLog(ref log_overload_counter))
            Console.WriteLine($"[Overload] immediate close fd={file_descriptor}, reason={reason}, sampled_count={log_overload_counter}.");
    }

    /// <summary>
    /// 尝试注册 fixed buffers（失败自动回退）。
    /// </summary>
    void TryRegisterFixedBuffers()
    {
        if (!fixed_buffers_active) return;

        // 仅注册响应缓冲区作为 fixed send buffer。
        // Only the response buffer pool is registered as the fixed send buffer.
        // Optimize: Use stackalloc for iovec, no need to keep it after registration.
        iovec* iovecs = stackalloc iovec[1];
        iovecs[0].iov_base = response_buffer_pool_pointer;
        iovecs[0].iov_len = (nuint)(ServerConfig->MAX_CONNECTIONS * ServerConfig->RESPONSE_BUFFER_CAPACITY);

        int register_result = IoUringLibcs.io_uring_register(ring_file_descriptor, IoUringLibcs.IORING_REGISTER_BUFFERS, iovecs, 1);
        if (register_result < 0)
        {
            int err = -register_result;
            string err_text = Marshal.PtrToStringAnsi((nint)LinuxAPI.strerror(err)) ?? "unknown";
            Console.WriteLine($"[FixedBuffers] register failed res={register_result} errno={err} ({err_text}), fallback to non-fixed path.");
            fixed_buffers_active = false;
            return;
        }

        Console.WriteLine($"[FixedBuffers] register ok send_buf_index={fixed_send_buffer_index}.");
    }

    /// <summary>
    /// Main event loop / 事件主循环。
    /// 执行顺序:
    /// 1. Submit pending SQEs (and wait for events if SQPOLL is disabled).
    /// 2. Consume completed CQEs from the Completion Queue.
    /// 3. Process events (Accept, Recv, Send, Close) and generate new SQEs.
    /// 4. Repeat until stop is requested.
    /// </summary>
    void EventLoop()
    {
        while (!stopping_requested)
        {
            if (submission_queue_polling_active)
            {
                SubmitPending(force: true);
                IoUringLibcs.io_uring_enter(ring_file_descriptor, 0, 1, IoUringLibcs.IORING_ENTER_GETEVENTS);
            }
            else
            {
                // Optimize: Merge submit and wait into a single syscall
                IoUringLibcs.io_uring_enter(ring_file_descriptor, submit_pending_count, 1, IoUringLibcs.IORING_ENTER_GETEVENTS);
                submit_pending_count = 0;
            }
            DrainCQ();
        }
    }

    /// <summary>CQ drain + dispatch / 消费 CQ 并分发。</summary>
    void DrainCQ()
    {
        uint head = *completion_queue_head_pointer;
        uint tail = *completion_queue_tail_pointer;

        while (head != tail)
        {
            io_uring_cqe* completion_queue_entry = completion_queue_entries_pointer + (head & *completion_queue_mask_pointer);
            ulong tag = GetTag(completion_queue_entry->user_data);
            int file_descriptor = GetFileDescriptor(completion_queue_entry->user_data);
            int result = completion_queue_entry->res;
            head++;

            if (tag == tag_accept_operation)
            {
                if (result >= 0)
                {
                    int client_file_descriptor = result;
                    if (active_connections_count >= max_active_connections_count)
                    {
                        CloseClientImmediate(client_file_descriptor, "accept-overload-throttle");
                    }
                    else
                    {
                        int* owner = SlotOwnerFor(client_file_descriptor);
                        if (*owner != -1 && *owner != client_file_descriptor)
                        {
                            if (ShouldSampleLog(ref log_connection_slot_counter))
                                Console.WriteLine($"[ConnSlot] collision slot={client_file_descriptor % ServerConfig->MAX_CONNECTIONS} old_fd={*owner} new_fd={client_file_descriptor}, close new fd. sampled_count={log_connection_slot_counter}");
                            if (!SubmitClose(client_file_descriptor))
                                CloseClientImmediate(client_file_descriptor, "slot-collision-close-submit-failed");
                        }
                        else
                        {
                            *owner = client_file_descriptor;
                            active_connections_count++;
                            *SendProgressFor(client_file_descriptor) = 0;
                            *ResponseLengthFor(client_file_descriptor) = 0;
                            *KeepAliveFor(client_file_descriptor) = 0;

                            if (!SubmitRecv(client_file_descriptor))
                                CloseClientImmediate(client_file_descriptor, "sq-full-on-initial-recv");
                        }
                    }
                }
                if (accept_multishot_active)
                {
                    if (result < 0)
                    {
                        if (IsFeaturePermanentlyUnsupported(result))
                        {
                            accept_multishot_active = false;
                            Console.WriteLine($"[ACCEPT][MULTISHOT] unsupported res={result}, fallback to single-shot accept.");
                        }
                        if (!SubmitAccept(file_descriptor))
                            Console.WriteLine("[Overload] accept repost skipped because SQ is full; waiting for next capacity window.");
                    }
                    else if ((completion_queue_entry->flags & IoUringLibcs.IORING_CQE_F_MORE) == 0)
                    {
                        if (!SubmitAccept(file_descriptor))
                            Console.WriteLine("[Overload] accept repost skipped because SQ is full; waiting for next capacity window.");
                    }
                }
                else
                {
                    if (!SubmitAccept(file_descriptor))
                        Console.WriteLine("[Overload] accept repost skipped because SQ is full; waiting for next capacity window.");
                }
            }
            else if (tag == tag_receive_operation)
            {
                if (!IsSlotOwnedBy(file_descriptor)) continue;

                if (result > 0)
                {
                    uint bytes_read = (uint)result;
                    UHttpContext* context = ContextFor(file_descriptor);

                    // 通知接收视图内核已写入 bytes_read 字节，更新 UsageSize。
                    // Inform the receive view that the kernel wrote bytes_read bytes; update UsageSize.
                    context->RequestHeader.ReLength(context->RequestHeader.UsageSize + bytes_read);

                    // 读满固定缓冲区容量，说明请求超出限制，关闭连接。
                    // Request exceeded the fixed buffer capacity; close the connection.
                    if (context->RequestHeader.UsageSize >= (uint)ServerConfig->READ_BUFFER_CAPACITY)
                    {
                        if (!SubmitClose(file_descriptor))
                            CloseClientImmediate(file_descriptor, "sq-full-on-recv-overflow");
                        continue;
                    }

                    // 数据已接收完整，进行快速预检。
                    // Data fully received; run fast pre-check.
                    if (!ServerFunctions.IsLikelyHttpRequest(context->RequestHeader.Pointer, context->RequestHeader.UsageSize))
                    {
                        if (ShouldSampleLog(ref log_protocol_error_counter))
                            Console.WriteLine($"[Protocol Error] fast reject non-http fd={file_descriptor} bytes={context->RequestHeader.UsageSize}. sampled_count={log_protocol_error_counter}");
                        if (!SubmitClose(file_descriptor))
                            CloseClientImmediate(file_descriptor, "sq-full-on-close-after-fast-reject");
                        continue;
                    }


                    // 检查请求是否完整，POST Body 可能分多次到达
                    if (!ServerFunctions.IsRequestComplete(context))
                    {
                        // 请求不完整，继续接收
                        if (!SubmitRecv(file_descriptor))
                            CloseClientImmediate(file_descriptor, "sq-full-on-incomplete-request-recv");
                        continue;
                    }


                    *SendProgressFor(file_descriptor) = 0;
                    if (ProcessRequestWithContext(file_descriptor))
                    {
                        if (!SubmitSend(file_descriptor))
                            CloseClientImmediate(file_descriptor, "sq-full-on-send");
                    }
                    else
                    {
                        if (!SubmitClose(file_descriptor))
                            CloseClientImmediate(file_descriptor, "sq-full-on-close-after-process-fail");
                    }
                }
                else if (result == 0)
                {
                    // result==0 表示对端正常关闭连接（FIN），区别于 result<0 的错误情形
                    if (!SubmitClose(file_descriptor))
                        CloseClientImmediate(file_descriptor, "sq-full-on-close-after-eof");
                }
                else
                {
                    // result<0 表示 recv 返回错误，记录 errno 以便排查
                    if (ShouldSampleLog(ref log_protocol_error_counter))
                        Console.WriteLine($"[RECV] error fd={file_descriptor} errno={-result} sampled_count={log_protocol_error_counter}");
                    if (!SubmitClose(file_descriptor))
                        CloseClientImmediate(file_descriptor, "sq-full-on-close-after-recv-error");
                }
            }
            else if (tag == tag_send_operation)
            {
                if (!IsSlotOwnedBy(file_descriptor)) continue;

                if (zero_copy_send_active && ((completion_queue_entry->flags & IoUringLibcs.IORING_CQE_F_NOTIF) != 0))
                {
                    zero_copy_notification_count++;
                    if (result < 0)
                    {
                        zero_copy_notification_error_count++;
                        if (ShouldSampleLog(ref log_zero_copy_notification_error_counter))
                            Console.WriteLine($"[ZC][NOTIF] err res={result}, total={zero_copy_notification_count}, err_total={zero_copy_notification_error_count}, sampled_count={log_zero_copy_notification_error_counter}.");
                    }
                    continue;
                }

                uint* sent_pointer = SendProgressFor(file_descriptor);
                uint* response_length_pointer = ResponseLengthFor(file_descriptor);
                byte* keep_alive_pointer = KeepAliveFor(file_descriptor);

                if (result > 0)
                {
                    uint sent_now = *sent_pointer + (uint)result;
                    uint response_length = *response_length_pointer;
                    *sent_pointer = sent_now;

                    if (sent_now < response_length)
                    {
                        if (!SubmitSend(file_descriptor))
                            CloseClientImmediate(file_descriptor, "sq-full-on-partial-send-continue");
                    }
                    else
                    {
                        *sent_pointer = 0;
                        if (*keep_alive_pointer != 0)
                        {
                            if (!SubmitRecv(file_descriptor))
                                CloseClientImmediate(file_descriptor, "sq-full-on-keepalive-recv");
                        }
                        else
                        {
                            if (!SubmitClose(file_descriptor))
                                CloseClientImmediate(file_descriptor, "sq-full-on-close-after-send");
                        }
                    }
                }
                else if (zero_copy_send_active && IsFeaturePermanentlyUnsupported(result))
                {
                    zero_copy_send_active = false;
                    Console.WriteLine($"[ZC] unsupported res={result}, fallback to SEND.");
                    if (!SubmitSend(file_descriptor))
                        CloseClientImmediate(file_descriptor, "sq-full-on-zc-fallback-send");
                }
                else if (zero_copy_send_active && IsTransientErrno(-result))
                {
                    if (ShouldSampleLog(ref log_zero_copy_transient_error_counter))
                        Console.WriteLine($"[ZC] transient send err res={result}, keep ZC and retry. sampled_count={log_zero_copy_transient_error_counter}");
                    if (!SubmitSend(file_descriptor))
                        CloseClientImmediate(file_descriptor, "sq-full-on-zc-transient-retry");
                }
                else
                {
                    if (!SubmitClose(file_descriptor))
                        CloseClientImmediate(file_descriptor, "sq-full-on-send-error-close");
                }
            }
            else if (tag == tag_close_operation)
            {
                int* owner = SlotOwnerFor(file_descriptor);
                if (*owner == file_descriptor)
                {
                    *owner = -1;
                    // 防止 uint 下溢
                    if (active_connections_count > 0)
                        active_connections_count--;
                    else
                        Console.WriteLine($"[Anomaly] active_connections_count underflow avoided on close, fd={file_descriptor}");
                    *SendProgressFor(file_descriptor) = 0;
                    *ResponseLengthFor(file_descriptor) = 0;
                    *KeepAliveFor(file_descriptor) = 0;
                    ContextFor(file_descriptor)->Clear();
                }
            }
            else if (tag == tag_stop_operation)
            {
                stopping_requested = true;
            }
            else
            {
                // 未知 tag，CQE 已通过 head++ 消费但无人处理，记录日志防止静默丢失。
                // 可能原因：内存损坏、未来新增 opcode 未同步处理、user_data 被意外覆写。
                if (ShouldSampleLog(ref log_protocol_error_counter))
                    Console.WriteLine($"[Anomaly] unknown CQE tag=0x{tag:X} fd={file_descriptor} res={result}, sampled_count={log_protocol_error_counter}");
            }
        }
        *completion_queue_head_pointer = head;
    }

    /// <summary>
    /// 使用 HttpContext 解析请求、执行用户回调并生成可发送响应。
    /// <para>
    /// 契约：user_logic_callback 内部不得调用 context->Clear() 或 context->Dispose()。
    /// Clear() 由本方法的 finally 块统一负责，保证无论成功还是异常都能释放资源。
    /// </para>
    /// <para>
    /// context->TotalResponseLength 在 finally 调用 Clear() 之前读出并写入
    /// response_length_pool 和 keep_alive_pool，之后 Clear() 只清 UHttpContext 元数据，
    /// 不影响已写入预分配 ResponseBufferFor(fd) 物理缓冲区中的响应字节。
    /// </para>
    /// </summary>
    bool ProcessRequestWithContext(int file_descriptor)
    {
        UHttpContext* context = ContextFor(file_descriptor);

        // 在 try 块外声明，确保 finally 中 Clear() 执行前已写入池字段。
        // Declared outside try so the pool writes happen before Clear() in finally.
        uint response_length = 0;
        bool keep_alive = false;
        bool success = false;

        try
        {
            byte* response_buffer = ResponseBufferFor(file_descriptor);

            bool processed;

            processed = ServerFunctions.ProcessUserLogic(
                ServerConfig, user_logic_callback, context, response_buffer, &file_descriptor);

            if (processed)
            {
                // 在 finally Clear() 之前读出长度，避免 Clear() 重置元数据后值丢失。
                response_length = context->TotalResponseLength;
                keep_alive = ServerFunctions.IsKeepAliveRequest(context);
                success = response_length > 0;
            }
            else
            {
                if (ShouldSampleLog(ref log_protocol_error_counter))
                    Console.WriteLine($"[Protocol Error] request parsing failed fd={file_descriptor}. sampled_count={log_protocol_error_counter}");
            }
        }
        finally
        {
            // 先把读出的值提交到连接池，再调 Clear()。
            if (success)
            {
                *ResponseLengthFor(file_descriptor) = response_length;
                *KeepAliveFor(file_descriptor) = (byte)(keep_alive ? 1 : 0);
            }

            context->Clear();
        }

        return success;
    }

    /// <summary>
    /// 尝试在有限背压下获取一个 SQE 槽位。
    /// </summary>
    bool TryAcquireSqe(out io_uring_sqe* submission_queue_entry, out uint index)
    {
        if (submission_queue_tail_pointer == null ||
            submission_queue_mask_pointer == null ||
            submission_queue_entries_pointer == null)
            throw new InvalidOperationException("io_uring SQ is not initialized. Call SetupIoUring() successfully before submitting SQEs.");

        uint head = *submission_queue_head_pointer;
        uint tail = *submission_queue_tail_pointer;
        uint entries = *submission_queue_entries_count_pointer;

        if ((tail - head) >= entries)
        {
            SubmitPending(force: true);
            head = *submission_queue_head_pointer;
            tail = *submission_queue_tail_pointer;
            if ((tail - head) >= entries)
            {
                submission_queue_entry = null;
                index = 0;
                return false;
            }
        }

        index = tail & *submission_queue_mask_pointer;
        submission_queue_entry = submission_queue_entries_pointer + index;
        return true;
    }

    /// <summary>
    /// 发布 SQE，并按需执行提交 syscall。
    /// </summary>
    void FlushSqe(uint index)
    {
        submission_queue_array_pointer[index] = index;
        System.Threading.Thread.MemoryBarrier();
        *submission_queue_tail_pointer = *submission_queue_tail_pointer + 1;
        submit_pending_count++;

        if (submit_pending_count >= GetDynamicSubmitBatchSize())
            SubmitPending(force: false);
    }

    uint GetDynamicSubmitBatchSize()
    {
        if (active_connections_count > (max_active_connections_count * 3U / 4U))
            return submit_batch_size_high;
        if (active_connections_count > (max_active_connections_count / 4U))
            return submit_batch_size_mid;
        return submit_batch_size_low;
    }

    void SubmitPending(bool force)
    {
        if (submit_pending_count == 0) return;
        uint batch_size = GetDynamicSubmitBatchSize();
        if (!force && submit_pending_count < batch_size) return;

        if (submission_queue_polling_active)
        {
            if (submission_queue_flags_pointer != null &&
                ((*submission_queue_flags_pointer & IoUringLibcs.IORING_SQ_NEED_WAKEUP) != 0))
            {
                int wake_result = IoUringLibcs.io_uring_enter(ring_file_descriptor, 0, 0, IoUringLibcs.IORING_ENTER_SQ_WAKEUP);
                if (wake_result < 0)
                {
                    int err = -wake_result;
                    if (IsFeaturePermanentlyUnsupported(wake_result))
                    {
                        submission_queue_polling_active = false;
                        Console.WriteLine($"[SQPOLL] wakeup failed permanently res={wake_result}, fallback to submit-enter mode.");
                        IoUringLibcs.io_uring_enter(ring_file_descriptor, submit_pending_count, 0, 0);
                    }
                    else if (IsTransientErrno(err))
                    {
                        int submit_result = IoUringLibcs.io_uring_enter(ring_file_descriptor, submit_pending_count, 0, 0);
                        if (submit_result < 0)
                        {
                            submission_queue_polling_active = false;
                            if (ShouldSampleLog(ref log_submission_queue_polling_counter))
                                Console.WriteLine($"[SQPOLL] wakeup transient but submit failed res={submit_result}, fallback. sampled_count={log_submission_queue_polling_counter}");
                        }
                    }
                    else
                    {
                        submission_queue_polling_active = false;
                        if (ShouldSampleLog(ref log_submission_queue_polling_counter))
                            Console.WriteLine($"[SQPOLL] wakeup failed res={wake_result}, fallback. sampled_count={log_submission_queue_polling_counter}");
                        IoUringLibcs.io_uring_enter(ring_file_descriptor, submit_pending_count, 0, 0);
                    }
                }
            }
            submit_pending_count = 0;
            return;
        }

        IoUringLibcs.io_uring_enter(ring_file_descriptor, submit_pending_count, 0, 0);
        submit_pending_count = 0;
    }

    void SetupIoUring()
    {
        io_uring_params parameters = default;

        if (submission_queue_polling_active)
        {
            parameters.flags |= IoUringLibcs.IORING_SETUP_SQPOLL;
            parameters.sq_thread_idle = 2000;
        }

        ring_file_descriptor = IoUringLibcs.io_uring_setup(queue_depth_entries, &parameters);
        if (ring_file_descriptor < 0 && submission_queue_polling_active)
        {
            Console.WriteLine($"[SQPOLL] setup failed res={ring_file_descriptor}, fallback to normal submit mode.");
            submission_queue_polling_active = false;
            parameters = default;
            ring_file_descriptor = IoUringLibcs.io_uring_setup(queue_depth_entries, &parameters);
        }
        if (ring_file_descriptor < 0)
            throw new InvalidOperationException($"io_uring_setup failed: {ring_file_descriptor}");

        submission_queue_ring_size_bytes = (nuint)(parameters.sq_off.array + parameters.sq_entries * sizeof(uint));
        submission_queue_ring_pointer = LinuxAPI.mmap(null, submission_queue_ring_size_bytes,
            IoUringLibcs.PROT_READ | IoUringLibcs.PROT_WRITE,
            IoUringLibcs.MAP_SHARED | IoUringLibcs.MAP_POPULATE,
            ring_file_descriptor, IoUringLibcs.IORING_OFF_SQ_RING);
        if (submission_queue_ring_pointer == IoUringLibcs.MAP_FAILED)
            throw new InvalidOperationException("mmap SQ ring failed.");

        byte* sq_buffer = (byte*)submission_queue_ring_pointer;
        submission_queue_head_pointer = (uint*)(sq_buffer + parameters.sq_off.head);
        submission_queue_tail_pointer = (uint*)(sq_buffer + parameters.sq_off.tail);
        submission_queue_mask_pointer = (uint*)(sq_buffer + parameters.sq_off.ring_mask);
        submission_queue_entries_count_pointer = (uint*)(sq_buffer + parameters.sq_off.ring_entries);
        submission_queue_array_pointer = (uint*)(sq_buffer + parameters.sq_off.array);
        submission_queue_flags_pointer = (uint*)(sq_buffer + parameters.sq_off.flags);

        submission_queue_entries_size_bytes = (nuint)(parameters.sq_entries * sizeof(io_uring_sqe));
        submission_queue_entries_pointer = (io_uring_sqe*)LinuxAPI.mmap(null, submission_queue_entries_size_bytes,
            IoUringLibcs.PROT_READ | IoUringLibcs.PROT_WRITE,
            IoUringLibcs.MAP_SHARED | IoUringLibcs.MAP_POPULATE,
            ring_file_descriptor, IoUringLibcs.IORING_OFF_SQES);
        if (submission_queue_entries_pointer == IoUringLibcs.MAP_FAILED)
            throw new InvalidOperationException("mmap SQEs failed.");

        completion_queue_ring_size_bytes = (nuint)(parameters.cq_off.cqes + parameters.cq_entries * sizeof(io_uring_cqe));
        completion_queue_ring_pointer = LinuxAPI.mmap(null, completion_queue_ring_size_bytes,
            IoUringLibcs.PROT_READ | IoUringLibcs.PROT_WRITE,
            IoUringLibcs.MAP_SHARED | IoUringLibcs.MAP_POPULATE,
            ring_file_descriptor, IoUringLibcs.IORING_OFF_CQ_RING);
        if (completion_queue_ring_pointer == IoUringLibcs.MAP_FAILED)
            throw new InvalidOperationException("mmap CQ ring failed.");

        byte* cq_buffer = (byte*)completion_queue_ring_pointer;
        completion_queue_head_pointer = (uint*)(cq_buffer + parameters.cq_off.head);
        completion_queue_tail_pointer = (uint*)(cq_buffer + parameters.cq_off.tail);
        completion_queue_mask_pointer = (uint*)(cq_buffer + parameters.cq_off.ring_mask);
        completion_queue_entries_pointer = (io_uring_cqe*)(cq_buffer + parameters.cq_off.cqes);
    }

    static bool IsFeaturePermanentlyUnsupported(int result)
    {
        if (result >= 0) return false;
        int err = -result;
        return err == 22 || err == 38 || err == 95; // EINVAL/ENOSYS/EOPNOTSUPP
    }

    static bool IsTransientErrno(int err)
        => err == 4 || err == 11 || err == 16 || err == 12 || err == 105; // EINTR/EAGAIN/EBUSY/ENOMEM/ENOBUFS

    /// <summary>
    /// 按依赖逆序释放原生资源。完全幂等，多次调用安全。
    /// </summary>
    public bool Dispose()
    {
        if (is_disposed) return false;  // 幂等保护
        is_disposed = true;

        if (listening_socket_file_descriptor >= 0)
        {
            LinuxAPI.close(listening_socket_file_descriptor);
            listening_socket_file_descriptor = -1;
        }

        if (stop_event_file_descriptor >= 0)
        {
            LinuxAPI.close(stop_event_file_descriptor);
            stop_event_file_descriptor = -1;
        }

        if (ring_file_descriptor >= 0 && fixed_buffers_active)
        {
            int unregister_result = IoUringLibcs.io_uring_register(ring_file_descriptor, IoUringLibcs.IORING_UNREGISTER_BUFFERS, null, 0);
            if (unregister_result < 0)
                Console.WriteLine($"[FixedBuffers] unregister failed res={unregister_result}.");
            else
                Console.WriteLine("[FixedBuffers] unregister ok.");
        }

        if (zero_copy_notification_count > 0)
            Console.WriteLine($"[ZC] notif_total={zero_copy_notification_count}, notif_err_total={zero_copy_notification_error_count}.");

        if (submission_queue_ring_pointer != null && submission_queue_ring_pointer != IoUringLibcs.MAP_FAILED && submission_queue_ring_size_bytes != 0)
            LinuxAPI.munmap(submission_queue_ring_pointer, submission_queue_ring_size_bytes);
        if (completion_queue_ring_pointer != null && completion_queue_ring_pointer != IoUringLibcs.MAP_FAILED && completion_queue_ring_size_bytes != 0)
            LinuxAPI.munmap(completion_queue_ring_pointer, completion_queue_ring_size_bytes);
        if (submission_queue_entries_pointer != null && submission_queue_entries_pointer != IoUringLibcs.MAP_FAILED && submission_queue_entries_size_bytes != 0)
            LinuxAPI.munmap(submission_queue_entries_pointer, submission_queue_entries_size_bytes);

        submission_queue_ring_pointer = null;
        completion_queue_ring_pointer = null;
        submission_queue_entries_pointer = null;
        submission_queue_ring_size_bytes = 0;
        completion_queue_ring_size_bytes = 0;
        submission_queue_entries_size_bytes = 0;

        // ring_file_descriptor 初始化为 -1，此处 >= 0 才关闭，避免误关 fd=0（stdin）
        if (ring_file_descriptor >= 0)
        {
            LinuxAPI.close(ring_file_descriptor);
            ring_file_descriptor = -1;
        }

        if (pool_block_pointer != null)
        {
            NativeMemory.Free(pool_block_pointer);
            pool_block_pointer = null;
            pool_block_size_bytes = 0;
        }


        request_buffer_pool_pointer = null;
        response_buffer_pool_pointer = null;
        send_progress_pool_pointer = null;
        response_length_pool_pointer = null;
        keep_alive_pool_pointer = null;
        slot_owner_pool = null;
        http_context_pool_pointer = null;

        return true;
    }
}
