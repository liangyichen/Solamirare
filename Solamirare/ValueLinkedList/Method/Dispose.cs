namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{

    /// <summary>
    /// 释放内存（正在使用的节点与空闲节点全部释放）
    /// </summary>
    public void Dispose()
    {
        if (head is null && tail is null && _cacheBlockHead is null) return;

        // 1. 释放所有活跃节点和空闲节点中，那些单独在堆上分配的节点 (isLocalNode == true)
        // isLocalNode=false 的节点不会被 NativeMemory.Free(node) 释放。
        _dispose(head);
        _dispose(_freeNodesHead);


        if (_cacheBlockHead is not null)
        {
            NativeMemory.Free(_cacheBlockHead);
            _cacheBlockHead = null;
        }

        head = null;
        tail = null;
        _freeNodesHead = null; // 修正：清空空闲池头指针

        _nodesCount = 0;
        _freeNodesCount = 0;

    }


    void _dispose(ValueLiskedListNode<T>* pool)
    {
        ValueLiskedListNode<T>* current = pool;

        while (current is not null)
        {
            ValueLiskedListNode<T>* node = current;

            current = current->Next;

            // 1. 释放值内存
            // 只有值复制方式存储到本地的数据才会允许释放
            if (node->isLocalValue && node->Value is not null)
            {
                NativeMemory.Clear(node->Value, (nuint)sizeof(T));

                
                    NativeMemory.Free(node->Value);
                    
            }

            // 2. 清理节点状态 (适用于所有节点，无论是否本地)
            node->Value = null;
            node->isLocalValue = false;
            node->Next = null; // 清除 Next 指针

            // 3. 释放节点结构本身 (仅限本地分配的节点)
            // 节点本身必须是当前对象创建的，才会由当前对象负责释放
            if (node->isLocalNode)
            {
                NativeMemory.Free(node);
                    
            }
        }
    }

}