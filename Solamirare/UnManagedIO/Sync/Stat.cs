namespace Solamirare;


/// <summary>
/// fstat 函数的文件元数据
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct Stat
{
    /// <summary>
    /// 设备 ID：文件所在的设备 ID（例如，文件系统所在的磁盘或分区）。
    /// </summary>
    [FieldOffset(0)] public ulong st_dev;

    /// <summary>
    /// 文件类型和权限：这是一个非常重要的字段。它是一个位掩码，包含文件的类型（如普通文件、目录、符号链接等）和所有者的读/写/执行权限、组权限以及其他用户权限。
    /// </summary>
    [FieldOffset(8)] public ushort st_mode;

    /// <summary>
    /// 硬链接数：指向该文件的硬链接（hard link）数量。当这个值为 0 时，文件才会被真正删除。
    /// </summary>
    [FieldOffset(10)] public ushort st_nlink;

    /// <summary>
    /// inode 号码：文件的唯一序列号。在单个文件系统内，每个文件的 inode 号码都是唯一的。
    /// </summary>
    [FieldOffset(12)] public ulong st_ino;

    /// <summary>
    /// 用户 ID：文件所有者的用户 ID。
    /// </summary>
    [FieldOffset(20)] public uint st_uid;

    /// <summary>
    /// 组 ID：文件所有者的组 ID。
    /// </summary>
    [FieldOffset(24)] public uint st_gid;

    /// <summary>
    /// 设备 ID（如果为设备文件）：如果文件是设备文件（字符设备或块设备），则此字段包含设备 ID。
    /// </summary>
    [FieldOffset(28)] public ulong st_rdev;

    /// <summary>
    /// 最后访问时间：文件最后被访问（读或执行）的时间。
    /// </summary>
    [FieldOffset(36)] public Timespec st_atimespec;

    /// <summary>
    /// 最后修改时间：文件内容最后被修改的时间。
    /// </summary>
    [FieldOffset(52)] public Timespec st_mtimespec;

    /// <summary>
    /// 最后状态改变时间：文件的 inode 信息（如权限、所有者、链接数）最后被改变的时间。
    /// </summary>
    [FieldOffset(68)] public Timespec st_ctimespec;

    /// <summary>
    /// 
    /// </summary>
    [FieldOffset(84)] public Timespec st_birthtimespec;

    /// <summary>
    /// 文件大小：以字节为单位的文件大小。
    /// </summary>
    [FieldOffset(96)] public long st_size;  // ✅ 强制偏移 96

    /// <summary>
    /// 块数：该文件实际占用的文件系统块数量。
    /// </summary>
    [FieldOffset(104)] public long st_blocks;

    /// <summary>
    /// 块大小：文件系统 I/O 优选的块大小。这对于高效读写文件很有用。
    /// </summary>
    [FieldOffset(112)] public long st_blksize;

    /// <summary>
    /// 
    /// </summary>
    [FieldOffset(120)] public uint st_flags;

    /// <summary>
    /// 
    /// </summary>
    [FieldOffset(124)] public uint st_gen;
}


/// <summary>
/// Represents a POSIX timespec value.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Timespec
{
    /// <summary>Whole seconds component.</summary>
    public long tv_sec;
    /// <summary>Nanoseconds component.</summary>
    public long tv_nsec;
}

