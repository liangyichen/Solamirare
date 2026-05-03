namespace Solamirare;


/// <summary>
/// 文件权限模式
/// </summary>
public static class FileMode
{
    // User (Owner) Permissions
    /// <summary>Owner read, write, and execute permissions.</summary>
    public const int S_IRWXU = 00700; // rwx
    /// <summary>Owner read permission.</summary>
    public const int S_IRUSR = 00400; // r--
    /// <summary>Owner write permission.</summary>
    public const int S_IWUSR = 00200; // -w-
    /// <summary>Owner execute permission.</summary>
    public const int S_IXUSR = 00100; // --x

    // Group Permissions
    /// <summary>Group read, write, and execute permissions.</summary>
    public const int S_IRWXG = 00070; // rwx
    /// <summary>Group read permission.</summary>
    public const int S_IRGRP = 00040; // r--
    /// <summary>Group write permission.</summary>
    public const int S_IWGRP = 00020; // -w-
    /// <summary>Group execute permission.</summary>
    public const int S_IXGRP = 00010; // --x

    // Others Permissions
    /// <summary>Other-user read, write, and execute permissions.</summary>
    public const int S_IRWXO = 00007; // rwx
    /// <summary>Other-user read permission.</summary>
    public const int S_IROTH = 00004; // r--
    /// <summary>Other-user write permission.</summary>
    public const int S_IWOTH = 00002; // -w-
    /// <summary>Other-user execute permission.</summary>
    public const int S_IXOTH = 00001; // --x

    // 八进制 0666，表示任何人可读可写
    /// <summary>Read/write permission for owner, group, and others.</summary>
    public const int S_IRWXRWX = S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH;
}
