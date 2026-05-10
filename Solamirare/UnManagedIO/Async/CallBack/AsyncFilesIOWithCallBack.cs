using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Solamirare;




/// <summary>
/// Provides platform-dispatched asynchronous file I/O APIs that report completion via unmanaged callbacks.
/// </summary>
public static unsafe class AsyncFilesIOWithCallBack
{
    /// <summary>
    /// Gets the backend selected for the current platform and configuration.
    /// </summary>
    public static readonly IOBackend ActiveBackend;


    static AsyncFilesIOWithCallBack()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ActiveBackend = IOBackend.IOCP;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            ActiveBackend = IOBackend.KQueue;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            LinuxKernelVersion version = LinuxKernelVersionChecker.GetLinuxKernelVersion();

            LinuxKernelVersion version61 = new LinuxKernelVersion(6, 1, 0);

            if (LinuxFilesIOSwitch.LinuxFilesIO == LinuxFilesIO.Auto)
            {
                // 内核版本 > 6.1.0 才启用异步环
                if (version >= version61)
                {
                    ActiveBackend = IOBackend.URing;
                }
                else
                {
                    // 否则降级到 POSIX AIO
                    ActiveBackend = IOBackend.EPoll;
                }
            }
            else if (LinuxFilesIOSwitch.LinuxFilesIO == LinuxFilesIO.IO_URing)
            {
                if (version <= version61) //禁止低版本核心路由到 IO_URing

                    ActiveBackend = IOBackend.EPoll;

                else
                    ActiveBackend = IOBackend.URing;
            }
            else if (LinuxFilesIOSwitch.LinuxFilesIO == LinuxFilesIO.AIO)
            {
                ActiveBackend = IOBackend.EPoll;
            }

        }
    }

    /// <summary>
    /// Writes bytes to a file asynchronously.
    /// </summary>
    /// <param name="path">Target file path.</param>
    /// <param name="content">Content to write.</param>
    /// <param name="cb">Completion callback.</param>
    /// <param name="args">User-supplied callback state.</param>
    /// <param name="offset">Byte offset to begin writing at.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteAsync(UnManagedCollection<char> path, UnManagedCollection<byte> content, delegate* unmanaged<void*, void> cb, void* args, long offset = 0)
    {
        // 静态分发：JIT 会在运行时直接剔除无效分支，生成最精简的机器码
        switch (ActiveBackend)
        {
            case IOBackend.URing:
                IO_URingIOWithCallBack.WriteAsync(path, content, cb, args, offset);
                break;
            case IOBackend.KQueue:
                MacOSAIOWithCallBack.WriteAsync(path, content, cb, args, offset);
                break;
            case IOBackend.IOCP:
                IOCPIOWithCallBack.WriteAsync(path, content, cb, args, (ulong)offset);
                break;
            case IOBackend.EPoll:
                LinuxAIOWithCallBack.WriteAsync(path, content, cb, args, offset);
                break;
        }
    }

    /// <summary>
    /// Reads file data asynchronously.
    /// </summary>
    /// <param name="path">Target file path.</param>
    /// <param name="cb">Completion callback that receives the read buffer.</param>
    /// <param name="args">User-supplied callback state.</param>
    /// <param name="offset">Byte offset to begin reading at.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadAsync(ReadOnlySpan<char> path, delegate* unmanaged<UnManagedMemory<byte>*, void*, void> cb, void* args, long offset = 0)
    {
        switch (ActiveBackend)
        {
            case IOBackend.URing:
                IO_URingIOWithCallBack.ReadAsync(path, cb, args, offset);
                break;
            case IOBackend.KQueue:
                MacOSAIOWithCallBack.ReadAsync(path, cb, args, offset);
                break;
            case IOBackend.IOCP:
                IOCPIOWithCallBack.ReadAsync(path, cb, args, (ulong)offset);
                break;
            case IOBackend.EPoll:
                LinuxAIOWithCallBack.ReadAsync(path, cb, args, offset);
                break;
        }
    }
}
