using System.Runtime.CompilerServices;

namespace Solamirare;


public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 将当前 UnManagedMemory 转换为另一个类型的 UnManagedMemory，实现内存段的快速重用。容量会重新设置为可以使用的最大容量，使用长度设为0
    /// <para>注意，为了保证性能最大化，在执行转换前内存不会清零。</para>
    /// </summary>
    /// <typeparam name="T2"></typeparam>
    /// <returns></returns>
    public UnManagedMemory<T2> Transformance<T2>()
    where T2 : unmanaged
    {

        if (Pointer is null || Capacity == 0 || !activated)
        {
            return UnManagedMemory<T2>.Empty;
        }

        int sizeOfT = sizeof(T);

        int sizeOfT2 = sizeof(T2);

        if (sizeOfT == 0 || sizeOfT2 == 0)
        {
            return UnManagedMemory<T2>.Empty;
        }

        if (sizeOfT == sizeOfT2)
        {
            return new UnManagedMemory<T2>((T2*)Pointer, Capacity, 0);
        }

        long TBytes = sizeOfT * Capacity;

        long T2Bytes = TBytes / sizeOfT2;

        uint new_size = (uint)T2Bytes;

        if (new_size < 1)
        {
            return UnManagedMemory<T2>.Empty;
        }

        UnManagedMemory<T2> result = new UnManagedMemory<T2>((T2*)Pointer, new_size, 0);

        fixed (void* self = &this)
        {

            NativeMemory.Copy((byte*)self, &result, (nuint)sizeof(UnManagedMemory<T2>));

            result.capacity = new_size;

            result.ReLength(0);

            if (typeof(T) != typeof(T2))
                result.initTypeCode();
        }

        return result;
    }

}