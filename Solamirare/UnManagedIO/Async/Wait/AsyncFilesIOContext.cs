using System.Runtime.CompilerServices;


namespace Solamirare;


/// <summary>
/// 文件操作上下文
/// </summary>
public unsafe struct AsyncFilesIOContext
{
    internal void* cb;

    internal IOCPContext* IOCPContext;

    internal IO_URingContext* IO_URingContext;

    internal int fd;

    internal bool isDone;

    internal bool isWrite;

    internal IOAsyncContextPlatform Platform;

    bool disposed;

    bool closed;

    /// <summary>
    /// 销毁资源
    /// </summary>
    public void Dispose()
    {
        if (disposed) return;

        bool release;

        if (IOCPContext is not null && Platform == IOAsyncContextPlatform.Windows)
        {
            release = IOCPIO.memoryPool.Return(IOCPContext, (ulong)sizeof(IOCPContext));
        }
        else if (Platform == IOAsyncContextPlatform.MacOS)
        {
            release = MacOSAIO.memoryPool.Return(cb, (ulong)sizeof(MacOSAIOContext));
        }
        else if (Platform == IOAsyncContextPlatform.LinuxAIO)
        {
            release = LinuxAIO.memoryPool.Return(cb, (ulong)sizeof(LinuxAIOContext));
        }
        else if (Platform == IOAsyncContextPlatform.LinuxIOUring)
        {
            release = IO_UringIO.memoryPool.Return(IO_URingContext, (ulong)sizeof(IO_URingContext));
        }


        disposed = true;
    }

    /// <summary>
    /// 关闭文件操作
    /// </summary>
    public void Close()
    {
        if (closed) return;

        if (IOCPContext is not null && Platform == IOAsyncContextPlatform.Windows)
        {
            IOCPIO.Close(IOCPContext);
        }
        else
        {
            if (Platform == IOAsyncContextPlatform.MacOS)
            { if (fd > 0) MacOSAPI.close(fd); }
            else if (Platform == IOAsyncContextPlatform.LinuxAIO)
            { if (fd > 0) LinuxAPI.close(fd); }
            else if (IO_URingContext is not null && Platform == IOAsyncContextPlatform.LinuxIOUring)
            { if (fd > 0) LinuxAPI.close(fd); }
        }

        closed = true;
    }

    /// <summary>
    /// 等待操作完成
    /// </summary>
    /// <returns></returns>
    public uint Wait()
    {
        if (isDone) return 0;

        uint result;

        if (IOCPContext is not null && Platform == IOAsyncContextPlatform.Windows)
        {
            result = IOCPIO.Wait(IOCPContext);
        }
        else
        {
            fixed (AsyncFilesIOContext* self = &this)
            {

                if (Platform == IOAsyncContextPlatform.LinuxAIO)
                {
                    result = LinuxAIO.Wait(self);
                }
                else if (IO_URingContext is not null && Platform == IOAsyncContextPlatform.LinuxIOUring)
                {
                    result = IO_UringIO.Wait(IO_URingContext);
                }
                else if (Platform == IOAsyncContextPlatform.MacOS)
                {
                    result = MacOSAIO.Wait(self);
                }
                else
                {
                    result = 0;
                }
            }
        }

        isDone = true;

        return result;

    }
}
