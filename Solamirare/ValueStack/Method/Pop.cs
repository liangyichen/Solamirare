namespace Solamirare;

public unsafe partial struct ValueStack<T>
where T : unmanaged
{

    /// <summary>
    /// 尝试出栈，应该首先检查返回的 bool 状态，再从 result 取值
    /// </summary>
    public bool TryPop(out T* result, bool @lock = true)
    {
        if (@lock) AcquireSpinlock();

        if (_count == 0)
        {
            result = null;
            if (@lock) ReleaseSpinlock(); // 释放锁
            return false;
        }

        // 1. 获取要 Pop 元素的逻辑索引
        ulong logicalIndex = _count - 1;

        // 2. 查找要读取的分段和本地索引
        ulong localIndex;
        StackSegment<T>* segment = FindSegment(logicalIndex, out localIndex);

        if (segment == null)
        {
            result = null;
            // 尽管理论上 segment 存在，但为安全起见，应避免使用 TryPop 失败的返回值。
            if (@lock) ReleaseSpinlock();
            return false;
        }

        // 3. 读取数据指针
        result = segment->DataPtr + localIndex;

        // 4. 更新计数 (在锁内，无需 Interlocked)
        _count--;

        // 5. 缓存重校准 (当 Pop 跨越分段边界或栈清空时)
        // 如果 count == 0，或者 count 恰好等于当前缓存段的起始索引（说明该段已清空）
        if (_count == 0 || (_lastSegmentPtr != null && _count == _lastSegmentPtr->StartIndex))
        {
            // 重置缓存，强制下一次 Push 走 FindSegment 路径来重新定位
            _lastSegmentPtr = null;
            _lastSegmentEndIndex = 0;
        }


        if (@lock) ReleaseSpinlock(); // 释放锁
        return true;
    }


    /// <summary>
    /// 从栈顶移除并返回项目。
    /// </summary>
    public T* Pop(bool @lock = true)
    {
        T* result;

        if (TryPop(out result, @lock))
        {
            return result;
        }

        return null;
    }


}