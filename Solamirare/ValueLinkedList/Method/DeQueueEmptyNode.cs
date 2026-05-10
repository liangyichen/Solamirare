namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{

    /// <summary>
    /// 获取可以利用的节点（有可能是重复利用）
    /// </summary>
    /// <returns></returns>
    public ValueLiskedListNode<T>* DeQueueEmptyNode()
    {
        ValueLiskedListNode<T>* node;

        if (_freeNodesHead != null)
        {
            node = _freeNodesHead;
            _freeNodesHead = _freeNodesHead->Next;
            node->Next = null;
            _freeNodesCount -= 1;
            return node;
        }

        return null;
    }

}