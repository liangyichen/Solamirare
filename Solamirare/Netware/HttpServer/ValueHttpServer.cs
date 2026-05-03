
namespace Solamirare;


/// <summary>
/// HTTP 服务器
/// </summary>
public unsafe struct ValueHttpServer
{
    bool isRunning;
    HTTPSeverConfig* config;

    // Windows 分支：记录启动的工作线程数量，Stop 时用于投递等量哨兵
    int windows_worker_thread_count;

    UnManagedMemory<nint> Threads;

    bool disposed;

    internal void Init(HTTPSeverConfig* config)
    {
        isRunning = false;
        this.config = config;
        windows_worker_thread_count = 0;
        Threads = new UnManagedMemory<nint>(8);
        disposed = false;
    }

    internal void Start()
    {
        if (isRunning || disposed) return;
        isRunning = true;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            ThreadStartInfo info = new ThreadStartInfo();
            info.Worker = &LinuxServerThreadWorker;
            info.Arg = config;
            NativeThread.Create(out void* handle, &info);
            Threads.Add((nint)handle);
            Thread.Sleep(100);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            //kqueue 仅需要单线程运行即可接近网络峰值， 多线程没有意义， 并且多线程的配合机制太过复杂，考虑到macOS版本服务器绝大多数情况下仅仅是用于开发环境，
            //因此放弃多线程支持，单线程版本的性能已经足够好。

            ThreadStartInfo info = new ThreadStartInfo();
            info.Worker = &MacOSServerThreadWorker;
            info.Arg = config;
            NativeThread.Create(out void* handle, &info);
            Threads.Add((nint)handle);
            Thread.Sleep(100);

        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            int n = Environment.ProcessorCount;

            // 只创建一个 WindowsHttpServer 实例，持有唯一的 IOCP
            WindowsHttpServer* server = (WindowsHttpServer*)NativeMemory.AllocZeroed((nuint)sizeof(WindowsHttpServer));
            server->Init(config);

            server->StartListening(config->ResponseCallback);


            // 启动 N 个工作线程，共享同一个 IOCP
            windows_worker_thread_count = n;
            for (int i = 0; i < n; i++)
            {
                ThreadStartInfo info = new ThreadStartInfo();
                info.Worker = &WindowsServerWorkerThreadWorker;
                info.Arg = server;

                NativeThread.Create(out void* handle, &info);
                Threads.Add((nint)handle);

                Thread.Sleep(100);
            }
        }
    }

    /// <summary>
    /// 停止服务器
    /// </summary>
    public void Stop()
    {
        if (!isRunning) return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            LinuxServerBootstrap.Stop(config->Instances);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            foreach (nint* i in config->Instances)
            {
                if (i is not null && *i != 0)
                {
                    MacOSHttpServer* server = (MacOSHttpServer*)*i;
                    server->Stop();
                    if (server->Dispose())
                        NativeMemory.Free(server);
                }
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            foreach (nint* i in config->Instances)
            {
                WindowsHttpServer* server = (WindowsHttpServer*)*i;

                // 传入工作线程数，Stop 内部向 IOCP 投递等量哨兵确保所有 WorkerThread 都能退出
                server->Stop(windows_worker_thread_count);

                // 等待所有工作线程退出后再释放内存
                while (!server->Stopped)
                    Thread.Yield();

                server->Dispose();
                NativeMemory.Free(server);
            }
        }

        config->Instances.Dispose();
        config->MemoryPool.Dispose();

        NativeMemory.Free(config);

        Threads.Dispose();

        isRunning = false;
        disposed = true;
    }

    // =========================================================================
    // 线程入口
    // =========================================================================

    [UnmanagedCallersOnly]
    private static uint WindowsServerListenThreadWorker(void* arg)
    {
        WindowsHttpServer* server = (WindowsHttpServer*)arg;
        server->StartListening((delegate*<UHttpContext*, bool>)server->ServerConfig->ResponseCallback);
        return 0;
    }

    [UnmanagedCallersOnly]
    private static uint WindowsServerWorkerThreadWorker(void* arg)
    {
        WindowsHttpServer* server = (WindowsHttpServer*)arg;
        server->WorkerLoop();
        return 0;
    }

    [UnmanagedCallersOnly]
    private static uint LinuxServerThreadWorker(void* arg)
    {
        HTTPSeverConfig* config = (HTTPSeverConfig*)arg;
        LinuxServerBootstrap.Start(config);
        return 0;
    }

    [UnmanagedCallersOnly]
    private static uint MacOSServerThreadWorker(void* arg)
    {
        HTTPSeverConfig* config = (HTTPSeverConfig*)arg;

        MacOSHttpServer* server = (MacOSHttpServer*)NativeMemory.AllocZeroed((nuint)sizeof(MacOSHttpServer));
        *server = new MacOSHttpServer(config);
        config->Instances.Add((nint)server);
        server->Start(config->ResponseCallback);

        Thread.Sleep(100);

        return 0;
    }
}