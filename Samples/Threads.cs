

public static unsafe class ThreadsTest
{


    /// <summary>
    /// 示例参数
    /// </summary>
    public struct ExampleThreadArgs
    {
        public int InputValue;
        public int ResultValue;
    }

    [UnmanagedCallersOnly]
    /// <summary>
    /// Windows 线程工作函数示例
    /// </summary>
    private static uint ExampleWorker(void* arg)
    {
        ExampleThreadArgs* args = (ExampleThreadArgs*)arg;
        args->ResultValue = args->InputValue * 2;
        return 0;
    }


    public static bool Test()
    {
        // 1. 在栈上分配线程句柄 (pthread_t 在 macOS 上是指针)
        void* threadId;

        // 2. 在栈上分配参数
        // 注意：如果线程是 Detach 的或者主线程不等待，则必须分配在非托管堆 (NativeMemory.Alloc) 上
        // 这里因为使用了 Join，栈内存是安全的
        ExampleThreadArgs args;
        args.InputValue = 1024;
        args.ResultValue = 0;

        // 3. 创建线程

        ThreadStartInfo info = new ThreadStartInfo();
        info.Worker = &ExampleWorker;
        info.Arg = &args;

        bool result = NativeThread.Create(out threadId, &info);

        if (result)
        {
            // 4. 等待线程完成
            NativeThread.Join(threadId);

            // 此时 args.ResultValue 应该已经被修改
            return args.ResultValue == 2048;
        }

        return false;
    }
}