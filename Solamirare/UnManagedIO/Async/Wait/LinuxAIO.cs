namespace Solamirare;

/// <summary>
/// Linux AIO 内部上下文，用于追踪 IO 控制块及读取缓冲区
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct LinuxAIOContext
{
    public linux_aiocb cb;
    public UnManagedMemory<byte>* readBuffer;
}


/// <summary>
/// 异步文件读写，等待版本（挂起等待，不占用CPU），但是主线程会阻塞， 不适合用于UI环境
/// </summary>
internal static unsafe class LinuxAIO
{
    // --- Linux 核心常量 ---
    private const int O_RDONLY = 0;
    private const int O_WRONLY = 1;
    private const int O_RDWR = 2;
    private const int O_CREAT = 64;
    private const int O_TRUNC = 512;

    private const int EINPROGRESS = 115;

    /// <summary> 允许的最大并发异步 IO 任务数 </summary>
    private const int MaxPending = 16;
    /// <summary> 当前活跃任务计数 </summary>
    private static int _taskCount = 0;
    /// <summary> 保护提交逻辑的自旋锁 </summary>
    private static SpinLock _taskLock = new(false);

    internal static MemoryPoolCluster memoryPool;

    static LinuxAIO()
    {
        Span<MemoryPoolSchema> schemas = stackalloc MemoryPoolSchema[] {
            new MemoryPoolSchema(64, 128),
            new MemoryPoolSchema((uint)sizeof(LinuxAIOContext), 128),
            new MemoryPoolSchema(512, 128)
        };

        memoryPool.Init(schemas);
    }

    /// <summary>
    /// 异步写入
    /// </summary>
    /// <param name="path"></param>
    /// <param name="content"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static AsyncFilesIOContext WriteAsync(ReadOnlySpan<char> path, UnManagedCollection<byte> content, long offset = 0)
    {
        AsyncFilesIOContext context = new AsyncFilesIOContext();
        context.fd = -1;

        if (path.IsEmpty) return context;

        if (Volatile.Read(ref _taskCount) >= MaxPending) return context;


        int pathBytesLength = path.Length * 3 + 1;//最保守的分配数字

        byte* pathBytes = stackalloc byte[pathBytesLength];

        path.CopyToBytes(pathBytes, (uint)pathBytesLength);

        pathBytes[pathBytesLength] = (byte)'\0'; //只需要确保这个位置是0，路径表示就不会有错，后面多余部分可以不管


        // 权限 0644
        int fd = LinuxAPI.open(pathBytes, O_WRONLY | O_CREAT | O_TRUNC, 420);

        if (fd < 0) return context;

        var allocResult = memoryPool.Alloc((nuint)sizeof(LinuxAIOContext));
        if (allocResult.Address == null)
        {
            LinuxAPI.close(fd);
            return context;
        }

        LinuxAIOContext* lctx = (LinuxAIOContext*)allocResult.Address;
        lctx->readBuffer = null;
        context.cb = lctx;

        context.Platform = IOAsyncContextPlatform.LinuxAIO;
        context.fd = fd;

        lctx->cb.aio_fildes = fd;
        lctx->cb.aio_buf = content.InternalPointer;
        lctx->cb.aio_nbytes = content.Size;
        lctx->cb.aio_offset = offset;

        bool lockTaken = false;
        try
        {
            _taskLock.Enter(ref lockTaken);
            if (_taskCount < MaxPending && LinuxAPI.aio_write(&lctx->cb) == 0)
            {
                _taskCount++;
            }
            else
            {
                if (lockTaken) { _taskLock.Exit(); lockTaken = false; }
                if (fd > 0) LinuxAPI.close(fd);
                memoryPool.Return(context.cb, (ulong)sizeof(LinuxAIOContext));
                context.fd = -1;
                context.cb = null;
                context.isDone = true;
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
        //一定要分配在堆上，因为方法会立即返回，否则会导致 LinuxAPI.aio_read 读取脏数据
        AsyncFilesIOContext context = new AsyncFilesIOContext();
        context.fd = -1;

        if (path.IsEmpty) return context;

        if (Volatile.Read(ref _taskCount) >= MaxPending) return context;


        int pathBytesLength = path.Length * 3 + 1; //最保守的分配数字

        byte* pathBytes = stackalloc byte[pathBytesLength];

        path.CopyToBytes(pathBytes, (uint)pathBytesLength);

        pathBytes[pathBytesLength] = (byte)'\0'; //只需要确保这个位置是0，路径表示就不会有错，后面多余部分可以不管


        int fd = LinuxAPI.open(pathBytes, O_RDONLY, 0);
        if (fd < 0) return context;

        long len = LinuxAPI.lseek(fd, 0, 2);
        if (len < 0 || len > 16777216)
        {
            LinuxAPI.close(fd);
            return context;
        }
        LinuxAPI.lseek(fd, 0, 0);

        var allocResult = memoryPool.Alloc((nuint)sizeof(LinuxAIOContext));
        if (allocResult.Address == null)
        {
            LinuxAPI.close(fd);
            return context;
        }

        LinuxAIOContext* lctx = (LinuxAIOContext*)allocResult.Address;
        lctx->readBuffer = result;
        context.cb = lctx;

        context.Platform = IOAsyncContextPlatform.LinuxAIO;
        context.fd = fd;

        result->EnsureCapacity((uint)len);
        result->ReLength((uint)len);

        lctx->cb.aio_fildes = fd;
        lctx->cb.aio_buf = result->Pointer;
        lctx->cb.aio_nbytes = result->UsageSize;
        lctx->cb.aio_offset = offset;

        bool lockTaken = false;
        try
        {
            _taskLock.Enter(ref lockTaken);
            if (_taskCount < MaxPending && LinuxAPI.aio_read(&lctx->cb) == 0)
            {
                _taskCount++;
            }
            else
            {
                if (lockTaken) { _taskLock.Exit(); lockTaken = false; }
                if (fd > 0) LinuxAPI.close(fd);
                memoryPool.Return(context.cb, (ulong)sizeof(LinuxAIOContext));
                context.fd = -1;
                context.cb = null;
                context.isDone = true;
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
    public static uint Wait(AsyncFilesIOContext* context)
    {
        if (context is null || context->fd <= 0 || context->isDone) return 0;

        LinuxAIOContext* lctx = (LinuxAIOContext*)context->cb;
        linux_aiocb* pCb = &lctx->cb;
        linux_aiocb** list = stackalloc linux_aiocb*[1];
        list[0] = pCb;

        // 让渡 CPU 循环：直到 IO 完成
        while (LinuxAPI.aio_error(pCb) == EINPROGRESS)
        {
            if (LinuxAPI.aio_suspend(list, 1, null) != 0) continue;
        }

        nint result = LinuxAPI.aio_return(pCb);

        // 更新实际读取到的字节数
        if (lctx->readBuffer != null && result > 0)
        {
            lctx->readBuffer->ReLength((uint)result);
        }

        context->isDone = true;
        Interlocked.Decrement(ref _taskCount);

        return result > 0 ? (uint)result : 0;
    }

}