namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 搜索指定的对象，并返回整个集合中最后一个匹配项的从零开始的索引。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int LastIndexOf(in T target)
    {
        if (!activated) return -1;

        fixed (T* p_target = &target)
        {
            return ValueTypeHelper.LastIndexOf(Pointer, UsageSize, p_target, 1);
        }

    }

    /// <summary>
    /// 搜索指定的对象，并返回整个集合中最后一个匹配项的从零开始的索引。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int LastIndexOf(T* target)
    {
        if (!activated) return -1;

        if (target is null) return -1;

        return ValueTypeHelper.LastIndexOf(Pointer, UsageSize, target, 1);
    }

    /// <summary>
    /// 搜索指定的对象，并返回整个集合中最后一个匹配项的从零开始的索引。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int LastIndexOf(UnManagedMemory<T>* target)
    {
        if (!activated) return -1;

        if (target is null) return -1;

        if (target->IsEmpty) return (int)UsageSize; // C# 规则，必须是当前的长度

        return ValueTypeHelper.LastIndexOf(Pointer, UsageSize, target->Pointer, target->UsageSize);
    }


    /// <summary>
    /// 搜索指定的对象，并返回整个集合中最后一个匹配项的从零开始的索引。
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public int LastIndexOf(ReadOnlySpan<T> value)
    {
        if (!activated) return -1;

        if (value.IsEmpty) return (int)UsageSize; // C# 规则，必须是当前的长度

        fixed (T* p_value = value)

            return ValueTypeHelper.LastIndexOf(Pointer, UsageSize, p_value, (uint)value.Length);
    }

}