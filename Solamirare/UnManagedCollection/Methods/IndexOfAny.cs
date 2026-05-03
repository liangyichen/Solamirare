namespace Solamirare;


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{


    /// <summary>
    /// 报告指定集合中的任何元素在此实例中第一次出现的索引。
    /// <para>Reports the zero-based index of the first occurrence in this instance of any element in the specified collection.</para>
    /// </summary>
    /// <param name="target">一个包含要寻找的一个或多个元素的集合。<para>A collection containing one or more elements to seek.</para></param>
    /// <returns>target 中任何元素在此实例中第一次出现的从零开始的索引；如果未找到，则为 -1。<para>The zero-based index of the first occurrence in this instance of any element in target; -1 if not found.</para></returns>
    public int IndexOfAny(UnManagedMemory<T>* target)
    {
        if (target is null || InternalPointer is null) return -1;

        if (target->IsEmpty) return 0; //C# 的规则，查找空集合会得到 0

        if (!target->IsEmpty && Size > 0)
        {
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < target->UsageSize; j++)
                {
                    bool equal = ValueTypeHelper.IndexOf(InternalPointer + i, 1, target->Pointer + j, 1) == 0;


                    if (equal)
                    {
                        return i;
                    }
                }
            }
        }

        return -1;
    }



    /// <summary>
    /// 报告指定只读范围中的任何元素在此实例中第一次出现的索引。
    /// <para>Reports the zero-based index of the first occurrence in this instance of any element in the specified read-only span.</para>
    /// </summary>
    /// <param name="target">一个包含要寻找的一个或多个元素的只读范围。<para>A read-only span containing one or more elements to seek.</para></param>
    /// <returns>target 中任何元素在此实例中第一次出现的从零开始的索引；如果未找到，则为 -1。<para>The zero-based index of the first occurrence in this instance of any element in target; -1 if not found.</para></returns>
    public int IndexOfAny(ReadOnlySpan<T> target)
    {
        if (InternalPointer is null) return -1;

        if (target.IsEmpty) return 0; //C# 的规则，查找空集合会得到 0

        if (Size > 0)
        {
            fixed (T* p_target = target)
                for (int i = 0; i < Size; i++)
                {
                    for (int j = 0; j < target.Length; j++)
                    {
                        bool equal = ValueTypeHelper.IndexOf(InternalPointer + i, 1, p_target + j, 1) == 0;

                        if (equal)
                        {
                            return i;
                        }
                    }
                }
        }

        return -1;
    }


}