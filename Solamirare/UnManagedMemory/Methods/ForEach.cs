namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 遍历元素（不要传入 Lambda 表达式，会造成 GC。 应该传入既有的基于代理模式的传统方法）。
    /// </summary>
    /// <param name="onReaded"></param>
    /// <param name="caller"></param>
    /// <returns></returns>
    public void ForEach(delegate*<int, T*, void*, bool> onReaded, void* caller)
    {
        if (activated)
        fixed (UnManagedCollection<T>* p = &Prototype)
            p->ForEach(onReaded, caller);
    }

}