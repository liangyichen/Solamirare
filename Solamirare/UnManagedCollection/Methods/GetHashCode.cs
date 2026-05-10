namespace Solamirare;

// GetHashCode() 是 Object.GetHashCode() 的方法重写，不可以做虚表与扩展方法


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{

    /// <summary>
    /// 获取内容的哈希码。
    /// </summary>
    /// <returns>一个 32 位有符号整数哈希码。</returns>
    public override int GetHashCode()
    {
        if (InternalPointer is null) return 0;


        uint code = ValueTypeHelper.HashCode((char*)InternalPointer, (int)Size);

        return (int)code;

    }



}