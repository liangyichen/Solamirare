

public static unsafe class CoroutineTest
{
    public static void Run()
    {
        
        // ══════════════════════════════════════════════════════════════════════════════
        //  第一步：初始化（整个进程只需调用一次）
        // ══════════════════════════════════════════════════════════════════════════════

        CoroutineStack.Initialize(
            totalSize: 32 * 1024 * 1024,
            slotSize: 512 * 1024);


        // ══════════════════════════════════════════════════════════════════════════
        //  第二步：为当前线程创建调度器
        //  创建后存起来，日常使用中不再需要传递给任何方法
        // ══════════════════════════════════════════════════════════════════════════

        void* scheduler = CoroutineScheduler.Create();
        if (scheduler == null) return;

        // ══════════════════════════════════════════════════════════════════════════
        //  第三步：准备业务数据
        //  业务结构体只需存 coroutine 指针，不再需要存 scheduler
        // ══════════════════════════════════════════════════════════════════════════

        CoroutineData data;
        data.Coroutine = null;  // 创建协程后填入
        data.Value = 0;

        // ══════════════════════════════════════════════════════════════════════════
        //  第四步：创建协程
        //  scheduler 只在 Create 时传入一次，之后对调用方透明
        // ══════════════════════════════════════════════════════════════════════════

        void* coroutine = Coroutine.Create(scheduler, &MyEntry, &data);
        data.Coroutine = coroutine; // 填入 coroutine 指针供入口函数使用

        // ══════════════════════════════════════════════════════════════════════════
        //  第五步：驱动协程执行
        //  只需要 coroutine 指针，不需要传 scheduler
        // ══════════════════════════════════════════════════════════════════════════

        Console.WriteLine("[主线程] 准备第一次 Resume");

        Coroutine.Resume(coroutine);
        Console.WriteLine($"[主线程] 第一次 Yield 返回，Value = {data.Value}");

        Coroutine.Resume(coroutine);
        Console.WriteLine($"[主线程] 第二次 Yield 返回，Value = {data.Value}");

        Coroutine.Resume(coroutine);
        Console.WriteLine($"[主线程] 协程完成，IsFinished = {Coroutine.IsFinished(coroutine)}");


        // ══════════════════════════════════════════════════════════════════════════
        //  第六步：释放资源
        //  栈槽已在最后一次 Resume 时自动归还
        //  只需释放协程结构体和调度器
        // ══════════════════════════════════════════════════════════════════════════

        Coroutine.Destroy(coroutine);
        CoroutineScheduler.Destroy(scheduler);
        CoroutineStack.Dispose();

        Console.WriteLine("[主线程] 资源已释放");

        CoroutineStack.Initialize(
            totalSize: 32 * 1024 * 1024,
            slotSize: 512 * 1024);

        void* scheduler2 = CoroutineScheduler.Create();
        if (scheduler2 != null)
        {
            CoroutineScheduler.Destroy(scheduler2);
            Console.WriteLine("[主线程] 调度器重建检查通过");
        }

        CoroutineStack.Dispose();
    }




    // ══════════════════════════════════════════════════════════════════════════════
    //  业务数据结构体
    //  只存 coroutine 指针，不再需要 scheduler
    // ══════════════════════════════════════════════════════════════════════════════

    unsafe struct CoroutineData
    {
        /// <summary>
        /// 当前协程指针，用于在入口函数内部调用 Yield。
        /// 不再需要存储 scheduler。
        /// </summary>
        public void* Coroutine;

        /// <summary>示例业务数据。</summary>
        public int Value;
    }

    // ══════════════════════════════════════════════════════════════════════════════
    //  协程入口函数
    //  Yield 只需传 coroutine 指针，不需要 scheduler
    // ══════════════════════════════════════════════════════════════════════════════

    [UnmanagedCallersOnly]
    static unsafe void MyEntry(void* param)
    {
        CoroutineData* data = (CoroutineData*)param;

        data->Value = 100;
        Console.WriteLine($"[协程] 阶段一，Value = {data->Value}");
        Coroutine.Yield(data->Coroutine);  // 只传 coroutine，不传 scheduler

        data->Value = 200;
        Console.WriteLine($"[协程] 阶段二，Value = {data->Value}");
        Coroutine.Yield(data->Coroutine);

        data->Value = 300;
        Console.WriteLine($"[协程] 阶段三，自然返回");
    }

    // ══════════════════════════════════════════════════════════════════════════════
    //  预期输出：
    //
    //  [主线程] 准备第一次 Resume
    //  [协程] 阶段一，Value = 100
    //  [主线程] 第一次 Yield 返回，Value = 100
    //  [协程] 阶段二，Value = 200
    //  [主线程] 第二次 Yield 返回，Value = 200
    //  [协程] 阶段三，自然返回
    //  [主线程] 协程完成，IsFinished = True
    //  [调试] Resume=3 Yield=2 ResumeAfterFinished=0 Mismatch=0
    //  [主线程] 资源已释放
    // ══════════════════════════════════════════════════════════════════════════════

}