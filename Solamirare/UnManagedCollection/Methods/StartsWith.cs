namespace Solamirare;


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{

    /// <summary>
    /// 是否以指定的序列开始
    /// <para>Determines whether the start of this instance matches the specified sequence.</para>
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool StartsWith(UnManagedCollection<T>* target)
    {
        // 将搜索范围限制在 source 的前缀部分，以避免 IndexOf 搜索整个 Span。
        return StartsWith(target->InternalPointer, target->Size);
    }


    /// <summary>
    /// 是否以指定的序列开始
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool StartsWith(UnManagedMemory<T>* target)
    {
        return StartsWith(target->Pointer, target->UsageSize);
    }

    /// <summary>
    /// 是否以指定的序列开始
    /// </summary>
    /// <param name="target"></param>
    /// <param name="Length"></param>
    /// <returns></returns>
    public bool StartsWith(T* target, uint Length)
    {

        if (target is null || InternalPointer is null)
            return false;

        // 长度检查：如果目标比源长，立即返回 false。
        if (Length > Size)
        {
            return false;
        }

        // 目标长度为 0 的特殊情况，这是 C# 规范，空值起始一定是 true
        if (Length == 0)
        {
            return true;
        }

        byte* p_source = (byte*)InternalPointer;

        byte targetFirstByte;

        targetFirstByte = ((byte*)target)[0];


        // 如果第一个字节不匹配，则整个参数都肯定不匹配
        if (p_source[0] != targetFirstByte)
        {
            return false;
        }


        // 如果长度相等，且第一个元素已匹配，则可以直接比较（且可能被 JIT 优化）
        if (Size == Length)
        {
            return Equals(target, Length);
        }

        int sizeOfT = sizeof(T); 

        int valueBytesLength = (int)Length * sizeOfT;

        int sourceBytesLength = (int)Size * sizeOfT;

        bool result = ValueTypeHelper.StartsWith(p_source, sourceBytesLength, (byte*)target, valueBytesLength);


        return result;
    }




    /// <summary>
    /// 是否以指定的序列开始
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool StartsWith(ReadOnlySpan<T> target)
    {
        bool result;

        if (target.IsEmpty) return true; //这是 C# 规范，空值起始一定是 true

        fixed (T* p = target)
        {
            result = StartsWith(p, (uint)target.Length);
        }

        return result;
    }


    /// <summary>
    /// 是否以指定元素起始
    /// <para>Determines whether the start of this instance matches the specified element.</para>
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool StartsWith(T* target)
    {
        if (Size < 1)
        {
            return false;
        }

        return IndexOf(target) == 0;
    }

    /// <summary>
    /// 是否以指定元素起始
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool StartsWith(in T target)
    {
        if (Size < 1)
        {
            return false;
        }

        return IndexOf(target) == 0;
    }

}