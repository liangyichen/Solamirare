namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{


    /// <summary>
    /// Replaces the value stored at the specified node index.
    /// </summary>
    /// <param name="index">Zero-based node index.</param>
    /// <param name="value">Pointer to the new value.</param>
    public void Update(int index, T* value)
    {
        if (index < 0 || index >= _nodesCount || value is null)
            return;

        ValueLiskedListNode<T>* current = head;
        int currentIndex = 0;
        nuint tSize = (nuint)sizeof(T); // 在循环外计算尺寸，提高效率

        while (current != null)
        {
            if (currentIndex == index)
            {
                if (current->Value is null)
                {
                    // 目标节点的值指针为空，需要分配新内存
                    // 以下两个分配不需要清零操作，因为接着马上就会被全覆盖
                    
                        current->Value = (T*)NativeMemory.AllocZeroed(tSize); 

                    // 标志：新分配的值内存，现在归链表所有
                    current->isLocalValue = true;

                    // 检查 Alloc 是否失败
                    if (current->Value is null) return;

                    // 执行位复制
                    NativeMemory.Copy(value, current->Value, (uint)tSize);
                }
                else
                {
                    // 目标节点的值指针已存在，直接覆盖内容。
                    // isLocalValue 保持不变（无论是本地值还是外部引用）。
                    NativeMemory.Copy(value, current->Value, (uint)tSize);

                }

                return;
            }

            current = current->Next;

            currentIndex++;
        }
    }

}
