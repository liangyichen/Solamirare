namespace Solamirare;

public unsafe partial struct ValueFrozenStack<T>
where T : unmanaged
{

    /// <summary>
    /// 获取对象内容的哈希码。
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        uint code = ValueTypeHelper.HashCode(_buffer, (int)_capacity);

        return (int)code;
    }

}