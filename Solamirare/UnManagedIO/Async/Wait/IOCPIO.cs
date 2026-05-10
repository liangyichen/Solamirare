namespace Solamirare;



/// <summary>
/// 异步文件读写，等待版本（挂起等待，不占用CPU），但是主线程会阻塞， 不适合用于UI环境
/// </summary>
public static unsafe class IOCPIO
{

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


    /// <summary> 允许的最大并发异步 IO 任务数 </summary>
    private const int MaxPending = 16;
    /// <summary> 当前活跃任务计数 </summary>
    private static int _taskCount = 0;
    /// <summary> 保护提交逻辑的自旋锁 </summary>
    private static SpinLock _taskLock = new(false);


    internal static MemoryPoolCluster memoryPool;


    static IOCPIO()
    {

        Span<MemoryPoolSchema> schemas = stackalloc MemoryPoolSchema[] {
            new MemoryPoolSchema(64, 128),
            new MemoryPoolSchema(192, 128),
            new MemoryPoolSchema(256, 128)
        };


        memoryPool.Init(schemas);

    }


    /// <summary>
    /// 提交异步读取任务
    /// </summary>
    public static AsyncFilesIOContext ReadAsync(UnManagedCollection<char> path, UnManagedMemory<byte>* result, ulong offset = 0)
    {
        AsyncFilesIOContext context = new AsyncFilesIOContext();

        // 1. 尽早检查并发限制
        if (Volatile.Read(ref _taskCount) >= MaxPending) return context;

        // 2. 从内存池分配上下文，并检查 null
        var allocResult = memoryPool.Alloc((nuint)sizeof(IOCPContext));
        if (allocResult.Address == null) return context;

        context.IOCPContext = (IOCPContext*)allocResult.Address;

        context.Platform = IOAsyncContextPlatform.Windows;


        // 3. 打开文件，使用正确的 INVALID_HANDLE_VALUE 检查
        context.IOCPContext->hFile = WindowsAPI.CreateFileW(path.InternalPointer, GENERIC_READ, FILE_SHARE_READ, null, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, null);

        if (context.IOCPContext->hFile == (void*)(-1) || context.IOCPContext->hFile == null)
        {
            memoryPool.Return(context.IOCPContext, (ulong)sizeof(IOCPContext));
            return context;
        }

        context.IOCPContext->hEvent = WindowsAPI.CreateEventW(null, true, false, null);
        context.IOCPContext->Overlapped.Offset = (uint)(offset & 0xFFFFFFFF);
        context.IOCPContext->Overlapped.OffsetHigh = (uint)(offset >> 32);
        context.IOCPContext->Overlapped.hEvent = context.IOCPContext->hEvent;
        context.IOCPContext->isDone = false;

        // 检查事件句柄创建是否成功
        if (context.IOCPContext->hEvent == null)
        {
            CleanupFail(context.IOCPContext);
            return context;
        }

        uint dummy = 0;


        // 4. 获取文件长度并执行 16MB 上限检查
        long fileSize = 0;

        if (WindowsAPI.GetFileSizeEx(context.IOCPContext->hFile, &fileSize))
        {
            if (fileSize < 0 || fileSize > 16777216)
            {
                CleanupFail(context.IOCPContext);
                return context;
            }
            // 确保非托管内存空间足够，并更新其有效长度标记
            result->EnsureCapacity((uint)fileSize);
            result->ReLength((uint)fileSize);
        }
        else
        {
            // 步骤：获取长度失败需清理资源并返回
            context.IOCPContext->ErrorCode = (uint)Marshal.GetLastWin32Error();
            CleanupFail(context.IOCPContext);
            return context;
        }

        // 5. 在锁内原子化增加计数并执行 ReadFile
        bool lockTaken = false;
        try
        {
            _taskLock.Enter(ref lockTaken);
            if (_taskCount < MaxPending)
            {
                _taskCount++;
                
                bool statusOfRead = WindowsAPI.ReadFile(context.IOCPContext->hFile, result->Pointer, result->UsageSize, &dummy, &context.IOCPContext->Overlapped);
            
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
                CleanupFail(context.IOCPContext);
            }
        }
        finally { if (lockTaken) _taskLock.Exit(); }

        return context;
    }

    /// <summary>
    /// 提交异步写入任务
    /// </summary>
    public static AsyncFilesIOContext WriteAsync(UnManagedCollection<char> path, UnManagedCollection<byte> content, ulong offset = 0)
    {
        AsyncFilesIOContext context = new AsyncFilesIOContext();

        if (Volatile.Read(ref _taskCount) >= MaxPending) return context;

        var allocResult = memoryPool.Alloc((nuint)sizeof(IOCPContext));
        if (allocResult.Address == null) return context;

        context.IOCPContext = (IOCPContext*)allocResult.Address;

        context.Platform = IOAsyncContextPlatform.Windows;


        context.IOCPContext->hFile = WindowsAPI.CreateFileW(path.InternalPointer, GENERIC_WRITE, FILE_SHARE_WRITE, null, CREATE_ALWAYS, FILE_FLAG_OVERLAPPED, null);

        if (context.IOCPContext->hFile == (void*)(-1) || context.IOCPContext->hFile == null)
        {
            memoryPool.Return(context.IOCPContext, (ulong)sizeof(IOCPContext));
            return context;
        }

        context.IOCPContext->hEvent = WindowsAPI.CreateEventW(null, true, false, null);
        context.IOCPContext->Overlapped.Offset = (uint)(offset & 0xFFFFFFFF);
        context.IOCPContext->Overlapped.OffsetHigh = (uint)(offset >> 32);
        context.IOCPContext->Overlapped.hEvent = context.IOCPContext->hEvent;
        context.IOCPContext->isDone = false;

        if (context.IOCPContext->hEvent == null)
        {
            CleanupFail(context.IOCPContext);
            return context;
        }

        uint dummy = 0;

        bool lockTaken = false;
        try
        {
            _taskLock.Enter(ref lockTaken);
            if (_taskCount < MaxPending)
            {
                _taskCount++;
                
                bool statusOfWrite = WindowsAPI.WriteFile(context.IOCPContext->hFile, content.InternalPointer, content.Size, &dummy, &context.IOCPContext->Overlapped);
                
                // if (!statusOfWrite)
                // {
                //     uint err = (uint)Marshal.GetLastWin32Error();
                //     if (err != ERROR_IO_PENDING) ctx->ErrorCode = err;
                // }
            }
            else
            {
                if (lockTaken) { _taskLock.Exit(); lockTaken = false; }
                CleanupFail(context.IOCPContext);
            }
        }
        finally { if (lockTaken) _taskLock.Exit(); }

        return context;
    }

    /// <summary>
    /// 等待任务完成并释放资源
    /// </summary>
    internal static uint Wait(IOCPContext* ctx)
    {
        if (ctx->hFile == null || ctx->hFile == (void*)(-1) || ctx->isDone) return 0;

        uint bytesTransferred = 0;

        // 如果 IO 挂起，则等待事件触发
        if (ctx->ErrorCode == ERROR_IO_PENDING)
        {
            WindowsAPI.WaitForSingleObject(ctx->hEvent, INFINITE);
        }

        // 获取结果
        WindowsAPI.GetOverlappedResult(ctx->hFile, &ctx->Overlapped, &bytesTransferred, false);

        ctx->isDone = true;

        // 完成后递减计数
        Interlocked.Decrement(ref _taskCount);

        return bytesTransferred;
    }

    /// <summary>
    /// 关闭文件操作
    /// </summary>
    /// <param name="ctx"></param>
    internal static void Close(IOCPContext* ctx)
    {
        // 步骤：分别验证句柄有效性，防止关闭 0(stdin) 或 -1
        if (ctx->hEvent != null && ctx->hEvent != (void*)(-1))
        {
            WindowsAPI.CloseHandle(ctx->hEvent);
            ctx->hEvent = null;
        }
        if ((nint)ctx->hFile > 0 && ctx->hFile != (void*)(-1))
        {
            WindowsAPI.CloseHandle(ctx->hFile);
            ctx->hFile = null;
        }
    }

    /// <summary>
    /// 内部辅助方法：处理任务同步阶段失败时的资源回收
    /// </summary>
    private static void CleanupFail(IOCPContext* ctx)
    {
        if (ctx == null) return;
        Close(ctx);
        memoryPool.Return(ctx, (ulong)sizeof(IOCPContext));
    }

}
