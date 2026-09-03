namespace Solamirare;

// 通过外部内存地址构造对象，内部逻辑不会再次创建内存，不可释放外部内存


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{

    /// <summary>
    /// 查询第一个符合条件的元素
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public T* FirstOrDefault(delegate*<T*, bool> predicate)
    {
        T* current = InternalPointer;

        T* end = current + Size;

        while (current < end)
        {
            if (predicate(current))
                return current;

            current++;
        }

        return null;
    }

    /// <summary>
    /// 查询第一个符合条件的元素
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public T* FirstOrDefault(T* value)
    {
        if (value is null)
            return null;

        T* current = InternalPointer;
        T* end = current + Size;

        while (current < end)
        {
            bool equals = ValueTypeHelper.Equals(current,value);

            if (equals)
                return current;

            current++;
        }

        return null;
    }

    /// <summary>
    /// 查询第一个符合条件的元素
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public T* FirstOrDefault(in T value)
    {
        T* current = InternalPointer;
        T* end = current + Size;

        fixed(T* selectValue = &value)

        while (current < end)
        {
            bool equals = ValueTypeHelper.Equals(current,selectValue);

            if (equals)
                return current;

            current++;
        }

        return null;
    }

}