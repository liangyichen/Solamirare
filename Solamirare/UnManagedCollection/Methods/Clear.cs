using System.Runtime.CompilerServices;

namespace Solamirare;


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{


    /// <summary>
    /// 将集合中的所有元素按照当前长度执行内容清零。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (InternalPointer is not null && size > 0)
            NativeMemory.Clear(InternalPointer, (nuint)(size * sizeof(T)));
    }

}