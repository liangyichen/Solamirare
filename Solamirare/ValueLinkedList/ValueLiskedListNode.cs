using System.Runtime.CompilerServices;

namespace Solamirare;


/// <summary>
/// 值类型链表节点
/// </summary>
/// <typeparam name="T"></typeparam>
[SkipLocalsInit]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8, Size = 32)]
public unsafe ref struct ValueLiskedListNode<T>
where T : unmanaged
{
    /// <summary>
    /// 当前值
    /// </summary>
    public T* Value;

    /// <summary>
    /// 下一个节点
    /// </summary>
    public ValueLiskedListNode<T>* Next;

    /// <summary>
    /// 当前 Value 是否指向由本地创建的内存段（由 NativeMemory.Alloc 把外部数据在本地克隆的独立副本）， true 可以理解为存储值，false 可以理解为存储引用
    /// </summary>
    public bool isLocalValue;

    /// <summary>
    /// 当前节点本身是否由对象内部分配， 如果是，则节点本身的释放由对象的 Dispose 方法负责， 如果否，则释放的过程由外部自行处理
    /// </summary>
    public bool isLocalNode;
}
