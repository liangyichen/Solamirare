namespace Solamirare;


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{


    /// <summary>
    /// 把当前内容复制到指定的内存
    /// <para>Copies the current content to the specified memory.</para>
    /// <para>以当前的 UsageSize 作为复制长度，如果目标的长度大于当前对象的 UsageSize，执行结果是局部覆盖</para>
    /// <para>Uses the current UsageSize as the copy length. If the target length is greater than the current object's UsageSize, the result is a partial overwrite.</para>
    /// <para>如果目标长度不足以容纳当前值，则取当前的局部值去替换目标</para>
    /// <para>If the target length is insufficient to hold the current value, the current local value is used to replace the target.</para>
    /// </summary>
    /// <param name="destination"></param>
    /// <returns></returns>
    public bool CopyTo(UnManagedCollection<T>* destination)
    {
        if (destination is null)
            return false;

        if (destination->IsEmpty)
            return false;

        return CopyTo(destination->InternalPointer, destination->Size);
    }



    /// <summary>
    /// 把当前内容复制到指定的内存
    /// <para>Copies the current content to the specified memory.</para>
    /// <para>以当前的 UsageSize 作为复制长度，如果目标的长度大于当前对象的 UsageSize，执行结果是局部覆盖</para>
    /// <para>Uses the current UsageSize as the copy length. If the target length is greater than the current object's UsageSize, the result is a partial overwrite.</para>
    /// <para>如果目标长度不足以容纳当前值，则取当前的局部值去替换目标</para>
    /// <para>If the target length is insufficient to hold the current value, the current local value is used to replace the target.</para>
    /// </summary>
    /// <param name="destination"></param>
    /// <returns></returns>
    public bool CopyTo(UnManagedMemory<T>* destination)
    {
        if (destination is null)
            return false;

        Span<T> span = destination->AsSpan();

        if (span.IsEmpty)
            return false;

        return CopyTo(destination->Pointer, destination->UsageSize);
    }



    /// <summary>
    /// 把当前内容复制到指定的内存
    /// <para>Copies the current content to the specified memory.</para>
    /// <para>以当前的 UsageSize 作为复制长度，如果目标的长度大于当前对象的 UsageSize，执行结果是局部覆盖</para>
    /// <para>Uses the current UsageSize as the copy length. If the target length is greater than the current object's UsageSize, the result is a partial overwrite.</para>
    /// <para>如果目标长度不足以容纳当前值，则取当前的局部值去替换目标</para>
    /// <para>If the target length is insufficient to hold the current value, the current local value is used to replace the target.</para>
    /// </summary>
    /// <param name="destination"></param>
    /// <returns></returns>
    public bool CopyTo(ReadOnlySpan<T> destination)
    {
        bool result;

        if (destination.Length > 0) //<--- 防止负数导致 uint 出现无限大的值
        {
            fixed (T* distinationMemory = destination)
            {
                result = CopyTo(distinationMemory, (uint)destination.Length);
            }
        }
        else
        {
            result = false;
        }

        return result;
    }

    /// <summary>
    /// 把当前内容复制到指定的内存
    /// <para>Copies the current content to the specified memory.</para>
    /// <para>以当前的 UsageSize 作为复制长度，如果目标的长度大于当前对象的 UsageSize，执行结果是局部覆盖</para>
    /// <para>Uses the current UsageSize as the copy length. If the target length is greater than the current object's UsageSize, the result is a partial overwrite.</para>
    /// <para>如果目标长度不足以容纳当前值，则取当前的局部值去替换目标</para>
    /// <para>If the target length is insufficient to hold the current value, the current local value is used to replace the target.</para>
    /// </summary>
    /// <param name="pointer"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public bool CopyTo(void* pointer, uint length)
    {
        if (pointer is not null && length > 0)
        {
            if (Size > 0 && Size > 0 && InternalPointer is not null)
            {
                //目标长度大于等于当前的值
                if (length >= Size)
                {
                    NativeMemory.Copy(InternalPointer, pointer, (uint)(Size * sizeof(T)));
                }
                else //目标长度不足以容纳当前值，则取当前的局部值去替换目标
                {
                    NativeMemory.Copy(InternalPointer, pointer, (uint)(length * sizeof(T)));
                }

                return true;
            }

            return false;
        }
        else
        {
            return false;
        }
    }


}