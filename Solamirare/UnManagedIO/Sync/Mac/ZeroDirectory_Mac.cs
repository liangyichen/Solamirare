namespace Solamirare;


internal unsafe class ZeroDirectory_Mac : Posix_Functions, ISyncDirectory
{

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct dirent
    {
        public ulong d_ino;      // inode 号 (macOS 也是 uint64_t)
        public ulong d_seekoff;  // 文件偏移 (macOS 专有)
        public ushort d_reclen;  // 目录项长度
        public byte d_namlen;    // 文件名长度 (macOS 专有)
        public byte d_type;      // 文件类型
        public fixed byte d_name[1024]; // macOS 文件名长度是 1024
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

        char* dir = MacOSAPI.opendir(pathPtr);

        char* entryPtr = null;

        while ((entryPtr = MacOSAPI.readdir(dir)) is not null)
        {
            dirent* entry = (dirent*)entryPtr;
            if (entry == null) continue;

            byte* filename = entry->d_name;
            bool valid = filenameValidator(&filename);

            if (!valid) continue;

            //在某些系统中（如 macOS 和某些 Linux 发行版），文件系统没有提供文件类型信息，因此它会返回 DT_UNKNOWN
            //所以忽略掉文件类型判断的逻辑， 直接取文件名了
            // if (entry->d_type == DT_REG)
            // {}

            chars_to_UnManagedMemory_2X(filename, filenames);
        }

        MacOSAPI.closedir(dir);

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

        char* dir = MacOSAPI.opendir(pathPtr);

        char* entryPtr = null;

        while ((entryPtr = MacOSAPI.readdir(dir)) is not null)
        {
            dirent* entry = (dirent*)entryPtr;
            if (entry == null) continue;

            byte* filename = entry->d_name;
            bool valid = filenameValidator(&filename);

            if (!valid) continue;

            //在某些系统中（如 macOS 和某些 Linux 发行版），文件系统没有提供文件类型信息，因此它会返回 DT_UNKNOWN
            //所以忽略掉文件类型判断的逻辑， 直接取文件名了
            // if (entry->d_type == DT_REG)
            // {}

            chars_to_UnManagedMemory_2X(dirPath, filename, action, dynamicTemp);
        }

        MacOSAPI.closedir(dir);

        return true;
    }



}

