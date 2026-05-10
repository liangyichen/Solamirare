namespace Solamirare;


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{
    /// <summary>
    /// 应该只在调试阶段使用
    /// <para>Should only be used during the debugging phase.</para>
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return this.AsSpan().ToString();
    }

}