

// 模拟 UI 或业务层的上下文对象
public struct UserState
{
    public int TaskId;
    
    public long StartTicks;
}


/// <summary>
/// 文件读写，异步回调函数风格
/// </summary>
public static unsafe class FilesIOWithCallBack
{
    public static void ReadAndWrite()
    {
        // 准备一个自定义参数，证明我们可以透传状态
        UserState* state = (UserState*)NativeMemory.Alloc((nuint)sizeof(UserState));
        state->TaskId = 101;
        state->StartTicks = DateTime.Now.Ticks;

        Console.WriteLine($"[Main] 发起异步写入任务 ID: {state->TaskId}...");

        // 步骤 1: 发起异步写入
        AsyncFilesIOWithCallBack.WriteAsync("AsyncIOCallbackTest.txt", "Hello Callback! This is Zero-GC Async IO."u8, &OnWriteCompleted, state);

        // UI 线程依然可以做别的事情，这里用 ReadLine 模拟
        Console.WriteLine("[Main] UI 线程现在是空闲的，可以响应用户输入...");
        Console.ReadLine();
    }

    /// <summary>
    /// 步骤 2: 写入完成后的回调 (在 ReaperThread 中执行)
    /// </summary>
    [UnmanagedCallersOnly]
    public static unsafe void OnWriteCompleted(void* args)
    {
        UserState* state = (UserState*)args;
        Console.WriteLine($"[Step 2] 写入已确认。耗时: {(DateTime.Now.Ticks - state->StartTicks) / 10000} ms");

        // 步骤 3: 链式发起异步读取
        Console.WriteLine("[Step 2] 正在发起回读...");
        AsyncFilesIOWithCallBack.ReadAsync("AsyncIOCallbackTest.txt", &OnReadCompleted, state);
    }

    /// <summary>
    /// 步骤 4: 读取完成后的打印回调 (在 ReaperThread 中执行)
    /// </summary>
    [UnmanagedCallersOnly]
    public static unsafe void OnReadCompleted(UnManagedMemory<byte>* result, void* args)
    {
        UserState* state = (UserState*)args;

        Console.WriteLine($"[Step 3] 读取已完成。数据大小: {result->UsageSize} 字节");

        // 打印读取到的内容
        Console.Write("内容展示: ");
        DebugHelper.PrintUtf8Buffer(result);
        Console.WriteLine();

        // 整个流程结束，清理自定义分配的内存
        NativeMemory.Free(state);
        Console.WriteLine("[Step 3] 流程闭环，所有资源已回收。");
    }
}