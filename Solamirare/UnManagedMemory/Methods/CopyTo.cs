namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 把当前内容复制到指定的内存
    /// <para>以当前的 Length 作为复制长度，如果目标的长度大于当前对象的 Length，执行结果是局部覆盖</para>
    /// <para>如果目标长度不足以容纳当前值，则取当前的局部值去替换目标</para>
    /// </summary>
    /// <param name="distination"></param>
    /// <returns></returns>
    public bool CopyTo(UnManagedCollection<T>* distination)
    {
        fixed (UnManagedCollection<T>* p = &Prototype)
            return p->CopyTo(distination);
    }


    /// <summary>
    /// 把当前内容复制到指定的内存
    /// <para>以当前的 Length 作为复制长度，如果目标的长度大于当前对象的 Length，执行结果是局部覆盖</para>
    /// <para>如果目标长度不足以容纳当前值，则取当前的局部值去替换目标</para>
    /// </summary>
    /// <param name="distination"></param>
    /// <returns></returns>
    public bool CopyTo(ReadOnlySpan<T> distination)
    {
        fixed (UnManagedCollection<T>* p = &Prototype)
            return p->CopyTo(distination);
    }



    /// <summary>
    /// 把当前内容复制到指定的内存
    /// <para>以当前的 UsageSize 作为复制长度，如果目标的长度大于当前对象的 UsageSize，执行结果是局部覆盖</para>
    /// <para>如果目标长度不足以容纳当前值，则取当前的局部值去替换目标</para>
    /// </summary>
    /// <param name="pointer"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public bool CopyTo(void* pointer, uint length)
    {
        fixed (UnManagedCollection<T>* p = &Prototype)
            return p->CopyTo(pointer, length);
    }


}