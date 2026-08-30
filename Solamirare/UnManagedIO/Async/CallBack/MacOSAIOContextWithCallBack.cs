namespace Solamirare;



/// <summary>
/// Holds the state for a macOS asynchronous file I/O callback request.
/// </summary>
public unsafe struct MacOSAIOContextWithCallBack
{
    /// <summary>File descriptor associated with the request.</summary>
    public int fd;
    /// <summary>Native AIO control block pointer.</summary>
    public macos_aiocb* cb;
    /// <summary>Read buffer populated on asynchronous reads.</summary>
    public UnManagedMemory<byte> DataOnRead;
    /// <summary>Callback invoked when a read completes.</summary>
    public delegate* unmanaged<UnManagedMemory<byte>*, void*, void> CallbackOnRead;
    /// <summary>Callback invoked when a write completes.</summary>
    public delegate* unmanaged<void*, void> CallbackOnWrite;
    /// <summary>User-supplied callback state.</summary>
    public void* args;

    internal void Close()
    {
        if (fd > 0) MacOSAPI.close(fd);
        fd = -1;
    }
}
