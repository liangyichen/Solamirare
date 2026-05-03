namespace Solamirare;

/// <summary>
/// MacOS 上的 open 函数标志
/// </summary>
[Flags]
public enum OpenFlags_MacOS : int
{
    /// <summary>
    /// 以读写模式打开
    /// </summary>
    O_RDWR = 0x0002,

    /// <summary>
    /// 如果文件不存在，则创建它。
    /// </summary>
    O_CREAT = 0x0200,

    /// <summary>
    /// 如果文件已存在，则截断为零长度。
    /// </summary>
    O_TRUNC = 0x0400,

    /// <summary>
    /// 以只写模式打开。
    /// </summary>
    O_WRONLY = 0x0001,

    /// <summary>
    /// 写入时追加到文件末尾。
    /// </summary>
    O_APPEND = 0x0008,

    /// <summary>
    /// 以只读模式打开。
    /// </summary>
    O_RDONLY = 0x0000,

    /// <summary>
    /// 与 O_CREAT 搭配使用，如果文件已存在，则失败。
    /// </summary>
    O_EXCL = 0x0800,

    /// <summary>
    /// 在 execve 调用后关闭文件描述符。
    /// </summary>
    O_CLOEXEC = 0x00100000
}

/// <summary>
/// Linux 上的 open 函数标志
/// </summary>
[Flags]
public enum OpenFlags_Linux : int
{
    /// <summary>
    /// 以读写模式打开
    /// </summary>
    O_RDWR = 2,

    /// <summary>
    /// 如果文件不存在，则创建它。
    /// </summary>
    O_CREAT = 64,

    /// <summary>
    /// 如果文件已存在，则截断为零长度。
    /// </summary>
    O_TRUNC = 512,

    /// <summary>
    /// 以只写模式打开。
    /// </summary>
    O_WRONLY = 1,

    /// <summary>
    /// 写入时追加到文件末尾。
    /// </summary>
    O_APPEND = 1024,

    /// <summary>
    /// 以只读模式打开。
    /// </summary>
    O_RDONLY = 0,

    /// <summary>
    /// 与 O_CREAT 搭配使用，如果文件已存在，则失败。
    /// </summary>
    O_EXCL = 128,

    /// <summary>
    /// 在 execve 调用后关闭文件描述符。
    /// </summary>
    O_CLOEXEC = 2097152
}