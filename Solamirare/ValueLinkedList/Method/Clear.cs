namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{


    /// <summary>
    /// 清空活跃节点链表，将所有节点移动到空闲节点池，但不释放节点本身的内存。
    /// 直接调用 SetAsFree(node) 函数，简化逻辑，并保证 _freeNodesCount 正确更新。
    /// </summary>
    public void Clear()
    {
        if (head is null) return;

        ValueLiskedListNode<T>* current = head;
        ValueLiskedListNode<T>* next;

        // 遍历活跃链表，释放本地值，并将节点一个个移动到空闲池头部
        while (current is not null)
        {
            next = current->Next;

            // 如果是本地值副本，则释放值内存
            if (current->isLocalValue && current->Value is not null)
            {
                
                    NativeMemory.Free(current->Value);
                    

                current->Value = null;
            }

            // 重置所有权标记，准备放入空闲池
            current->isLocalValue = false;

            // 使用 SetAsFree 函数将节点放入空闲池，并自动递增 _freeNodesCount
            // SetAsFree 内部会将 current->Next 设置为 _freeNodesHead，并更新 _freeNodesHead
            SetAsFree(current); // SetAsFree(node) 会递增 _freeNodesCount

            current = next;
        }

        // 更新活跃节点计数和头尾指针
        // _freeNodesCount 已在循环中通过 SetAsFree(node) 递增
        _nodesCount = 0;

        head = null;

        tail = null;
    }

}