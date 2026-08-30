namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
    where T : unmanaged
{
    /// <summary>
    /// 根据所有节点的实际内容计算哈希值。
    /// <para>逐节点串联计算，节点顺序不同必然产生不同结果。</para>
    /// </summary>
    public override int GetHashCode()
    {
        if (_nodesCount == 0 || head == null)
            return 0;

        uint hash = 0;

        ValueLiskedListNode<T>* current = head;

        while (current != null)
        {
            if (current->Value != null)
            {
                // 用上一轮的 hash 作为 seed，实现跨节点状态串联
                hash = ValueTypeHelper.HashCode(current->Value, 1, hash);

                //链表这里 HashCode 的 length 传的是 1，而不是某个 Capacity，
                //因为每个节点的 Value 始终指向恰好一个 T。也不需要额外混入边界信息，
                // 因为每个节点本身就是一个独立的边界单位，[123][456] 和 [12][3456] 在链表里是完全不同的节点结构，
                // T 的二进制内容本身就不同，串联后自然产生不同结果。
            }

            current = current->Next;
        }

        return (int)hash;
    }
}