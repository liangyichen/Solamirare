namespace Solamirare;

[StructLayout(LayoutKind.Sequential,Size = 32)]
internal unsafe struct AsyncWithThreadsPool
{

    private ValueFrozenStack<nint> _pool;

    void* memory;

    uint capacity;

    bool _disposed;

    public AsyncWithThreadsPool():this(64)
    {

    }


    public AsyncWithThreadsPool(uint capacity)
    {
         _pool = new ValueFrozenStack<nint>(capacity);

        this.capacity = capacity;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            AsyncWithThreadLinux* ts = (AsyncWithThreadLinux*)NativeMemory.AllocZeroed((nuint)sizeof(AsyncWithThreadLinux) * capacity);
            for(int i = 0; i < capacity; i++)
            {
                ts[i].Init();
                _pool.Push((IntPtr)(&ts[i]));
            }
            memory = ts;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AsyncWithThreadWindows* ts = (AsyncWithThreadWindows*)NativeMemory.AllocZeroed((nuint)sizeof(AsyncWithThreadWindows) * capacity);
            for(int i = 0; i < capacity; i++)
            {
                ts[i].Init();
                _pool.Push((IntPtr)(&ts[i]));
            }
            memory = ts;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            AsyncWithThreadMac* ts = (AsyncWithThreadMac*)NativeMemory.AllocZeroed((nuint)sizeof(AsyncWithThreadMac) * capacity);
            for(int i = 0; i < capacity; i++)
            {
                ts[i].Init();
                _pool.Push((nint)(&ts[i]));
            }
            memory = ts;
        }

        _disposed = false;
    }

    /// <summary>
    /// 租用线程
    /// </summary>
    /// <returns></returns>
    public void* Rent()
    {
        void* result = null;

        if (_pool.Count > 0)
        {
            nint pop = _pool.Pop();
            result = (void*)pop;
        }

        if (result != null) return result;

        return null;
    }

    /// <summary>
    /// 返回到线程池
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool Return(void* obj)
    {
        if (obj == null) return false;

        bool returned = false;

        if (_pool.Count < capacity)
        {
            returned = _pool.Push((nint)obj);
        }

        return returned;
    }

    /// <summary>
    /// 销毁所有线程
    /// </summary>
    public bool Dispose()
    {
        if(_disposed) return false;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            foreach(nint* i in _pool)
            {
                AsyncWithThreadLinux* item = (AsyncWithThreadLinux*)i;

                item->Dispose();
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            foreach(nint* i in _pool)
            {
                AsyncWithThreadWindows* item = (AsyncWithThreadWindows*)i;

                item->Dispose();
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            foreach(nint* i in _pool)
            {
                AsyncWithThreadMac* item = (AsyncWithThreadMac*)i;

                item->Dispose();
            }
        }

        NativeMemory.Free(memory);

        _disposed = true;

        return _disposed;
    }
}