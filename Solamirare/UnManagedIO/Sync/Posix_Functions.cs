using System.Text;

namespace Solamirare;


/// <summary>
/// MAC, LINUX 通用 IO 功能
/// </summary>
internal unsafe abstract class Posix_Functions : ISyncFilesIO
{



    /// <summary>
    /// 获取应用程序所在目录
    /// </summary>
    public UnManagedString AppContextBaseDirectory()
    {

        UnManagedString result = new UnManagedString();

        int maxPathBytesSize = (int)ValueFilesIO.MAX_PATH_LENGTH * sizeof(char);

        byte* buffer = stackalloc byte[maxPathBytesSize * 2];

        byte* resolvedPath = buffer + maxPathBytesSize;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // 获取当前工作目录
            MacOSAPI.getcwd(buffer, ValueFilesIO.MAX_PATH_LENGTH);

            // 解析绝对路径
            if (MacOSAPI.realpath(buffer, resolvedPath) == null)
            {
                return result;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // 获取当前工作目录
            LinuxAPI.getcwd(buffer, ValueFilesIO.MAX_PATH_LENGTH);

            // 解析绝对路径
            if (LinuxAPI.RealPath(buffer, resolvedPath) == null)
            {
                return result;
            }
        }
        else
        {
            return result;
        }

        int lastSlashIndex = 0;
        while (resolvedPath[lastSlashIndex] != '\0')
        {
            lastSlashIndex += 1;
        }

        int resultPathLength = Encoding.UTF8.GetCharCount(resolvedPath, lastSlashIndex);

        result.EnsureCapacity((uint)resultPathLength);

        Encoding.UTF8.GetChars(resolvedPath, lastSlashIndex, result.Pointer, resultPathLength);

        result.ReLength((uint)resultPathLength);

        return result;
    }




    /// <summary>
    /// 判断文件是否存在
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public bool FileExists(ReadOnlySpan<char> filePath)
    {
        int bytesSize = filePath.Length * 3 + 1;
        byte* pathBytes = stackalloc byte[bytesSize];
        filePath.CopyToBytes(pathBytes, (uint)bytesSize);

        // 使用 access 检查文件是否存在
        int _access;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            _access = MacOSAPI.access(pathBytes, 0);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            _access = LinuxAPI.access(pathBytes, 0);
        else
            return false;

        return _access == 0;
    }



    /// <summary>
    /// 把字节集合写入文件
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="bytesLength"></param>
    /// <param name="filePath"></param>
    public bool WriteBytesToFile(byte* bytes, uint bytesLength, ReadOnlySpan<char> filePath)
    {
        if (bytes is null || filePath.IsEmpty || filePath.Length > ValueFilesIO.MAX_PATH_LENGTH)
            return false;

        bool file1_success = writeFile(bytes, bytesLength, filePath);

        return file1_success;
    }


    int createFile(byte* filePathBytes, bool append)
    {
        int fd;
        int flags;
        // 权限通常统一设置，如果有区别可以在下方if中单独赋值
        uint mode = FileMode.S_IRWXU;

        // 1. 根据平台和模式计算 Flags
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (append)
            {
                // 注意：建议加上 O_CREAT，否则如果文件不存在（或刚被我们删除），append 会失败
                flags = (int)(OpenFlags_MacOS.O_APPEND | OpenFlags_MacOS.O_WRONLY | OpenFlags_MacOS.O_CREAT);
            }
            else
            {
                flags = (int)(OpenFlags_MacOS.O_CREAT | OpenFlags_MacOS.O_WRONLY | OpenFlags_MacOS.O_TRUNC);
            }

            // 2. 第一次尝试打开
            fd = MacOSAPI.open(filePathBytes, flags, (int)mode);

            // 3. 检查是否失败 (fd == -1)
            if (fd == -1)
            {
                // 尝试删除文件 (unlink 返回 0 表示成功，-1 表示失败，但我们主要关注执行动作)
                // 即使 unlink 失败（例如文件本就不存在），我们依然尝试再次 open，看看是否能恢复
                MacOSAPI.unlink(filePathBytes);

                // 4. 删除后，进行第二次尝试 (重试)
                fd = MacOSAPI.open(filePathBytes, flags, (int)mode);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (append)
            {
                flags = (int)(OpenFlags_Linux.O_WRONLY | OpenFlags_Linux.O_CREAT | OpenFlags_Linux.O_APPEND);
            }
            else
            {
                flags = (int)(OpenFlags_Linux.O_CREAT | OpenFlags_Linux.O_WRONLY | OpenFlags_Linux.O_TRUNC);
            }

            // 2. 第一次尝试打开
            fd = LinuxAPI.open(filePathBytes, flags, mode);

            // 3. 检查是否失败 (fd == -1)
            if (fd == -1)
            {
                // 尝试删除文件 (unlink 返回 0 表示成功，-1 表示失败，但我们主要关注执行动作)
                // 即使 unlink 失败（例如文件本就不存在），我们依然尝试再次 open，看看是否能恢复
                LinuxAPI.unlink(filePathBytes);

                // 4. 删除后，进行第二次尝试 (重试)
                fd = LinuxAPI.open(filePathBytes, flags, mode);
            }
        }
        else
        {
            fd = -1;
        }

        return fd;
    }


    /// <summary>
    /// 把字节集合写入文件
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="bytesLength"></param>
    /// <param name="filePath"></param>
    /// <param name="append">当前是否属于追加操作</param>
    bool writeFile(byte* bytes, uint bytesLength, ReadOnlySpan<char> filePath, bool append = false)
    {

        int bytesSize = filePath.Length * 3 + 1;

        byte* pathBytes = stackalloc byte[bytesSize];

        filePath.CopyToBytes(pathBytes, (uint)bytesSize);

        int fd = createFile(pathBytes, append);

        if (fd == -1)
        {
            int errcode = Marshal.GetLastPInvokeError();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                MacOSAPI.close(fd);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                LinuxAPI.close(fd);

            return false;
        }
        else
        {
            int chmod_result, writeSuccess, fsync_result, closeSuccess;
            bool result;


            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                //重置权限，一定要做，否则后续会出现即使是自己创建的文件，也不可读写
                chmod_result = MacOSAPI.fchmod(fd, FileMode.S_IRWXRWX);

                // 写入文件内容
                writeSuccess = MacOSAPI.write(fd, bytes, (nuint)bytesLength); // 0与正整数结果表示写入多少字节，-1表示本次操作失败

                fsync_result = MacOSAPI.fsync(fd); // 0 if success,

                closeSuccess = MacOSAPI.close(fd); // 0 if success,

                result = chmod_result == 0 && writeSuccess is not -1 && fsync_result == 0 && closeSuccess is not -1;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                //重置权限，一定要做，否则后续会出现即使是自己创建的文件，也不可读写
                chmod_result = LinuxAPI.fchmod(fd, FileMode.S_IRWXRWX);

                // 写入文件内容
                writeSuccess = LinuxAPI.write(fd, bytes, (nuint)bytesLength); // 0与正整数结果表示写入多少字节，-1表示本次操作失败

                fsync_result = LinuxAPI.fsync(fd); // 0 if success,

                closeSuccess = LinuxAPI.close(fd); // 0 if success,

                result = chmod_result == 0 && writeSuccess is not -1 && fsync_result == 0 && closeSuccess is not -1;
            }
            else
            {
                result = false;
            }



            return result;
        }
    }


    /// <summary>
    /// 把文本文件写入到文件，如果文件不存在，则创建；如果文件已经存在，则更新。
    /// </summary>
    /// <param name="content"></param>
    /// <param name="filePath"></param>
    public bool WriteTextToFile(ReadOnlySpan<char> content, ReadOnlySpan<char> filePath)
    {
        if (filePath.IsEmpty || filePath.Length > ValueFilesIO.MAX_PATH_LENGTH || content.IsEmpty)
            return false;

        // 计算内容的字节长度
        int contentBytesSize = Encoding.UTF8.GetByteCount(content);

        byte* contentBytes;

        bool _onHeap;

        if (contentBytesSize < ValueFilesIO.MAX_STACK_SIZE)
        {
            // 使用栈内存分配 buffer
            byte* _contentPtr = stackalloc byte[contentBytesSize];
            contentBytes = _contentPtr;
            _onHeap = false;
        }
        else
        {
            // 使用堆内存分配 buffer
            contentBytes = (byte*)NativeMemory.AllocZeroed((nuint)(sizeof(byte) * contentBytesSize));
            _onHeap = true;
        }

        bool result = false;

        try
        {
            // 将文本内容转换为 UTF-8 编码的字节流
            Encoding.UTF8.GetBytes(content, new Span<byte>(contentBytes, contentBytesSize));

            result = writeFile(contentBytes, (uint)contentBytesSize, filePath);
        }
        finally
        {
            if (_onHeap)
            {
                NativeMemory.Free(contentBytes);
            }
        }

        return result;
    }




    /// <summary>
    /// 
    /// </summary>
    /// <param name="content"></param>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public bool AppendText(ReadOnlySpan<char> content, ReadOnlySpan<char> filePath)
    {

        //===============
        if (filePath.IsEmpty || filePath.Length > ValueFilesIO.MAX_PATH_LENGTH || content.IsEmpty) return false;

        // 计算内容的字节长度
        int contentLength = Encoding.UTF8.GetByteCount(content);

        byte* contentBytes;
        bool _onHeap;

        // 如果内容长度小于 4096 字节，使用 stackalloc 分配内存
        if (contentLength < 4096)
        {
            // 使用栈内存分配 buffer
            byte* _contentPtr = stackalloc byte[(int)contentLength];
            contentBytes = _contentPtr;
            _onHeap = false;
        }
        else
        {
            // 使用堆内存分配 buffer
            contentBytes = (byte*)NativeMemory.AllocZeroed((nuint)(sizeof(byte) * contentLength));
            _onHeap = true;
        }

        bool result = false;

        try
        {
            // 将文本内容转换为 UTF-8 编码的字节流
            Encoding.UTF8.GetBytes(content, new Span<byte>(contentBytes, (int)contentLength));


            result = writeFile(contentBytes, (uint)contentLength, filePath, true);
        }
        finally
        {
            if (_onHeap)
            {
                NativeMemory.Free(contentBytes);
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
        if (path.IsEmpty) return -1;

        int bytesSize = path.Length * 3 + 1;
        byte* pathBytes = stackalloc byte[bytesSize];
        path.CopyToBytes(pathBytes, (uint)bytesSize);

        int fd;

        long fileSize;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            fd = MacOSAPI.open(pathBytes, (int)OpenFlags_MacOS.O_RDONLY, FileMode.S_IRUSR | FileMode.S_IWUSR);

            int _fs = MacOSAPI.fstat(fd, out Stat fileStat);

            fileSize = fileStat.st_size;

            MacOSAPI.close(fd);

        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            fd = LinuxAPI.open(pathBytes, (int)OpenFlags_Linux.O_RDONLY, FileMode.S_IRUSR | FileMode.S_IWUSR);

            int _fs = LinuxAPI.fstat(fd, out Stat fileStat);

            fileSize = fileStat.st_size;

            LinuxAPI.close(fd);
        }
        else
        {
            return -1;
        }

        return (int)fileSize;

    }

    /// <summary>
    /// 读取文件内容，自行确保外部的 UnManagedMemory&lt;char&gt; 具备安全容量
    /// <para>在不确定外部传入的 UnManagedMemory&lt;char&gt; 结果对象是否能够容纳文件容量时，因为可能会发生扩容操作（堆内存模式），它的 Pointer 地址有可能会改变</para>
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="externalMemory"></param>
    /// <param name="stackBufferSize">设置内部使用栈空间作为 Buffer 读取的容量</param>
    /// <returns></returns>
    public bool ReadTextFile(ReadOnlySpan<char> filePath, UnManagedString* externalMemory, uint stackBufferSize = 1024)
    {

        if (externalMemory is null) return false;

        if (stackBufferSize >= ValueFilesIO.MAX_STACK_SIZE)
            stackBufferSize = ValueFilesIO.MAX_STACK_SIZE;

        int fd = -1;


        int bytesSize = filePath.Length * 3 + 1;
        byte* pathBytes = stackalloc byte[bytesSize];
        filePath.CopyToBytes(pathBytes, (uint)bytesSize);


        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            fd = MacOSAPI.open(pathBytes, (int)OpenFlags_MacOS.O_RDONLY, FileMode.S_IRUSR | FileMode.S_IWUSR);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            fd = LinuxAPI.open(pathBytes, (int)OpenFlags_MacOS.O_RDONLY, FileMode.S_IRUSR | FileMode.S_IWUSR);
        }
        else
        {
            return false;
        }



        if (fd < 0)
        {
            return false;
        }

        int _fs;
        Stat fileStat;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            _fs = MacOSAPI.fstat(fd, out fileStat);
        else
            _fs = LinuxAPI.fstat(fd, out fileStat);


        long fileSize = fileStat.st_size;

        if (fileSize <= 0)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                MacOSAPI.close(fd);
            else
                LinuxAPI.close(fd);

            return false;
        }

        UnManagedMemory<byte> temp_bytes;

        byte* bufferPtr;

        bool readOnece;

        if (fileSize > 4096)
        {
            //大于 4KB 的文件需要分多次都写
            //Buffer 与临时存储器需要分开，先读取到 Buffer，然后逐渐累加到临时存储器
            temp_bytes = new UnManagedMemory<byte>((uint)fileSize, 0);
            byte* buffer_when_largethan_4096 = stackalloc byte[(int)stackBufferSize];
            bufferPtr = buffer_when_largethan_4096;

            readOnece = false;
        }
        else
        {
            //小于 4KB 的文件将读取到栈中
            //并且临时存储器本身就是 Buffer
            byte* stack_temp_file = stackalloc byte[(int)fileSize];
            temp_bytes = new UnManagedMemory<byte>(stack_temp_file, (uint)fileSize, 0);
            bufferPtr = stack_temp_file;
            stackBufferSize = (uint)fileSize;
            readOnece = true;
        }

        long bytesRead;


    RELOAD:

        //Read函数会自动维护读取偏移量，从上一次读取的结束点 + 1 开始新的读取

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            bytesRead = MacOSAPI.read(fd, bufferPtr, (int)stackBufferSize);
        else
            bytesRead = LinuxAPI.read(fd, bufferPtr, (int)stackBufferSize);



        if (bytesRead > 0)
        {
            if (readOnece)
            {
                //不需要多次读写
                temp_bytes.ReLength((uint)bytesRead);
            }
            else
            {
                temp_bytes.Add(bufferPtr, (uint)bytesRead, MemoryScaleMode.X2);

                goto RELOAD; //继续下一次都写
            }
        }


        if (fd >= 0)
        {

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                MacOSAPI.close(fd);
            else
                LinuxAPI.close(fd);

            temp_bytes.CopyToChars(externalMemory);

            temp_bytes.Dispose();

            return true;
        }
        else
        {
            return false;
        }
    }




    protected bool filenameValidator(byte** p_filename)
    {
        byte* filename = *p_filename;

        if (filename == null || filename[0] == 0)
            return false;

        if (filename[0] == 0x04 || filename[0] == '\b')
            p_filename[0] = *&filename += 1; //过滤特殊控制符号    

        //过滤 "." 或 ".." ，这是特殊文件，对于业务毫无意义
        if ((filename[0] == '.' && filename[1] == 0) || (filename[0] == '.' && filename[1] == '.' && filename[2] == 0))
            return false;


        return true;
    }


    protected void chars_to_UnManagedMemory_2X(ReadOnlySpan<char> parentPath, byte* chars, delegate*<ReadOnlySpan<char>, UnManagedString*, void*, void> action, void* dynamicTemp)
    {

        if (chars is null) return;

        int _len = 0;

        // 计算chars长度
        while (chars[_len] != 0)
        {
            _len++;
        }

        //按照 mac 文件名最长 1024 字节，utf8 每字最长 4字节， 这不是合法文件名
        if (_len > 4096) return;

        int decodedCharCount = Encoding.UTF8.GetCharCount(chars, _len);

        char* stack_temp_filename = stackalloc char[decodedCharCount];

        Encoding.UTF8.GetChars(chars, _len, stack_temp_filename, decodedCharCount);

        Span<char> name = new Span<char>(stack_temp_filename, decodedCharCount);

        UnManagedString _name = new UnManagedString(name);

        action(parentPath, &_name, dynamicTemp);
    }


    protected void chars_to_UnManagedMemory_2X(byte* chars, UnManagedMemory<UnManagedString>* string_array)
    {
        if (chars is null || string_array is null) return;

        int _len = 0;

        // 计算chars长度
        while (chars[_len] != 0)
        {
            _len++;
        }

        //按照 mac 文件名最长 1024 字节，utf8 每字最长 4字节， 这不是合法文件名
        if (_len > 4096) return;

        int decodedCharCount = Encoding.UTF8.GetCharCount(chars, _len);

        char* stack_temp_filename = stackalloc char[decodedCharCount];

        Encoding.UTF8.GetChars(chars, _len, stack_temp_filename, decodedCharCount);

        Span<char> name = new Span<char>(stack_temp_filename, decodedCharCount);

        UnManagedString _name = new UnManagedString(name);

        string_array->Add(_name);
    }


    public bool DeleteFile(ReadOnlySpan<char> path)
    {
        if (path.Length <= ValueFilesIO.MAX_STACK_SIZE)
        {
            int bytesSize = path.Length * 3 + 1;
            byte* pathBytes = stackalloc byte[bytesSize];
            path.CopyToBytes(pathBytes, (uint)bytesSize);


            bool result;

            int fd;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                fd = MacOSAPI.remove(pathBytes);
            else
                fd = LinuxAPI.remove(pathBytes);


            //不需要再进行 fsync 或 close 操作，这两个方法是用于操作正在打开的文件，此时已经不存在打开的文件，整个流程就此结束

            result = fd == 0;

            return result;
        }
        else
        {
            return false;
        }
    }


    public bool EnsureDirectoryExists(ReadOnlySpan<char> path)
    {

        int bytesSize = path.Length * 3 + 1;
        byte* pathBytes = stackalloc byte[bytesSize];
        path.CopyToBytes(pathBytes, (uint)bytesSize);


        bool result = false;

        // 检查路径是否存在

        int accessResult;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            accessResult = MacOSAPI.access(pathBytes, 0);
        else
            accessResult = LinuxAPI.access(pathBytes, 0);

        if (accessResult == 0)
        {
            result = true; // 路径存在即返回成功
        }

        // 路径不存在，尝试创建
        int result_mkdir;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            result_mkdir = MacOSAPI.mkdir(pathBytes, FileMode.S_IRWXRWX);
        else
            result_mkdir = LinuxAPI.mkdir(pathBytes, FileMode.S_IRWXRWX);

        if (result_mkdir == 0)
        {
            result = true; // 创建成功
        }


        if (result) return result;

        // 检查是否因已存在而失败
        int errorCode = Marshal.GetLastWin32Error();
        return errorCode == 17; // 17 表示路径已经存在，这是 posix 常量
    }


}
