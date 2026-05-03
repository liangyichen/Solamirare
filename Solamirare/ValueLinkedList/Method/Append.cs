namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{

    /// <summary>
    /// 添加元素（值复制）。 如果在外部内存模式下，只要外部内存足够，也可以一直添加下去，可以无视最开始构造函数中设置的初始化节点数量
    /// </summary>
    /// <param name="value"></param>
    /// <param name="valueAddress">指定node指向的地址</param>
    public bool Append(in T value, T* valueAddress = null)
    {
        fixed (T* p_value = &value)
        {
            return Append(p_value, valueAddress);
        }
    }

    /// <summary>
    /// 添加元素（值复制或外部引用）。 如果在外部内存模式下，只要外部内存足够，也可以一直添加下去，可以无视最开始构造函数中设置的初始化节点数量。
    /// 明确隔离值复制模式和外部引用模式的逻辑，避免在引用模式下写入外部内存。
    /// </summary>
    /// <param name="value">要插入的值的内容。</param>
    /// <param name="valueAddress">如果非空，节点将引用此外部地址（纯引用模式）。</param>
    /// <returns></returns>
    public bool Append(T* value, T* valueAddress = null)
    {
        if (value is null) return false;

        bool isNodeReused = true;
        ValueLiskedListNode<T>* newNode = DeQueueEmptyNode();

        if (newNode is null)
        {
            isNodeReused = false; // 节点是新分配的
            newNode = createNode_on_heap();
            if (newNode == null) return false;
        }
        else // 节点从空闲池中取出，需要清理
        {
            // 释放该节点可能保留的、且需要内部负责释放的旧值内存
            if (newNode->isLocalValue && newNode->Value is not null)
            {
                
                    NativeMemory.Free(newNode->Value);
                    
            }

            // 重置状态
            newNode->Value = null;
            newNode->isLocalValue = false;
        }

        bool result;

        if (valueAddress is not null)
        {
            // --- 纯引用模式 ---
            // 节点直接指向外部地址，链表不负责释放值内存，不执行值复制。
            newNode->Value = valueAddress;
            newNode->isLocalValue = false;
            result = true;
        }
        else // --- 值复制模式 ---
        {
            int tSize = sizeof(T);

            // 1. 分配新内存来存储值
           
                newNode->Value = (T*)NativeMemory.AllocZeroed((nuint)tSize);
                


            if (newNode->Value is null)
            {
                // 值内存分配失败，执行清理
                if (isNodeReused)
                {
                    // 节点来自空闲池，放回空闲池，以供重用。
                    SetAsFree(newNode);
                }
                else
                {
                    // 节点是新分配的，直接释放。
                    
                        NativeMemory.Free(newNode);
                        
                }
                return false;
            }

            NativeMemory.Clear(newNode->Value, (nuint)tSize);
            newNode->isLocalValue = true; // 内部地址，需要释放

            // 2. 复制值
            NativeMemory.Copy(value, newNode->Value, (uint)tSize);
            result = true;
        }

        // 链接到链表
        if (result)
        {
            LinkToLocalNode(newNode);
        }

        return result;
    }

    /// <summary>
    /// 从非托管集合添加元素（值复制）
    /// </summary>
    /// <param name="target"></param>
    public bool Append(UnManagedMemory<T>* target)
    {
        if (target is null || target->IsEmpty || target->Pointer is null)

            return false;

        for (int i = 0; i < target->UsageSize; i++)
        {
            T* value = &target->Pointer[i];
            Append(value, null);
        }

        return true;
    }


    /// <summary>
    /// 从托管集合添加元素（值复制）
    /// </summary>
    /// <param name="target"></param>
    public bool Append(ReadOnlySpan<T> target)
    {
        if (target.IsEmpty) return false;

        fixed (T* p_target = target)
        {
            for (int i = 0; i < target.Length; i++)
            {
                Append(&p_target[i], null);
            }
        }

        return true;
    }


}