namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{



    /// <summary>
    /// 获取指定值的下标
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int IndexOf(in T target)
    {
        fixed (T* p_value = &target)
        {
            return IndexOf(p_value);
        }
    }

    /// <summary>
    /// 获取指定值的下标
    /// 【修正】：确保值比较结果必须为 0 才视为匹配。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int IndexOf(T* target)
    {
        if (target is null) return -1;

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

            // 假设 ValueTypeHelper.IndexOf(P1, L1, P2, L2) 只有在完全匹配时返回 0
            int _equalIndex = ValueTypeHelper.IndexOf(current->Value, 1, target, 1);

            // 确保只有完全匹配（返回 0）时才返回索引
            if (_equalIndex == 0)
                return index;

            current = current->Next;

            index++;
        }

        return -1;
    }



    /// <summary>
    /// 获取值等同指定集合的下标。
    /// 【修复】: 确保内部循环的指针移动和边界检查是正确的。
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int IndexOf(ReadOnlySpan<T> target)
    {
        // 快速检查
        if (target.Length == 0)
            return -1; // 空集合不匹配

        // 长度检查
        if (target.Length > _nodesCount)
            return -1;

        ValueLiskedListNode<T>* current = head;
        int index = 0;

        // 外部循环：遍历所有可能的起始点
        while (current != null)
        {
            ValueLiskedListNode<T>* node = current;
            bool isMatch = true;

            // 内部循环：检查完整子序列匹配
            for (int i = 0; i < target.Length; i++)
            {
                // 1. 边界检查：在解引用 node->Value 之前，确保 node 存在。
                // 如果 node 在子序列匹配中提前到达末尾，则匹配失败。
                if (node is null)
                {
                    isMatch = false;
                    break;
                }

                // 2. 值比较
                fixed (T* p_value_item = &target[i])
                {
                    // 检查节点的值指针是否为空，或值比较失败
                    // 假设 ValueTypeHelper.IndexOf 返回 0 表示匹配
                    if (node->Value is null || ValueTypeHelper.IndexOf(node->Value, 1, p_value_item, 1) != 0)
                    {
                        isMatch = false;
                        break;
                    }
                }

                // 3. 准备下一个匹配点：如果匹配成功，立即移动到下一个节点，准备检查 target[i+1]。
                // 这个移动独立于 i < target.Length - 1 的条件，是修正的关键。
                node = node->Next;
            }

            if (isMatch)
                return index;

            // 移动到下一个可能的起始点 (外部循环控制)
            current = current->Next;
            index++;
        }

        return -1;
    }

}