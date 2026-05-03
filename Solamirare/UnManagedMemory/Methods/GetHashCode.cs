namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 获取内容的哈希码
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        
        return Prototype.GetHashCode();
    }


}