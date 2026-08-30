using System.Runtime.CompilerServices;

namespace Solamirare;


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{

    /// <summary>
    /// 根据下标获取元素的指针。此方法功能等同于索引器，主要用于当对象作为指针时无法使用索引器的场景。
    /// <para>Gets the pointer to the element at the specified index. This method is functionally equivalent to the indexer, mainly used in scenarios where the indexer cannot be used when the object is a pointer.</para>
    /// </summary>
    /// <param name="index">元素的从零开始的索引。<para>The zero-based index of the element.</para></param>
    /// <returns>指向指定索引处元素的指针；如果索引越界，则返回 null。<para>A pointer to the element at the specified index; returns null if the index is out of bounds.</para></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T* Index(int index)
    {

        if (InternalPointer is null || index < 0 || index >= Size)
        {
            return null;
        }
        else
        {
            return &InternalPointer[index];
        }
    }

    /// <summary>
    /// 根据下标获取元素
    /// <para>Gets the element at the specified index.</para>
    /// <para>0 下标既是内部指针的起始位置</para>
    /// <para>Index 0 is the starting position of the internal pointer.</para>
    /// <para>越界访问的结果是null</para>
    /// <para>Out of bounds access results in null.</para>
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public T* this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (InternalPointer is null || index < 0 || index >= Size)
            {
                return null;
            }
            else
            {
                return &InternalPointer[index];
            }
        }
    }


}