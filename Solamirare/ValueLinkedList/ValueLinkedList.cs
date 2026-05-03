namespace Solamirare;


/// <summary>
/// 值类型链表
/// </summary>
/// <typeparam name="T"></typeparam>
[Guid(SolamirareEnvironment.ValueLinkedListGuid)]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8, Size = 64)]
public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{
    ValueLiskedListNode<T>* head;

    ValueLiskedListNode<T>* _freeNodesHead;  // 空闲节点池的头指针

    ValueLiskedListNode<T>* tail;

    //用于追踪预分配内存块的起始地址
    ValueLiskedListNode<T>* _cacheBlockHead;


    uint _nodesCount;

    uint _freeNodesCount;



    /// <summary>
    /// 空闲节点的数量
    /// </summary>
    public uint FreeNodesCount => _freeNodesCount;


    /// <summary>
    /// 正在使用的节点的数量
    /// </summary>
    public uint NodesCount => _nodesCount;


    /// <summary>
    /// 获取第一个元素
    /// </summary>
    /// <returns></returns>
    public T* First => head != null ? head->Value : null;


    /// <summary>
    /// 获取最后一个元素
    /// </summary>
    /// <returns></returns>
    public T* Last => tail != null ? tail->Value : null;



    /// <summary>
    /// 是否空集合
    /// </summary>
    public bool IsEmpty => _nodesCount == 0;



    /// <summary>
    /// 值类型链表,无预分配节点
    /// </summary>
    public ValueLinkedList()
    {
        _nodesCount = 0;
        _freeNodesHead = null;
        _freeNodesCount = 0;
        _cacheBlockHead = null; // 初始化新字段

    }




    /// <summary>
    /// 值类型链表,预先分配节点
    /// </summary>
    /// <param name="TCount">预先分配节点数量，用于优化效率，并不构成数量限制</param>
    /// <param name="onMemoryPool"></param>
    public ValueLinkedList(uint TCount, bool onMemoryPool = false) : this()
    {
        AdditionMemory(TCount);
    }


    /// <summary>
    /// 值类型链表，节点由外部指定内存地址， 外部必须确保 nodeValuesMemory 的实际数量与 TCount 相等
    /// </summary>
    /// <param name="nodeValuesMemory">外部内存，存储值</param>
    /// <param name="TCount">外部内存 nodeValuesMemory 可以容纳的 T 数量</param>
    /// <param name="nodeValueIsEmpty">nodeValuesMemory 内存段是否是作为预备空间？true: 成为预留池的一部分（引用链接），留待将来添加新值。false: 外部内存段本身已经是新值，现在就成为新节点，从末端加入链接（引用链接）。</param>
    public ValueLinkedList(T* nodeValuesMemory, uint TCount, bool nodeValueIsEmpty) : this()
    {
        AppendReferences(nodeValuesMemory, TCount, nodeValueIsEmpty);
    }


    /// <summary>
    /// 值类型链表，节点与节点的值都由外部指定内存地址， 外部必须确保 nodeValuesMemory 和 nodesMemory 的实际数量一致， 并且都与 TCount 相等
    /// </summary>
    /// <param name="nodeValuesMemory">外部内存，存储值</param>
    /// <param name="nodesMemory">外部内存，存储链表节点</param>
    /// <param name="TCount">同时等同于 nodesMemory 和 nodeValuesMemory 的长度</param>
    /// <param name="nodeValueIsEmpty">nodeValuesMemory 内存段是否是作为预备空间？true: 成为预留池的一部分（引用链接），留待将来添加新值。false: 外部内存段本身已经是新值，现在就成为新节点，从末端加入链接（引用链接）。</param>
    public ValueLinkedList(T* nodeValuesMemory, ValueLiskedListNode<T>* nodesMemory, uint TCount, bool nodeValueIsEmpty) : this()
    {
        AppendReferences(nodeValuesMemory, nodesMemory, TCount, nodeValueIsEmpty);
    }



    /// <summary>
    /// 在堆上创建新节点
    /// </summary>
    /// <returns></returns>
    ValueLiskedListNode<T>* createNode_on_heap()
    {
        ValueLiskedListNode<T>* node;

        nuint tSize = (nuint)sizeof(ValueLiskedListNode<T>);


        node = (ValueLiskedListNode<T>*)NativeMemory.AllocZeroed(tSize);
        

        // 检查 NativeMemory.AllocZeroed 是否失败
        if (node == null)
        {
            return null;
        }

        node->Next = null;
        node->isLocalNode = true;

        return node;
    }


}