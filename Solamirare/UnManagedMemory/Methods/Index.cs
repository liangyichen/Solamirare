using System.Runtime.CompilerServices;

namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 根据下标获取元素，该方法的唯一目的是为解决指针查询时无法使用索引器而创建，功能等同于对象模式的索引器。
    /// <para>如果当前为对象模式，使用该方法与使用索引器没有任何区别，如果当前为指针模式，只能使用该方法根据下标获取元素（因为对指针使用索引器是另一种操作）。</para>
    /// <para>越界访问的结果是null。</para>
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public T* Index(int index)
    {
        //禁止越界
        if (index < 0 || (uint)index >= Capacity || !activated) return null;

        if (!onMemoryPool)
        {
            fixed (UnManagedCollection<T>* p = &Prototype)

                return p->Index(index);
        }
        else
        {
            T* result = Pointer + index;

            return result;
        }
    }


    /// <summary>
    /// 根据下标获取元素。
    /// <para>越界访问的结果是null。</para>
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public T* this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return Index(index);
        }
    }

}