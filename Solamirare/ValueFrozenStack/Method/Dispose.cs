namespace Solamirare;

public unsafe partial struct ValueFrozenStack<T>
where T : unmanaged
{

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {

        if (_buffer != null && !_externalMemory)
        {
            ulong size = (_capacity & 0x3FFFFFFFu) * (ulong)sizeof(T);
            
            NativeMemory.Free(_buffer);
        }

        _buffer = null;
        _capacity = 0;
        _count = 0;

    }

}