namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 包含任意一个元素。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int IndexOfAny(UnManagedMemory<T>* target)
    {
        if (!activated) return -1;

        fixed (UnManagedCollection<T>* p = &Prototype)

            return p->IndexOfAny(target);
    }


    /// <summary>
    /// 包含任意一个元素。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int IndexOfAny(ReadOnlySpan<T> target)
    {
        if (!activated) return -1;

        fixed (UnManagedCollection<T>* p = &Prototype)

            return p->IndexOfAny(target);
    }

}