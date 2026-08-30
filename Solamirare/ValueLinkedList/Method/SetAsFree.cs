namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{


    /// <summary>
    /// 释放节点到池中
    /// </summary>
    /// <param name="node"></param>
    void SetAsFree(ValueLiskedListNode<T>* node)
    {
        if (node is null) return;

        node->Next = _freeNodesHead;  // 将节点加入空闲池
        _freeNodesHead = node;
        _freeNodesCount += 1;
    }


    /// <summary>
    /// 根据下标移除元素
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool SetAsFree(int index)
    {
        if (index < 0) return false;

        ValueLiskedListNode<T>* node = head;
        ValueLiskedListNode<T>* previous = null;
        int currentIndex = 0;

        while (node != null)
        {
            if (currentIndex == index)
            {
                if (previous == null)
                {
                    head = node->Next;
                    if (head == null)
                        tail = null;
                }
                else
                {
                    previous->Next = node->Next;
                    if (node == tail)
                        tail = previous;
                }


                if (node->isLocalValue)
                {
                    T* distinationValue = node->Value;

                    if (distinationValue is not null)
                    {
                         
                            NativeMemory.Free(distinationValue); 
                    }

                    node->Value = null;
                }

                _nodesCount -= 1;

                SetAsFree(node);

                return true;
            }

            previous = node;
            node = node->Next;
            currentIndex++;
        }

        return false;
    }

}