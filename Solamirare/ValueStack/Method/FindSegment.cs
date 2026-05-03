namespace Solamirare;

public unsafe partial struct ValueStack<T>
where T : unmanaged
{


    /// <summary>
    /// 根据逻辑索引定位到具体的分段和段内偏移。
    /// 使用二分查找 (Binary Search)，复杂度 O(log N_segments)。
    /// </summary>
    /// <param name="logicalIndex">从 0 开始的逻辑索引。</param>
    /// <param name="localIndex">输出：该元素在分段内的本地索引。</param>
    /// <returns>指向 StackSegment 的指针，如果超出容量返回 null。</returns>
    StackSegment<T>* FindSegment(ulong logicalIndex, out ulong localIndex)
    {
        localIndex = 0; // 默认初始化

        // 1. 索引超出容量检查
        if (logicalIndex >= _totalCapacity || _segmentCount == 0)
        {
            return null;
        }

        // 2. 二分查找实现
        ulong low = 0;
        // high 是最后一个分段的索引
        ulong high = _segmentCount - 1;

        while (low <= high)
        {
            // 确保 mid 不会溢出，即使 low 和 high 接近 ulong.MaxValue
            ulong mid = low + (high - low) / 2;
            StackSegment<T>* segment = _segments + mid;

            // 当前分段的结束索引 (不包含)
            ulong segmentEndIndex = segment->StartIndex + segment->Capacity;

            // 检查逻辑索引是否在该分段范围内
            if (logicalIndex >= segment->StartIndex && logicalIndex < segmentEndIndex)
            {
                // 找到了分段，计算分段内的本地索引
                localIndex = logicalIndex - segment->StartIndex;
                return segment;
            }
            else if (logicalIndex >= segmentEndIndex)
            {
                // 目标在右侧（更高的索引），因为当前的段结束了
                low = mid + 1;
            }
            else
            {
                // 目标在左侧（更低的索引），因为 logicalIndex < segment->StartIndex
                // 并且我们知道 logicalIndex < _totalCapacity，所以它可能在左侧的段
                high = mid - 1;
            }
        }

        // 理论上，如果索引 < _totalCapacity，不会到达这里
        return null;
    }

}