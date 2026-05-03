namespace Solamirare;

public unsafe partial struct ValueStack<T>
where T : unmanaged
{


    // 创建新的数据分段
    // 为避免死锁，Resize 不加锁，依赖于上层调用者的锁保护。
    bool Resize(uint newCapacity)
    {
        // Resize 被 EnsureCapacity 或 Push 内部调用。
        // 如果是从 Push 调用的，Push 已经持有锁，这里不应重复加锁。
        // 如果是从 EnsureCapacity 调用的，EnsureCapacity 通常在单线程环境使用，或者调用者自己负责同步。

        if (isExternalMemory) return false;

        if (newCapacity <= _totalCapacity) return true;

        // 计算需要新增的容量（注意这是增加的数量，而不是新的容量，例如原始容量6，需要扩容到新容量10，那么这里的值是4，而不是10）
        uint newSegmentCapacity = newCapacity - _totalCapacity;
        if (newSegmentCapacity == 0) return true;

        // 1. 分配新的数据缓冲区

        T* newData;

        uint newSize = newSegmentCapacity * (uint)sizeof(T);

        newData = (T*)NativeMemory.AllocZeroed(newSize);

        if (newData == null) return false;

        // 2. 将新分段附加到 Segment Table
        if (AttachSegment(newData, newSegmentCapacity))
        {
            // _totalCapacity 已在 AttachSegment 中更新
            return true;
        }
        else
        {
            // 如果附加失败，释放刚刚分配的数据缓冲区

            NativeMemory.Free(newData);

            return false;
        }
    }


}