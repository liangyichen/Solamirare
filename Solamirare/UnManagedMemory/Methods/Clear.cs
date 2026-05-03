namespace Solamirare;


public unsafe partial struct UnManagedMemory<T>
{
    /// <summary>
    /// 将集合中的所有元素按照使用长度执行清零。
    /// </summary>
    /// <returns></returns>
    public void Clear()
    {
        if (@readonly || !activated) return;

        fixed (UnManagedCollection<T>* p = &Prototype)
        {
            p->Clear();
        }
    }
}