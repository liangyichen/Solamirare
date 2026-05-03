namespace Solamirare;


public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{


    /// <summary>
    /// 获取指定值的最后一个下标
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int LastIndexOf(in T target)
    {
        fixed (T* p_value = &target)
        {
            return LastIndexOf(p_value);
        }
    }


    /// <summary>
    /// 获取指定值的最后一个下标
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int LastIndexOf(T* target)
    {
        ValueLiskedListNode<T>* current = head;

        int index = 0;

        int lastIndex = -1;

        while (current != null)
        {
            if (current->Value is null)
            {
                current = current->Next;
                index++;
                continue;
            }

            int _equalIndex = ValueTypeHelper.IndexOf(current->Value, 1, target, 1);

            if (_equalIndex == 0)
                lastIndex = index; //依然是顺序遍历， 但是通过一直覆盖新查找到的值，获得最靠右的值

            current = current->Next;

            index++;
        }

        return lastIndex;
    }

}