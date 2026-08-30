namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{

    /// <summary>
    /// 根据下标获取链表节点
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public ValueLiskedListNode<T>* IndexNode(int index)
    {
        if (index < 0 || index >= _nodesCount)
            return null;

        ValueLiskedListNode<T>* current = head;
        int currentIndex = 0;

        while (current != null)
        {
            if (currentIndex == index)
                return current;

            current = current->Next;
            currentIndex++;
        }

        return null;
    }

}