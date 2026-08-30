namespace Solamirare;

public unsafe partial struct ValueStack<T>
where T : unmanaged
{

    /// <summary>
    /// 根据所有分段的实际内容计算哈希值。
    /// <para>每段数据与其容量均参与计算，段边界信息不会丢失。</para>
    /// </summary>
    public override int GetHashCode()
    {
        if (_segmentCount == 0 || _segments == null)
            return 0;
 
        uint hash = 0;
 
        for (uint i = 0; i < _segmentCount; i++)
        {
            StackSegment<T>* segment = _segments + i;
 
            if (segment->DataPtr == null || segment->Capacity == 0)
                continue;
 
            // 用上一轮的 hash 作为 seed，实现跨段状态串联
            hash = ValueTypeHelper.HashCode(segment->DataPtr, (int)segment->Capacity, hash);
 
            // 混入本段容量，消除不同分段边界下内容拼接相同的碰撞问题
            hash = ValueTypeHelper.HashCode(&hash, 1, segment->Capacity);
        }
 
        return (int)hash;
    }

}