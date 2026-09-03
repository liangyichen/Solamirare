namespace Solamirare;


/// <summary>
/// 表示基于条件筛选的非托管内存查询。
/// </summary>
/// <typeparam name="T">集合元素类型。</typeparam>
public unsafe ref struct WhereQuery<T>
    where T : unmanaged
{
    private readonly T* _pointer;
    private readonly uint _size;
    private readonly delegate*<T*, bool> _predicate;

    private uint _index;

    /// <summary>
    /// 获取当前匹配的元素指针。
    /// </summary>
    public T* Current { get; private set; }

    internal WhereQuery(
        T* pointer,
        uint size,
        delegate*<T*, bool> predicate)
    {
        _pointer = pointer;
        _size = size;
        _predicate = predicate;
        _index = 0;
        Current = null;
    }

    /// <summary>
    /// 移动到下一个满足条件的元素。
    /// </summary>
    /// <returns>
    /// 如果找到满足条件的元素，则返回 <see langword="true"/>；
    /// 否则返回 <see langword="false"/>。
    /// </returns>
    public bool MoveNext()
    {
        while (_index < _size)
        {
            T* current = _pointer + _index++;

            if (_predicate(current))
            {
                Current = current;
                return true;
            }
        }

        Current = null;
        return false;
    }

    /// <summary>
    /// 重置查询位置，使下一次调用 <see cref="MoveNext"/> 时从第一个元素开始。
    /// </summary>
    public void Reset()
    {
        _index = 0;
        Current = null;
    }

    /// <summary>
    /// 判断是否存在满足条件的元素。
    /// </summary>
    /// <returns>
    /// 如果存在至少一个满足条件的元素，则返回 <see langword="true"/>；
    /// 否则返回 <see langword="false"/>。
    /// </returns>
    public bool Any()
    {
        for (uint index = 0; index < _size; index++)
        {
            if (_predicate(_pointer + index))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 查找第一个满足条件的元素。
    /// </summary>
    /// <returns>
    /// 第一个满足条件的元素指针；如果不存在满足条件的元素，则返回 <see langword="null"/>。
    /// </returns>
    public T* FirstOrDefault()
    {
        for (uint index = 0; index < _size; index++)
        {
            T* current = _pointer + index;

            if (_predicate(current))
                return current;
        }

        return null;
    }



    /// <summary>
    /// 将所有满足条件的元素复制到新的非托管内存中。
    /// </summary>
    /// <returns>
    /// 包含所有满足条件元素的新 <see cref="UnManagedMemory{T}"/>。
    /// </returns>
    public UnManagedMemory<T> CopyToMemory()
    {
        uint count = 0;

        for (uint index = 0; index < _size; index++)
        {
            if (_predicate(_pointer + index))
                count++;
        }

        UnManagedMemory<T> memory = new(count, 0);
        
        for (uint index = 0; index < _size; index++)
        {
            T* current = _pointer + index;

            if (_predicate(current))
                memory.Add(current);
        }

        return memory;
    }
}

