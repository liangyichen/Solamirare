namespace Solamirare;

/// <summary>
/// MacOS AIO 内部上下文，用于追踪 IO 控制块及读取缓冲区
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct MacOSAIOContext
{
    public macos_aiocb cb;
    public UnManagedMemory<byte>* readBuffer;
}


/// <summary>
/// 异步文件读写，等待版本（挂起等待，不占用CPU），但是主线程会阻塞， 不适合用于UI环境
/// </summary>
public static unsafe class MacOSAIO
{

    // --- 权限与常量 ---
    private const int O_RDWR = 0x0002;
    private const int O_CREAT = 0x0200;
    private const int O_TRUNC = 0x0400; // 核心：打开时直接清空文件，替代 unlink
    private const int F_NOCACHE = 48;
    private const int F_FULLFSYNC = 51;

    /// <summary> 允许的最大并发异步 IO 任务数 </summary>
    private const int MaxPending = 16;
    /// <summary> 当前活跃任务计数 </summary>
    private static int _taskCount = 0;
    /// <summary> 保护提交逻辑的自旋锁 </summary>
    private static SpinLock _taskLock = new(false);

    internal static MemoryPoolCluster memoryPool;

    static MacOSAIO()
    {

        Span<MemoryPoolSchema> schemas = stackalloc MemoryPoolSchema[] {
            new MemoryPoolSchema(64, 128),
            new MemoryPoolSchema((uint)sizeof(MacOSAIOContext), 128),
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

        MemoryPollAllocatedResult allocResult = memoryPool.Alloc((nuint)sizeof(MacOSAIOContext));
        if (allocResult.Address == null) return context;

        MacOSAIOContext* mctx = (MacOSAIOContext*)allocResult.Address;
        mctx->readBuffer = null;
        context.cb = mctx;

        context.Platform = IOAsyncContextPlatform.MacOS;
        context.isWrite = true;



        int pathBytesLength = path.Length * 3 + 1;//最保守的分配数字

        byte* pathBytes = stackalloc byte[pathBytesLength];

        path.CopyToBytes(pathBytes, (uint)pathBytesLength);

        pathBytes[pathBytesLength] = (byte)'\0'; //只需要确保这个位置是\0，路径表示就不会有错，后面多余部分可以不管


        // 1. 使用 O_TRUNC 代替 unlink，保留文件路径但清空内容
        // 权限设为 0666 (八进制) -> 438 (十进制)
        int fd = MacOSAPI.open(pathBytes, O_RDWR | O_CREAT | O_TRUNC, 438);
        if (fd < 0)
        {
            memoryPool.Return(context.cb, (ulong)sizeof(MacOSAIOContext));
            return context;
        }

        if (fd > 0)
        {
            // 2. 显式修复权限，确保外部编辑器能读
            MacOSAPI.chmod(pathBytes, 438);
            // 3. 禁用缓存
            MacOSAPI.fcntl(fd, F_NOCACHE, 1);
        }

        context.fd = fd;
        mctx->cb.aio_fildes = fd;
        mctx->cb.aio_buf = content.InternalPointer;
        mctx->cb.aio_nbytes = content.Size;
        mctx->cb.aio_offset = offset;

        bool lockTaken = false;
        try
        {
            _taskLock.Enter(ref lockTaken);
            if (_taskCount < MaxPending && MacOSAPI.aio_write(&mctx->cb) == 0)
            {
                _taskCount++;
            }
            else
            {
                if (lockTaken) { _taskLock.Exit(); lockTaken = false; }
                if (fd > 0) MacOSAPI.close(fd);
                memoryPool.Return(context.cb, (ulong)sizeof(MacOSAIOContext));
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
        AsyncFilesIOContext context = new AsyncFilesIOContext();
        context.fd = -1;

        if (path.IsEmpty || result is null) return context;

        if (Volatile.Read(ref _taskCount) >= MaxPending) return context;

        MemoryPollAllocatedResult allocResult = memoryPool.Alloc((nuint)sizeof(MacOSAIOContext));
        if (allocResult.Address == null) return context;

        MacOSAIOContext* mctx = (MacOSAIOContext*)allocResult.Address;
        mctx->readBuffer = result;
        context.cb = mctx;

        context.Platform = IOAsyncContextPlatform.MacOS;
        context.isWrite = false;




        int pathBytesLength = path.Length * 3 + 1;//最保守的分配数字

        byte* pathBytes = stackalloc byte[pathBytesLength];

        path.CopyToBytes(pathBytes, (uint)pathBytesLength);

        pathBytes[pathBytesLength] = (byte)'\0'; //只需要确保这个位置是0，路径表示就不会有错，后面多余部分可以不管



        // 纯只读打开 (O_RDONLY = 0)
        int fd = MacOSAPI.open(pathBytes, 0, 0);
        if (fd < 0)
        {
            memoryPool.Return(context.cb, (ulong)sizeof(MacOSAIOContext));
            return context;
        }

        context.fd = fd;
        long len = MacOSAPI.lseek(fd, 0, 2);
        if (len < 0 || len > 16777216)
        {
            if (fd > 0) MacOSAPI.close(fd);
            memoryPool.Return(context.cb, (ulong)sizeof(MacOSAIOContext));
            context.fd = -1;
            return context;
        }

        result->EnsureCapacity((uint)len);
        result->ReLength((uint)len);

        mctx->cb.aio_fildes = fd;
        mctx->cb.aio_buf = result->Pointer;
        mctx->cb.aio_nbytes = result->UsageSize;
        mctx->cb.aio_offset = offset;

        MacOSAPI.lseek(fd, 0, 0); // 回到开

        bool lockTaken = false;
        try
        {
            _taskLock.Enter(ref lockTaken);
            if (_taskCount < MaxPending && MacOSAPI.aio_read(&mctx->cb) == 0)
            {
                _taskCount++;
            }
            else
            {
                if (lockTaken) { _taskLock.Exit(); lockTaken = false; }
                if (fd > 0) MacOSAPI.close(fd);
                memoryPool.Return(context.cb, (ulong)sizeof(MacOSAIOContext));
                context.fd = -1;
                context.cb = null;
                context.isDone = true;
            }
        }
        finally { if (lockTaken) _taskLock.Exit(); }

        return context;
    }


    internal static uint Wait(AsyncFilesIOContext* context)
    {
        if (context is null || context->fd <= 0 || context->isDone) return 0;

        MacOSAIOContext* mctx = (MacOSAIOContext*)context->cb;
        macos_aiocb* pCb = &mctx->cb;
        macos_aiocb** list = &pCb;

        while (MacOSAPI.aio_error(pCb) == 36) // EINPROGRESS
        {
            if (MacOSAPI.aio_suspend(list, 1, null) != 0) continue;
        }

        uint readCount = (uint)MacOSAPI.aio_return(pCb);

        // 更新实际读取到的字节数
        if (mctx->readBuffer != null && readCount > 0)
        {
            mctx->readBuffer->ReLength(readCount);
        }

        // 写模式必须强制刷新元数据到 Finder
        if (context->isWrite) MacOSAPI.fcntl(context->fd, F_FULLFSYNC, 0);

        context->isDone = true;
        Interlocked.Decrement(ref _taskCount);

        return readCount;
    }

}
