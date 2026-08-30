namespace Solamirare;

public unsafe partial struct ValueStack<T>
where T : unmanaged
{

    // 附加一个新的数据分段
    private bool AttachSegment(T* dataPtr, uint capacity)
    {
        if (_segmentCount >= _segmentsTableCapacity)
        {
            // 分段表扩容
            if (!ResizeSegmentsTable(_segmentsTableCapacity == 0 ? INITIAL_SEGMENTS_CAPACITY : _segmentsTableCapacity * 2))
                return false;
        }

        StackSegment<T>* newSegment = _segments + _segmentCount;

        newSegment->DataPtr = dataPtr;
        newSegment->Capacity = capacity;
        newSegment->StartIndex = _totalCapacity; // 以前的总容量是新段的起始索引

        _totalCapacity += capacity;
        _segmentCount++;

        return true;
    }

}