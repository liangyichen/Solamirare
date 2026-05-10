namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{

    /// <summary>
    /// 把节点链接到链表中
    /// </summary>
    /// <param name="newNode"></param>
    public void LinkToLocalNode(ValueLiskedListNode<T>* newNode)
    {
        if (newNode is null) return;

        if (head is null)
        {
            head = newNode;
            tail = newNode;
        }
        else
        {
            tail->Next = newNode;
            tail = newNode;
        }

        _nodesCount += 1;
    }

}