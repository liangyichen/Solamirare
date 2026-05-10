
/// <summary>
/// 同步IO
/// </summary>
public unsafe class FilesIO_SYNC
{



    public static bool AppendContent()
    {

        UnManagedMemory<char> file_path = ValueFilesIO.ProcessRelativePath("txtfiles/test_append.txt");

        Span<char> span_FilePath = file_path.AsSpan();

        bool result = false;

        ReadOnlySpan<char> content = "test";

        if (ValueFilesIO.WriteTextToFile(content, span_FilePath))
        {
            bool writeAgain = ValueFilesIO.AppendText(",append this chars", span_FilePath);

            UnManagedMemory<char> newContent = ValueFilesIO.ReadTextFile(span_FilePath);

            bool newContentValidate = newContent.Equals("test,append this chars");

            newContent.Dispose();

            bool delete_result = ValueFilesIO.DeleteFile(span_FilePath);

            result = writeAgain && newContentValidate && delete_result;
        }

        file_path.Dispose();

        return result;

    }



    public static bool ReadTextFile()
    {


        char* path_buffer = stackalloc char[(int)ValueFilesIO.MAX_PATH_LENGTH];

        ValueFilesIO.ProcessRelativePath("txtfiles/s1.txt", path_buffer, out int pathLength);

        Span<char> span_FilePath = new Span<char>(path_buffer, pathLength);



        int resultBufferSize = 64;

        //【mac BenchmarkDotNet错误：】
        // macOS 上的 C 库分配器（可能是 libmalloc 或 jemalloc 的变种）通常使用线程本地缓存 (Thread-Local Caching) 来提高速度。BDN 的高性能环境和 P/Invoke 的高频调用会大量利用这个缓存。
        // 调用 NativeMemory.Alloc/Free 时，它改变了 C 库堆 的状态。这种状态改变（比如分配器元数据的更新、缓存块的合并或分裂）可能恰好破坏了或改变了 read 系统调用依赖的内存布局或元数据。
        // mac 上运行必须使用栈分配（Windows 与 Fedora 上运行可以使用堆内存分配）
        // 甚至是在 mac 上运行的时候在这里通过 NativeMemory.Alloc 分配一个完全无关的独立内存段都不行
        char* stack_temp = stackalloc char[resultBufferSize];

        UnManagedMemory<char> text = new UnManagedMemory<char>(stack_temp, (uint)resultBufferSize, 0);


        ValueFilesIO.ReadTextFile(span_FilePath, &text, (uint)resultBufferSize);


        //【mac BenchmarkDotNet错误：】
        // MAC 上运行 BenchmarkDotNet 必须保留以下“无意义”的循环。 （Fedora 上去掉以下循环后正确运行）
        // 目前认定为  macOS CoreCLR JIT Bug
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            for (int i = 0; i < text.UsageSize; i++)
            {
                char c = *text[0];
            }
        }


        bool result = text.UsageSize > 0;

        text.Dispose();

        return result;
    }




    public static bool FileExists()
    {
        UnManagedMemory<char> file_path = ValueFilesIO.ProcessRelativePath("txtfiles/g1.txt");

        bool exist = ValueFilesIO.FileExists(file_path);

        file_path.Dispose();

        return exist;
    }


    public static bool FileBytesSize()
    {
        UnManagedMemory<char> file_path = ValueFilesIO.ProcessRelativePath("txtfiles/g1.txt");


        long size = ValueFilesIO.FileBytesSize(file_path.AsSpan());

        file_path.Dispose();

        return size > 0;
    }

    public static bool WriteTextToFile()
    {
        UnManagedMemory<char> path_buffer = ValueFilesIO.ProcessRelativePath("txtfiles/test_WriteTextToFile.txt");

        Span<char> span_FilePath = path_buffer.AsSpan();

        bool result = false;

        ReadOnlySpan<char> content = "test";

        bool write_result = ValueFilesIO.WriteTextToFile(content, span_FilePath);

        if (write_result)
        {

            ReadOnlySpan<char> override_content = "override";

            bool writeAgain = ValueFilesIO.WriteTextToFile(override_content, span_FilePath);


            int stack_size = 128;
            char* buffer_of_ReadTextFile = stackalloc char[stack_size];

            UnManagedMemory<char> overrideContent = new UnManagedMemory<char>(buffer_of_ReadTextFile, (uint)stack_size, 0);


            ValueFilesIO.ReadTextFile(span_FilePath, &overrideContent, (uint)stack_size); //性能测试阶段的问题出在这里，检测到 null 

            bool newContent = overrideContent.Equals(override_content);

            bool delete = ValueFilesIO.DeleteFile(span_FilePath);

            result = writeAgain && newContent && delete;

        }

        path_buffer.Dispose();

        return result;
    }



    public static bool DeleteFile()
    {
        UnManagedMemory<char> file_path = ValueFilesIO.ProcessRelativePath("txtfiles/test_delete.txt");

        Span<char> span_FilePath = file_path.AsSpan();

        bool result = false;

        ReadOnlySpan<char> content = "test";

        if (ValueFilesIO.WriteTextToFile(content, span_FilePath))
        {

            result = ValueFilesIO.DeleteFile(span_FilePath);

        }

        file_path.Dispose();

        return result;
    }


}
