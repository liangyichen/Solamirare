using System.Text;

namespace Solamirare;


internal unsafe class ZeroIO_Windows : ISyncFilesIO
{



    private const uint FILE_APPEND_DATA = 0x00000004;

    private const uint GENERIC_READ = 0x80000000;

    private const uint FILE_SHARE_READ = 0x00000001;

    private const uint OPEN_EXISTING = 3;

    private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

    private const uint GENERIC_WRITE = 0x40000000;

    private const uint CREATE_ALWAYS = 2;  // Creates a new file, always

    private const uint OPEN_ALWAYS = 4;  // Open file if it exists, create it if it doesn't

    private const uint FILE_SHARE_WRITE = 2;



    public bool DeleteFile(ReadOnlySpan<char> path)
    {
        if (path.Length <= ValueFilesIO.MAX_STACK_SIZE)
        {
            char* stackPtr = stackalloc char[path.Length + 1];

            Span<char> stackBuffer = new Span<char>(stackPtr, path.Length + 1);

            path.CopyTo(stackBuffer);

            stackBuffer[path.Length] = '\0';

            return WindowsAPI.DeleteFileW(stackPtr);

        }
        else
        {
            return false;
        }

    }


    public bool AppendText(ReadOnlySpan<char> content, ReadOnlySpan<char> filePath)
    {

        int bytesLength = Encoding.UTF8.GetByteCount(content);


        byte* bytes = null;


        if (bytesLength <= 1024)
        {
            byte* _contentPtr = stackalloc byte[bytesLength];

            bytes = _contentPtr;
        }
        else
        {
            bytes = (byte*)NativeMemory.AllocZeroed((nuint)bytesLength);
        }

        Encoding.UTF8.GetBytes(content, new Span<byte>(bytes, bytesLength));


        bool result = writeBytes(bytes, (uint)bytesLength, filePath, true);


        if (bytesLength > 1024 && bytes is not null)
        {
            NativeMemory.Free(bytes);
        }

        return result;
    }


    bool writeBytes(byte* bytes, uint bytesLength, ReadOnlySpan<char> filePath, bool append = false)
    {
        if (bytes is null || filePath.IsEmpty || filePath.Length > ValueFilesIO.MAX_STACK_SIZE)
            return false;

        char* ptr = stackalloc char[filePath.Length + 1];

        Span<char> stackBuffer = new Span<char>(ptr, filePath.Length + 1);

        filePath.CopyTo(stackBuffer);

        stackBuffer[filePath.Length] = '\0';


        char* hFile;

        uint dwDesiredAccess = append ? FILE_APPEND_DATA : GENERIC_WRITE;

        uint dwCreationDisposition = append ? OPEN_EXISTING : CREATE_ALWAYS;

        hFile = (char*)WindowsAPI.CreateFileW(
            ptr,
            dwDesiredAccess,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            null,
            dwCreationDisposition,
            FILE_ATTRIBUTE_NORMAL,
            null);


        if (hFile == null)
        {
            return false;
        }

        try
        {
            bool success = WindowsAPI.WriteFile(hFile, bytes, bytesLength, out uint bytesWritten, null);
        }
        finally
        {
            if (hFile != null)
            {
                bool flushResult = WindowsAPI.FlushFileBuffers(hFile);
                if (flushResult)
                {
                    WindowsAPI.CloseHandle(hFile);
                }
            }
        }

        return true;
    }



    public bool WriteBytesToFile(byte* bytes, uint bytesLength, ReadOnlySpan<char> filePath)
    {
        return writeBytes(bytes, bytesLength, filePath, false);
    }



    public bool WriteTextToFile(ReadOnlySpan<char> content, ReadOnlySpan<char> filePath)
    {
        if (filePath.IsEmpty) return false;

        // Convert content to UTF-8 byte* (using Encoding.UTF8)
        int contentLength = Encoding.UTF8.GetByteCount(content);
        byte* contentPtr;

        // windows 的栈内存小，为了安全起见，只要 1024 即可。
        if (contentLength <= 1024)
        {
            byte* _contentPtr = stackalloc byte[contentLength];  // Stack-based buffer
            contentPtr = _contentPtr;
        }
        else
        {
            // Otherwise, use NativeMemory.Alloc to allocate from the heap
            contentPtr = (byte*)NativeMemory.AllocZeroed((nuint)contentLength);
        }

        // Convert content to bytes
        Encoding.UTF8.GetBytes(content, new Span<byte>(contentPtr, contentLength));

        // Write content to file using WriteBytesToFile
        bool result = WriteBytesToFile(contentPtr, (uint)contentLength, filePath);

        // Free allocated memory if heap-based buffer was used
        if (contentLength > 1024)
        {
            NativeMemory.Free(contentPtr);
        }

        return result;
    }



    /// <summary>
    /// 读取文本文件
    /// <para>在不确定外部传入的 UnManagedMemory&lt;char&gt; 结果对象是否能够容纳文件容量时，因为可能会发生扩容操作（堆内存模式），它的 Pointer 地址有可能会改变</para>
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="externalMemory"></param>
    /// <param name="stack_size_limit">设置内部使用栈空间作为 Buffer 读取的容量</param>
    /// <returns>是否成功读取</returns>
    public bool ReadTextFile(ReadOnlySpan<char> filePath, UnManagedString* externalMemory, uint stack_size_limit = 1024)
    {
        bool result = false;

        if (filePath.IsEmpty) return false;

        uint bytesRead;

        if (stack_size_limit > 4096) stack_size_limit = 4096;

        if (stack_size_limit == 0) stack_size_limit = 64;


        int charCount = filePath.Length;

        if (charCount + 1 > 260)
        {
            return false;
        }

        char* pathBuffer = stackalloc char[charCount + 1];

        filePath.CopyTo(new Span<char>(pathBuffer, charCount));

        pathBuffer[charCount] = '\0';

        char* hFile = (char*)WindowsAPI.CreateFileW(pathBuffer, GENERIC_READ, FILE_SHARE_READ, null, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, null);

        if (hFile is null)
        {
            result = false;
        }
        else
        {
            WindowsAPI.GetFileSizeEx(hFile, out long fileSize);

            byte* byteBuffer;

            bool bufferOnece;

            if (fileSize < stack_size_limit)
            {
                byte* _yteBuffer = stackalloc byte[(int)fileSize];
                byteBuffer = _yteBuffer;
                bufferOnece = true;
            }
            else
            {
                byte* _yteBuffer = stackalloc byte[(int)stack_size_limit];
                byteBuffer = _yteBuffer;
                bufferOnece = false;
            }

            if (bufferOnece)
            {

                WindowsAPI.ReadFile(hFile, byteBuffer, (uint)fileSize, out bytesRead, IntPtr.Zero);

                WindowsAPI.CloseHandle(hFile);

                int decodedCharCount = Encoding.UTF8.GetCharCount(byteBuffer, (int)fileSize);

                bool _promise = externalMemory->EnsureRemainingCapacity(externalMemory->UsageSize + (uint)decodedCharCount, MemoryScaleMode.AppendEquals);

                if (_promise)
                {
                    Encoding.UTF8.GetChars(byteBuffer, (int)fileSize, externalMemory->Pointer + externalMemory->UsageSize, decodedCharCount);
                    externalMemory->ReLength((uint)decodedCharCount);
                }

                result = true;
            }
            else
            {
                UnManagedMemory<byte> temp = new UnManagedMemory<byte>((uint)fileSize);

                while (true)
                {
                    bool readed = WindowsAPI.ReadFile(hFile, byteBuffer, (uint)stack_size_limit, out bytesRead, IntPtr.Zero);

                    if (readed && bytesRead > 0)
                    {
                        temp.Add(byteBuffer, bytesRead);
                    }
                    else
                    {
                        break;
                    }
                }

                WindowsAPI.CloseHandle(hFile);

                if (temp.UsageSize > 0)
                {
                    temp.CopyToChars(externalMemory);

                    result = true;
                }
                else
                {
                    result = false;
                }

                temp.Dispose();
            }
        }



        return result;
    }



    /// <summary>
    /// 读取文件大小，单位是 Bytes
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public int FileBytesSize(ReadOnlySpan<char> path)
    {
        int charCount = path.Length;

        if (charCount > ValueFilesIO.MAX_STACK_SIZE)
        {
            return -1;
        }

        char* pathBuffer = stackalloc char[charCount + 1];

        path.CopyTo(new Span<char>(pathBuffer, charCount));

        pathBuffer[charCount] = '\0';


        long fileSize;

        char* hFile = (char*)WindowsAPI.CreateFileW(pathBuffer, GENERIC_READ, FILE_SHARE_READ, null, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, null);

        if (hFile is not null)
        {
            WindowsAPI.GetFileSizeEx(hFile, out fileSize);
            WindowsAPI.CloseHandle(hFile);
        }
        else
        {
            fileSize = -1;
        }

        return (int)fileSize;
    }

    /// <summary>
    /// 判断文件是否存在
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public bool FileExists(ReadOnlySpan<char> filePath)
    {

        if (filePath.IsEmpty || filePath.Length > ValueFilesIO.MAX_STACK_SIZE)
            return false;


        char* ptr = stackalloc char[filePath.Length + 1];

        Span<char> stackBuffer = new Span<char>(ptr, filePath.Length + 1);


        filePath.CopyTo(stackBuffer);


        stackBuffer[filePath.Length] = '\0';


        char* hFile = (char*)WindowsAPI.CreateFileW(
            ptr,
            GENERIC_READ,
            FILE_SHARE_READ,
            null,
            OPEN_EXISTING,
            0,
            null);


        if (hFile is null)
        {
            return false;
        }

        WindowsAPI.CloseHandle(hFile);

        return true;
    }









    /// <summary>
    /// 获取当前工作目录
    /// </summary>
    public UnManagedString AppContextBaseDirectory()
    {
        UnManagedString result = new UnManagedString();

        char* mem = (char*)NativeMemory.AllocZeroed((nuint)ValueFilesIO.MAX_PATH_LENGTH * sizeof(char));

        int resultPathLength = (int)WindowsAPI.GetCurrentDirectoryW(ValueFilesIO.MAX_PATH_LENGTH, mem);


        if (resultPathLength > 0 && resultPathLength < ValueFilesIO.MAX_PATH_LENGTH)
        {
            result.Add(mem, (uint)resultPathLength);
        }

        NativeMemory.Free(mem);


        return result;
    }

}