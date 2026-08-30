namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{

    /// <summary>
    /// 替换指定元素集合（值等同判断查询）
    /// </summary>
    /// <param name="select"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Replace_Result Replace(ReadOnlySpan<T> select, ReadOnlySpan<T> value)
    {
        int indexSelect = IndexOf(select);

        // 1. 如果 IndexOf 找不到，或者 select 长度为 0，则返回未找到。
        // 2. 如果要替换的子序列的末尾索引超出了当前链表的总节点数，也返回未找到。
        if (indexSelect < 0 || select.Length == 0 || (indexSelect + select.Length > _nodesCount))
            return new Replace_Result { Status = Replace_Result.NotFound };


        // 1. 移除旧元素：
        // 连续调用 SetAsFree(indexSelect) 是正确的，因为每次移除后，
        // 原本在 indexSelect 位置之后的节点都会向前移动，新的目标节点仍然位于 indexSelect。
        for (int i = 0; i < select.Length; i++)
        {
            // SetAsFree(indexSelect) 负责：
            //   a. 从主链表中移除节点。
            //   b. 释放该节点可能持有的值内存 (isLocalValue == true)。
            //   c. 将节点结构本身放入空闲池。
            //   d. 递减 _nodesCount。
            SetAsFree(indexSelect);
        }

        // 2. 插入新元素：
        for (int i = 0; i < value.Length; i++)
        {
            fixed (T* p_value = &value[i])
            {
                // 插入位置是 indexSelect + i，确保新值从原起始位置开始连续插入
                InsertAt(indexSelect + i, p_value);
            }
        }


        return new Replace_Result { Status = Replace_Result.Success_Code };
    }

}