namespace Solamirare;


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{

    /// <summary>
    /// 把指向的内存克隆一份到新的内存
    /// <para>Clones the pointed memory to new memory.</para>
    /// </summary>
    /// <param name="destination">如果传入值，将以外部指针指向地址来存储数据，外部需要自行保证具备足够空间<para>If a value is passed, the data will be stored at the address pointed to by the external pointer; the external caller must ensure sufficient space.</para></param>
    /// <returns></returns>
    public UnManagedCollection<T> Clone(T* destination = null)
    {
        if (InternalPointer is null) return Empty;

        UnManagedCollection<T> temp;

        if (destination == null)
            temp = UnManagedCollection<T>.Empty;
        else
        {
            temp = new UnManagedCollection<T>(destination, Size);
            NativeMemory.Copy(InternalPointer, temp.InternalPointer, Size);
        }


        return temp;
    }


}