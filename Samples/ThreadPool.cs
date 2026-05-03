

public static unsafe class ThreadPoolUsage
{
     
    public static void Run()
    {
        Console.WriteLine(">>> 正在初始化原生线程池...");

        NativeThreadPool* pool = stackalloc NativeThreadPool[1];

        // 1. 初始化：4个工作线程，队列容量 128
        pool->Init(threadCount: 4, queueSize: 128);

        Console.WriteLine(">>> 正在提交任务...");

        // 2. 提交任务
        for (int i = 0; i < 10; i++)
        {
            // 模拟分配一些非托管数据作为参数
            // 在实际场景中，这可能是 NativeMemory.Alloc 分配的结构体指针
            int* arg = (int*)NativeMemory.Alloc(sizeof(int));
            *arg = i;

            // 提交任务：传递函数指针 (&TaskCallback) 和参数 (arg)
            // 注意：TaskCallback 必须标记为 [UnmanagedCallersOnly]
            if (!pool->Enqueue(&TaskCallback, arg))
            {
                Console.WriteLine($"任务 {i} 提交失败：队列已满");
                NativeMemory.Free(arg); // 提交失败需手动释放内存
            }
        }

        Console.WriteLine(">>> 任务已提交，等待执行...");

        // 简单等待，让后台线程有机会执行输出
        System.Threading.Thread.Sleep(1000);

        Console.WriteLine(">>> 正在销毁线程池...");

        // 3. 销毁
        pool->Dispose();

        Console.WriteLine(">>> 测试完成。");
    }

    // --- 任务回调函数 ---
    [UnmanagedCallersOnly]
    private static void TaskCallback(void* context)
    {
        int taskId = *(int*)context;

        // 模拟工作负载
        Console.WriteLine($"[线程 {NativeThread.GetCurrentThreadId()}] 正在处理任务 ID: {taskId}");

        // 清理参数内存（谁分配谁释放原则）
        NativeMemory.Free(context);
    }
}
