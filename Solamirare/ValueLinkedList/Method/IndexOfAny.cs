namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{


    /// <summary>
    /// 获取指定值集合中任意一个值的下标
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int IndexOfAny(ReadOnlySpan<T> target)
    {
        if (target.IsEmpty) return -1;

        if (head is null) return -1;

        ValueLiskedListNode<T>* current = head;

        int index = 0;

        while (current != null)
        {
            if (current->Value is null)
            {
                current = current->Next;
                index++;
                continue;
            }

            for (int i = 0; i < target.Length; i++)
            {

                fixed (T* p_value = &target[i])
                {
                    int _equalIndex = ValueTypeHelper.IndexOf(current->Value, 1, p_value, 1);

                    if (_equalIndex == 0) return index;
                }
            }

            current = current->Next;
            index++;
        }

        return -1; // None of the values found
    }

}