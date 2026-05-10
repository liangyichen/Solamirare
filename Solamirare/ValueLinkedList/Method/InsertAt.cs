namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{


    /// <summary>
    /// 根据下标插入元素（值复制）
    /// </summary>
    /// <param name="index"></param>
    /// <param name="value"></param>
    public void InsertAt(int index, T* value)
    {
        // 1. 基本参数检查
        if (index < 0 || value == null) return;

        // 2. 节点分配：必须独立构造节点
        // createNode_on_heap 会分配节点内存并设置 isLocalNode = true。
        ValueLiskedListNode<T>* newNode = createNode_on_heap();
        if (newNode == null)
        {
            // 节点结构分配失败（内存不足）
            return;
        }

        nuint tSize = (nuint)sizeof(T);

        nuint nodeSise = (nuint)sizeof(ValueLiskedListNode<T>);

        // 3. 值内存分配
        if (newNode->Value is null)
        {

            newNode->Value = (T*)NativeMemory.AllocZeroed(tSize);

            // 检查值内存分配是否成功
            if (newNode->Value is null)
            {
                // 值内存分配失败，释放已分配的节点结构

                NativeMemory.Free(newNode);

                return;
            }

        }

        // 4. 值复制和状态设置
        // 此时 newNode->Value 必然是有效的指针
        NativeMemory.Copy(value, newNode->Value, (uint)tSize);

        // 设置所有权标记
        // newNode->isLocalNode 保持由 createNode_on_heap 设置的 true。
        newNode->isLocalValue = true; // 值是复制进来的，归链表负责释放。


        // 5. 链接操作
        if (index == 0)
        {
            // 头插
            newNode->Next = head;
            head = newNode;
            if (tail == null)
                tail = newNode;
        }
        else
        {
            // 中间或尾部插入：定位到目标位置的前一个节点
            ValueLiskedListNode<T>* current = head;
            int currentIndex = 0;

            // 循环寻找 index - 1 处的节点
            while (current != null && currentIndex < index - 1)
            {
                current = current->Next;
                currentIndex++;
            }

            // 越界检查 (index > _nodesCount) 或 链表为空时的边界情况 (虽然 index=0 已处理)
            if (current == null)
            {
                // 目标索引超出当前链表长度，或者链表为空但 index != 0

                // 必须释放已分配的 Value 和 Node 内存
                if (newNode->Value is not null)
                {

                    NativeMemory.Free(newNode->Value);

                }


                // 节点结构本身也必须释放，因为它不是从空闲池中获取的

                NativeMemory.Free(newNode);


                return;
            }

            // 执行插入：current 是 index - 1 处的节点
            newNode->Next = current->Next;
            current->Next = newNode;

            // 如果新节点插入后成为了末尾，更新 tail
            if (newNode->Next == null)
                tail = newNode;
        }

        // 6. 更新计数
        _nodesCount += 1;
    }

}