namespace Solamirare;

public unsafe partial struct ValueStack<T>
where T : unmanaged
{



    /// <summary>
    /// 入栈
    /// </summary>
    /// <param name="item"></param>
    /// <param name="lock"></param>
    /// <returns></returns>
    public bool Push(in T item, bool @lock = true)
    {
        fixed (T* p = &item)
            return Push(p, @lock);
    }



    /// <summary>
    /// 入栈
    /// </summary>
    /// <param name="item"></param>
    /// <param name="lock"></param>
    public bool Push(T* item, bool @lock = true)
    {
        if (@lock) AcquireSpinlock(); // 1. 获取锁，开始原子操作

        ulong localIndex;

        // 2. 尝试使用缓存进行快速写入 (O(1) 路径)
        // 检查条件：
        // a) _count > 0 (避免空栈访问)
        // b) _count < _totalCapacity (容量未满)
        // c) _count < _lastSegmentEndIndex (下一个写入位置落在缓存分段内)
        if (_count > 0 && _count < _lastSegmentEndIndex)
        {
            // 写入位置就是当前的 _count (逻辑索引)
            // 仅在缓存命中时，计算本地索引（这比 FindSegment 快得多）
            localIndex = _count - _lastSegmentPtr->StartIndex;

            // 写入数据 (值复制)
            _lastSegmentPtr->DataPtr[localIndex] = *item;

            // 更新计数
            _count++;

            if (@lock) ReleaseSpinlock(); // 释放锁
            return true;
        }

        // --- 3. 缓存未命中或容量不足，走完整路径 ---

        // 容量已满 (针对 _totalCapacity，而不是缓存命中)
        if (_count >= _totalCapacity)
        {
            // A. 外部内存模式：无法自动扩容
            if (isExternalMemory)
            {
                if (@lock) ReleaseSpinlock();
                return false;
            }

            // B. 内部模式：尝试自动扩容
            uint newCapacity = _totalCapacity == 0 ? DEFAULT_DATA_CAPACITY : _totalCapacity * 2;

            if (!Resize(newCapacity))
            {
                if (@lock) ReleaseSpinlock(); // 扩容失败 (如内存耗尽)
                return false;
            }

            // 扩容成功后，继续执行写入逻辑，保证原子性。
        }

        // --- 4. 写入逻辑 (此时已保证 _count < _totalCapacity) ---

        // 查找要写入的分段和本地索引 (使用 FindSegment，可能是 O(log N) 开销)

        StackSegment<T>* segment = FindSegment(_count, out localIndex);

        if (segment == null)
        {
            if (@lock) ReleaseSpinlock();
            return false;
        }

        // 写入数据 (值复制)
        segment->DataPtr[localIndex] = *item;

        // 5. 更新计数
        _count++;

        // 6. 缓存更新：更新缓存指针到刚刚写入的分段
        _lastSegmentPtr = segment;
        _lastSegmentEndIndex = segment->StartIndex + segment->Capacity;

        // 7. 操作完成，释放锁
        if (@lock) ReleaseSpinlock();
        return true;
    }


}