namespace Solamirare;




public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{


    /// <summary>
    /// 从当前集合创建指定范围的 Span 视图。
    /// <para>Creates a Span view of the specified range from the current collection.</para>
    /// </summary>
    /// <param name="index">切片的起始索引。<para>The starting index of the slice.</para></param>
    /// <param name="length">切片的长度。<para>The length of the slice.</para></param>
    /// <returns>表示集合子集的 Span。<para>A Span representing a subset of the collection.</para></returns>
    public Span<T> AsSpan(int index, uint length)
    {
        if (Size == 0) return Span<T>.Empty;

        if (index >= Size) index = (int)Size - 1; //限制最大下标，超出后取最大下标值

        //允许的最大长度
        uint limit_max_size = Size - (uint)index;

        if (length > limit_max_size) length = limit_max_size;

        Span<T> span;

        if (InternalPointer is not null)

            span = new Span<T>(InternalPointer + index, (int)length);

        else

            span = Span<T>.Empty;

        return span;
    }


    /// <summary>
    /// 从当前集合的全部内容创建 Span 视图。
    /// <para>Creates a Span view from the entire content of the current collection.</para>
    /// </summary>
    /// <returns>表示整个集合的 Span。<para>A Span representing the entire collection.</para></returns>
    public Span<T> AsSpan()
    {
        if (Size > 0)
        {
            return AsSpan(0, Size);
        }
        else
        {
            return Span<T>.Empty;
        }
    }

}