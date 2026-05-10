namespace Solamirare;


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{



    /// <summary>
    /// 确定集合中是否包含指定的元素。
    /// <para>Determines whether the collection contains the specified element.</para>
    /// </summary>
    /// <param name="value">要在集合中定位的元素。<para>The element to locate in the collection.</para></param>
    /// <returns>如果找到元素，则为 true；否则为 false。<para>true if the element is found; otherwise, false.</para></returns>
    public bool Contains(in T value)
    {
        fixed (T* p_value = &value)
        {
            return IndexOf(p_value) > -1;
        }
    }


    /// <summary>
    /// 确定集合中是否包含指定的元素。
    /// <para>Determines whether the collection contains the specified element.</para>
    /// </summary>
    /// <param name="value">指向要在集合中定位的元素的指针。<para>Pointer to the element to locate in the collection.</para></param>
    /// <returns>如果找到元素，则为 true；否则为 false。<para>true if the element is found; otherwise, false.</para></returns>
    public bool Contains(T* value)
    {
        if (value is null) return false;

        return IndexOf(value) > -1;
    }



    /// <summary>
    /// 确定集合中是否包含指定的子序列。
    /// <para>Determines whether the collection contains the specified subsequence.</para>
    /// </summary>
    /// <param name="target">要在集合中定位的子序列。<para>The subsequence to locate in the collection.</para></param>
    /// <returns>如果找到子序列，则为 true；否则为 false。<para>true if the subsequence is found; otherwise, false.</para></returns>
    public bool Contains(ReadOnlySpan<T> target)
    {
        int _indexof = IndexOf(target);

        if (target.IsEmpty && _indexof == 0 && !IsEmpty)
        {
            //C# 规则：查询空值得到的结果会是 0
            return false;
        }

        return _indexof > -1;
    }



}