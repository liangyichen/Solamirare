namespace Solamirare;

public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{

    /// <summary>
    /// 包含指定判断值的元素个数
    /// <para>Counts the number of elements containing the specified value.</para>
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public uint Count(in T target)
    {
        Span<T> span = this.AsSpan();
        return (uint)span.Count(target);
    }


    /// <summary>
    /// 计算指定模式在非托管内存中出现的次数 (性能优化版)
    /// <para>Calculates the number of occurrences of the specified pattern in unmanaged memory (performance optimized version).</para>
    /// </summary>
    /// <param name="target">要匹配的模式<para>The pattern to match.</para></param>
    /// <returns>模式出现的次数<para>The number of occurrences of the pattern.</para></returns>
    public uint Count(ReadOnlySpan<T> target)
    {
        Span<T> span = this.AsSpan();
        return (uint)span.Count(target);
    }

}