namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{


    /// <summary>
    /// 把指向的内存克隆一份到新的内存
    /// </summary>
    /// <param name="distination">如果传入值，将以外部指针指向地址来存储数据，外部需要自行保证具备足够空间</param>
    /// <returns></returns>
    public UnManagedMemory<T> Clone(T* distination = null)
    {
        if (!activated) return Empty;

        UnManagedMemory<T> temp;


        if (distination == null)
            temp = new UnManagedMemory<T>(Capacity, 0);
        else
        {
            temp = new UnManagedMemory<T>(distination, Capacity, 0);
        }

        temp.InsertRange(0u, Pointer, UsageSize);


        return temp;
    }


}