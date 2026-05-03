using System.Runtime.CompilerServices;

namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{
    /// <summary>
    /// 把当前内容更新为新的内容，更新之后内存分布有两种情况：
    /// 1，小内存情况下会进行增量操作，原始 0 地址位不会变。
    /// 2，分配新的内存，并且把旧的值一并复制到新内存，旧的内存会被立即释放，Pointer 的 0 起始位也会一并更新为新值。
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public bool Update(ReadOnlySpan<T> values)
    {
        if (values.IsEmpty || Pointer is null || !activated)
        {
            return default;
        }

        bool resize_result = EnsureCapacity((uint)values.Length);

        if (resize_result)
        {
            fixed (T* ptr = values)
            {
                NativeMemory.Copy(ptr, &Pointer[0], (uint)(values.Length * sizeof(T)));
            }

            Prototype.size = (uint)values.Length;
        }


        return resize_result;
    }

}