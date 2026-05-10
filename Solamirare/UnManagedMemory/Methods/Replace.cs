namespace Solamirare;

public unsafe partial struct UnManagedMemory<T> where T : unmanaged
{
    private const int StackAllocThreshold = 1024;

    /// <summary>
    /// 内容替换
    /// </summary>
    /// <param name="select"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public unsafe Replace_Result Replace(ReadOnlySpan<T> select, ReadOnlySpan<T> value)
    {
        Replace_Result result = new Replace_Result { Status = Replace_Result.Success_Code };

        if (!activated)
        {
            result.Status = Replace_Result.UnActivated;
            return result;
        }

        // 1. 硬防御：零 GC 项目通常运行在特定内存分配器上，禁止混合模式
        if (OnStack || isExternalMemory || @readonly)
        {
            result.Status = Replace_Result.Failed_FixedSize;
            return result;
        }

        if (IsEmpty || Pointer is null)
        {
            result.Status = Replace_Result.SourceIsEmptyOrNull;
            return result;
        }

        int selectLen = select.Length;
        int valueLen = value.Length;
        uint sourceUsage = UsageSize;

        if (selectLen == 0)
        {
            return result;
        }

        if (selectLen > sourceUsage)
        {
            result.Status = Replace_Result.LengthNotMatch;
            return result;
        }

        uint* offsetIndices = null;
        bool isOffsetsHeap = false;

        try
        {
            int sizeOfT = sizeof(T);

            // 3. 偏移量缓冲区优化分配
            // 使用 (Total / PatternLen) 这种紧凑预测，减少非托管内存占用
            long maxPossibleMatches = (sourceUsage / (uint)selectLen) + 1;
            long offsetIndicesByteLen = maxPossibleMatches * sizeof(uint);

            if (offsetIndicesByteLen <= StackAllocThreshold && maxPossibleMatches <= int.MaxValue)
            {
                byte* _offsetIndices = stackalloc byte[(int)offsetIndicesByteLen];
                offsetIndices = (uint*)_offsetIndices;
            }
            else
            {
                offsetIndices = (uint*)NativeMemory.AllocZeroed((nuint)offsetIndicesByteLen);
                isOffsetsHeap = true;
            }

            // --- 4. 极速探测阶段 (单核 SIMD 优化路径) ---
            uint matchCount = 0;
            T* basePtr = Pointer;
            uint currentPos = 0;

            while (currentPos <= sourceUsage - (uint)selectLen)
            {
                // 核心：利用内置 IndexOf 的 SIMD 加速（它是单核下的最快实现）
                int remainingLen = (int)(sourceUsage - currentPos);
                int foundIdx = MemoryExtensions.IndexOf(
                    new ReadOnlySpan<T>(basePtr + currentPos, remainingLen),
                    select);

                if (foundIdx < 0) break;

                uint absoluteOffset = currentPos + (uint)foundIdx;
                offsetIndices[matchCount++] = absoluteOffset;

                // 跳过已匹配部分
                currentPos = absoluteOffset + (uint)selectLen;
            }

            if (matchCount == 0)
            {
                result.Status = Replace_Result.NotFound;
                return result;
            }

            // --- 5. 执行替换 ---
            long totalDiff = (long)matchCount * (valueLen - selectLen);
            uint finalLength = (uint)((long)sourceUsage + totalDiff);

            if (valueLen <= selectLen)
            {
                ExecuteInPlaceReplaceByOffsets(offsetIndices, matchCount, value, selectLen, finalLength);
            }
            else
            {
                // 扩容替换
                if (!ExecuteAllocReplaceByOffsets(offsetIndices, matchCount, value, selectLen, finalLength))
                    result.Status = Replace_Result.Failed_StackResize;
            }

            return result;
        }
        finally
        {
            if (isOffsetsHeap && offsetIndices != null) NativeMemory.Free(offsetIndices);
        }
    }

    private unsafe void ExecuteInPlaceReplaceByOffsets(uint* offsets, uint matchCount, ReadOnlySpan<T> value, int selectLen, uint finalLength)
    {
        T* basePtr = Pointer;
        int valueLen = value.Length;
        nuint valueByteLen = (nuint)valueLen * (nuint)sizeof(T);

        fixed (T* pValue = value)
        {
            // 优化路径：等长替换，完全避免内存移动，仅做随机写
            if (valueLen == selectLen)
            {
                for (uint i = 0; i < matchCount; i++)
                    NativeMemory.Copy(pValue, basePtr + offsets[i], valueByteLen);
            }
            else
            {
                // 缩容替换：必须移动数据，但通过 offsets 批量化
                uint writeOffset = 0;
                uint lastReadOffset = 0;
                nuint sizeofT = (nuint)sizeof(T);

                for (uint i = 0; i < matchCount; i++)
                {
                    uint currentMatchOffset = offsets[i];
                    uint gapLen = currentMatchOffset - lastReadOffset;

                    if (gapLen > 0)
                    {
                        NativeMemory.Copy(basePtr + lastReadOffset, basePtr + writeOffset, (nuint)gapLen * sizeofT);
                        writeOffset += gapLen;
                    }

                    if (valueByteLen > 0)
                    {
                        NativeMemory.Copy(pValue, basePtr + writeOffset, valueByteLen);
                        writeOffset += (uint)valueLen;
                    }
                    lastReadOffset = currentMatchOffset + (uint)selectLen;
                }

                uint tailLen = UsageSize - lastReadOffset;
                if (tailLen > 0)
                    NativeMemory.Copy(basePtr + lastReadOffset, basePtr + writeOffset, (nuint)tailLen * sizeofT);
            }
        }
        ReLength(finalLength);
    }

    /// <summary>
    /// 策略 B：利用记录好的偏移量，一次性组装新块
    /// <para>Strategy B: Assemble a new block at once using recorded offsets.</para>
    /// </summary>
    private unsafe bool ExecuteAllocReplaceByOffsets(uint* offsets, uint matchCount, ReadOnlySpan<T> value, int selectLen, uint finalLength)
    {
        T* newBlock;
        int sizeOfT = sizeof(T);
        nuint totalElementsSize = (nuint)finalLength * (nuint)sizeOfT;

        if (!onMemoryPool)
        {
            // 使用 Alloc 而非 AllocZeroed，因为后续逻辑会完全覆盖内存
            newBlock = (T*)NativeMemory.AllocZeroed(totalElementsSize);
        }
        else
        {
            // 内存池模式
            // 移除手动对齐逻辑，防止 Pointer 偏移导致后续 Free/Return 失败
            // 假设 PoolCluster 返回的内存已满足基本对齐要求
            byte* ptr = (byte*)NativeMemory.AllocZeroed(totalElementsSize);
            if (ptr == null) return false;
            newBlock = (T*)ptr;
        }

        if (newBlock == null) return false;

        // --- 执行搬运逻辑 (顺序写入) ---
        T* oldBlock = Pointer;
        uint writeOffset = 0;
        uint lastReadOffset = 0;
        nuint valueByteLen = (nuint)(value.Length * sizeOfT);

        fixed (T* pValue = value)
        {
            for (uint i = 0; i < matchCount; i++)
            {
                uint currentMatchOffset = offsets[i];
                uint gapLen = currentMatchOffset - lastReadOffset;

                if (gapLen > 0)
                {
                    // 对齐后的地址使得 NativeMemory.Copy 能最大化利用 SIMD 吞吐
                    NativeMemory.Copy(oldBlock + lastReadOffset, newBlock + writeOffset, (nuint)(gapLen * sizeOfT));
                    writeOffset += gapLen;
                }

                if (valueByteLen > 0)
                {
                    NativeMemory.Copy(pValue, newBlock + writeOffset, valueByteLen);
                    writeOffset += (uint)value.Length;
                }
                lastReadOffset = currentMatchOffset + (uint)selectLen;
            }
        }

        uint tailLen = UsageSize - lastReadOffset;
        if (tailLen > 0)
            NativeMemory.Copy(oldBlock + lastReadOffset, newBlock + writeOffset, (nuint)(tailLen * sizeOfT));

        // --- 更新状态与清理 ---
        uint _old_capacity = capacity;
        T* oldBlockPtr = Pointer;

        // 注意：这里需要考虑如何存储 allocatedPtr 以便后续释放
        // 如果你的架构中 Pointer 必须是 newBlock，
        // 则需要 Prototype 记录原始分配地址 (allocatedPtr) 用于 Return
        Pointer = newBlock;
        capacity = finalLength;

        if (!onMemoryPool)
        {
            if (oldBlockPtr != null && _old_capacity > 0)
                NativeMemory.Free(oldBlockPtr);
        }
        else
        {
            // ⚠️ 内存池释放必须使用原始指针
            if (oldBlockPtr != null && _old_capacity > 0)
            {
                // 这里假设你的内存池管理会自动处理原始指针寻找，
                // 否则你需要在实例中增加一个字段存储 InternalAllocatedPtr
                NativeMemory.Free(oldBlockPtr);
            }
        }

        ReLength(finalLength);
        return true;
    }
}