namespace Solamirare;


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{

    /// <summary>
    /// 遍历元素（不要传入 Lambda 表达式，会造成 GC。 应该传入既有的基于代理模式的传统方法）
    /// <para>Iterates through elements (do not pass Lambda expressions, as it will cause GC. Pass existing delegate-based traditional methods instead).</para>
    /// </summary>
    /// <param name="onReaded"></param>
    /// <param name="caller"></param>
    /// <returns></returns>
    public void ForEach(delegate*<int, T*, void*, bool> onReaded, void* caller)
    {
        if (onReaded is null) return;

        if (InternalPointer is null) return;

        for (int i = 0; i < Size; i++)
        {
            if (onReaded is not null)
            {
                bool load = onReaded(i, &InternalPointer[i], caller);

                if (!load) break;
            }
        }
    }


}