namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{


    /// <summary>
    /// 添加元素引用
    /// </summary>
    /// <param name="value"></param>
    public void AppendReferences(in T value)
    {
        fixed (T* p_value = &value)
        {
            AppendReferences(p_value, 1, false);
        }
    }


    /// <summary>
    /// 从非托管集合添加元素引用
    /// </summary>
    /// <param name="source"></param>
    public void AppendReferences(UnManagedMemory<T>* source)
    {
        if (source is null || source->IsEmpty || source->Pointer is null) return;

        for (int i = 0; i < source->UsageSize; i++)
            AppendReferences(&source->Pointer[i], 1, false);

    }


    /// <summary>
    /// 从非托管集合添加元素引用
    /// </summary>
    /// <param name="source"></param>
    public void AppendReferences(ReadOnlySpan<T> source)
    {
        if (source.IsEmpty) return;

        fixed (T* p_source = source)
        {
            for (int i = 0; i < source.Length; i++)
                AppendReferences(&p_source[i], 1, false);
        }
    }


    /// <summary>
    /// 添加元素引用，节点由外部指定内存地址， 外部必须确保 nodeValuesMemory 的实际数量与 TCount 相等
    /// </summary>
    /// <param name="nodeValuesMemory">外部内存，存储值</param>
    /// <param name="TCount">外部内存 nodeValuesMemory 可以容纳的 T 数量</param>
    /// <param name="nodeValueIsEmpty">nodeValuesMemory 内存段是否是作为预备空间？true: 成为预留池的一部分（引用链接），留待将来添加新值。false: 外部内存段本身已经是新值，现在就成为新节点，从末端加入链接（引用链接）。</param>
    public bool AppendReferences(T* nodeValuesMemory, uint TCount, bool nodeValueIsEmpty)
    {

        if (nodeValuesMemory is null || TCount == 0)
            return false;


        for (int i = 0; i < TCount; i++)
        {
            ValueLiskedListNode<T>* newNode = createNode_on_heap();

            if (newNode is null) return false; // 内存分配失败

            newNode->isLocalValue = false; // 值是引用，正确

            newNode->Value = nodeValuesMemory + i;

            if (nodeValueIsEmpty)
                SetAsFree(newNode);
            else
                LinkToLocalNode(newNode);
        }


        return true;
    }


    /// <summary>
    /// 添加元素引用，节点与节点的值都由外部指定内存地址， 外部必须确保 nodeValuesMemory 和 nodesMemory 的实际数量一致， 并且都与 TCount 相等
    /// </summary>
    /// <param name="nodesValueMemory">外部内存，存储节点的值</param>
    /// <param name="nodesMemory">外部内存，存储链表节点</param>
    /// <param name="nodesCount">同时等同于 nodesMemory 和 nodeValuesMemory 的长度</param>
    /// <param name="nodeValueIsEmpty">nodeValuesMemory 内存段是否是作为预备空间？true: 成为预留池的一部分（引用链接），留待将来添加新值。false: 外部内存段本身已经是新值，现在就成为新节点，从末端加入链接（引用链接）。</param>
    public bool AppendReferences(T* nodesValueMemory, ValueLiskedListNode<T>* nodesMemory, uint nodesCount, bool nodeValueIsEmpty)
    {

        if (nodesValueMemory is null || nodesMemory is null || nodesCount == 0)
            return false;

        for (int i = 0; i < nodesCount; i++)
        {
            ValueLiskedListNode<T>* newNode = nodesMemory + i;

            newNode->isLocalValue = false;

            newNode->isLocalNode = false;

            newNode->Value = nodesValueMemory + i;

            if (nodeValueIsEmpty)
                SetAsFree(newNode);
            else
                LinkToLocalNode(newNode);
        }


        return true;
    }

}