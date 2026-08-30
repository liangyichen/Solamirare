namespace Solamirare;



/// <summary>
/// 回调上下文
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct LinuxAIOContextWithCallBack
{
    /// <summary>Native AIO control block pointer.</summary>
    public linux_aiocb* cb;
    /// <summary>File descriptor associated with the request.</summary>
    public int fd;
    /// <summary>Callback invoked when a write completes.</summary>
    public delegate* unmanaged<void*, void> OnWriteCompleted;
    /// <summary>Callback invoked when a read completes.</summary>
    public delegate* unmanaged<UnManagedMemory<byte>*, void*, void> OnReadCompleted;
    /// <summary>User-supplied callback state.</summary>
    public void* UserArgs;
    /// <summary>Read buffer populated on asynchronous reads.</summary>
    public UnManagedMemory<byte> ReadResult;
    /// <summary>Operation kind: 0 for read, 1 for write.</summary>
    public byte OpType; // 0: Read, 1: Write
}
