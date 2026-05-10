namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{


    /// <summary>
    /// 根据下标获取元素，为解决对象作为指针时无法使用索引， 例如对象模式的 obj[0] 等同于指针模式的 obj->Index(0)
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public T* Index(int index)
    {
        if (index < 0 || index >= _nodesCount)
            return null;

        ValueLiskedListNode<T>* current = head;
        int currentIndex = 0;

        while (current != null)
        {
            if (currentIndex == index)
                return current->Value;

            current = current->Next;
            currentIndex++;
        }

        return null;
    }



    /// <summary>
    /// 根据下标获取元素
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public T* this[int index] => Index(index);


}