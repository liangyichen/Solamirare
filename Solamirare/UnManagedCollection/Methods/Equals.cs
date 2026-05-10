namespace Solamirare;

// Equals 涉及到 Object.Equals 的重写， 不可以做虚表与扩展方法
// 调用者使用 obj.Equals 的时候不会自动识别扩展方法，全都会被引导到 Object.Equals


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{
    /// <summary>
    /// 与指定的内存段比较，值是否相等
    /// <para>Compares with the specified memory segment to see if the values are equal.</para>
    /// </summary>
    /// <param name="target"></param>
    /// <param name="targetLength"></param>
    /// <returns></returns>
    public bool Equals(T* target, uint targetLength)
    {
        return ValueTypeHelper.Equals(InternalPointer, Size, target, targetLength);
    }


    #region override
    //==============


    /// <summary>
    /// 禁止使用，防止误引发 GC
    /// <para>Do not use, to prevent accidental GC triggering.</para>
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    [Obsolete("UnSupport", true)]
    public override bool Equals(object obj)
    {
        //throw new Exception("Do not use Object.Equals(object obj).");
        return false;
    }



    /// <summary>
    /// 判断指针指向的所有元素值匹配
    /// <para>Determines whether all element values pointed to by the pointer match.</para>
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Equals(UnManagedCollection<T>* target)
    {
        if (target is null) return false;

        return Equals(target->InternalPointer, target->Size);
    }


    /// <summary>
    /// 判断指针指向的所有元素值匹配
    /// <para>Determines whether all element values pointed to by the pointer match.</para>
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Equals(in UnManagedCollection<T> target)
    {
        return Equals(target.InternalPointer, target.Size);
    }


    /// <summary>
    /// 判断指针指向的所有元素值匹配
    /// <para>Determines whether all element values pointed to by the pointer match.</para>
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Equals(in ValueLinkedList<T> target)
    {
        if (target.NodesCount != Size || InternalPointer is null)
        {
            return false;
        }

        //链表的内存分布不连续，只能遍历比较
        for (int i = 0; i < Size; i++)
        {
            T _this = InternalPointer[i];
            T* p_target = target.Index(i);


            if (!EqualityComparer<T>.Default.Equals(_this, *p_target))
            {
                return false;
            }
        }

        return true;
    }



    /// <summary>
    /// 判断指针指向的所有元素值匹配
    /// <para>Determines whether all element values pointed to by the pointer match.</para>
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Equals(ReadOnlySpan<T> target)
    {
        //长度是否匹配是判断相等的首要检测因素
        if (target.Length != Size)
            return false;

        bool result;

        fixed (T* p = target)
        {
            result = Equals(p, (uint)target.Length);
        }

        return result;
    }



    #endregion
}