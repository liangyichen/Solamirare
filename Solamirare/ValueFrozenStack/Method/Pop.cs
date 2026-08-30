namespace Solamirare;

public unsafe partial struct ValueFrozenStack<T>
where T : unmanaged
{


    /// <summary>
    /// 值复制的方式返回栈顶数据，并移除
    /// <para>必须是值类型返回，因为元素数据会在返回后进行逻辑删除，不可以做引用返回</para>
    /// </summary>
    /// <returns></returns>
    public T Pop()
    {
        T value;
        if (_buffer is null || _count == 0)
        {
            return default;
        }

        // 4. 减少计数器
        _count--;

        // 5. 返回之前位于栈顶的值
        value = _buffer[_count]; // 等价于 return *(_buffer + _count);

        return value;
    }
}