using System.Runtime.CompilerServices;

namespace Solamirare;

internal enum IOAsyncContextPlatform : byte
{
    MacOS = 0,

    Windows = 1,

    LinuxAIO = 2,

    LinuxIOUring = 3
}


/// <summary>
/// 异步文件操作
/// </summary>
public static unsafe class AsyncFilesIO
{
    static LinuxKernelVersion kernelVersion;

    static LinuxKernelVersion version61;

    static AsyncFilesIO()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            kernelVersion = LinuxKernelVersionChecker.GetLinuxKernelVersion();

            version61 = new LinuxKernelVersion(6, 1, 0);
        }
    }

    /// <summary>
    /// 异步写入
    /// </summary>
    /// <param name="path"></param>
    /// <param name="content"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static AsyncFilesIOContext WriteAsync(ReadOnlySpan<char> path, UnManagedCollection<byte> content, long offset = 0)
    {
        AsyncFilesIOContext context;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            context = IOCPIO.WriteAsync(path, content, (ulong)offset);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (kernelVersion >= version61)
            {
                context = IO_UringIO.WriteAsync(path, &content, offset);
            }
            else
            {
                context = LinuxAIO.WriteAsync(path, content, offset);
            }
        }
        else
        {
            context = MacOSAIO.WriteAsync(path, content, offset);
        }

        return context;
    }


    /// <summary>
    /// 异步读取
    /// </summary>
    /// <param name="path"></param>
    /// <param name="result"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static AsyncFilesIOContext ReadAsync(ReadOnlySpan<char> path, UnManagedMemory<byte>* result, long offset = 0)
    {
        AsyncFilesIOContext context;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            context = IOCPIO.ReadAsync(path, result, (ulong)offset);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (kernelVersion >= version61)
            {
                context = IO_UringIO.ReadAsync(path, result, offset);
            }
            else
            {
                context = LinuxAIO.ReadAsync(path, result, offset);
            }
        }
        else
        {
            context = MacOSAIO.ReadAsync(path, result, offset);
        }

        return context;
    }

}