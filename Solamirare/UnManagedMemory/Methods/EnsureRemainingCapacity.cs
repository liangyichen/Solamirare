namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 保证剩余可用空间具备至少 capacity 的长度。
    /// </summary>
    /// <param name="capacity">保证具备该长度。</param>
    /// <param name="mode"></param>
    /// <returns></returns>
    public bool EnsureRemainingCapacity(uint capacity, MemoryScaleMode mode = MemoryScaleMode.X3)
    {
        if (@readonly || !activated) return false;

        bool _promise;

        uint currentLength = Capacity - UsageSize; //尚未使用的部分长度

        if (currentLength >= capacity)
        {
            _promise = true;
        }
        else
        {
            uint x = capacity - currentLength; //还差这么多

            uint y = Capacity + currentLength + x; //至少需要保证 _realSize 的总长度是这么多

            _promise = EnsureCapacity(y, mode);
        }

        return _promise;

    }
}