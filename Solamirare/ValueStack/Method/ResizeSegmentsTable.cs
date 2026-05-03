namespace Solamirare;

public unsafe partial struct ValueStack<T>
where T : unmanaged
{


    // 扩容分段表自身（非数据）
    bool ResizeSegmentsTable(ulong newSegmentsCapacity)
    {

        if (newSegmentsCapacity <= _segmentsTableCapacity) return true;

        int SegmentSize = sizeof(StackSegment<T>);

        uint newSize = (uint)newSegmentsCapacity * (uint)SegmentSize;
        StackSegment<T>* newSegments;



        newSegments = (StackSegment<T>*)NativeMemory.AllocZeroed(newSize);


        if (newSegments == null) return false;

        if (_segments != null)
        {
            // 复制现有分段元数据
            nuint copySize = (nuint)_segmentCount * (nuint)SegmentSize;

            NativeMemory.Copy(_segments, newSegments, copySize);

            NativeMemory.Clear(_segments, _segmentsTableAllocatedBytes);

            // 释放旧的分段表

            NativeMemory.Free(_segments);
        }

        _segments = newSegments;

        _segmentsTableCapacity = newSegmentsCapacity;

        _segmentsTableAllocatedBytes = newSize;

        return true;
    }

}