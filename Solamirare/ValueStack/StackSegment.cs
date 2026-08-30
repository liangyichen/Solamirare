namespace Solamirare;



/// <summary>
/// 非托管栈分段，用于存储每个外部或内部内存块的元数据
/// </summary>
/// <typeparam name="T"></typeparam>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct StackSegment<T> where T : unmanaged
{
    /// <summary>
    /// 内存块指针
    /// </summary>
    public T* DataPtr;

    /// <summary>
    /// 该内存块的容量 (TCount)
    /// </summary>
    public uint Capacity;

    /// <summary>
    /// 该内存块在整个逻辑栈中的起始索引
    /// </summary>
    public uint StartIndex;
}
