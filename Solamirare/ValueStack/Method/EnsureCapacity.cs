namespace Solamirare;

public unsafe partial struct ValueStack<T>
where T : unmanaged
{


    /// <summary>
    /// 确保容量至少为指定的值，如果满足条件或者传入0，内部不会执行操作，如果不满足，则执行扩容到指定值
    /// </summary>
    public void EnsureCapacity(uint capacity, bool @lock = true)
    {
        // 外部内存模式下禁止 EnsureCapacity 自动分配内存
        if (capacity < 1 || isExternalMemory) return;

        if (_totalCapacity < capacity)
        {
            if (@lock) AcquireSpinlock();
            try
            {
                Resize(capacity);
            }
            finally
            {
                if (@lock) ReleaseSpinlock();
            }
        }
    }

}