using System.Runtime.CompilerServices;
using System.Text;

namespace Solamirare;


/// <summary>
/// 常用的 IO 同步模式操作
/// </summary>
public unsafe class ValueFilesIO
{
    /// <summary>
    /// 跨平台IO操作对象
    /// </summary>
    static readonly ISyncFilesIO IOInstance;

    /// <summary>
    /// 跨平台文件夹操作对象
    /// </summary>
    static readonly ISyncDirectory DirectoryInstance;

    /// <summary>
    /// 路径分隔符
    /// </summary>
    static readonly char CombinePathsChar;

    /// <summary>
    /// 本地路径最大长度，单位是字符数量
    /// </summary>
    public static readonly uint MAX_PATH_LENGTH;

    /// <summary>
    /// 栈内存的限制大小，单位是字节
    /// </summary>
    public static readonly uint MAX_STACK_SIZE;




    static ValueFilesIO()
    {
        //无论操作系统可以容纳的最大方法栈容量多多少，都不应该在 c# 中分配达到 1M 的栈内存
        //保守起见，取一半值作为最大栈分配容量


        //RuntimeInformation 判断会在编译期间得到优化，消除不必要的判断
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            CombinePathsChar = '\\';
            IOInstance = new ZeroIO_Windows();
            DirectoryInstance = new ZeroDirectory_Windows();
            MAX_PATH_LENGTH = 260;
            MAX_STACK_SIZE = 512 * 1024;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            CombinePathsChar = '/';
            IOInstance = new ZeroIO_Linux(); //
            DirectoryInstance = new ZeroDirectory_Linux();
            MAX_PATH_LENGTH = 4096;
            MAX_STACK_SIZE = MAX_STACK_SIZE = 512 * 1024;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            CombinePathsChar = '/';
            IOInstance = new ZeroIO_Mac();
            DirectoryInstance = new ZeroDirectory_Mac();
            MAX_PATH_LENGTH = 1024;
            MAX_STACK_SIZE = 512 * 1024;
            //根据 MAC OS 13.0.1, Visual Studio Code 调试环境在 while 循环中进行 stackalloc 测试得出可以达到 1000 * 1024

        }
        else //Others
        {
            CombinePathsChar = '/';
            IOInstance = null;
            DirectoryInstance = null;
            MAX_PATH_LENGTH = 260;
        }


    }

    /// <summary>
    /// 获取应用程序所在目录，返回结果保存于堆内存，调用者必须手动释放
    /// </summary>
    /// <returns></returns>
    public static UnManagedString AppContextBaseDirectory()
    {
        if (IOInstance is null)
            return new UnManagedString();

        return IOInstance.AppContextBaseDirectory();
    }


    /// <summary>
    /// 获取应用程序所在目录
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool AppContextBaseDirectory(UnManagedString* result)
    {
        if (IOInstance is null) return false;

        if (result is null) return false;

        UnManagedString st_temp = IOInstance.AppContextBaseDirectory();

        if (!st_temp.IsEmpty && st_temp.UsageSize <= ValueFilesIO.MAX_PATH_LENGTH)
        {
            result->Zero();

            result->AddRange((UnManagedCollection<char>*)&st_temp);

            return true;
        }

        st_temp.Dispose();

        return false;
    }

    /// <summary>
    /// 根据输入的相对路径（基于当前工作目录），获取绝对路径
    /// </summary>
    /// <param name="relativePath">输入一个相对路径</param>
    /// <param name="result_fullPath">保存结果</param>
    /// <param name="resultLength">结果中的真实字符串长度</param>
    public static bool ProcessRelativePath(ReadOnlySpan<char> relativePath, char* result_fullPath, out int resultLength)
    {

        if (relativePath.Length > ValueFilesIO.MAX_PATH_LENGTH || result_fullPath is null)
        {
            resultLength = -1;

            return false;
        }

        // 如果路径已经是绝对路径，则直接返回
        if (relativePath.Length > 0 && relativePath[0] == '/')
        {
            fixed (char* p_relativePath = relativePath)
                Unsafe.CopyBlock(result_fullPath, p_relativePath, (uint)relativePath.Length);

            resultLength = relativePath.Length;
            return true;
        }

        resultLength = -1;
        bool result = false;

        UnManagedString appPath = AppContextBaseDirectory();

        if (!appPath.IsEmpty)
        {

            Span<char> appPath_chars = appPath.AsSpan();

            char* combineResult = (char*)NativeMemory.AllocZeroed((nuint)ValueFilesIO.MAX_PATH_LENGTH * sizeof(char));

            if (ValueFilesIO.CombinePaths(appPath_chars, relativePath, combineResult, out resultLength))
            {
                //因为两段字符串的编码排列有可能不一致，需要在字节视角重新再次解释一遍

                Span<char> span_combineResult = new Span<char>(combineResult, resultLength);

                int _combineBytesLength = Encoding.UTF8.GetByteCount(combineResult, resultLength);

                Span<byte> combinePathBytes = stackalloc byte[_combineBytesLength];

                Encoding.UTF8.GetBytes(span_combineResult, combinePathBytes);

                int charsWritten = Encoding.UTF8.GetChars(combinePathBytes, new Span<char>(result_fullPath, resultLength));

                result = true;
            }

            NativeMemory.Free(combineResult);

            return result;
        }

        appPath.Dispose();

        return result;
    }

    /// <summary>
    /// 根据输入的相对路径（基于当前工作目录），获取绝对路径。返回结果保存于堆内存，调用者必须手动释放
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    public static UnManagedString ProcessRelativePath(ReadOnlySpan<char> relativePath)
    {
        UnManagedString result = new UnManagedString(MAX_PATH_LENGTH, 0);

        if (ProcessRelativePath(relativePath, result.Pointer, out int resultLength))
        {
            result.ReLength((uint)resultLength);
        }

        return result;
    }



    /// <summary>
    /// 删除文件（外部必须判断文件是否存在）
    /// </summary>
    /// <param name="path">文件的绝对路径</param>
    public static bool DeleteFile(ReadOnlySpan<char> path)
    {
        if (IOInstance is null || path.IsEmpty)
        {
            return false;
        }

        return IOInstance.DeleteFile(path);
    }


    /// <summary>
    /// 判断文件是否存在
    /// </summary>
    /// <param name="path">文件的绝对路径</param>
    /// <returns></returns>
    public static bool FileExists(ReadOnlySpan<char> path)
    {
        if (IOInstance is null || path.IsEmpty)
        {
            return false;
        }

        return IOInstance.FileExists(path);
    }

    /// <summary>
    /// 判断文件是否存在
    /// </summary>
    /// <param name="path">文件的绝对路径</param>
    /// <returns></returns>
    public static bool FileExists(UnManagedString path)
    {
        if (IOInstance is null)
        {
            return false;
        }

        return IOInstance.FileExists(path.AsSpan());
    }


    /// <summary>
    /// 把字节内容写入文件
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="bytesLength"></param>
    /// <param name="filePath">文件的绝对路径</param>
    public static bool WriteBytesToFile(byte* bytes, uint bytesLength, ReadOnlySpan<char> filePath)
    {
        if (IOInstance is null)
        {
            return false;
        }

        IOInstance.WriteBytesToFile(bytes, bytesLength, filePath);

        return true;
    }

    /// <summary>
    /// 往既有的文本文件追加内容
    /// </summary>
    /// <param name="content"></param>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static bool AppendText(ReadOnlySpan<char> content, ReadOnlySpan<char> filePath)
    {
        if (IOInstance is null)
        {
            return false;
        }

        bool success = IOInstance.AppendText(content, filePath);

        return success;
    }


    /// <summary>
    /// 把文本内容写入文件，如果文件不存在则创建，如果文件已经存在则覆盖
    /// </summary>
    /// <param name="content">文本内容</param>
    /// <param name="path">文件的绝对路径</param>
    public static bool WriteTextToFile(ReadOnlySpan<char> content, ReadOnlySpan<char> path)
    {
        if (IOInstance is null)
        {
            return false;
        }

        bool success = IOInstance.WriteTextToFile(content, path);

        return success;
    }



    /// <summary>
    /// 读取文件大小，单位是 Bytes
    /// </summary>
    /// <param name="filePathBytes">文件的绝对路径</param>
    /// <returns></returns>
    public static long FileBytesSize(ReadOnlySpan<char> filePathBytes)
    {
        if (IOInstance is null || filePathBytes.IsEmpty)
        {
            return -1;
        }

        return IOInstance.FileBytesSize(filePathBytes);
    }

    /// <summary>
    /// 检测目录是否存在,如果不存在则创建
    /// </summary>
    /// <param name="path">目录的绝对路径</param>
    /// <returns>description beford status: Core: directory exist(true) or not(false)</returns>
    public static void CheckDir(ReadOnlySpan<char> path)
    {
        if (path.IsEmpty || DirectoryInstance is null) return;

        DirectoryInstance.EnsureDirectoryExists(path);
    }

    /// <summary>
    /// 拼接路径字符串，返回到外部内存， 执行完毕后外部内存指针会自动偏移拼接后的字符串长度
    /// </summary>
    /// <param name="path1"></param>
    /// <param name="path2"></param>
    /// <param name="MemoryOutside"></param>
    /// <returns></returns>
    public static UnManagedString CombinePaths(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, char** MemoryOutside)
    {
        if (MemoryOutside is null) goto FAILURE;

        char* p_outSide = *MemoryOutside;

        if (p_outSide is null) goto FAILURE;

        char* pathBuffer = stackalloc char[(int)MAX_PATH_LENGTH];

        if (CombinePaths(path1, path2, pathBuffer, out int length))
        {
            UnManagedString result = new UnManagedString(p_outSide, (uint)length, 0);

            result.Add(pathBuffer, (uint)length);

            p_outSide += length;

            return result;
        }

    FAILURE:
        return new UnManagedString();
    }



    /// <summary>
    /// 拼接路径字符串，返回结果保存于堆内存，调用者必须手动释放
    /// </summary>
    /// <param name="path1"></param>
    /// <param name="path2"></param>
    /// <returns></returns>
    public static UnManagedString CombinePaths(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2)
    {
        char* pathBuffer = stackalloc char[(int)MAX_PATH_LENGTH];

        CombinePaths(path1, path2, pathBuffer, out int length);

        UnManagedString result = new UnManagedString();

        result.Add(pathBuffer, (uint)length);

        return result;
    }

    /// <summary>
    /// 拼接路径字符串，存储到第三个参数 UnManagedMemory
    /// </summary>
    /// <param name="path1"></param>
    /// <param name="path2"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool CombinePaths(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, UnManagedString* result)
    {
        if (result is null)
        {
            return false;
        }

        char* pathBuffer = stackalloc char[(int)MAX_PATH_LENGTH];

        CombinePaths(path1, path2, pathBuffer, out int length);

        result->Add(pathBuffer, (uint)length);

        return true;
    }

    /// <summary>
    /// 拼接路径字符串，返回路径的字符长度
    /// </summary>
    /// <param name="path1"></param>
    /// <param name="path2"></param>
    /// <param name="result">外部传入的内存，用于接收结果</param>
    /// <param name="resultLength">resultLength 赋值后的长度</param>
    /// <returns></returns>
    public static bool CombinePaths(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, char* result, out int resultLength)
    {
        if (result is null)
        {
            resultLength = -1;
            return false;
        }

        int path_1_length = path1.Length;

        fixed (char* currentDirPtr = path1)
        {
            // 处理相对路径中的 "./" 部分
            ReadOnlySpan<char> cleanedPath = path2.Length > 1 && path2[0] == '.' && path2[1] == '/'
                ? path2.Slice(2)
                : path2;

            bool currentDirEndsWithSlash = currentDirPtr[path_1_length - 1] == '/';

            int totalLength = path_1_length + cleanedPath.Length + (currentDirEndsWithSlash ? 0 : 1); // +1 : \0

            // 拷贝当前工作目录到目标缓冲区
            for (int i = 0; i < path_1_length; i++)
            {
                result[i] = currentDirPtr[i];
            }

            // 如果当前工作目录没有以 '/' 结尾，则手动添加 '/'
            if (!currentDirEndsWithSlash)
            {
                result[path_1_length] = '/';
                path_1_length++;
            }


            for (int i = 0; i < cleanedPath.Length; i++)
            {
                result[path_1_length + i] = cleanedPath[i];
            }

            resultLength = totalLength;

            return true;
        }
    }


    /// <summary>
    /// 获取指定目录下面的所有文件，不含子目录
    /// </summary>
    /// <param name="parentPath"></param>
    /// <param name="filenames"></param>
    /// <returns></returns>
    public static bool FileNamesFromDirectory(ReadOnlySpan<char> parentPath, UnManagedMemory<UnManagedString>* filenames)
    {
        if (parentPath.IsEmpty || filenames is null || DirectoryInstance is null)
            return false;

        return DirectoryInstance.GetFilesNames(parentPath, filenames);
    }

    /// <summary>
    /// 获取指定目录下面的所有文件内容，不含子目录。 回调函数获取每次迭代的（上级路径, 文件名）
    /// </summary>
    /// <param name="parentPath">所在目录的完整路径</param>
    /// <param name="onLoaded">遍历迭代的每个文件的上级路径、文件名、collection</param>
    /// <param name="collection">指向集合形态的对象，用于保存文件内容</param>
    /// <returns></returns>
    public static bool FilesContentsFromDirectory(ReadOnlySpan<char> parentPath, delegate*<ReadOnlySpan<char>, UnManagedString*, void*, void> onLoaded, void* collection)
    {
        if (parentPath.IsEmpty || onLoaded is null || collection is null || DirectoryInstance is null)
            return false;

        return DirectoryInstance.GetFilesContents(parentPath, onLoaded, collection);
    }




    /// <summary>
    /// 读取文本文件，返回结果保存于堆内存，调用者必须手动释放
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="stackBufferSize"></param>
    /// <returns></returns>
    public static UnManagedString ReadTextFile(ReadOnlySpan<char> filePath, uint stackBufferSize = 1024)
    {
        UnManagedString result_chars = new UnManagedString();

        ReadTextFile(filePath, &result_chars, stackBufferSize);

        return result_chars;
    }

    /// <summary>
    /// 读取文件内容，自行确保外部的 UnManagedMemory&lt;char&gt; 具备安全容量
    /// <para>在不确定外部传入的 UnManagedMemory&lt;char&gt; 结果对象是否能够容纳文件容量时，因为可能会发生扩容操作（堆内存模式），它的 Pointer 地址有可能会改变</para>
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="externalMemory"></param>
    /// <param name="stackBufferSize"></param>
    /// <returns></returns>
    public static bool ReadTextFile(ReadOnlySpan<char> filePath, UnManagedString* externalMemory, uint stackBufferSize = 1024)
    {
        if (!filePath.IsEmpty && IOInstance is not null && externalMemory is not null)
        {
            bool result = IOInstance.ReadTextFile(filePath, externalMemory, stackBufferSize);

            return result;
        }
        else
        {
            return false;
        }
    }


    /// <summary>
    /// 读取多个文本文件，回调处理
    /// </summary>
    /// <param name="files"></param>
    /// <param name="action"></param>
    /// <param name="stackBufferSize"></param>
    /// <returns></returns>
    public static bool ReadFiles(UnManagedMemory<UnManagedString>* files, delegate*<ReadOnlySpan<char>, UnManagedString*, void> action, uint stackBufferSize = 1024)
    {
        if (files is null || files->IsEmpty || action is null || IOInstance is null)
            return false;

        for (int i = 0; i < files->UsageSize; i++)
        {
            ReadOnlySpan<char> filePath = files->Index(i)->AsSpan();

            UnManagedString result = ReadTextFile(filePath, stackBufferSize);

            if (!result.IsEmpty)
            {
                ReadOnlySpan<char> filename = Path.GetFileName(filePath);

                action(filename, &result);
            }
        }

        return true;
    }


    /// <summary>
    /// 读取多个文本文件
    /// </summary>
    /// <param name="files">传入文件地址集合</param>
    /// <param name="results">读取完成后，各个文本文件的结果保存输出到该链表中</param>
    /// <param name="stackBufferSize"></param>
    public static bool ReadFiles(UnManagedMemory<UnManagedString>* files, ValueLinkedList<UnManagedString>* results, uint stackBufferSize = 1024)
    {
        if (files is null || files->IsEmpty || results is null || IOInstance is null)
            return false;

        char* absPath = stackalloc char[(int)ValueFilesIO.MAX_PATH_LENGTH];

        for (int i = 0; i < files->UsageSize; i++)
        {
            ReadOnlySpan<char> filePath = files->Index(i)->AsSpan();

            ProcessRelativePath(filePath, absPath, out int absPath_Length);

            Span<char> span_abs_path = new Span<char>(absPath, absPath_Length);

            UnManagedString result = ReadTextFile(span_abs_path, stackBufferSize);

            if (!result.IsEmpty)
            {
                results->Append(&result);
            }
        }

        return true;
    }


    /// <summary>
    /// 读取多个文本文件
    /// </summary>
    /// <param name="files">传入文件地址集合</param>
    /// <param name="results">读取完成后，各个文本文件的结果保存输出到该字典中，字典的 Key 和 Value 分别是 文件名（不含上级路径）和文件内容</param>
    /// <param name="stackBufferSize"></param>
    public static bool ReadFiles(UnManagedMemory<UnManagedString>* files, ValueDictionary<UnManagedString, UnManagedString>* results, uint stackBufferSize = 1024)
    {
        if (files is null || files->IsEmpty || results is null || IOInstance is null)
            return false;

        for (int i = 0; i < files->UsageSize; i++)
        {
            ReadOnlySpan<char> filePath = files->Index(i)->AsSpan();

            UnManagedString result = ReadTextFile(filePath, stackBufferSize);

            if (!result.IsEmpty)
            {
                UnManagedString filename = Path.GetFileName(filePath).CopyToUnManagedMemory();

                results->AddOrUpdate(&filename, &result);
            }
        }

        return true;
    }

}
