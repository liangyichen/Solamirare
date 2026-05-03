using System.Runtime.CompilerServices;

namespace Solamirare;


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{


    /// <summary>
    /// 将集合中的所有元素按照真实容量长度执行清零。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (InternalPointer is not null && size > 0)
            NativeMemory.Clear(InternalPointer, (nuint)(Size * sizeof(T)));
    }

}