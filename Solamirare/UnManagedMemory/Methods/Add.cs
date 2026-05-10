namespace Solamirare;

/*

Append 是对 InsertAt 的简单封装，
原因在于如果直接使用 InsertAt 代替 Append 的话，需要以当前对象的长度作为第一个参数，增加了语义的复杂性

*/


public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 在 <see cref="UnManagedMemory{T}"/> 的末尾添加一个元素 (指针模式)
    /// <para>如果仅添加一次，<paramref name="memoryScaleMode"/> 保留默认值以维持最小内存占用</para>
    /// <para>如果连续添加，首次调用应指定 <paramref name="memoryScaleMode"/> 为 <see cref="MemoryScaleMode.X2"/>，后续使用默认值</para>
    /// </summary>
    /// <param name="value">要添加的元素的指针</param>
    /// <param name="memoryScaleMode">
    /// 内存扩容模式，决定了当容量不足时如何扩展内存
    /// <para>默认 <see cref="MemoryScaleMode.AppendEquals"/>, 每次追加时按需分配</para>
    /// <para>对于频繁追加的场景，建议使用 <see cref="MemoryScaleMode.X2"/> 预先分配，减少后续分配次数</para>
    /// </param>
    public bool Add(T* value, MemoryScaleMode memoryScaleMode = MemoryScaleMode.AppendEquals)
    {
        if (value is null) return false;

        return InsertRange(UsageSize, value, 1, memoryScaleMode);
    }

    /// <summary>
    /// 在 <see cref="UnManagedMemory{T}"/> 的末尾添加指定数量的元素 (指针模式)
    /// <para>如果仅添加一次，<paramref name="memoryScaleMode"/> 保留默认值以维持最小内存占用</para>
    /// <para>如果连续添加，首次调用应指定 <paramref name="memoryScaleMode"/> 为 <see cref="MemoryScaleMode.X2"/>，后续使用默认值</para>
    /// </summary>
    /// <param name="value">要添加的元素的指针</param>
    /// <param name="length">要添加的元素的数量</param>
    /// <param name="memoryScaleMode">
    /// 内存扩容模式，决定了当容量不足时如何扩展内存
    /// <para>默认 <see cref="MemoryScaleMode.AppendEquals"/>, 每次追加时按需分配</para>
    /// <para>对于频繁追加的场景，建议使用 <see cref="MemoryScaleMode.X2"/> 预先分配，减少后续分配次数</para>
    /// </param>
    public bool Add(T* value, uint length, MemoryScaleMode memoryScaleMode = MemoryScaleMode.AppendEquals)
    {
        return InsertRange(UsageSize, new Span<T>(value, (int)length), memoryScaleMode);
    }


    /// <summary>
    /// 在 <see cref="UnManagedMemory{T}"/> 的末尾添加一个元素 (值类型复制)
    /// <para>如果仅添加一次，<paramref name="memoryScaleMode"/> 保留默认值以维持最小内存占用</para>
    /// <para>如果连续添加，首次调用应指定 <paramref name="memoryScaleMode"/> 为 <see cref="MemoryScaleMode.X2"/>，后续使用默认值</para>
    /// </summary>
    /// <param name="value">要添加的元素的值</param>
    /// <param name="memoryScaleMode">
    /// 内存扩容模式，决定了当容量不足时如何扩展内存
    /// <para>默认 <see cref="MemoryScaleMode.AppendEquals"/>, 每次追加时按需分配</para>
    /// <para>对于频繁追加的场景，建议使用 <see cref="MemoryScaleMode.X2"/> 预先分配，减少后续分配次数</para>
    /// </param>
    public bool Add(in T value, MemoryScaleMode memoryScaleMode = MemoryScaleMode.AppendEquals)
    {
        return Insert(UsageSize, value, memoryScaleMode);
    }


    /// <summary>
    /// 在 <see cref="UnManagedMemory{T}"/> 的末尾添加一个集合的元素
    /// <para>如果仅添加一次，<paramref name="memoryScaleMode"/> 保留默认值以维持最小内存占用</para>
    /// <para>如果连续添加，首次调用应指定 <paramref name="memoryScaleMode"/> 为 <see cref="MemoryScaleMode.X2"/>，后续使用默认值</para>
    /// </summary>
    /// <param name="values">要添加的元素的集合</param>
    /// <param name="memoryScaleMode">
    /// 内存扩容模式，决定了当容量不足时如何扩展内存
    /// <para>默认 <see cref="MemoryScaleMode.AppendEquals"/>, 每次追加时按需分配</para>
    /// <para>对于频繁追加的场景，建议使用 <see cref="MemoryScaleMode.X2"/> 预先分配，减少后续分配次数</para>
    /// </param>
    public bool AddRange(ReadOnlySpan<T> values, MemoryScaleMode memoryScaleMode = MemoryScaleMode.AppendEquals)
    {
        return InsertRange(UsageSize, values, memoryScaleMode);
    }


    /// <summary>
    /// 在 <see cref="UnManagedMemory{T}"/> 的末尾添加一个非托管集合的元素
    /// <para>如果仅添加一次，<paramref name="memoryScaleMode"/> 保留默认值以维持最小内存占用</para>
    /// <para>如果连续添加，首次调用应指定 <paramref name="memoryScaleMode"/> 为 <see cref="MemoryScaleMode.X2"/>，后续使用默认值</para>
    /// </summary>
    /// <param name="values">要添加的非托管集合</param>
    /// <param name="memoryScaleMode">
    /// 内存扩容模式，决定了当容量不足时如何扩展内存
    /// <para>默认 <see cref="MemoryScaleMode.AppendEquals"/>, 每次追加时按需分配</para>
    /// <para>对于频繁追加的场景，建议使用 <see cref="MemoryScaleMode.X2"/> 预先分配，减少后续分配次数</para>
    /// </param>
    public bool AddRange(UnManagedCollection<T>* values, MemoryScaleMode memoryScaleMode = MemoryScaleMode.AppendEquals)
    {
        return InsertRange(UsageSize, values->InternalPointer, values->Size, memoryScaleMode);
    }

    /// <summary>
    /// 将指定集合的元素添加到 UnManagedMemory&lt;T&gt; 的末尾。
    /// <para>如果仅添加一次，<paramref name="memoryScaleMode"/> 保留默认值以维持最小内存占用</para>
    /// <para>如果连续添加，首次调用应指定 <paramref name="memoryScaleMode"/> 为 <see cref="MemoryScaleMode.X2"/>，后续使用默认值</para>
    /// </summary>
    /// <param name="values">要添加的 <see cref="UnManagedMemory{T}"/> 实例的指针</param>
    /// <param name="memoryScaleMode">
    /// 内存扩容模式，决定了当容量不足时如何扩展内存
    /// <para>默认 <see cref="MemoryScaleMode.AppendEquals"/>, 每次追加时按需分配</para>
    /// <para>对于频繁追加的场景，建议使用 <see cref="MemoryScaleMode.X2"/> 预先分配，减少后续分配次数</para>
    /// </param>
    public bool AddRange(UnManagedMemory<T>* values, MemoryScaleMode memoryScaleMode = MemoryScaleMode.AppendEquals)
    {
        return InsertRange(UsageSize, values->Pointer, values->UsageSize, memoryScaleMode);
    }
}