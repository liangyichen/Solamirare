namespace Solamirare;

public unsafe partial struct ValueFrozenStack<T>
where T : unmanaged
{



    /// <summary>
    /// 引用返回栈顶数据，不移除
    /// </summary>
    /// <returns></returns>
    public T* Peek()
    {
        if (_buffer is null || _count == 0)
        {
            return null;
        }

        return &_buffer[_count - 1]; // 返回栈顶元素
    }


}