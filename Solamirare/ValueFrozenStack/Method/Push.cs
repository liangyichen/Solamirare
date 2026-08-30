namespace Solamirare;

public unsafe partial struct ValueFrozenStack<T>
where T : unmanaged
{

    /// <summary>
    /// 把数据压入栈
    /// </summary>
    /// <param name="item">The item to push.</param>
    public bool Push(T item)
    {
        if (_buffer is null || _count >= (_capacity & 0x3FFFFFFFu))
        {
            return false;
        }

        // 2. 在栈顶位置写入数据
        // 指针算术：_buffer + _count 计算出下一个可用元素的地址
        _buffer[_count] = item;

        // 3. 增加计数器
        _count++;

        return true;
    }


}