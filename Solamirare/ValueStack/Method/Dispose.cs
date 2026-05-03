namespace Solamirare;

public unsafe partial struct ValueStack<T>
where T : unmanaged
{



    /// <summary>
    /// 销毁所有数据，下标与容量都清零
    /// <para>自承载模式将把内存全部重置为零，并且释放内存，所有内部状态清零</para>
    /// <para>外部内存模式将把内存全部重置为零，所有内部状态清零</para>
    /// </summary>
    public void Dispose(bool @lock = true)
    {
        if (Interlocked.Exchange(ref _disposedFlag, 1) == 1)
        {
            // 线程 B 立即发现已被 Dispose，马上返回。
            return;
        }

        if (@lock) AcquireSpinlock();

        if (_segments != null)
        {

            _clear();

            // 2. 释放分段数据 (仅在内部模式下，因为外部数据归外部所有)
            if (!isExternalMemory)
            {
                for (ulong i = 0; i < _segmentCount; i++)
                {
                    StackSegment<T>* segment = _segments + i;

                    if (segment->DataPtr != null)
                    {

                        NativeMemory.Free(segment->DataPtr);

                    }
                }
            }

            // 3. 释放分段表本身 (栈拥有分段表的内存，无论内部/外部模式)

            NativeMemory.Free(_segments);

        }

        // 4. 重置所有状态
        _segments = null;
        _totalCapacity = 0;
        _segmentCount = 0;
        _segmentsTableCapacity = 0;
        _count = 0;
        isExternalMemory = false;

        // _spinlock = 0; // 即使在 Dispose 中，也需要通过 Exchange 释放。
        // 但由于 Dispose 是最后操作，且我们将所有状态清零，直接设置 _spinlock = 0 是可以的。
        // 为了严格遵守 lock/unlock，我们使用 ReleaseSpinlock。

        if (@lock) ReleaseSpinlock(); // 释放锁
    }

}