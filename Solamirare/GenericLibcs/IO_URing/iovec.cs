namespace Solamirare;


[StructLayout(LayoutKind.Sequential)]
internal unsafe struct iovec
{
    /// <summary>Buffer address / 缓冲区地址。</summary>
    public void* iov_base;
    /// <summary>Buffer length / 缓冲区长度。</summary>
    public nuint iov_len;
}
