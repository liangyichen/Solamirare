namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
{
    /// <summary>
    /// 将集合中的所有元素清零，下标归0，等待新的数据从0开始填充。真实容量保留。
    /// </summary>
    /// <returns></returns>
    public void Zero()
    {
        if (@readonly || !activated) return;
        fixed (UnManagedCollection<T>* p = &Prototype)
        {
            p->Zero();
        }
    }
}