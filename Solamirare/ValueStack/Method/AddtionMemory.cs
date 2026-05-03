namespace Solamirare;

public unsafe partial struct ValueStack<T>
where T : unmanaged
{


    /// <summary>
    /// 在外部内存模式下，通过附加额外的内存块来扩充栈的容量。
    /// <para>只有在 isExternalMemory 为 true 时有效。</para>
    /// </summary>
    /// <param name="externalMemory">新的外部内存块指针。</param>
    /// <param name="capacity">新内存块的容量。</param>
    /// <param name="lock">是否进行线程锁</param>
    /// <returns>如果附加成功返回 true；如果不是外部内存模式或附加失败返回 false。</returns>
    public bool AddtionMemory(T* externalMemory, uint capacity, bool @lock = true)
    {

        if (capacity == 0 || externalMemory == null) return false;

        if (@lock) AcquireSpinlock(); // 加锁，保护分段表结构体的修改

        if (!isExternalMemory)
        {
            if (@lock) ReleaseSpinlock();

            return false;
        }

        // 分段表可能需要初始化 (如果 Ctor(0, null) 导致状态不完整)
        if (_segments == null)
        {
            if (!ResizeSegmentsTable(INITIAL_SEGMENTS_CAPACITY))
            {
                if (@lock) ReleaseSpinlock();

                return false;
            }
        }

        bool success = AttachSegment(externalMemory, capacity);

        if (@lock) ReleaseSpinlock(); // 释放锁

        return success;
    }


}