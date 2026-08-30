namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{
    /// <summary>
    /// 确保此列表的容量至少为指定的 capacity。 
    /// <para>如果当前容量小于 capacity，并且属于内部堆分配模式时，容量会至少增加到指定的 capacity，并且现有的值一并复制到新的地址。</para>
    /// <para>如果当前容量小于 capacity，但是属于 栈内存、外部内存、只读模式 这三种情况时，会返回 false。</para>
    /// <para>如果当前对象尚未分配内部指向的内存，则立即通过内部堆分配对应 capacity 数量的内存。</para>
    /// <para>如果当前容量大于等于 capacity，会立即返回 true，表明当前对象符合检测需求。</para>
    /// </summary>
    /// <param name="capacity">指定长度</param>
    /// <param name="mode"></param>
    /// <returns>在具备足够长度的情况下，栈内存与外部内存都会返回 true， 但是如果涉及到扩容的情况， 栈内存与外部内存肯定会返回 false。</returns>
    public bool EnsureCapacity(uint capacity, MemoryScaleMode mode = MemoryScaleMode.X3)
    {
        if (Capacity >= capacity) return true;

        if (@readonly || !activated)
            return false;

        if (capacity > Capacity)
        {
            if (OnStack || isExternalMemory)
                return false;
            else
            {
                if (Capacity < capacity)
                {
                    uint new_size = processNewSize(capacity, mode);

                    Resize(new_size);
                }

                return true;
            }
        }
        else
        {
            return true;
        }
    }

    uint processNewSize(uint TCount, MemoryScaleMode mode)
    {
        uint new_size;

        if (mode == MemoryScaleMode.X3)
        {
            new_size = Capacity + TCount;

            new_size += new_size / 2;

            if (new_size % 2 != 0) new_size += 1;
        }
        else if (mode == MemoryScaleMode.X2)
        {
            new_size = Capacity * 2;

            if (new_size == 0)
            {
                new_size = TCount; //指针尚未初始化的情况
            }
            else
            {
                while (new_size < TCount)
                {
                    new_size = new_size * 2;
                }
            }
        }
        else
        {
            new_size = TCount;
        }

        return new_size;
    }

}