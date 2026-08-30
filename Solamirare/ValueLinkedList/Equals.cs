namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{

    #region Equals

    /// <summary>
    /// 指向值的等同判断（所有元素值匹配）
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Equals(UnManagedCollection<T>* target)
    {
        if (target is null) return false;

        Span<T> span = target->AsSpan();

        return IndexOf(span) == 0;
    }

    /// <summary>
    /// 指向值的等同判断（所有元素值匹配）
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Equals(ValueLinkedList<T>* target)
    {

        if (target is null || target->NodesCount != _nodesCount)
            return false;

        ValueLiskedListNode<T>* currentThis = head;
        ValueLiskedListNode<T>* currentTarget = target->head; // 引入 target 的当前节点指针

        while (currentThis != null)
        {
            // 确保两个节点的值指针都有效，否则除非两者都为 null，否则视为不匹配
            bool thisValueNull = currentThis->Value is null;
            bool targetValueNull = currentTarget->Value is null;

            if (thisValueNull && targetValueNull)
            {
                // 都为空，继续
            }
            else if (thisValueNull || targetValueNull)
            {
                // 只有一个为空，不匹配
                return false;
            }
            else
            {
                // 均不为空，进行值比较
                if (ValueTypeHelper.IndexOf(currentThis->Value, 1, currentTarget->Value, 1) != 0)
                    return false;
            }

            currentThis = currentThis->Next;
            currentTarget = currentTarget->Next;
        }

        // 理论上 while 循环应该在 _nodesCount 相等时自然结束，但为了安全，确保两个指针同时到达末尾。
        return currentThis is null && currentTarget is null;
    }

    /// <summary>
    /// 指向值的等同判断（所有元素值匹配）
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Equals(in UnManagedCollection<T> target)
    {
        fixed (UnManagedCollection<T>* p_target = &target)
        {
            return Equals(p_target);
        }
    }

    /// <summary>
    /// 指向值的等同判断（所有元素值匹配）
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Equals(ReadOnlySpan<T> target)
    {
        return IndexOf(target) == 0;
    }

    #endregion


}