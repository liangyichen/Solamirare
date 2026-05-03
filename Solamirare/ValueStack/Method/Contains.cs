namespace Solamirare;



public unsafe partial struct ValueStack<T>
where T : unmanaged
{

    /// <summary>
    /// 确定某元素是否在 ValueStack&lt;T&gt; 中。
    /// </summary>
    /// <param name="value">要在 ValueStack&lt;T&gt; 中定位的对象。</param>
    /// <returns>如果在 ValueStack&lt;T&gt; 中找到 value，则为 true；否则为 false。</returns>
    public bool Contains(in T value)
    {
        if (_count == 0)
        {
            return false;
        }

        ulong processedCount = 0;
        for (uint i = 0; i < _segmentCount; i++)
        {
            StackSegment<T>* segment = &_segments[i];

            // 计算此段中实际包含的元素数量
            ulong itemsInThisSegment;
            if (processedCount + segment->Capacity > _count)
            {
                itemsInThisSegment = _count - processedCount;
            }
            else
            {
                itemsInThisSegment = segment->Capacity;
            }

            // 在此段内搜索
            for (uint j = 0; j < itemsInThisSegment; j++)
            {
                // EqualityComparer<T>.Default 对于 unmanaged 类型是高效的
                if (EqualityComparer<T>.Default.Equals(segment->DataPtr[j], value))
                {
                    return true;
                }
            }

            processedCount += itemsInThisSegment;
            if (processedCount >= _count) break;
        }

        return false;
    }

}