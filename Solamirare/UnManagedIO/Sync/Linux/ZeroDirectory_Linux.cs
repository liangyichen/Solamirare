namespace Solamirare;


internal unsafe class ZeroDirectory_Linux : Posix_Functions, ISyncDirectory
{
    const int DT_REG = 8; // 只读取普通文件 (DT_REG)

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct dirent
    {
        public ulong d_ino;    // inode 号
        public long d_off;
        public ushort d_reclen;
        public byte d_type;    // 文件类型
        public fixed byte d_name[256]; // 256 字节的文件名数组
    }

    bool ISyncDirectory.GetFilesNames(ReadOnlySpan<char> dirPath, UnManagedMemory<UnManagedString>* filenames)
    {
        if (dirPath.IsEmpty || filenames is null) return false;

        int length = dirPath.Length + 1;

        byte* pathPtr = stackalloc byte[length];

        for (int i = 0; i < dirPath.Length; i++)
        {
            pathPtr[i] = (byte)dirPath[i];
        }

        pathPtr[length - 1] = 0;

        char* dir = LinuxAPI.opendir(pathPtr);

        if (dir is null)
        {
            return false;
        }


        Span<char> stack_temp_filename = stackalloc char[260];

        char* entryPtr = null;
        while ((entryPtr = LinuxAPI.readdir(dir)) is not null)
        {
            dirent* entry = (dirent*)entryPtr;
            if (entry == null) continue;

            byte* filename = entry->d_name;
            bool valid = filenameValidator(&filename);

            if (!valid) continue;

            // 只显示普通文件 (DT_REG)
            if (entry->d_type == DT_REG)
            {
                chars_to_UnManagedMemory_2X(filename, filenames);
            }

        }

        LinuxAPI.closedir(dir);

        return true;

    }

    bool ISyncDirectory.GetFilesContents(ReadOnlySpan<char> dirPath, delegate*<ReadOnlySpan<char>, UnManagedString*, void*, void> action, void* dynamicTemp)
    {
        if (dirPath.IsEmpty) return false;

        int length = dirPath.Length + 1;

        byte* pathPtr = stackalloc byte[length];

        for (int i = 0; i < dirPath.Length; i++)
        {
            pathPtr[i] = (byte)dirPath[i];
        }

        pathPtr[length - 1] = 0;

        char* dir = LinuxAPI.opendir(pathPtr);

        if (dir is null)
        {
            return false;
        }


        Span<char> stack_temp_filename = stackalloc char[260];

        char* entryPtr = null;

        while ((entryPtr = LinuxAPI.readdir(dir)) is not null)
        {
            dirent* entry = (dirent*)entryPtr;
            if (entry == null) continue;

            byte* filename = entry->d_name;
            bool valid = filenameValidator(&filename);

            if (!valid) continue;

            // 只显示普通文件 (DT_REG)
            if (entry->d_type == DT_REG)
            {
                chars_to_UnManagedMemory_2X(dirPath, filename, action, dynamicTemp);
            }

        }

        LinuxAPI.closedir(dir);

        return true;
    }
}
