namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{


    /// <summary>
    /// 添加预留空间
    /// </summary>
    /// <param name="TCount"></param>
    public void AdditionMemory(uint TCount)
    {
        ulong totalBytesLength = (ulong)(TCount * sizeof(ValueLiskedListNode<T>));

        ValueLiskedListNode<T>* memory;

        memory = (ValueLiskedListNode<T>*)NativeMemory.AllocZeroed((nuint)totalBytesLength);

        if (memory is null) return;

        //存储内存块的起始地址，用于 Dispose 时一次性释放。
        _cacheBlockHead = memory;

        for (int i = 0; i < TCount; i++)
        {
            ValueLiskedListNode<T>* memory_item = memory + i;

            memory_item->Value = null;

            // 释放责任交给 _cacheBlockHead。
            memory_item->isLocalNode = false;

            // 显式设置值所有权为 false
            memory_item->isLocalValue = false;

            SetAsFree(memory_item);
        }
    }

}