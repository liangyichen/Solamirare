namespace Solamirare;


public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{
    /// <summary>
    /// 把值转换到常规.Net字符串（建议只在调试阶段使用）
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return this.AsSpan().ToString();
    }

}