namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 包含指定判断值的元素个数
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int Count(in T target)
    {
        if (!activated) return 0;

        fixed(T* pTarget = &target)
        return ValueTypeHelper.Count(Pointer, UsageSize, pTarget, 1);
    }


    /// <summary>
    /// 计算指定模式在非托管内存中出现的次数
    /// </summary>
    /// <param name="target">要匹配的模式</param>
    /// <returns>模式出现的次数</returns>
    public int Count(ReadOnlySpan<T> target)
    {
        if (!activated) return 0;

        Span<T> selfSpan = AsSpan();
        
        fixed (T* pSource = target)
        {
            return ValueTypeHelper.Count(Pointer, UsageSize, pSource, (uint)target.Length);
        }
    }


    /// <summary>
    /// 计算指定模式在非托管内存中出现的次数
    /// </summary>
    /// <param name="target">要匹配的模式</param>
    /// <returns>模式出现的次数</returns>
    public int Count(in UnManagedMemory<T> target)
    {
        if (!activated || !target.activated) return 0;
        
        return ValueTypeHelper.Count(Pointer, UsageSize, target.Pointer, target.UsageSize);
    }


    /// <summary>
    /// 计算指定模式在非托管内存中出现的次数
    /// </summary>
    /// <param name="target">要匹配的模式</param>
    /// <returns>模式出现的次数</returns>
    public int Count(UnManagedMemory<T>* target)
    {
        if (!activated || target is null || !target->activated) return 0;
        
        return ValueTypeHelper.Count(Pointer, UsageSize, target->Pointer, target->UsageSize);
    }

    /// <summary>
    /// 计算指定模式在非托管内存中出现的次数
    /// </summary>
    /// <param name="target">要匹配的模式</param>
    /// <returns>模式出现的次数</returns>
    public int Count(UnManagedCollection<T>* target)
    {
        if (!activated || target is null) return 0;
        
        return ValueTypeHelper.Count(Pointer, UsageSize, target->InternalPointer, target->Size);
    }


    /// <summary>
    /// 计算指定模式在非托管内存中出现的次数
    /// </summary>
    /// <param name="target">要匹配的模式</param>
    /// <returns>模式出现的次数</returns>
    public int Count(in UnManagedCollection<T> target)
    {
        if (!activated) return 0;
        
        return ValueTypeHelper.Count(Pointer, UsageSize, target.InternalPointer, target.Size);
    }



}