namespace Solamirare;


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{



    /// <summary>
    /// 判断当前集合是否以指定的非托管序列结尾。
    /// </summary>
    /// <param name="target">目标序列指针。</param>
    /// <param name="Length">目标序列元素数量。</param>
    /// <returns>若当前集合以目标序列结尾则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public bool EndsWith(T* target, uint Length)
    {

        int sourceBytesLen = (int)Size * sizeof(T);
        int valueBytesLen = (int)Length * sizeof(T);


        bool ends = ValueTypeHelper.EndsWith((byte*)InternalPointer, sourceBytesLen, (byte*)target, valueBytesLen);

        return ends;
    }

    /// <summary>
    /// 判断当前集合是否以指定的非托管内存结尾。
    /// </summary>
    /// <param name="target">目标内存指针。</param>
    /// <returns>若当前集合以目标内容结尾则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public bool EndsWith(UnManagedMemory<T>* target)
    {
        if(target is null) return false;

        return EndsWith(target->Prototype);
    }

    /// <summary>
    /// 判断当前集合是否以指定的非托管内存结尾。
    /// </summary>
    /// <param name="target">目标内存。</param>
    /// <returns>若当前集合以目标内容结尾则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public bool EndsWith(UnManagedMemory<T> target)
    {
        return EndsWith(target.Prototype);
    }

    /// <summary>
    /// 判断当前集合是否以指定的集合结尾。
    /// </summary>
    /// <param name="target">目标集合指针。</param>
    /// <returns>若当前集合以目标内容结尾则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public bool EndsWith(UnManagedCollection<T>* target)
    {
        if(target is null) return false;
        
        return EndsWith(target->InternalPointer, target->Size);
    }

    /// <summary>
    /// 判断当前集合是否以指定的集合结尾。
    /// </summary>
    /// <param name="target">目标集合。</param>
    /// <returns>若当前集合以目标内容结尾则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public bool EndsWith(UnManagedCollection<T> target)
    {
        
        bool ends = EndsWith(target.InternalPointer, target.Size);

        return ends;
    }

    /// <summary>
    /// 判断当前集合是否以指定的只读跨度结尾。
    /// </summary>
    /// <param name="target">目标只读跨度。</param>
    /// <returns>若当前集合以目标内容结尾则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public bool EndsWith(ReadOnlySpan<T> target)
    {
        fixed(T* p = target)
        {
            return EndsWith(p, (uint)target.Length);
        }
    }

}
