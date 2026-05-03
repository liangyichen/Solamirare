namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 与指定的内存段比较，值是否相等。
    /// </summary>
    /// <param name="target"></param>
    /// <param name="targetLength"></param>
    /// <returns></returns>
    public bool Equals(T* target, uint targetLength)
    {
       
        if (target is null || !activated) return false;


        return ValueTypeHelper.Equals(Pointer, UsageSize, target, targetLength);
    }


    /// <summary>
    /// 判断指针指向的所有元素值匹配。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Equals(UnManagedCollection<T>* target)
    {
        if (target is null || !activated) return false;
        if (target->IsEmpty != IsEmpty) return false;

        return ValueTypeHelper.Equals(Pointer, UsageSize, target->InternalPointer, target->Size);
    }

    /// <summary>
    /// 判断指针指向的所有元素值匹配。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Equals(UnManagedMemory<T>* target)
    {
        if (target is null || !activated) return false;
        if (target->IsEmpty != IsEmpty) return false;

        return ValueTypeHelper.Equals(Pointer, UsageSize, target->Pointer, target->UsageSize);
    }

    /// <summary>
    /// 判断指针指向的所有元素值匹配。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Equals(UnManagedMemory<T> target)
    {
        if (target.IsEmpty != IsEmpty || !activated) return false;

        return ValueTypeHelper.Equals(Pointer, UsageSize, target.Pointer, target.UsageSize);
    }

    /// <summary>
    /// 禁止使用，防止误引发 GC。
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    [Obsolete("UnSupport", true)]
    public override bool Equals(object obj)
    {
        throw new Exception("Do not use Object.Equals(object obj).");
    }






    /// <summary>
    /// 判断指针指向的所有元素值匹配。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Equals(in UnManagedCollection<T> target)
    {
        if (target.IsEmpty != IsEmpty || !activated) return false;

        return ValueTypeHelper.Equals(Pointer, UsageSize, target.InternalPointer, target.Size);
    }



    /// <summary>
    /// 判断指针指向的所有元素值匹配。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Equals(in ValueLinkedList<T> target)
    {
        if (target.IsEmpty != IsEmpty || !activated) return false;

        return Prototype.Equals(target);
    }



    /// <summary>
    /// 判断指针指向的所有元素值匹配。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Equals(ReadOnlySpan<T> target)
    {
        if (target.IsEmpty != IsEmpty || !activated) return false;

        fixed (T* p = target)
            return ValueTypeHelper.Equals(Pointer, UsageSize, p, (uint)target.Length);

    }

}