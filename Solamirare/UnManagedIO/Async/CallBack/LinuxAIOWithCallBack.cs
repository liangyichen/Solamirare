namespace Solamirare;

/// <summary>
/// 异步文件读写，回调函数版本
/// </summary>
internal static unsafe class LinuxAIOWithCallBack
{
    private const int EINPROGRESS = 115;

    /// <summary> 允许的最大并发异步 IO 任务数 </summary>
    private const int MaxPending = 16;

    /// <summary> 非托管任务数组，存储上下文指针 </summary>
    private static LinuxAIOContextWithCallBack** _pendingTasks;

    /// <summary> 当前活跃任务计数 </summary>
    private static int _taskCount = 0;

    /// <summary> 保护任务数组和计数器的自旋锁 </summary>
    private static SpinLock _taskLock = new(false);

    /// <summary> Reaper 线程的挂起与唤醒信号 </summary>
    private static readonly ManualResetEventSlim _reaperWait = new(false);

    static MemoryPoolCluster memoryPool;

    static void* callbackThreadHandle;

    static LinuxAIOWithCallBack()
    {


        ThreadStartInfo info = new ThreadStartInfo();
        info.Worker = &ReaperLoop;
        info.Arg = null;


        NativeThread.Create(out callbackThreadHandle, &info);

        Thread.Sleep(10); //必须有这个等待，否则线程还没启动完成，后续操作就来了，会造成进程崩溃


        // 预分配非托管内存
        int _pendingTasksSize = MaxPending * sizeof(void*);


        memoryPool = new MemoryPoolCluster();

        Span<MemoryPoolSchema> schemas = stackalloc MemoryPoolSchema[]
        {
            new MemoryPoolSchema(64,1024),
            new MemoryPoolSchema(128,1024),
            new MemoryPoolSchema(256,1024),
            new MemoryPoolSchema((uint)_pendingTasksSize,1),
        };

        memoryPool.Init(schemas);

        _pendingTasks = (LinuxAIOContextWithCallBack**)memoryPool.Alloc((ulong)_pendingTasksSize).Address;

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
        if (path.IsEmpty) return;
        
        if (Volatile.Read(ref _taskCount) >= MaxPending) return;

        byte* pathBytes = stackalloc byte[path.Length * 3 + 1];
        
        // 假设 path 扩展方法 CopyToBytes 可用
        path.CopyToBytes(pathBytes, (uint)(path.Length * 3 + 1));


        int fd = LinuxAPI.open(pathBytes, 1 | 64 | 512, 420);
        
        if (fd < 0) return;

        LinuxAIOContextWithCallBack* ctx = (LinuxAIOContextWithCallBack*)memoryPool.Alloc((uint)sizeof(LinuxAIOContextWithCallBack)).Address;
        
        if (ctx == null) { LinuxAPI.close(fd); return; }

        ctx->cb = (linux_aiocb*)memoryPool.Alloc((nuint)sizeof(linux_aiocb)).Address;
        
        if (ctx->cb == null) 
        { 
            memoryPool.Return(ctx, (ulong)sizeof(LinuxAIOContextWithCallBack)); 

            LinuxAPI.close(fd); 

            return; 
        }

        ctx->fd = fd;
        ctx->OnWriteCompleted = callback;
        ctx->UserArgs = args;
        ctx->OpType = 1;
        ctx->ReadResult = default;

        ctx->cb->aio_fildes = fd;
        ctx->cb->aio_buf = content.InternalPointer;
        ctx->cb->aio_nbytes = content.Size;
        ctx->cb->aio_offset = offset;

        // 修复：必须在锁内原子化提交并注册，防止 Use-After-Free
        bool lockTaken = false;
        try
        {
            _taskLock.Enter(ref lockTaken);
            if (_taskCount < MaxPending && LinuxAPI.aio_write(ctx->cb) == 0)
            {
                _pendingTasks[_taskCount] = ctx;
                _taskCount++;
                _reaperWait.Set();
            }
            else
            {
                if (lockTaken) { _taskLock.Exit(); lockTaken = false; }
                CleanupFail(ctx);
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
        if (path.IsEmpty) return;

        if (Volatile.Read(ref _taskCount) >= MaxPending) return;

        byte* pathBytes = stackalloc byte[path.Length * 3 + 1];

        path.CopyToBytes(pathBytes, (uint)(path.Length * 3 + 1));

        int fd = LinuxAPI.open(pathBytes, 0, 0);

        if (fd < 0) { return; }

        long len = LinuxAPI.lseek(fd, 0, 2);

        // 限制：禁止读取超过 16MB 的文件，并防止溢出
        if (len < 0 || len > 16777216) { LinuxAPI.close(fd); return; }

        LinuxAPI.lseek(fd, 0, 0);

        UnManagedMemory<byte> result;

        if (memoryPool.Support((uint)len))
        {
            fixed (MemoryPoolCluster* p_memoryPool = &memoryPool)
                result = new UnManagedMemory<byte>((uint)len, (uint)len, p_memoryPool);
        }
        else
        {
            result = new UnManagedMemory<byte>((uint)len, (uint)len);
        }

        LinuxAIOContextWithCallBack* ctx = (LinuxAIOContextWithCallBack*)memoryPool.Alloc((nuint)sizeof(LinuxAIOContextWithCallBack)).Address;
        
        if (ctx == null) { result.Dispose(); LinuxAPI.close(fd); return; }

        ctx->cb = (linux_aiocb*)memoryPool.Alloc((nuint)sizeof(linux_aiocb)).Address;
        
        if (ctx->cb == null) { result.Dispose(); memoryPool.Return(ctx, (ulong)sizeof(LinuxAIOContextWithCallBack)); LinuxAPI.close(fd); return; }

        ctx->fd = fd;
        ctx->OnReadCompleted = callback;
        ctx->ReadResult = result;
        ctx->UserArgs = args;
        ctx->OpType = 0;

        ctx->cb->aio_fildes = fd;
        ctx->cb->aio_buf = result.Pointer;
        ctx->cb->aio_nbytes = result.UsageSize;
        ctx->cb->aio_offset = offset;

        bool lockTaken = false;
        try
        {
            _taskLock.Enter(ref lockTaken);

            if (_taskCount < MaxPending && LinuxAPI.aio_read(ctx->cb) == 0)
            {
                _pendingTasks[_taskCount] = ctx;
                _taskCount++;
                _reaperWait.Set();
            }
            else
            {
                if (lockTaken) 
                { 
                    _taskLock.Exit(); 
                    lockTaken = false; 
                }

                CleanupFail(ctx);
            }
        }
        finally 
        { 
            if (lockTaken) 
            _taskLock.Exit(); 
        }
    }


    [UnmanagedCallersOnly]
    private static uint ReaperLoop(void* arg)
    {
        // 在栈上分配 waitList，确保内核访问的地址在当前线程上下文是私有且稳定的
        linux_aiocb** waitList = stackalloc linux_aiocb*[MaxPending];
        LinuxAIOContextWithCallBack** ctxSnapshot = stackalloc LinuxAIOContextWithCallBack*[MaxPending];

        while (true)
        {
            int currentCount = 0;
            bool lockTaken = false;
            try
            {
                _taskLock.Enter(ref lockTaken);
                currentCount = _taskCount;
                if (currentCount > 0)
                {
                    for (int i = 0; i < currentCount; i++)
                    {
                        ctxSnapshot[i] = _pendingTasks[i];
                        waitList[i] = ctxSnapshot[i]->cb;
                    }
                }
            }
            finally { if (lockTaken) _taskLock.Exit(); }

            if (currentCount <= 0)
            {
                _reaperWait.Wait(10);
                _reaperWait.Reset();
                continue;
            }

            if (LinuxAPI.aio_suspend(waitList, currentCount, null) == 0)
            {
                for (int i = 0; i < currentCount; i++)
                {
                    LinuxAIOContextWithCallBack* ctx = ctxSnapshot[i];

                    int err = LinuxAPI.aio_error(ctx->cb);
                    if (err != EINPROGRESS)
                    {
                        nint ret = LinuxAPI.aio_return(ctx->cb);

                        // 修正：根据内核返回的实际长度更新缓冲区
                        if (ctx->OpType == 0 && ret > 0) ctx->ReadResult.ReLength((uint)ret);

                        if (ctx->OpType == 1)
                        {
                            if (ctx->OnWriteCompleted != null) ctx->OnWriteCompleted(ctx->UserArgs);
                        }
                        else
                        {
                            if (ctx->OnReadCompleted != null)
                                ctx->OnReadCompleted(&ctx->ReadResult, ctx->UserArgs);
                            ctx->ReadResult.Dispose();
                        }

                        if (ctx->fd > 0) LinuxAPI.close(ctx->fd);

                        // 锁内原子化移除
                        bool removeLockTaken = false;
                        try
                        {
                            _taskLock.Enter(ref removeLockTaken);
                            int liveCount = _taskCount;
                            for (int k = 0; k < liveCount; k++)
                            {
                                if (_pendingTasks[k] == ctx)
                                {
                                    int lastIdx = liveCount - 1;
                                    if (k < lastIdx) _pendingTasks[k] = _pendingTasks[lastIdx];
                                    _taskCount = lastIdx;
                                    break;
                                }
                            }
                        }
                        finally { if (removeLockTaken) _taskLock.Exit(); }

                        memoryPool.Return(ctx->cb, (ulong)sizeof(linux_aiocb));
                        memoryPool.Return(ctx, (ulong)sizeof(LinuxAIOContextWithCallBack));
                    }
                }
            }
        }

        return 0;
    }

    private static void CleanupFail(LinuxAIOContextWithCallBack* ctx)
    {
        // 通知失败
        if (ctx->OpType == 1 && ctx->OnWriteCompleted != null) ctx->OnWriteCompleted(ctx->UserArgs);
        else if (ctx->OpType == 0 && ctx->OnReadCompleted != null) ctx->OnReadCompleted(&ctx->ReadResult, ctx->UserArgs);

        if (ctx->fd > 0) LinuxAPI.close(ctx->fd);

        if (ctx->OpType == 0 && ctx->ReadResult.Activated)
        {
            ctx->ReadResult.Dispose();
        }

        if (ctx->cb != null) memoryPool.Return(ctx->cb, (ulong)sizeof(linux_aiocb));
        memoryPool.Return(ctx, (ulong)sizeof(LinuxAIOContextWithCallBack));
    }
}
