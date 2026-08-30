namespace Solamirare;


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{

    /// <summary>
    /// 将当前集合的内存映射到 UnManagedMemory&lt;T&gt;。
    /// </summary>
    /// <returns></returns>
    public UnManagedMemory<T> AsUnManagedMemory()
    {
        UnManagedMemory<T> mem = new UnManagedMemory<T>(InternalPointer, Size, Size, MemoryTypeDefined.Unknown);

        return mem;
    }

}