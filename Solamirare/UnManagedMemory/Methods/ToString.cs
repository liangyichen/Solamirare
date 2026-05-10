namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    #if DEBUG

    /// <summary>
    /// （GC警告）该功能仅为了方便调试阶段查看数据。
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        if (Pointer is null || UsageSize == 0 || !activated)
            return string.Empty;
        return AsSpan().ToString();
    }

    #endif

}