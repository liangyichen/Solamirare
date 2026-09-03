namespace Solamirare;


public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 集合筛选
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public WhereQuery<T> Where(
        delegate*<T*, bool> predicate)
    {
        return Prototype.Where(predicate);
    }


}