namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 创建一个覆盖整个已分配容量 (`Capacity`) 的 `SpanSpan&lt;;T&gt;` 视图。
    /// </summary>
    /// <returns>一个覆盖从内存起始位置到 `Capacity` 的 `SpanSpan&lt;;T&gt;`。</returns>
    public Span<T> AsRealSizeSpan()
    {
        Span<T> span;

        if (Pointer is not null && Capacity > 0 && activated)
            span = new Span<T>(Pointer, (int)Capacity);
        else
            span = Span<T>.Empty;

        return span;
    }


    /// <summary>
    /// 创建一个覆盖当前已使用内存区域 (`UsageSize`) 的 `Span&lt;;T&gt;` 视图。
    /// </summary>
    /// <returns>一个覆盖从内存起始位置到 `UsageSize` 的 `Span&lt;;T&gt;`。</returns>
    public Span<T> AsSpan()
    {
        if (!activated) return Span<T>.Empty;

        if (!onMemoryPool)
        {
            fixed (UnManagedCollection<T>* p = &Prototype)
            {
                return p->AsSpan();
            }
        }
        else
        {
            Span<T> span = new Span<T>(Pointer, (int)UsageSize);
            return span;
        }
    }


    /// <summary>
    /// 从指定索引开始，创建并返回一个包含特定数量元素的 `Span&lt;;T&gt;` 视图。
    /// </summary>
    /// <param name="startIndex">视图的起始索引。</param>
    /// <param name="count">视图中要包含的元素数量。</param>
    /// <returns>一个从指定位置开始的 `Span&lt;;T&gt;` 视图。</returns>
    public Span<T> AsSpan(int startIndex, uint count)
    {
        if (!activated) return Span<T>.Empty;

        if (!onMemoryPool)
            fixed (UnManagedCollection<T>* p = &Prototype)
            {
                return p->AsSpan(startIndex, count);
            }
        else
        {
            Span<T> span = AsSpan().Slice(startIndex, (int)count);
            return span;
        }
    }

}