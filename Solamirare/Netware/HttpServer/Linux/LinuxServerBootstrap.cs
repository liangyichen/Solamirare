namespace Solamirare;

/// <summary>
/// Linux 服务器启动引导。
/// 在启动时读取内核版本号，按以下策略选择服务器实现：
///   · 内核 >= 6.15 → IoUringServer（io_uring 原生异步，性能最优）
///   · 内核 &lt;  6.15 → EPoolServer （epoll ET 模式，兼容旧内核）
///
/// 版本读取通过 uname(2) 系统调用完成，零 GC，无文件 I/O。
/// </summary>
public static unsafe class LinuxServerBootstrap
{




    static LinuxKernelVersion currentVersion;

    static LinuxKernelVersion Kernel_6_15_0;

    static bool useIO_URing;



    static LinuxServerBootstrap()
    {
        currentVersion = LinuxKernelVersionChecker.GetLinuxKernelVersion();

        Kernel_6_15_0 = new LinuxKernelVersion(6, 15, 0);

        useIO_URing = currentVersion >= Kernel_6_15_0;
    }



    /// <summary>
    /// 启动服务器。自动根据内核版本选择 IO_URing 或 EPool。
    /// </summary>
    /// <param name="config">服务器配置（调用方负责生命周期）。</param>
    public static void Start(
        HTTPSeverConfig* config)
    {
        if (useIO_URing)
        {
            StartIoUring(config);
        }
        else
        {
            StartEPool(config);
        }

    }

    /// <summary>
    /// 停止服务器。根据内核版本调用对应实现的停止方法，确保资源正确释放。
    /// </summary>
    public static void Stop(UnManagedMemory<nint> instances)
    {
        if (instances.IsEmpty) return;

        if (useIO_URing)
        {
            foreach(nint* i in instances)
            {
                IoUringServer* io_uring_server = (IoUringServer*)*i;
                io_uring_server->Stop();

                if(io_uring_server->Dispose())
                {
                    NativeMemory.Clear(io_uring_server, (nuint)sizeof(IoUringServer));

                    NativeMemory.Free(io_uring_server);
                }
            }
        }
        else
        {
            foreach(nint* i in instances)
            {
                EPoolServer* epool_server = (EPoolServer*)*i;
                epool_server->Stop();

                NativeMemory.Free(epool_server);
            }
        }
    }


    static void StartIoUring(
        HTTPSeverConfig* config)
    {

        IoUringOptimizationOptions options = new IoUringOptimizationOptions
        {
            enable_submission_queue_polling = true,
            enable_fixed_buffers_registration = true,
            enable_zero_copy_send = true,
            queue_depth_entries = 256
        };

        IoUringServer* io_uring_server = (IoUringServer*)NativeMemory.AllocZeroed((nuint)sizeof(IoUringServer));
        
        io_uring_server->Init(config, options);

        config->Instances.Add((nint)io_uring_server);

        io_uring_server->Start((delegate*<UHttpContext*, bool>)config->ResponseCallback);
    }

    static void StartEPool(
        HTTPSeverConfig* config)
    {
        
        EPoolServer* epool_server = (EPoolServer*)NativeMemory.AllocZeroed((nuint)sizeof(EPoolServer));
        
        epool_server->Init(config);

        config->Instances.Add((nint)epool_server); // 存储服务器实例指针，供停止时使用
        
        epool_server->Start((delegate*<UHttpContext*, bool>)config->ResponseCallback);
            
    }


}
