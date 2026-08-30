namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{


    /// <summary>
    /// 检查链表中是否包含指定值集合中的所有元素，并且顺序一致。 例如原始集合为 [1,2,3,4,5]， 包含 [2,3,4] ，但是不包含 [2,3,5]
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Contains(ReadOnlySpan<T> target)
    {
        return IndexOf(target) > -1;
    }

    /// <summary>
    /// 检查链表中是否包含指定值的元素
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Contains(in T target)
    {
        return IndexOf(target) > -1;
    }


    /// <summary>
    /// 检查链表中是否包含指定地址的元素(判断值等同)（例如 node == *item）
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Contains(T* target)
    {
        return IndexOf(target) > -1;
    }


    /// <summary>
    /// 检查链表中是否包含指定地址的元素
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public bool ContainsAddress(T* address)
    {
        if (address is null)
            return false;

        ValueLiskedListNode<T>* current = head;
        while (current != null)
        {
            if (current->Value == address)
                return true;

            current = current->Next;
        }

        return false;
    }


}