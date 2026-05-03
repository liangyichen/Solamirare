namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{


    /// <summary>
    /// 从头部弹出一个元素，如果链表为空，返回 false
    /// 【修正】：增加对 node->Value 的空指针检查，提高安全性。
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryPop(out T value)
    {
        if (head is null)
        {
            value = default;
            return false;
        }

        ValueLiskedListNode<T>* node = head;

        // 1. 安全获取值
        if (node->Value is null)
        {
            // 如果活跃节点的值指针为空（不应该发生，但为安全起见），
            // 仍然移除节点并返回 default 值。
            value = default;
        }
        else
        {
            value = *node->Value;
        }

        // 2. 移除节点
        head = head->Next;

        if (head is null)
            tail = null;

        _nodesCount -= 1;


        // 3. 释放值内存 (如果归链表所有)
        if (node->isLocalValue && node->Value is not null)
        {
             
                NativeMemory.Free(node->Value); 

            node->Value = null; // 清空指针，以防重用时误用
        }

        // 4. 重置节点状态并放入空闲池
        node->isLocalValue = false; // 节点现在位于空闲池，所有权标志重置

        SetAsFree(node); // 将节点放入空闲池

        return true;
    }

}