namespace Solamirare;

public unsafe partial struct ValueDictionary<TKey, TValue>
where TKey : unmanaged
where TValue : unmanaged
{

    /// <summary>
    /// 获取对象内容的哈希码。
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        uint code = ValueTypeHelper.HashCode(_ctrl, (int)_capacity);

        return (int)code;
    }

}