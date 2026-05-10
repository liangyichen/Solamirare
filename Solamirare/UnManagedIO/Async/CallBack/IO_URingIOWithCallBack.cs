using System.Runtime.CompilerServices;

namespace Solamirare;


/// <summary>
/// 异步文件读写，回调函数版本
/// </summary>
public static unsafe class IO_URingIOWithCallBack
{

    // --- 运行时全局状态与共享内存指针 ---

    /// <summary>
    /// 当前 io_uring 实例的文件描述符。初始化失败通常为 -1。
    /// </summary>
    private static int _ringFd = -1;

    /// <summary>
    /// 指向用户态与内核态共享的提交队列条目 (SQE) 数组的指针。
    /// 应用程序在此处填充具体的 I/O 请求数据。
    /// </summary>
    private static io_uring_sqe* _sqes;

    /// <summary>
    /// 指向提交队列头部 (Head) 索引的指针。由内核更新，指示内核已取走了多少请求。
    /// </summary>
    private static uint* _sq_head;

    /// <summary>
    /// 指向提交队列尾部 (Tail) 索引的指针。由应用程序更新，指示用户已填充了多少请求。
    /// </summary>
    private static uint* _sq_tail;

    /// <summary>
    /// 指向提交队列掩码 (Mask) 的指针。用于将自增索引转换为数组下标：<c>index &amp; mask</c>。
    /// </summary>
    private static uint* _sq_mask;

    /// <summary>
    /// 指向提交队列索引数组 (Array) 的指针。
    /// 该数组建立了环形队列下标到 <see cref="_sqes"/> 物理位置的映射。
    /// </summary>
    private static uint* _sq_array;

    /// <summary>
    /// 指向完成队列头部 (Head) 索引的指针。由应用程序更新，指示用户已消费了多少结果。
    /// </summary>
    private static uint* _cq_head;

    /// <summary>
    /// 指向完成队列尾部 (Tail) 索引的指针。由内核更新，指示内核已写回了多少完成结果。
    /// </summary>
    private static uint* _cq_tail;

    /// <summary>
    /// 指向完成队列掩码 (Mask) 的指针。用于计算 <see cref="_cqes"/> 的有效数组下标。
    /// </summary>
    private static uint* _cq_mask;

    /// <summary>
    /// 指向完成队列条目 (CQE) 数组的指针。
    /// 内核处理完 I/O 后，会将结果（成功/错误码、字节数等）写入此数组。
    /// </summary>
    private static io_uring_cqe* _cqes;

    /// <summary>
    /// 负责处理完成队列 (CQE) 或执行回调任务的专用线程句柄。
    /// </summary>
    static void* callbackThreadHandle;

    /// <summary> 允许的最大并发异步 IO 任务数 </summary>
    private const int MaxPending = 16;
    /// <summary> 当前活跃任务计数 </summary>
    private static int _taskCount = 0;
    /// <summary> 保护提交队列同步的自旋锁 </summary>
    private static SpinLock _taskLock = new(false);

    static MemoryPoolCluster memoryPool;

    static IO_URingIOWithCallBack()
    {
        if (_ringFd != -1) return;

        io_uring_params p = default;
        _ringFd = (int)LinuxAPI.syscall(IO_URingConsts.SYS_io_uring_setup, 32, (long)&p, 0, 0, 0);
        if (_ringFd < 0) throw new Exception("io_uring setup failed");

        // 内存映射 (保持你的原始逻辑)
        nuint sqRingSize = p.sq_off.array + p.sq_entries * sizeof(uint);
        byte* sqPtr = (byte*)LinuxAPI.mmap(null, sqRingSize, 3, 1, _ringFd, IO_URingConsts.IORING_OFF_SQ_RING);
        _sq_head = (uint*)(sqPtr + p.sq_off.head);
        _sq_tail = (uint*)(sqPtr + p.sq_off.tail);
        _sq_mask = (uint*)(sqPtr + p.sq_off.ring_mask);
        _sq_array = (uint*)(sqPtr + p.sq_off.array);

        nuint cqRingSize = p.cq_off.cqes + p.cq_entries * (nuint)sizeof(io_uring_cqe);
        byte* cqPtr = (byte*)LinuxAPI.mmap(null, cqRingSize, 3, 1, _ringFd, IO_URingConsts.IORING_OFF_CQ_RING);
        _cq_head = (uint*)(cqPtr + p.cq_off.head);
        _cq_tail = (uint*)(cqPtr + p.cq_off.tail);
        _cq_mask = (uint*)(cqPtr + p.cq_off.ring_mask);
        _cqes = (io_uring_cqe*)(cqPtr + p.cq_off.cqes);

        _sqes = (io_uring_sqe*)LinuxAPI.mmap(null, p.sq_entries * (nuint)sizeof(io_uring_sqe), 3, 1, _ringFd, IO_URingConsts.IORING_OFF_SQES);

        // --- 启动后台收割线程 ---

        ThreadStartInfo info = new ThreadStartInfo();
        info.Worker = &ReaperLoop;
        info.Arg = null;


        NativeThread.Create(out callbackThreadHandle, &info);

        Thread.Sleep(10); //必须有这个等待，否则线程还没启动完成，后续操作就来了，会造成进程崩溃

        // 2. 初始化内存池 (复用你的 MemoryPoolCluster)
        memoryPool = new MemoryPoolCluster();

        Span<MemoryPoolSchema> schemas = stackalloc MemoryPoolSchema[] {
            new MemoryPoolSchema(64, 1024),
            new MemoryPoolSchema(128, 1024),
            new MemoryPoolSchema(256, 1024),
            new MemoryPoolSchema(512, 1024),
        };

        memoryPool.Init(schemas);
    }

    /// <summary>
    /// 异步写入
    /// </summary>
    /// <param name="path"></param>
    /// <param name="content"></param>
    /// <param name="callback"></param>
    /// <param name="args"></param>
    /// <param name="offset"></param>
    public static void WriteAsync(ReadOnlySpan<char> path, UnManagedCollection<byte> content, delegate* unmanaged<void*, void> callback, void* args, long offset = 0)
    {

        int pathBytesLength = path.Length * 3 + 1;//最保守的分配数字

        byte* pathBytes = stackalloc byte[pathBytesLength];

        path.CopyToBytes(pathBytes, (uint)pathBytesLength);

        pathBytes[pathBytesLength] = (byte)'\0'; //只需要确保这个位置是0，路径表示就不会有错，后面多余部分可以不管



        int fd = LinuxAPI.open(pathBytes, 1 | 64 | 512, 420);
        if (fd < 0) return;

        IO_URingContextWithCallBack* ctx = (IO_URingContextWithCallBack*)memoryPool.Alloc((nuint)sizeof(IO_URingContextWithCallBack)).Address;
        // 2. 如果 ctx 分配失败，必须关闭已打开的 fd
        if (ctx == null) { LinuxAPI.close(fd); return; }

        ctx->fd = fd;
        ctx->OnWriteCompleted = callback;
        ctx->UserArgs = args;
        ctx->OpType = 1;

        // 3. 在锁内原子化提交请求并增加活跃任务数
        bool lockTaken = false;
        try
        {
            _taskLock.Enter(ref lockTaken);
            if (_taskCount < MaxPending)
            {
                _taskCount++;
                SubmitSQE(fd, content.InternalPointer, content.Size, offset, (ulong)ctx, IO_URingConsts.IORING_OP_WRITE);
            }
            else
            {
                LinuxAPI.close(fd);
                memoryPool.Return(ctx, (ulong)sizeof(IO_URingContextWithCallBack));
            }
        }
        finally { if (lockTaken) _taskLock.Exit(); }
    }

    /// <summary>
    /// 异步读取
    /// </summary>
    /// <param name="path"></param>
    /// <param name="callback"></param>
    /// <param name="args"></param>
    /// <param name="offset"></param>
    public static void ReadAsync(ReadOnlySpan<char> path, delegate* unmanaged<UnManagedMemory<byte>*, void*, void> callback, void* args, long offset = 0)
    {

        int pathBytesLength = path.Length * 3 + 1;//最保守的分配数字

        byte* pathBytes = stackalloc byte[pathBytesLength];

        path.CopyToBytes(pathBytes, (uint)pathBytesLength);

        pathBytes[pathBytesLength] = (byte)'\0'; //只需要确保这个位置是0，路径表示就不会有错，后面多余部分可以不管


        int fd = LinuxAPI.open(pathBytes, 0, 0);
        if (fd < 0) return;

        UnManagedMemory<byte> result = new UnManagedMemory<byte>();

        long len = LinuxAPI.lseek(fd, 0, 2);
        // 4. 禁止读取超过 16MB 的文件，并防止 long 到 uint 转换溢出
        if (len < 0 || len > 16777216) { LinuxAPI.close(fd); return; }

        LinuxAPI.lseek(fd, 0, 0);

        if(memoryPool.Support((uint)len))
        {
            fixed(MemoryPoolCluster* p_memoryPool = &memoryPool)
            result = new UnManagedMemory<byte>((uint)len, (uint)len, p_memoryPool);
        }
        else
            result = new UnManagedMemory<byte>((uint)len, (uint)len);


        IO_URingContextWithCallBack* ctx = (IO_URingContextWithCallBack*)memoryPool.Alloc((nuint)sizeof(IO_URingContextWithCallBack)).Address;
        // 如果 ctx 分配失败，必须释放 result 缓冲区并关闭 fd
        if (ctx == null) { result.Dispose(); LinuxAPI.close(fd); return; }

        ctx->fd = fd;
        ctx->OnReadCompleted = callback;
        ctx->ReadResult = result;
        ctx->UserArgs = args;
        ctx->OpType = 0;

        // 在锁内原子化提交请求
        bool lockTaken = false;
        try
        {
            _taskLock.Enter(ref lockTaken);
            if (_taskCount < MaxPending)
            {
                _taskCount++;
                SubmitSQE(fd, result.Pointer, result.UsageSize, offset, (ulong)ctx, IO_URingConsts.IORING_OP_READ);
            }
            else
            {
                result.Dispose();
                LinuxAPI.close(fd);
                memoryPool.Return(ctx, (ulong)sizeof(IO_URingContextWithCallBack));
            }
        }
        finally { if (lockTaken) _taskLock.Exit(); }
    }

    private static void SubmitSQE(int fd, byte* addr, uint len, long offset, ulong udata, byte opcode)
    {
        uint tail = *_sq_tail;
        uint index = tail & (*_sq_mask);
        io_uring_sqe* sqe = &_sqes[index];

        // 清理并填充
        Unsafe.InitBlock(sqe, 0, (uint)sizeof(io_uring_sqe));
        sqe->opcode = opcode;
        sqe->fd = fd;
        sqe->addr = (ulong)addr;
        sqe->len = len;
        sqe->off = (ulong)offset;
        sqe->user_data = udata;

        _sq_array[index] = index;
        Interlocked.MemoryBarrier();
        *_sq_tail = tail + 1;
        Interlocked.MemoryBarrier();

        // 提交到内核
        LinuxAPI.syscall(IO_URingConsts.SYS_io_uring_enter, _ringFd, 1, 0, 0, 0);
    }

    [UnmanagedCallersOnly]
    private static uint ReaperLoop(void* arg)
    {
        while (true)
        {
            uint head = Volatile.Read(ref *_cq_head);
            uint tail = Volatile.Read(ref *_cq_tail);

            if (head == tail)
            {
                // 异步通知：进入内核阻塞等待至少 1 个事件 ---
                LinuxAPI.syscall(IO_URingConsts.SYS_io_uring_enter, _ringFd, 0, 1, IO_URingConsts.IORING_ENTER_GETEVENTS, 0);
                continue;
            }

            uint mask = *_cq_mask;
            while (head != tail)
            {
                io_uring_cqe* cqe = &_cqes[head & mask];
                IO_URingContextWithCallBack* ctx = (IO_URingContextWithCallBack*)cqe->user_data;

                if (ctx != null)
                {
                    ctx->result = cqe->res;
                    // 执行回调
                    if (ctx->OpType == 1 && ctx->OnWriteCompleted != null)
                        ctx->OnWriteCompleted(ctx->UserArgs);
                    else if (ctx->OpType == 0 && ctx->OnReadCompleted != null)
                    {
                        // 5. 根据内核返回的实际字节数修正有效长度
                        if (ctx->result > 0) ctx->ReadResult.ReLength((uint)ctx->result);
                        ctx->OnReadCompleted(&ctx->ReadResult, ctx->UserArgs);
                        // 6. 读取完成后必须释放 ReadResult 内部的非托管缓冲区
                        ctx->ReadResult.Dispose();
                    }

                    // 确保不关闭 fd 0
                    if (ctx->fd > 0) LinuxAPI.close(ctx->fd);
                    memoryPool.Return(ctx, (ulong)sizeof(IO_URingContextWithCallBack));

                    // 完成任务，递减并发计数器
                    Interlocked.Decrement(ref _taskCount);
                }

                head++;
                Volatile.Write(ref *_cq_head, head);
            }
        }

        return 0;
    }

}