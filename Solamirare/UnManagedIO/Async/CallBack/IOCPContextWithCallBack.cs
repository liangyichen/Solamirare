namespace Solamirare;



/// <summary>
/// Holds the state for a Windows IOCP asynchronous file I/O callback request.
/// </summary>
public unsafe struct IOCPContextWithCallBack
{
    internal void* hFile;
    internal OVERLAPPED Overlapped;
    internal uint ErrorCode;
    internal UnManagedMemory<byte> DataOnRead;
    internal void* args;



    // 签名：void Callback(byte* data, uint size)
    internal delegate* unmanaged<UnManagedMemory<byte>*, void*, void> CallbackOnRead;

    internal delegate* unmanaged<void*, void> CallbackOnWrite;



    /// <summary>
    /// Releases the native resources associated with the request.
    /// </summary>
    public void Close()
    {
        fixed (IOCPContextWithCallBack* self = &this)
        {
            IOCPIOWithCallBack.Close(self);
        }
    }
}
