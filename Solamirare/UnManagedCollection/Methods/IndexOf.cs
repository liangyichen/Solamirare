namespace Solamirare;

public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{

    

    /// <summary>
    /// 搜索指定的集合值相等的子集合，并返回整个集合中第一个匹配项的从零开始的索引。
    /// <para>Searches for the specified sub-collection with equal values and returns the zero-based index of the first occurrence within the entire collection.</para>
    /// <para>Searches for the specified sub-collection with equal values and returns the zero-based index of the first occurrence within the entire collection.</para>
    /// <para>Searches for the specified sub-collection with equal values and returns the zero-based index of the first occurrence within the entire collection.</para>
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int IndexOf(ReadOnlySpan<T> target)
    {
        //C#规则：查询空值得到的结果会是 0
        if (target.IsEmpty) return 0;

        if (IsEmpty)
        {
            return -1; //空值中不会存在任何实数，不用继续查了
        }

        Span<T> span = AsSpan();

        int result = ValueTypeHelper.IndexOf(span, target);

        return result;
    }



    /// <summary>
    /// 搜索指定的集合值相等的子集合，并返回整个集合中第一个匹配项的从零开始的索引。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int IndexOf(UnManagedCollection<T>* target)
    {

        //备注： 托管集合查询 null 不会得到结果， 而是抛出运行时错误， 这里返回 -1 不会影响逻辑
        if (target is null || target->InternalPointer is null) return -1;

        //C# 规则：查询空值得到的结果会是 0
        if (Size == 0) return 0;

        int result = ValueTypeHelper.IndexOf(InternalPointer, Size, target->InternalPointer, target->Size);

        return result;
    }



    /// <summary>
    /// 搜索指定的集合值相等的子集合，并返回整个集合中第一个匹配项的从零开始的索引。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int IndexOf(UnManagedMemory<T>* target)
    {
        if (target is null) return -1;

        T* p = target->Pointer;

        //备注： 托管集合查询 null 不会得到结果， 而是抛出运行时错误， 这里返回 -1 不会影响逻辑
        if (p is null) return -1;

        //C# 规则：查询空值得到的结果会是 0
        if (Size == 0) return 0;

        int result = ValueTypeHelper.IndexOf(InternalPointer, Size, p, target->UsageSize);

        return result;
    }



    /// <summary>
    /// 搜索指定的对象，并返回整个集合中第一个匹配项的从零开始的索引。
    /// <para>Searches for the specified object and returns the zero-based index of the first occurrence within the entire collection.</para>
    /// <para>Searches for the specified object and returns the zero-based index of the first occurrence within the entire collection.</para>
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int IndexOf(T* target)
    {
        if (target is null) return -1;

        if (InternalPointer is null) return -1;

        return ValueTypeHelper.IndexOf(InternalPointer, Size, target, 1);
    }



    /// <summary>
    /// 搜索指定的对象，并返回整个集合中第一个匹配项的从零开始的索引。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int IndexOf(in T target)
    {
        int result;

        fixed (T* p_target = &target)
        {
            result = IndexOf(p_target);
        }

        return result;
    }


}
