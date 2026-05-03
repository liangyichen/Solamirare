[StructLayout(LayoutKind.Sequential,Size = 32)]
internal unsafe struct AsyncCallbackContext
{
    /// <summary>用户原始回调函数指针。</summary>
    public delegate* unmanaged<void*, void> UserCallback;

    /// <summary>用户原始数据指针。</summary>
    public void* UserData;

    /// <summary>当前 asyncObj 的指针，用于归还线程池。</summary>
    public void* AsyncObj;
}