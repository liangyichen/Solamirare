namespace Solamirare;


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{


    /// <summary>
    /// 从指定索引开始，创建一个到集合末尾的切片视图。此操作不复制内存。
    /// <para>Creates a slice view from the specified index to the end of the collection. This operation does not copy memory.</para>
    /// </summary>
    /// <param name="index">切片的起始索引。<para>The starting index of the slice.</para></param>
    /// <returns>一个表示原始集合一部分的 UnManagedCollection&lt;T&gt; 实例。<para>An UnManagedCollection&lt;T&gt; instance representing a part of the original collection.</para></returns>
    public UnManagedCollection<T> Slice(uint index)
    {
        uint sliceLength = Size - index;

        return Slice(index, sliceLength);
    }



    /// <summary>
    /// 从指定索引开始，创建一个具有指定长度的切片视图。此操作不复制内存。
    /// <para>Creates a slice view with the specified length starting from the specified index. This operation does not copy memory.</para>
    /// </summary>
    /// <param name="index">切片的起始索引。<para>The starting index of the slice.</para></param>
    /// <param name="length">切片的长度。<para>The length of the slice.</para></param>
    /// <returns>一个表示原始集合一部分的 UnManagedCollection&lt;T&gt; 实例。<para>An UnManagedCollection&lt;T&gt; instance representing a part of the original collection.</para></returns>
    public UnManagedCollection<T> Slice(uint index, uint length)
    {

        if (index >= Size || InternalPointer is null)
        {
            return Empty; //起始位超出范围的语义是不存在数据，所以返回空对象
        }



        //允许的最大长度
        uint limit_max_size = Size - index;

        if (length > limit_max_size) length = limit_max_size;

        UnManagedCollection<T> temp = new UnManagedCollection<T>(InternalPointer + index, length);

        return temp;
    }



}