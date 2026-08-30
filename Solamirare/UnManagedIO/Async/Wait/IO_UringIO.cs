using System.Runtime.InteropServices;
namespace Solamirare;


/// <summary>
/// 异步文件读写，等待版本（挂起等待，不占用CPU），但是主线程会阻塞， 不适合用于UI环境
/// </summary>
public static unsafe class IO_UringIO
{



    // --- 全局内核状态与环形缓冲区指针 ---

    /// <summary>
    /// io_uring 实例的文件描述符。失败时通常为 -1。
    /// </summary>
    private static int _ringFd = -1;

    /// <summary>
    /// 指向提交队列条目 (SQE) 数组的原始指针。
    /// 应用程序在此处填充 I/O 请求。
    /// </summary>
    private static io_uring_sqe* _sqes;

    /// <summary>
    /// 指向提交队列头部 (Head) 的指针（映射自内核）。
    /// 指示内核已处理到的位置。
    /// </summary>
    private static uint* _sq_head;

    /// <summary>
    /// 指向提交队列尾部 (Tail) 的指针（映射自内核）。
    /// 应用程序通过增加此值来提交新的请求。
    /// </summary>
    private static uint* _sq_tail;

    /// <summary>
    /// 提交队列的掩码指针。用于计算索引：<c>index = tail &amp; mask</c>。
    /// </summary>
    private static uint* _sq_mask;

    /// <summary>
    /// 提交队列索引数组指针。
    /// 提供从环形缓冲区到实际 SQE 数组的映射层。
    /// </summary>
    private static uint* _sq_array;

    /// <summary>
    /// 指向完成队列头部 (Head) 的指针。
    /// 应用程序通过增加此值来消费已完成的事件。
    /// </summary>
    private static uint* _cq_head;

    /// <summary>
    /// 指向完成队列尾部 (Tail) 的指针。
    /// 指示内核已写回的完成事件位置。
    /// </summary>
    private static uint* _cq_tail;

    /// <summary>
    /// 完成队列的掩码指针。用于计算完成事件索引。
    /// </summary>
    private static uint* _cq_mask;

    /// <summary>
    /// 指向完成队列条目 (CQE) 数组的原始指针。
    /// 内核在此处写回 I/O 操作的结果。
    /// </summary>
    private static io_uring_cqe* _cqes;

    /// <summary>
    /// 负责轮询或处理完成通知的回调线程句柄。
    /// </summary>
    static void* callbackThreadHandle;

    /// <summary> 允许的最大并发异步 IO 任务数 </summary>
    private const int MaxPending = 16;
    /// <summary> 当前活跃任务计数 </summary>
    private static int _taskCount = 0;
    /// <summary> 保护提交队列同步的自旋锁 </summary>
    private static SpinLock _taskLock = new(false);


    internal static MemoryPoolCluster memoryPool;


    // ================== 初始化 (动态偏移) ==================

    static IO_UringIO()
    {
        if (_ringFd != -1) return;

        io_uring_params p = default;
        _ringFd = (int)LinuxAPI.syscall(IO_URingConsts.SYS_io_uring_setup, 32, (long)&p, 0, 0, 0);
        if (_ringFd < 0) throw new Exception($"setup failed: {Marshal.GetLastWin32Error()}");

        // 1. SQ 环映射
        nuint sqRingSize = p.sq_off.array + p.sq_entries * sizeof(uint);
        byte* sqPtr = (byte*)LinuxAPI.mmap(null, sqRingSize, 3, 1, _ringFd, IO_URingConsts.IORING_OFF_SQ_RING);
        if (sqPtr == (void*)(-1)) throw new Exception("mmap SQ ring failed");
        _sq_head = (uint*)(sqPtr + p.sq_off.head);
        _sq_tail = (uint*)(sqPtr + p.sq_off.tail);
        _sq_mask = (uint*)(sqPtr + p.sq_off.ring_mask);
        _sq_array = (uint*)(sqPtr + p.sq_off.array);

        // 2. CQ 环映射
        nuint cqRingSize = p.cq_off.cqes + p.cq_entries * (nuint)sizeof(io_uring_cqe);
        byte* cqPtr = (byte*)LinuxAPI.mmap(null, cqRingSize, 3, 1, _ringFd, IO_URingConsts.IORING_OFF_CQ_RING);
        if (cqPtr == (void*)(-1)) throw new Exception("mmap CQ ring failed");
        _cq_head = (uint*)(cqPtr + p.cq_off.head);
        _cq_tail = (uint*)(cqPtr + p.cq_off.tail);
        _cq_mask = (uint*)(cqPtr + p.cq_off.ring_mask);
        _cqes = (io_uring_cqe*)(cqPtr + p.cq_off.cqes);

        // 3. SQEs 映射
        _sqes = (io_uring_sqe*)LinuxAPI.mmap(null, p.sq_entries * (nuint)sizeof(io_uring_sqe), 3, 1, _ringFd, IO_URingConsts.IORING_OFF_SQES);
        if (_sqes == (void*)(-1)) throw new Exception("mmap SQEs failed");

        Span<MemoryPoolSchema> schemas = stackalloc MemoryPoolSchema[] {
            new MemoryPoolSchema(64, 128),
            new MemoryPoolSchema(192, 128),
            new MemoryPoolSchema(256, 128)
        };

        memoryPool.Init(schemas);
    }

    // ================== 异步核心 ==================

    /// <summary>
    /// 异步写入
    /// </summary>
    /// <param name="path"></param>
    /// <param name="content"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static AsyncFilesIOContext WriteAsync(ReadOnlySpan<char> path, UnManagedCollection<byte>* content, long offset = 0)
    {
        AsyncFilesIOContext context = new AsyncFilesIOContext();
        context.fd = -1;

        if (Volatile.Read(ref _taskCount) >= MaxPending) return context;

        var alloc = memoryPool.Alloc((nuint)sizeof(IO_URingContext));
        if (alloc.Address == null) return context;

        context.IO_URingContext = (IO_URingContext*)alloc.Address;
        context.Platform = IOAsyncContextPlatform.LinuxIOUring;


        int pathBytesLength = path.Length * 3 + 1;//最保守的分配数字

        byte* pathBytes = stackalloc byte[pathBytesLength];

        path.CopyToBytes(pathBytes, (uint)pathBytesLength);

        pathBytes[pathBytesLength] = (byte)'\0'; //只需要确保这个位置是0，路径表示就不会有错，后面多余部分可以不管


        int fd = LinuxAPI.open(pathBytes, 1 | 64 | 512, 420); // O_WRONLY|O_CREAT|O_TRUNC
        if (fd < 0) { memoryPool.Return(context.IO_URingContext, (ulong)sizeof(IO_URingContext)); return context; }

        context.IO_URingContext->isDone = false;
        context.IO_URingContext->fd = fd;
        context.IO_URingContext->readBuffer = null;
        context.fd = fd;

        bool lockTaken = false;
        try
        {
            _taskLock.Enter(ref lockTaken);
            if (_taskCount < MaxPending)
            {
                uint tail = *_sq_tail;
                uint index = tail & (*_sq_mask);
                io_uring_sqe* sqe = &_sqes[index];

                for (int i = 0; i < sizeof(io_uring_sqe); i++) ((byte*)sqe)[i] = 0;
                sqe->opcode = IO_URingConsts.IORING_OP_WRITE;
                sqe->fd = fd;
                sqe->addr = (ulong)content->InternalPointer;
                sqe->len = content->Size;
                sqe->off = (ulong)offset;
                sqe->user_data = (ulong)context.IO_URingContext;

                _sq_array[index] = index;
                Interlocked.MemoryBarrier();
                *_sq_tail = tail + 1;
                Interlocked.MemoryBarrier();
                _taskCount++;
                LinuxAPI.syscall(IO_URingConsts.SYS_io_uring_enter, _ringFd, 1, 0, 0, 0);
            }
            else
            {
                LinuxAPI.close(fd);
                memoryPool.Return(context.IO_URingContext, (ulong)sizeof(IO_URingContext));
            }
        }
        finally { if (lockTaken) _taskLock.Exit(); }

        return context;
    }

    /// <summary>
    /// 异步读取
    /// </summary>
    /// <param name="path"></param>
    /// <param name="result"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static AsyncFilesIOContext ReadAsync(ReadOnlySpan<char> path, UnManagedMemory<byte>* result, long offset = 0)
    {

        AsyncFilesIOContext context = new AsyncFilesIOContext();
        context.fd = -1;

        if (Volatile.Read(ref _taskCount) >= MaxPending) return context;

        var alloc = memoryPool.Alloc((nuint)sizeof(IO_URingContext));
        if (alloc.Address == null) return context;
        context.IO_URingContext = (IO_URingContext*)alloc.Address;

        int pathBytesLength = path.Length * 3 + 1;//最保守的分配数字

        byte* pathBytes = stackalloc byte[pathBytesLength];

        path.CopyToBytes(pathBytes, (uint)pathBytesLength);

        pathBytes[pathBytesLength] = (byte)'\0'; //只需要确保这个位置是0，路径表示就不会有错，后面多余部分可以不管



        int fd = LinuxAPI.open(pathBytes, 0, 0);
        if (fd < 0) { memoryPool.Return(context.IO_URingContext, (ulong)sizeof(IO_URingContext)); return context; }

        context.Platform = IOAsyncContextPlatform.LinuxIOUring;

        long len = LinuxAPI.lseek(fd, 0, 2);
        if (len < 0 || len > 16777216) { LinuxAPI.close(fd); memoryPool.Return(context.IO_URingContext, (ulong)sizeof(IO_URingContext)); return context; }

        result->EnsureCapacity((uint)len);
        result->ReLength((uint)len);

        LinuxAPI.lseek(fd, 0, 0); // 回到开头

        context.IO_URingContext->isDone = false;
        context.IO_URingContext->fd = fd;
        context.IO_URingContext->readBuffer = result;
        context.fd = fd;

        bool lockTaken = false;
        try
        {
            _taskLock.Enter(ref lockTaken);
            if (_taskCount < MaxPending)
            {
                uint tail = *_sq_tail;
                uint index = tail & (*_sq_mask);
                io_uring_sqe* sqe = &_sqes[index];

                for (int i = 0; i < sizeof(io_uring_sqe); i++) ((byte*)sqe)[i] = 0;
                sqe->opcode = IO_URingConsts.IORING_OP_READ;
                sqe->fd = fd;
                sqe->addr = (ulong)result->Pointer;
                sqe->len = result->UsageSize;
                sqe->off = (ulong)offset;
                sqe->user_data = (ulong)context.IO_URingContext;

                _sq_array[index] = index;
                Interlocked.MemoryBarrier();
                *_sq_tail = tail + 1;
                Interlocked.MemoryBarrier();
                _taskCount++;
                LinuxAPI.syscall(IO_URingConsts.SYS_io_uring_enter, _ringFd, 1, 0, 0, 0);
            }
            else
            {
                LinuxAPI.close(fd);
                memoryPool.Return(context.IO_URingContext, (ulong)sizeof(IO_URingContext));
            }
        }
        finally { if (lockTaken) _taskLock.Exit(); }

        return context;
    }

    /// <summary>
    /// 等待完成
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static uint Wait(IO_URingContext* context)
    {
        while (!context->isDone)
        {
            // 如果 CQ 环为空，则进入内核让渡 CPU (min_complete = 1)
            if (Volatile.Read(ref *_cq_head) == Volatile.Read(ref *_cq_tail))
            {
                LinuxAPI.syscall(IO_URingConsts.SYS_io_uring_enter, _ringFd, 0, 1, IO_URingConsts.IORING_ENTER_GETEVENTS, 0);
            }
            PollCompletion(context);
        }
        return context->result >= 0 ? (uint)context->result : 0;
    }

    private static void PollCompletion(IO_URingContext* targetCtx)
    {
        bool lockTaken = false;
        try
        {
            _taskLock.Enter(ref lockTaken);
            uint head = *_cq_head;
            uint tail = Volatile.Read(ref *_cq_tail);
            uint mask = *_cq_mask;

            while (head != tail)
            {
                io_uring_cqe* cqe = &_cqes[head & mask];
                IO_URingContext* ctx = (IO_URingContext*)cqe->user_data;
                if (ctx != null)
                {
                    ctx->result = cqe->res;
                    if (ctx->readBuffer != null && cqe->res > 0)
                    {
                        ctx->readBuffer->ReLength((uint)cqe->res);
                    }
                    ctx->isDone = true;
                    _taskCount--;
                }
                head++;
                Volatile.Write(ref *_cq_head, head);
                if (ctx == targetCtx) break;
            }
        }
        finally { if (lockTaken) _taskLock.Exit(); }
    }

    // ================== 工具与 Native ==================


    /// <summary>
    /// 关闭文件操作
    /// </summary>
    /// <param name="fd"></param>
    public static void Close(int fd) { if (fd > 0) LinuxAPI.close(fd); }
}
