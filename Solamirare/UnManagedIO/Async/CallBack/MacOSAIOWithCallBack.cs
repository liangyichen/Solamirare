using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;
namespace Solamirare;



/// <summary>
/// 异步文件读写，回调函数版本
/// </summary>
public static unsafe class MacOSAIOWithCallBack
{
    /// <summary> POSIX 标准中表示异步操作正在进行的错误码 </summary>
    private const int EINPROGRESS = 36;
    /// <summary> 允许的最大并发异步 IO 任务数 </summary>
    private const int MaxPending = 16;

    /// <summary> Reaper 线程每轮处理的最大任务批次大小 </summary>
    const int BatchSize = 1024;

    /// <summary> 非托管任务指针容器，用于存储所有待处理任务的上下文地址 </summary>
    private static UnManagedMemory<nint> _pendingTasks;

    /// <summary> 指向非托管内存中存储的当前任务总数的计数器指针 </summary>
    private static int* _taskCountPtr;

    /// <summary> Reaper 线程的挂起与唤醒信号，用于在无任务时减少 CPU 占用 </summary>
    private static readonly ManualResetEventSlim _reaperWait;

    /// <summary> 保护任务数组和计数器的自旋锁，确保提交和收割操作的线程安全 </summary>
    private static SpinLock _taskLock;

    /// <summary> 非托管内存池集群，用于分配上下文和控制块 </summary>
    private static MemoryPoolCluster memoryPool;

    /// <summary> 后台收割线程的句柄 </summary>
    static void* callbackThreadHandle;

    /// <summary> 静态分配的 IO 控制块指针列表，供 aio_suspend 使用 </summary>
    static macos_aiocb** waitList;

    /// <summary> 静态分配的上下文指针列表，与 waitList 一一对应 </summary>
    static MacOSAIOContextWithCallBack** ctxList;

    /// <summary>
    /// 静态构造函数：初始化同步原语、非托管内存容器、内存池及后台收割线程
    /// </summary>
    static MacOSAIOWithCallBack()
    {
        _reaperWait = new(false);

        _taskLock = new(false);

        // 1. 初始化非托管存储
        _pendingTasks = new UnManagedMemory<nint>(MaxPending, MaxPending);


        ThreadStartInfo info = new ThreadStartInfo();
        info.Worker = &ReaperLoop;
        info.Arg = null;


        NativeThread.Create(out callbackThreadHandle, &info);

        Thread.Sleep(10);//必须有这个等待，否则线程还没启动完成，后续操作就来了，会造成进程崩溃


        uint listSize = (uint)(BatchSize * sizeof(void*));


        memoryPool = new MemoryPoolCluster();

        Span<MemoryPoolSchema> schemas = stackalloc MemoryPoolSchema[] {
            new MemoryPoolSchema(64, 128),
            new MemoryPoolSchema(128, 128),
            new MemoryPoolSchema(256, 128),
            new MemoryPoolSchema(listSize, 2),
        };

        memoryPool.Init(schemas);

        MemoryPollAllocatedResult allocTaskCount = memoryPool.Alloc(sizeof(int));
        _taskCountPtr = (int*)allocTaskCount.Address;
        *_taskCountPtr = 0;

        MemoryPollAllocatedResult allocWaitList = memoryPool.Alloc(listSize);
        waitList = (macos_aiocb**)allocWaitList.Address;

        MemoryPollAllocatedResult allocCtxList = memoryPool.Alloc(listSize);
        ctxList = (MacOSAIOContextWithCallBack**)allocCtxList.Address;

    }

    /// <summary>
    /// 后台收割循环：负责监听 IO 完成事件、触发回调并清理资源
    /// </summary>
    /// <param name="arg">线程启动参数</param>
    /// <returns>线程退出码</returns>
    [UnmanagedCallersOnly]
    private static uint ReaperLoop(void* arg)
    {
        while (true)
        {
            int count = 0;
            int currentBatch = 0;
            bool lockTaken = false;
            try
            {
                // 步骤 1: 进入锁保护区，获取当前待处理任务的快照
                _taskLock.Enter(ref lockTaken);
                count = *_taskCountPtr;
                if (count > 0)
                {
                    // 步骤 2: 计算当前批次大小，并将指针从全局容器复制到静态处理列表
                    currentBatch = Math.Min(count, BatchSize);
                    nint* basePtr = (nint*)_pendingTasks.Pointer;
                    for (int i = 0; i < currentBatch; i++)
                    {
                        MacOSAIOContextWithCallBack* ctx = (MacOSAIOContextWithCallBack*)basePtr[i];
                        ctxList[i] = ctx;
                        waitList[i] = ctx->cb;
                    }
                }
            }
            finally
            {
                // 步骤 3: 释放锁，允许其他线程在挂起等待期间继续提交任务
                if (lockTaken) _taskLock.Exit();
            }

            if (count == 0)
            {
                // 步骤 4: 若无任务，则进入阻塞等待状态，直到有新任务注册
                _reaperWait.Wait(10);
                _reaperWait.Reset();
                continue;
            }

            // --- 真正的内核挂起：aio_suspend ---
            // 只要 waitList 中有一个 I/O 完成，内核就会通过硬件中断唤醒线程
            if (MacOSAPI.aio_suspend(waitList, currentBatch, null) == 0)
            {
                // 步骤 5: 遍历当前批次，检查每个任务的 IO 状态
                for (int i = 0; i < currentBatch; i++)
                {
                    MacOSAIOContextWithCallBack* ctx = ctxList[i];
                    int err = MacOSAPI.aio_error(ctx->cb);

                    // 步骤 6: 如果状态不再是进行中，说明 IO 已完成（无论成功或失败）
                    if (err != EINPROGRESS)
                    {
                        // 1. 调用 aio_return 结束 IO 并释放内核维护的槽位
                        MacOSAPI.aio_return(ctx->cb);

                        // 2. 根据操作类型触发对应的用户回调函数
                        if (ctx->CallbackOnWrite != null) ctx->CallbackOnWrite(ctx->args);
                        else if (ctx->CallbackOnRead != null)
                        {
                            ctx->CallbackOnRead(&ctx->DataOnRead, ctx->args);
                            // 读取完成后，统一释放内部持有的数据缓冲区
                            ctx->DataOnRead.Dispose();
                        }

                        // 3. 关闭文件描述符
                        ctx->Close();

                        // 4. --- 线程安全的移除算法：Swap-to-back ---
                        bool removeLockTaken = false;
                        try
                        {
                            // 再次进入锁，因为需要修改全局待处理数组
                            _taskLock.Enter(ref removeLockTaken);
                            int liveCount = *_taskCountPtr;
                            nint* basePtr = (nint*)_pendingTasks.Pointer;

                            // 在全局列表中查找并移除该上下文（由于 Swap-to-back，位置可能已变动）
                            for (int k = 0; k < liveCount; k++)
                            {
                                if (basePtr[k] == (nint)ctx)
                                {
                                    int lastIdx = liveCount - 1;
                                    // 将数组末尾的元素移动到当前空出的槽位，保持数组紧凑
                                    if (k < lastIdx) basePtr[k] = basePtr[lastIdx];
                                    *_taskCountPtr = lastIdx;
                                    break;
                                }
                            }
                        }
                        finally
                        {
                            if (removeLockTaken) _taskLock.Exit();
                        }

                        // 5. 归还上下文和控制块内存到内存池
                        memoryPool.Return(ctx->cb, (ulong)sizeof(macos_aiocb));
                        memoryPool.Return(ctx, (ulong)sizeof(MacOSAIOContextWithCallBack));
                    }
                }
            }
        }

        return 0;
    }

    /// <summary>
    /// 异步写入
    /// </summary>
    /// <param name="path">目标文件路径</param>
    /// <param name="content">待写入的非托管集合内容</param>
    /// <param name="callback">写入完成后的回调函数</param>
    /// <param name="args">透传给回调的用户参数</param>
    /// <param name="offset">文件写入偏移量</param>
    public static void WriteAsync(ReadOnlySpan<char> path, UnManagedCollection<byte> content, delegate* unmanaged<void*, void> callback, void* args, long offset = 0)
    {
        if (path.IsEmpty) return;

        // 1. 尽早检查并发限制，避免无效的系统调用
        if (Volatile.Read(ref *_taskCountPtr) >= MaxPending) return;

        // 2. 从内存池分配任务上下文
        MemoryPollAllocatedResult allocCtx = memoryPool.Alloc((nuint)sizeof(MacOSAIOContextWithCallBack));

        if (allocCtx.Address == null) return;

        MacOSAIOContextWithCallBack* ctx = (MacOSAIOContextWithCallBack*)allocCtx.Address;
        // 步骤 1: 预初始化上下文，确保即使失败也能进行基本清理
        ctx->CallbackOnWrite = callback;
        ctx->args = args;
        ctx->fd = -1;
        ctx->DataOnRead = default;

        // 3. 从内存池分配 macos_aiocb 结构
        MemoryPollAllocatedResult allocCb = memoryPool.Alloc((nuint)sizeof(macos_aiocb));

        if (allocCb.Address == null)
        {
            CleanupFail(ctx);
            return;
        }

        ctx->cb = (macos_aiocb*)allocCb.Address;

        byte* pathBytes = stackalloc byte[path.Length * 3 + 1];
        path.CopyToBytes(pathBytes, (uint)(path.Length * 3 + 1));

        // O_WRONLY(0x0001) | O_CREAT(0x0200) | O_TRUNC(0x0400)
        int fd = MacOSAPI.open(pathBytes, 0x0001 | 0x0200 | 0x0400, 438);
        if (fd < 0) { CleanupFail(ctx); return; }



        ctx->fd = fd;
        ctx->cb->aio_fildes = fd;
        ctx->cb->aio_buf = content.InternalPointer;
        ctx->cb->aio_nbytes = content.Size;
        ctx->cb->aio_offset = offset;

        // 修复 Use-After-Free：必须在锁内原子化提交并注册
        bool lockTaken = false;
        try
        {
            // 步骤 5: 在获取锁的状态下，先检查并发数再执行提交，并立即记录指针
            _taskLock.Enter(ref lockTaken);
            if (*_taskCountPtr < MaxPending && MacOSAPI.aio_write(ctx->cb) == 0)
            {
                nint* basePtr = (nint*)_pendingTasks.Pointer;
                basePtr[*_taskCountPtr] = (nint)ctx;
                *_taskCountPtr += 1;
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
    /// <param name="path">源文件路径</param>
    /// <param name="callback">读取完成后的回调函数</param>
    /// <param name="args">透传给回调的用户参数</param>
    /// <param name="offset">文件读取偏移量</param>
    public static void ReadAsync(ReadOnlySpan<char> path, delegate* unmanaged<UnManagedMemory<byte>*, void*, void> callback, void* args, long offset = 0)
    {
        if (path.IsEmpty) return;

        // 1. 尽早检查并发限制
        if (Volatile.Read(ref *_taskCountPtr) >= MaxPending) return;

        // 2. 分配上下文内存
        MemoryPollAllocatedResult allocCtx = memoryPool.Alloc((nuint)sizeof(MacOSAIOContextWithCallBack));

        if (allocCtx.Address == null) return;

        MacOSAIOContextWithCallBack* ctx = (MacOSAIOContextWithCallBack*)allocCtx.Address;
        // 步骤 1: 初始化基本字段
        ctx->CallbackOnRead = callback;
        ctx->args = args;
        ctx->fd = -1;
        ctx->DataOnRead = default;

        // 步骤 2: 路径转换
        byte* pathBytes = stackalloc byte[path.Length * 3 + 1];
        path.CopyToBytes(pathBytes, (uint)(path.Length * 3 + 1));

        // 步骤 3: 以只读模式打开文件
        int fd = MacOSAPI.open(pathBytes, 0, 0); // O_RDONLY
        if (fd < 0) { CleanupFail(ctx); return; }

        // 步骤 4: 获取文件长度并执行 16MB 容量上限检查
        long seekLen = MacOSAPI.lseek(fd, 0, 2); // SEEK_END
        if (seekLen < 0 || seekLen > 16777216)
        {
            ctx->fd = fd;
            CleanupFail(ctx);
            return;
        }

        uint len = (uint)seekLen;
        MacOSAPI.lseek(fd, 0, 0);           // SEEK_SET

        // 步骤 5: 为读取结果准备非托管内存空间
        UnManagedMemory<byte> result;

        if (memoryPool.Support(len))
        {
            fixed (MemoryPoolCluster* p_memoryPool = &memoryPool)
                result = new UnManagedMemory<byte>(len, len, p_memoryPool);
        }
        else
        {
            result = new UnManagedMemory<byte>(len, len);
        }

        ctx->fd = fd;
        ctx->DataOnRead = result;

        // 步骤 6: 分配控制块并初始化参数
        MemoryPollAllocatedResult allocCb = memoryPool.Alloc((nuint)sizeof(macos_aiocb));
        if (allocCb.Address == null)
        {
            CleanupFail(ctx);
            return;
        }
        ctx->cb = (macos_aiocb*)allocCb.Address;

        ctx->cb->aio_fildes = fd;
        ctx->cb->aio_buf = result.Pointer;
        ctx->cb->aio_nbytes = result.UsageSize;
        ctx->cb->aio_offset = offset;

        // Use-After-Free：必须在锁内原子化提交并注册
        bool lockTaken = false;
        try
        {
            // 步骤 7: 在锁内原子化执行 aio_read 提交并将其注册到待处理数组
            _taskLock.Enter(ref lockTaken);
            if (*_taskCountPtr < MaxPending && MacOSAPI.aio_read(ctx->cb) == 0)
            {
                nint* basePtr = (nint*)_pendingTasks.Pointer;
                basePtr[*_taskCountPtr] = (nint)ctx;
                *_taskCountPtr += 1;
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
    /// 内部辅助方法：处理任务提交失败时的回调通知和资源回收
    /// </summary>
    /// <param name="ctx">任务上下文指针</param>
    private static void CleanupFail(MacOSAIOContextWithCallBack* ctx)
    {
        // 同步失败路径也必须触发回调，否则调用方会因为没有收到信号而导致业务逻辑阻塞
        if (ctx->CallbackOnWrite != null) ctx->CallbackOnWrite(ctx->args);
        else if (ctx->CallbackOnRead != null) ctx->CallbackOnRead(&ctx->DataOnRead, ctx->args);

        ctx->Close();
        // 如果是读取操作，确保释放已分配的非托管内存，防止泄漏
        if (ctx->DataOnRead.Activated)
        {
            ctx->DataOnRead.Dispose();
        }

        if (ctx->cb != null) memoryPool.Return(ctx->cb, (ulong)sizeof(macos_aiocb));
        memoryPool.Return(ctx, (ulong)sizeof(MacOSAIOContextWithCallBack));
    }


}
