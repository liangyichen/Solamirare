namespace Solamirare;

public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{

    /// <summary>
    /// 从集合的末尾开始查找，返回最后一个匹配元素的索引
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int LastIndexOf(ReadOnlySpan<T> target)
    {
        //C#规则：查询空值得到的结果会是 0
        if (target.IsEmpty) return 0;

        if (IsEmpty)
        {
            return -1; //空值中不会存在任何实数，不用继续查了
        }

        Span<T> span = AsSpan();

        int result = ValueTypeHelper.LastIndexOf(span, target);

        return result;
    }

    /// <summary>
    /// 从集合的末尾开始查找，返回最后一个匹配元素的索引
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int LastIndexOf(UnManagedCollection<T> target)
    {
        UnManagedCollection<T>* p = &target;
        {
            return LastIndexOf(p);
        }
    }

    /// <summary>
    /// 从集合的末尾开始查找，返回最后一个匹配元素的索引
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int LastIndexOf(UnManagedCollection<T>* target)
    {
        
        //备注： 托管集合查询 null 不会得到结果， 而是抛出运行时错误， 这里返回 -1 不会影响逻辑
        if (target is null || target->InternalPointer is null) return -1;

        //C#规则：查询空值得到的结果会是 0
        if (target->IsEmpty) return 0;

        //C# 规则：查询空值得到的结果会是 0
        if (Size == 0) return 0;

        int result = ValueTypeHelper.LastIndexOf(InternalPointer, Size, target->InternalPointer, target->Size);

        return result;
    }

    /// <summary>
    /// 从集合的末尾开始查找，返回最后一个匹配元素的索引
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int LastIndexOf(UnManagedMemory<T>* target)
    {
        if (target is null) return -1;

        T* p = target->Pointer;

        //备注： 托管集合查询 null 不会得到结果， 而是抛出运行时错误， 这里返回 -1 不会影响逻辑
        if (p is null) return -1;

        //C#规则：查询空值得到的结果会是 0
        if (target->IsEmpty) return 0;

        //C# 规则：查询空值得到的结果会是 0
        if (Size == 0) return 0;

        int result = ValueTypeHelper.LastIndexOf(InternalPointer, Size, p, target->UsageSize);

        return result;
    }

    /// <summary>
    /// 从集合的末尾开始查找，返回最后一个匹配元素的索引
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int LastIndexOf(UnManagedMemory<T> target)
    {
        UnManagedMemory<T>* p = &target;

        return LastIndexOf(p);
    }

}