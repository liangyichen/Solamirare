namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 转换到 UnManagedCollection
    /// </summary>
    /// <returns></returns>
    public UnManagedCollection<T> AsUnManagedCollection()
    {
        if (!activated) return UnManagedCollection<T>.Empty;

        return Prototype;
    }

}