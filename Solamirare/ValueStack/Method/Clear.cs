namespace Solamirare;

public unsafe partial struct ValueStack<T>
where T : unmanaged
{

    /// <summary>
    /// 下标归0，内容清空，内存地址不变，容量依旧，可以立即重复使用
    /// </summary>
    public void Clear(bool @lock = true)
    {
        if (@lock) AcquireSpinlock();

        if (_segments == null)
        {
            _count = 0;
            if (@lock) ReleaseSpinlock();
            return;
        }

        _clear();

        if (@lock) ReleaseSpinlock();
    }

    //真正的clear逻辑，外部调用必须包含在线程锁中
    void _clear()
    {
        // 遍历所有分段，清空数据
        for (ulong i = 0; i < _segmentCount; i++)
        {
            StackSegment<T>* segment = _segments + i;
            if (segment->DataPtr != null)
            {
                // 确保清空的是整个分段的容量
                nuint totalBytesToClear = (nuint)segment->Capacity * (nuint)sizeof(T);
                // DataPtr 现在是 T*，Clear 操作正确
                NativeMemory.Clear(segment->DataPtr, totalBytesToClear);
            }
        }

        _lastSegmentPtr = null;
        _lastSegmentEndIndex = 0;
        _count = 0;
    }

}