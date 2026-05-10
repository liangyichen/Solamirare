using System.Runtime.CompilerServices;

namespace Solamirare;


internal unsafe class ZeroDirectory_Windows : ISyncDirectory
{




    private const uint INVALID_FILE_ATTRIBUTES = 0xFFFFFFFF;



    // 文件属性结构
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct WIN32_FIND_DATA
    {
        public uint dwFileAttributes;
        public uint ftCreationTimeLow;
        public uint ftCreationTimeHigh;
        public uint ftLastAccessTimeLow;
        public uint ftLastAccessTimeHigh;
        public uint ftLastWriteTimeLow;
        public uint ftLastWriteTimeHigh;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        public fixed char cFileName[260];  // 修改为固定大小数组
        public fixed char cAlternateFileName[14]; // 修改为固定大小数组
    }

    // 常量
    const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
    const uint FILE_ATTRIBUTE_NORMAL = 0x80;
    const uint OPEN_EXISTING = 3;
    const uint GENERIC_READ = 0x80000000;


    bool ISyncDirectory.EnsureDirectoryExists(ReadOnlySpan<char> path)
    {
        // Windows: 使用 UTF-16
        int charCount = path.Length;
        if (charCount + 1 > 260)
            throw new ArgumentException("路径太长");

        char* pathBuffer = stackalloc char[charCount + 1];
        path.CopyTo(new Span<char>(pathBuffer, charCount));
        pathBuffer[charCount] = '\0';

        uint attributes = WindowsAPI.GetFileAttributesW(pathBuffer);

        if (attributes != INVALID_FILE_ATTRIBUTES)
        {
            return (attributes & FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY;
        }

        return WindowsAPI.CreateDirectoryW(pathBuffer, IntPtr.Zero);
    }

    public static int createWindowsDirPath(ReadOnlySpan<char> filePath, char* buffer)
    {
        if (filePath.IsEmpty)
        {
            buffer[0] = '\\';
            buffer[1] = '*';
            buffer[2] = '\0';
            return 2;
        }

        // 复制路径内容到 buffer
        fixed (char* source = filePath)
        {
            Unsafe.CopyBlock(buffer, source, (uint)(filePath.Length * sizeof(char)));
        }

        // **检查是否已经是 `\*` 或 `/*` 结尾**
        if (filePath.Length >= 2 &&
            (filePath[^2] == '\\' || filePath[^2] == '/') &&
            filePath[^1] == '*')
        {
            buffer[filePath.Length] = '\0'; // 确保 null 终止
            return filePath.Length; // 直接返回，不修改
        }

        // **计算新路径长度**
        char lastChar = filePath[^1];
        bool endsWithBackslash = lastChar == '\\';
        bool endsWithSlash = lastChar == '/';
        int newLength = filePath.Length + (endsWithBackslash || endsWithSlash ? 2 : 3); // `*` + `\0`

        // **追加 `\*` 或 `/*` 并确保 `\0` 结尾**
        if (endsWithBackslash || endsWithSlash)
        {
            buffer[filePath.Length] = '*';
            buffer[filePath.Length + 1] = '\0';
        }
        else
        {
            buffer[filePath.Length] = '\\'; // 统一使用 `\` 作为目录分隔符
            buffer[filePath.Length + 1] = '*';
            buffer[filePath.Length + 2] = '\0';
        }

        return newLength;
    }

    bool ISyncDirectory.GetFilesNames(ReadOnlySpan<char> dirPath, UnManagedMemory<UnManagedString>* filenames)
    {

        char* path = stackalloc char[1024];

        createWindowsDirPath(dirPath, path);



        WIN32_FIND_DATA findFileData;
        IntPtr findHandle = WindowsAPI.FindFirstFileW(path, out findFileData);

        if (findHandle == (IntPtr)(-1))
        {
            Console.WriteLine($"FindFirstFile 失败，错误代码: {Marshal.GetLastWin32Error()}");
            // 不应该调用 FindClose(findHandle)
            return false;
        }

        do
        {
            char* ptr = findFileData.cFileName;

            if (ptr is null) continue;

            int len = GetCStringLength(ptr, 260);

            Span<char> temp_span = new Span<char>(ptr, len);

            if (temp_span.SequenceEqual(".") || temp_span.SequenceEqual(".."))
                continue;

            UnManagedString node = new UnManagedString();

            node.Add(ptr, (uint)len);

            filenames->Add(node);

        } while (WindowsAPI.FindNextFileW(findHandle, out findFileData));

        WindowsAPI.FindClose(findHandle);

        return true;

    }

    bool ISyncDirectory.GetFilesContents(ReadOnlySpan<char> dirPath, delegate*<ReadOnlySpan<char>, UnManagedString*, void*, void> action, void* dynamicTemp)
    {

        char* path = stackalloc char[1024];

        createWindowsDirPath(dirPath, path);


        WIN32_FIND_DATA findFileData;
        IntPtr findHandle = WindowsAPI.FindFirstFileW(path, out findFileData);

        if (findHandle == (IntPtr)(-1))
        {
            Console.WriteLine($"FindFirstFile 失败，错误代码: {Marshal.GetLastWin32Error()}");
            // 不应该调用 FindClose(findHandle)
            return false;
        }

        do
        {
            char* ptr = findFileData.cFileName;

            if (ptr is null) continue;

            int len = GetCStringLength(ptr, 260);

            Span<char> temp_span = new Span<char>(ptr, len);

            if (temp_span.SequenceEqual(".") || temp_span.SequenceEqual(".."))
                continue;

            UnManagedString node = new UnManagedString();

            node.Add(ptr, (uint)len);

            action(dirPath, &node, dynamicTemp);

        } while (WindowsAPI.FindNextFileW(findHandle, out findFileData));

        WindowsAPI.FindClose(findHandle);

        return true;

    }

    /// <summary>
    /// 计算 C 风格字符串的长度（以 '\0' 结尾）。
    /// </summary>
    static unsafe int GetCStringLength(char* cStr, int maxLength)
    {
        int length = 0;
        while (length < maxLength && cStr[length] != '\0')
        {
            length++;
        }
        return length;
    }

}
