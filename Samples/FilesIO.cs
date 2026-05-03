

/// <summary>
/// 文件读写，同步风格
/// </summary>
public static unsafe class FilesIO
{
    /// <summary>
    /// 当前所在线程会进入让渡状态（无损耗），适合封装在线程中执行
    /// </summary>
    public static void ReadAndWrite()
    {

        ReadOnlySpan<char> path = "async_result.txt";

        AsyncFilesIOContext writeCtx = AsyncFilesIO.WriteAsync(path, "This is Async IO W/R."u8);

        // --- 进入让渡状态，线程睡眠直到 IO 完成 ---
        uint written = writeCtx.Wait();

        Console.WriteLine($"Bytes written: {written}");

        writeCtx.Close();

        writeCtx.Dispose();



        UnManagedMemory<byte> result = new UnManagedMemory<byte>();

        AsyncFilesIOContext readCtx = AsyncFilesIO.ReadAsync(path, &result);

        uint bytesRead = readCtx.Wait();


        DebugHelper.PrintUtf8Buffer(result);

        readCtx.Close();

        readCtx.Dispose();

        result.Dispose();


        Console.ReadLine();
    }
}
