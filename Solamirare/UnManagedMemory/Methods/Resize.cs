namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{



    /// <summary>
    /// <para>调整真实容量大小。</para>
    /// <para>仅限内部分配的堆内存模式及内存池模式使用，如果处于栈内存模式或外部内存模式会返回操作失败结果。</para>
    /// <para>可能有两种结果：</para>
    /// <para>1. 在现有地址处增量扩展，原始起始地址保持不变。</para>
    /// <para>2. 分配新的内存地址，将值从旧地址复制到新地址，并释放旧内存。指针的起始地址将更新为新的目标地址。</para>
    /// <para>结果取决于当前物理内存地址是否有连续的后续可用空间，无法通过编程控制。</para>
    /// <para>缩容操作在很大概率上能够保持原有地址，只不过尾部会受到裁剪。</para>
    /// </summary>
    /// <param name="TCount"></param>
    /// <returns></returns>
    public bool Resize(uint TCount)
    {
        if (OnStack || @readonly || isExternalMemory || TCount == capacity || !activated)
            return false;

        T* sourceMemory = Pointer;

        uint oldCapacity = capacity;

        nuint sizeofT = (nuint)sizeof(T);

        // 1. 安全检查：防止整数溢出
        ulong totalSize = (ulong)TCount * (ulong)sizeofT;
        if (totalSize > (ulong)nuint.MaxValue) return false;
        nuint newBytes = (nuint)totalSize;

        if (TCount == 0)
        {
            // 冷路径：清空数据但不释放物理内存
            if (sourceMemory != null && oldCapacity > 0)
                NativeMemory.Clear(sourceMemory, oldCapacity * sizeofT);

            ReLength(0);

            return true;
        }

        if (sourceMemory == null)
        {
            allocMemory(TCount, memoryPool); // 初始分配
            if (Pointer == null) return false; // 分配失败
        }
        else
        {

            if (onMemoryPool && memoryPool is not null)
            {
                // --- 内存池模式：必须手动搬运 ---
                // 内存池通常是预分配的大块，不支持 NativeMemory.Realloc

                // 备份状态以便回滚
                bool wasOnPool = onMemoryPool;

                bool allocSuccess = allocOnMemoryPool(TCount); // 内部更新 Pointer 到新地址

                if(!allocSuccess) // 内存池分配失败，改为堆分配
                {
                    onMemoryPool = false;
                    
                    allocOnHeap(TCount);
                }

                // 3. 安全性：检查分配是否成功
                if (Pointer != null)
                {
                    uint copyCount = TCount < oldCapacity ? TCount : oldCapacity;

                    NativeMemory.Copy(sourceMemory, Pointer, copyCount * sizeofT);

                    // 归还旧块给池
                    memoryPool->Return(sourceMemory, oldCapacity * (ulong)sizeofT);
                }
                else
                {
                    // 分配失败，恢复旧状态，返回 false
                    Pointer = sourceMemory;
                    onMemoryPool = wasOnPool;
                    return false;
                }
            }
            else
            {
                T* newPtr = (T*)NativeMemory.Realloc(sourceMemory, newBytes);

                if (newPtr == null) return false; // 分配失败

                // 2. 数据一致性：Realloc 不会清零扩展部分，需要手动清零以保持与 AllocZeroed 一致的行为
                if (TCount > oldCapacity)
                {
                    nuint offset = (nuint)oldCapacity * sizeofT;
                    nuint length = (nuint)(TCount - oldCapacity) * sizeofT;
                    NativeMemory.Clear((byte*)newPtr + offset, length);
                }

                Pointer = newPtr;
            }
        }

        // 状态统一更新
        memoryAllocated = true;

        disposed = false;

        capacity = TCount;

        if (UsageSize > TCount) ReLength(TCount);

        return true;
    }

}