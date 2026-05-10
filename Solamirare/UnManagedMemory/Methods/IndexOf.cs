namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 搜索指定的集合值相等的子集合，并返回整个集合中第一个匹配项的从零开始的索引。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int IndexOf(ReadOnlySpan<T> target)
    {
        //C#规则：查询空值得到的结果会是 0
        if (target.IsEmpty) return 0;

        if (IsEmpty || !activated)
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
        if (target is null || target->InternalPointer is null || !activated) return -1;

        //C# 规则：查询空值得到的结果会是 0
        if (Capacity == 0 || UsageSize == 0) return 0;

        int result = ValueTypeHelper.IndexOf(Pointer, UsageSize, target->InternalPointer, target->Size);

        return result;
    }



    /// <summary>
    /// 搜索指定的集合值相等的子集合，并返回整个集合中第一个匹配项的从零开始的索引。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int IndexOf(UnManagedMemory<T>* target)
    {
        if (target is null || !activated) return -1;

        T* p = target->Pointer;

        //备注： 托管集合查询 null 不会得到结果， 而是抛出运行时错误， 这里返回 -1 不会影响逻辑
        if (p is null) return -1;

        //C# 规则：查询空值得到的结果会是 0
        if (Capacity == 0 || UsageSize == 0) return 0;

        int result = ValueTypeHelper.IndexOf(Pointer, UsageSize, p, target->UsageSize);

        return result;
    }

    /// <summary>
    /// 搜索指定的对象，并返回整个集合中第一个匹配项的从零开始的索引。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int IndexOf(T* target)
    {
        if (target is null || !activated) return -1;

        if (Pointer is null) return -1;

        return ValueTypeHelper.IndexOf(Pointer, UsageSize, target, 1);
    }



    /// <summary>
    /// 搜索指定的对象，并返回整个集合中第一个匹配项的从零开始的索引。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int IndexOf(in T target)
    {
        if (!activated) return -1;

        int result;

        fixed (T* p_target = &target)
        {
            result = IndexOf(p_target);
        }

        return result;
    }

}