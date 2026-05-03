namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 是否包含指定地址的元素
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool Contains(in T value)
    {
        fixed (UnManagedCollection<T>* p = &Prototype)
            return p->Contains(value);
    }

    /// <summary>
    /// 是否包含指定地址的元素(判断值等同)（例如 current == *item）
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool Contains(T* value)
    {
        fixed (UnManagedCollection<T>* p = &Prototype)
            return p->Contains(value);
    }


    /// <summary>
    /// 是否包含元素片段
    /// <para>Whether a local fragment exists.</para>
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Contains(ReadOnlySpan<T> target)
    {
        fixed (UnManagedCollection<T>* p = &Prototype)
            return p->Contains(target);
    }

}