using System.Runtime.CompilerServices;

namespace Solamirare;


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{


    /// <summary>
    /// 将集合中的所有元素清零，并将已用大小设置为0。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Zero()
    {
        if (InternalPointer is not null && Size > 0)
        {
            NativeMemory.Clear(InternalPointer, (nuint)(Size * sizeof(T)));

            size = 0;
        }
    }

}