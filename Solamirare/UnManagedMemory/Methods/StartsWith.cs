
namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 是否以指定的序列开始。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool StartsWith(UnManagedCollection<T>* target)
    {
        fixed (UnManagedCollection<T>* p = &Prototype)

            return p->StartsWith(target->InternalPointer, target->Size);
    }


    /// <summary>
    /// 是否以指定的序列开始。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool StartsWith(UnManagedMemory<T>* target)
    {
        fixed (UnManagedCollection<T>* p = &Prototype)

            return p->StartsWith(target->Pointer, target->UsageSize);
    }

    /// <summary>
    /// 是否以指定的序列开始。
    /// </summary>
    /// <param name="target"></param>
    /// <param name="Length"></param>
    /// <returns></returns>
    public bool StartsWith(T* target, uint Length)
    {
        fixed (UnManagedCollection<T>* p = &Prototype)

            return p->StartsWith(target, Length);
    }



    /// <summary>
    /// 是否以指定的序列开始。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool StartsWith(ReadOnlySpan<T> target)
    {
        fixed (UnManagedCollection<T>* p = &Prototype)

            return p->StartsWith(target);
    }


    /// <summary>
    /// 是否以指定元素起始。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool StartsWith(T* target)
    {
        fixed (UnManagedCollection<T>* p = &Prototype)

            return p->StartsWith(target);
    }

    /// <summary>
    /// 是否以指定元素起始。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool StartsWith(in T target)
    {
        fixed (UnManagedCollection<T>* p = &Prototype)

            return p->StartsWith(target);
    }

}