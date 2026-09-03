namespace Solamirare;

// 通过外部内存地址构造对象，内部逻辑不会再次创建内存，不可释放外部内存


public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 查询第一个符合条件的元素
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public T* FirstOrDefault(delegate*<T*, bool> predicate)
    {
        return Prototype.FirstOrDefault(predicate);
    }

    /// <summary>
    /// 查询第一个符合条件的元素
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public T* FirstOrDefault(T* value)
    {
        return Prototype.FirstOrDefault(value);
    }

    /// <summary>
    /// 查询第一个符合条件的元素
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public T* FirstOrDefault(in T value)
    {
        return Prototype.FirstOrDefault(value);
    }

}