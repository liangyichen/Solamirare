using System.Runtime.CompilerServices;

namespace Solamirare;





/// <summary>
/// Identifies the active native asynchronous I/O backend.
/// </summary>
public enum IOBackend : byte
{
    /// <summary>Windows IOCP backend.</summary>
    IOCP,
    /// <summary>Linux io_uring backend.</summary>
    URing,
    /// <summary>Linux epoll or POSIX-style fallback backend.</summary>
    EPoll,
    /// <summary>macOS kqueue or AIO-style backend.</summary>
    KQueue
}

/// <summary>
/// Selects which Linux file I/O backend should be preferred.
/// </summary>
public enum LinuxFilesIO:byte
{
    /// <summary>Choose automatically based on platform capability.</summary>
    Auto = 0,

    /// <summary>Prefer the io_uring backend.</summary>
    IO_URing = 1,

    /// <summary>Prefer the POSIX AIO fallback backend.</summary>
    AIO = 2
}

/// <summary>
/// Stores the current Linux file-I/O backend preference.
/// </summary>
public static class LinuxFilesIOSwitch
{
    /// <summary>
    /// 手动设置在 Linux 上使用 IO_URing 或者 AIO，该设置在 Windows 或 MacOS 上无效
    /// </summary>
    /// <summary>
    /// Gets or sets the Linux file-I/O backend preference.
    /// </summary>
    public static LinuxFilesIO LinuxFilesIO;

}
