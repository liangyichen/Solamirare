namespace Solamirare;



/// <summary>
/// 异步文件读写，回调函数版本
/// </summary>
public static unsafe class IOCPIOWithCallBack
{
    /// <summary>
    /// 启用重叠 I/O (Overlapped I/O) 模式标志。
    /// 在调用 <c>CreateFile</c> 时指定此标志，使该句柄支持异步操作，
    /// 允许后续将其关联到完成端口 (IOCP)。
    /// </summary>
    private const uint FILE_FLAG_OVERLAPPED = 0x40000000;

    /// <summary>
    /// I/O 操作正在挂起中。
    /// 当异步函数（如 <c>ReadFile</c>）立即返回且未报错时，
    /// 该错误码表示操作已成功启动，结果将在稍后通过 IOCP 或事件通知。
    /// </summary>
    private const uint ERROR_IO_PENDING = 997;

    /// <summary>
    /// 无限期等待。
    /// 通常用于 <c>GetQueuedCompletionStatus</c> 的超时参数，
    /// 表示在收到完成包之前调用线程应一直阻塞。
    /// </summary>
    private const uint INFINITE = 0xFFFFFFFF;

    /// <summary>
    /// 通用读取权限 (Generic Read)。
    /// 用于请求对文件或设备的读取访问权。
    /// </summary>
    private const uint GENERIC_READ = 0x80000000;

    /// <summary>
    /// 通用写入权限 (Generic Write)。
    /// 用于请求对文件或设备的写入访问权。
    /// </summary>
    private const uint GENERIC_WRITE = 0x40000000;

    /// <summary>
    /// 文件共享读取标志。
    /// 允许其他进程在当前进程打开该文件时，以读取权限再次打开该文件。
    /// </summary>
    private const uint FILE_SHARE_READ = 1;

    /// <summary>
    /// 文件共享写入标志。
    /// 允许其他进程在当前进程打开该文件时，以写入权限再次打开该文件。
    /// </summary>
    private const uint FILE_SHARE_WRITE = 2;

    /// <summary>
    /// 仅当文件已存在时打开。
    /// 如果文件不存在，则 <c>CreateFile</c> 调用失败。
    /// </summary>
    private const uint OPEN_EXISTING = 3;

    /// <summary>
    /// 始终创建新文件。
    /// 如果文件已存在，则将其覆盖并截断为零字节。
    /// </summary>
    private const uint CREATE_ALWAYS = 2;

    /// <summary> 允许的最大并发异步 IO 任务数 </summary>
    private const int MaxPending = 16;
    /// <summary> 当前活跃任务计数 </summary>
    private static int _taskCount = 0;
    /// <summary> 保护提交逻辑的自旋锁 </summary>
    private static SpinLock _taskLock = new(false);

    static MemoryPoolCluster memoryPool;

    private static readonly void* _globalCompletionPort;

    static void* callbackThreadHandle;

    static IOCPIOWithCallBack()
    {
        // 1. 创建全局完成端口 (并发数设为 0 表示跟随 CPU 核心数)
        _globalCompletionPort = WindowsAPI.CreateIoCompletionPort((void*)(-1), null, 0, 0);

        // 2. 启动全局收割者线程
        ThreadStartInfo info = new ThreadStartInfo();
        info.Worker = &ReaperLoop;
        info.Arg = null;


        NativeThread.Create(out callbackThreadHandle, &info);
        Thread.Sleep(10);//必须有这个等待，否则线程还没启动完成，后续操作就来了，会造成进程崩溃

        memoryPool = new MemoryPoolCluster();

        Span<MemoryPoolSchema> schemas = stackalloc MemoryPoolSchema[]
        {
            new MemoryPoolSchema(64,1024),
            new MemoryPoolSchema(128,1024),
            new MemoryPoolSchema(256,1024),
            new MemoryPoolSchema(512,1024)
        };

        memoryPool.Init(schemas);

    }

    [UnmanagedCallersOnly]
    private static uint ReaperLoop(void* arg)
    {
        uint bytesTransferred = 0;
        nuint completionKey = 0; // 我们用来存放 Context 指针
        OVERLAPPED* pOverlapped = null;

        while (true)
        {
            // 阻塞等待内核通知
            bool success = WindowsAPI.GetQueuedCompletionStatus(
                _globalCompletionPort,
                &bytesTransferred,
                &completionKey,
                &pOverlapped,
                INFINITE);

            if (completionKey != 0)
            {
                // 将 CompletionKey 还原为 Context 指针
                IOCPContextWithCallBack* ctx = (IOCPContextWithCallBack*)completionKey;

                ctx->Close();


                if (ctx->CallbackOnRead != null && ctx->CallbackOnWrite is null)
                {
                    ctx->CallbackOnRead(&ctx->DataOnRead, ctx->args);

                    ctx->DataOnRead.Dispose();
                }

                if (ctx->CallbackOnWrite != null && ctx->CallbackOnRead is null)
                {
                    ctx->CallbackOnWrite(ctx->args);

                }

                memoryPool.Return(ctx, (ulong)sizeof(IOCPContextWithCallBack));

                // 完成任务，递减并发计数器
                Interlocked.Decrement(ref _taskCount);
            }
        }

        return 0;
    }

    public static void ReadAsync(UnManagedCollection<char> path, delegate* unmanaged<UnManagedMemory<byte>*, void*, void> callback, void* args, ulong offset = 0)
    {
        // 1. 尽早检查并发限制
        if (Volatile.Read(ref _taskCount) >= MaxPending) return;

        int contextSize = sizeof(IOCPContextWithCallBack);
        var allocResult = memoryPool.Alloc((nuint)contextSize);
        if (allocResult.Address == null) return;

        IOCPContextWithCallBack* ctx = (IOCPContextWithCallBack*)allocResult.Address;
        // 初始化基础字段，防止 CleanupFail 崩溃
        ctx->hFile = (void*)(-1);
        ctx->CallbackOnRead = callback;
        ctx->CallbackOnWrite = null;
        ctx->args = args;
        ctx->DataOnRead = default;

        // 2. 打开文件 (必须带 Overlapped 标志)
        ctx->hFile = WindowsAPI.CreateFileW(path.InternalPointer, GENERIC_READ, FILE_SHARE_READ, null, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, null);
        if (ctx->hFile == (void*)(-1) || ctx->hFile == null)
        {
            CleanupFail(ctx);
            return;
        }

        // 3. 绑定到全局 IOCP
        WindowsAPI.CreateIoCompletionPort(ctx->hFile, _globalCompletionPort, (nuint)ctx, 0);

        // 4. 获取长度并执行 16MB 上限检查
        long fileSize = 0;
        if (!WindowsAPI.GetFileSizeEx(ctx->hFile, &fileSize) || fileSize < 0 || fileSize > 16777216)
        {
            CleanupFail(ctx);
            return;
        }

        ctx->DataOnRead = new UnManagedMemory<byte>((uint)fileSize, (uint)fileSize);
        ctx->Overlapped = default;
        ctx->Overlapped.Offset = (uint)(offset & 0xFFFFFFFF);
        ctx->Overlapped.OffsetHigh = (uint)(offset >> 32);

        uint dummy = 0;

        // 5. 在锁内原子化提交请求
        bool lockTaken = false;
        try
        {
            _taskLock.Enter(ref lockTaken);
            if (_taskCount < MaxPending)
            {
                _taskCount++;

                bool statusOfRead = WindowsAPI.ReadFile(ctx->hFile, ctx->DataOnRead.Pointer, ctx->DataOnRead.UsageSize, &dummy, &ctx->Overlapped);
                
                //由于当前是异步操作，会立即返回，statusOfRead 的值会是 false，不要把它当作错误（此时获取错误码会是997），这个操作仅在同步模式才会为 true
                
                // if (!statusOfRead)
                // {
                //     uint err = (uint)Marshal.GetLastWin32Error();
                //     if (err != ERROR_IO_PENDING) ctx->ErrorCode = err;
                // }
            }
            else
            {
                if (lockTaken) { _taskLock.Exit(); lockTaken = false; }
                CleanupFail(ctx);
            }
        }
        finally { if (lockTaken) _taskLock.Exit(); }
    }

    public static void WriteAsync(UnManagedCollection<char> path, UnManagedCollection<byte> content, delegate* unmanaged<void*, void> callback, void* args, ulong offset = 0)
    {
        if (Volatile.Read(ref _taskCount) >= MaxPending) return;

        int contextSize = sizeof(IOCPContextWithCallBack);
        var allocResult = memoryPool.Alloc((nuint)contextSize);
        if (allocResult.Address == null) return;

        IOCPContextWithCallBack* ctx = (IOCPContextWithCallBack*)allocResult.Address;
        ctx->hFile = (void*)(-1);
        ctx->CallbackOnRead = null;
        ctx->CallbackOnWrite = callback;
        ctx->args = args;
        ctx->DataOnRead = default;

        ctx->hFile = WindowsAPI.CreateFileW(path.InternalPointer, GENERIC_WRITE, FILE_SHARE_WRITE, null, CREATE_ALWAYS, FILE_FLAG_OVERLAPPED, null);
        if (ctx->hFile == (void*)(-1) || ctx->hFile == null)
        {
            CleanupFail(ctx);
            return;
        }

        WindowsAPI.CreateIoCompletionPort(ctx->hFile, _globalCompletionPort, (nuint)ctx, 0);

        ctx->Overlapped = default;
        ctx->Overlapped.Offset = (uint)(offset & 0xFFFFFFFF);
        ctx->Overlapped.OffsetHigh = (uint)(offset >> 32);

        uint dummy = 0;

        bool lockTaken = false;
        try
        {
            _taskLock.Enter(ref lockTaken);
            if (_taskCount < MaxPending)
            {
                _taskCount++;
                bool statusOfWrite = WindowsAPI.WriteFile(ctx->hFile, content.InternalPointer, content.Size, &dummy, &ctx->Overlapped);
                
                //由于当前是异步操作，会立即返回，statusOfWrite 的值会是 false，不要把它当作错误（此时获取错误码会是997），这个操作仅在同步模式才会为 true
                
                // if (!statusOfWrite)
                // {
                //     uint err = (uint)Marshal.GetLastWin32Error();
                //     if (err != ERROR_IO_PENDING) ctx->ErrorCode = err;
                // }
            }
            else
            {
                if (lockTaken) { _taskLock.Exit(); lockTaken = false; }
                CleanupFail(ctx);
            }
        }
        finally { if (lockTaken) _taskLock.Exit(); }
    }

    internal static void Close(IOCPContextWithCallBack* ctx)
    {
        // 修正句柄验证逻辑：确保不关闭 0 或 -1
        if ((nint)ctx->hFile > 0 && ctx->hFile != (void*)(-1))
        {
            WindowsAPI.CloseHandle(ctx->hFile);
            ctx->hFile = null;
        }
    }

    private static void CleanupFail(IOCPContextWithCallBack* ctx)
    {
        if (ctx == null) return;
        // 触发失败通知
        if (ctx->CallbackOnWrite != null) ctx->CallbackOnWrite(ctx->args);
        else if (ctx->CallbackOnRead != null) ctx->CallbackOnRead(&ctx->DataOnRead, ctx->args);

        Close(ctx);
        if (ctx->DataOnRead.Activated) ctx->DataOnRead.Dispose();
        memoryPool.Return(ctx, (ulong)sizeof(IOCPContextWithCallBack));
    }
}