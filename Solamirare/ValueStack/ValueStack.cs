using System.Runtime.CompilerServices;
using System.Security;

namespace Solamirare;


/// <summary>
/// 可以扩展容量的栈 (分段存储，支持外部内存增量扩容)
/// </summary>
[SecurityCritical]
[SkipLocalsInit]
[Guid(SolamirareEnvironment.ValueStackGuid)]
[StructLayout(LayoutKind.Auto, CharSet = CharSet.Ansi, Size = 64)]
public unsafe partial struct ValueStack<T>
where T : unmanaged
{

    const uint INITIAL_SEGMENTS_CAPACITY = 4;

    const uint DEFAULT_DATA_CAPACITY = 16;

    // 使用量
    ulong _count;

    // 指向分段表本身的指针
    StackSegment<T>* _segments;

    // 缓存最近写入的分段指针
    StackSegment<T>* _lastSegmentPtr;

    /// <summary>
    /// 分段表 (_segments) 数组的容量
    /// </summary>
    ulong _segmentsTableCapacity;

    /// <summary>
    /// 所有分段的总容量
    /// </summary>
    uint _totalCapacity;

    /// <summary>
    /// 当前已附加的分段数量
    /// </summary>
    uint _segmentCount;



    // 缓存最近写入分段的结束索引 (不包含)
    uint _lastSegmentEndIndex;

    // 分段表实际分配的字节大小
    uint _segmentsTableAllocatedBytes;

    /// <summary>
    /// 线程安全：自旋锁状态 (0: unlock, 1: lock)
    /// </summary>
    int _spinlock;

    //Dispose 状态标记: 0: Not Disposed, 1: Disposed
    int _disposedFlag;

    // 是否依托于外部内存
    bool isExternalMemory;

 


    /// <summary>
    /// 当前使用量
    /// </summary>
    public ulong Count => _count;

    /// <summary>
    /// 当前总容量
    /// </summary>
    public ulong Capacity => _totalCapacity;

    /// <summary>
    /// 当前的分段数量
    /// </summary>
    public ulong SegmentCount => _segmentCount;

    /// <summary>
    /// 当前使用量是否为空
    /// </summary>
    public bool IsEmpty => _count == 0;


    void constructor_reset()
    {
        _segments = null;
        _totalCapacity = 0;
        _segmentCount = 0;
        _segmentsTableCapacity = 0;
        _count = 0;
        isExternalMemory = false;
        _lastSegmentPtr = null;
        _lastSegmentEndIndex = 0;
        _spinlock = 0;
    }


    /// <summary>
    /// 非托管栈，自承载模式
    /// </summary>
    /// <param name="capacity"></param>
    public ValueStack(uint capacity)
    {
        

        constructor_reset();

        isExternalMemory = false;

        // 始终初始化分段表
        if (!ResizeSegmentsTable(INITIAL_SEGMENTS_CAPACITY))
        {
            throw new OutOfMemoryException("Failed to allocate segment table.");
        }

        if (capacity > 0)
        {
            // 分配初始分段和数据
            EnsureCapacity(capacity);
        }
    }

    /// <summary>
    /// 非托管栈，外部内存承载（仅使用提供的第一个外部内存块）
    /// </summary>
    /// <param name="capacity"></param>
    /// <param name="externalMemory"></param>
    public ValueStack(uint capacity, T* externalMemory)
    {

        constructor_reset();

        if (capacity > 0 && externalMemory is not null)
        {
            isExternalMemory = true;

            // 附加第一个外部内存分段
            AttachSegment(externalMemory, capacity);
        }
    }




    /// <summary>
    /// 尝试将 ValueStack&lt;T&gt; 中的元素复制到 ReadOnlySpan&lt;T&gt; 中。
    /// </summary>
    /// <param name="destination">目标 ReadOnlySpan&lt;T&gt;。</param>
    /// <param name="destinationIndex">目标 ReadOnlySpan&lt;T&gt; 中开始复制的索引。</param>
    /// <param name="count">要复制的元素数量。</param>
    /// <returns>如果复制成功，则为 true；否则为 false。</returns>
    public bool TryCopyTo(ReadOnlySpan<T> destination, ulong destinationIndex, ulong count)
    {
        if (destination.IsEmpty)
        {
            return false;
        }

        if (destinationIndex + count > (ulong)destination.Length || count > _count)
        {
            return false;
        }

        fixed (T* destPtr = destination)
        {
            return TryCopyTo(destPtr, destinationIndex, count);
        }
    }

    /// <summary>
    /// 尝试将 ValueStack&lt;T&gt; 中的元素复制到 T* 数组中。
    /// </summary>
    /// <param name="destination">目标 T* 数组。</param>
    /// <param name="destinationIndex">目标数组中开始复制的索引。</param>
    /// <param name="count">要复制的元素数量。</param>
    /// <returns>如果复制成功，则为 true；否则为 false。</returns>
    public bool TryCopyTo(T* destination, ulong destinationIndex, ulong count)
    {
        if (destination == null)
        {
            return false;
        }

        if (destinationIndex < 0 || count < 0 || (ulong)count > _count)
        {
            return false;
        }

        ulong processedCount = 0;
        ulong currentDestinationIndex = destinationIndex;

        for (uint i = 0; i < _segmentCount; i++)
        {
            StackSegment<T>* segment = &_segments[i];

            ulong itemsInThisSegment = (processedCount + segment->Capacity > _count) ? _count - processedCount : segment->Capacity;

            // 计算需要复制的数量，避免超出 count 的范围
            ulong itemsToCopy = Math.Min((ulong)count, itemsInThisSegment);

            var copySize = (ulong)sizeof(T) * itemsToCopy;

            // 在此段内复制
            Unsafe.CopyBlock(destination + currentDestinationIndex, segment->DataPtr, (uint)copySize);

            currentDestinationIndex += itemsToCopy;
            processedCount += itemsInThisSegment;
            count -= itemsToCopy; // 减少剩余要复制的数量

            if (processedCount >= _count || count <= 0) break;
        }

        return count == 0; // 检查是否所有请求的元素都已成功复制
    }


}
