namespace Solamirare;


public unsafe partial struct UnManagedMemory<T>
{
    /// <summary>
    /// 两者组合生成一个新的对象
    /// <para>Combines the two to generate a new object.</para>
    /// </summary>
    /// <param name="_this"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static UnManagedMemory<T> operator +(in UnManagedMemory<T> _this, in UnManagedMemory<T> target)
    {
        if (!_this.activated || !target.activated) return Empty;

        UnManagedMemory<T> temp = _this.Clone();

        // 预先确保容量，避免 AddRange 时触发 Resize 导致二次分配
        temp.EnsureCapacity(_this.UsageSize + target.UsageSize);

        temp.InsertRange(_this.UsageSize, target.Pointer, target.UsageSize);

        return temp;
    }


    /// <summary>
    /// 两者组合生成一个新的对象
    /// </summary>
    /// <param name="_this"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static UnManagedMemory<T> operator +(in UnManagedMemory<T> _this, in ReadOnlySpan<T> target)
    {
        if (!_this.activated) return Empty;


        UnManagedMemory<T> temp = _this.Clone();

        // 预先确保容量
        temp.EnsureCapacity(_this.UsageSize + (uint)target.Length);

        temp.AddRange(target);

        return temp;
    }


    /// <summary>
    /// 指向值的等同判断（所有元素匹配）
    /// <para>Equality check of pointed values (all elements match).</para>
    /// </summary>
    /// <param name="_this"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool operator ==(in UnManagedMemory<T> _this, in UnManagedMemory<T> value)
    {
        if (!_this.activated) return false;


        // 1. 长度不同则必然不相等
        if (_this.UsageSize != value.UsageSize) return false;

        // 2. 长度都为 0 则视为相等 (处理 Pointer 为 null 的情况)
        if (_this.UsageSize == 0) return true;

        bool result = false;

        fixed (UnManagedMemory<T>* pValue = &value)
        {
            result = _this.Equals(pValue->Pointer, pValue->UsageSize);
        }

        return result;

    }


    /// <summary>
    /// 指向值的等同判断（所有元素匹配）
    /// </summary>
    /// <param name="_this"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool operator ==(in UnManagedMemory<T> _this, ReadOnlySpan<T> value)
    {
        if (!_this.activated) return false;

        bool result = _this.Equals(value);

        return result;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="_this"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool operator !=(in UnManagedMemory<T> _this, ReadOnlySpan<T> value)
    {
        if (!_this.activated) return false;

        return !_this.Equals(value);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="_this"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool operator !=(in UnManagedMemory<T> _this, in UnManagedMemory<T> value)
    {
        if (!_this.activated || !value.activated) return false;

        bool result = false;

        fixed (UnManagedMemory<T>* pValue = &value)
        {
            result = !_this.Equals(pValue->Pointer, pValue->UsageSize);
        }

        return result;
    }

}