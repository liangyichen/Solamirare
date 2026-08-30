namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{


    /// <summary>
    /// 将元素插入指定索引处。
    /// </summary>
    /// <param name="index">在该位置的前方插入。</param>
    /// <param name="value"></param>
    /// <param name="memoryScaleMode"></param>
    /// <returns></returns>
    public bool Insert(uint index, in T value, MemoryScaleMode memoryScaleMode = MemoryScaleMode.AppendEquals)
    {
        fixed (T* p = &value)
        {
            return InsertRange(index, p, 1, memoryScaleMode);
        }
    }


    /// <summary>
    /// 将集合的元素插入到指定索引处
    /// <para>Inserts the elements of the collection at the specified index.</para>
    /// </summary>
    /// <param name="index">在该位置的前方插入<para>Insert before this position.</para></param>
    /// <param name="p"></param>
    /// <param name="count"></param>
    /// <param name="memoryScaleMode"></param>
    /// <returns></returns>
    public bool InsertRange(uint index, T* p, uint count, MemoryScaleMode memoryScaleMode = MemoryScaleMode.AppendEquals)
    {
        if (p is null || @readonly || count == 0 || !activated) return false;

        bool result = false;

        uint newSize = UsageSize + count;

        bool valid = EnsureCapacity(newSize, memoryScaleMode);

        if (!valid)
        {
            //长度不足并且扩容失败
            result = false;
            goto RETURN;
        }

        uint lengthOfAfter = UsageSize - index;

        T* point = Pointer;

        NativeMemory.Copy(&point[index], &point[index + count], (uint)(lengthOfAfter * sizeof(T)));

        NativeMemory.Copy(p, &point[index], (uint)(count * sizeof(T)));

        Prototype.size += count;

        result = true;

    RETURN:
        return result;
    }


    /// <summary>
    /// 将集合的元素插入到指定索引处。
    /// </summary>
    /// <param name="index">在该位置的前方插入。</param>
    /// <param name="value"></param>
    /// <param name="memoryScaleMode"></param>
    /// <returns></returns>
    public bool InsertRange(uint index, ReadOnlySpan<T> value, MemoryScaleMode memoryScaleMode = MemoryScaleMode.AppendEquals)
    {

        fixed (T* p_value = value)
        {
            return InsertRange(index, p_value, (uint)value.Length, memoryScaleMode);
        }

    }


    /// <summary>
    /// 将集合的元素插入到指定索引处。
    /// </summary>
    /// <param name="index">在该位置的前方插入。</param>
    /// <param name="memory"></param>
    /// <param name="memoryScaleMode"></param>
    /// <returns></returns>
    public bool InsertRange(uint index, UnManagedCollection<T>* memory, MemoryScaleMode memoryScaleMode = MemoryScaleMode.AppendEquals)
    {
        if (memory is null) return false;

        return InsertRange(index, memory->InternalPointer, memory->Size, memoryScaleMode);
    }

}