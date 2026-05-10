namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 创建一个分隔观测器 （不会占用额外内存，不允许 Dispose)
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public UnManagedMemory<T> Slice(uint index)
    {
        uint sliceLength = Capacity - index;

        return Slice(index, sliceLength);
    }


    /// <summary>
    /// 创建一个分隔观测器 （不会占用额外内存，不允许 Dispose)
    /// </summary>
    /// <param name="index"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public UnManagedMemory<T> Slice(uint index, uint length)
    {
        if (!activated) return Empty;

        UnManagedCollection <T> col = Prototype.Slice(index, length);
        
        UnManagedMemory<T> result = col.AsUnManagedMemory();
        result.disposed = true;

        return result;
    }


    /// <summary>
    /// 创建一个 UnManagedCollection 表示的分隔观测器
    /// </summary>
    /// <param name="index"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public UnManagedCollection<T> SliceToUnManagedCollection(uint index, uint length)
    {
        if (!activated) return Empty;

        UnManagedCollection <T> col = Prototype.Slice(index, length);

        return col;
    }

    /// <summary>
    /// 创建一个 Span 表示的分隔观测器
    /// </summary>
    /// <param name="index"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public Span<T> SliceToSpan(uint index, uint length)
    {
        if (!activated) return Empty;

        UnManagedCollection <T> col = Prototype.Slice(index, length);

        return col.AsSpan();
    }

}