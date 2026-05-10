namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{
    /// <summary>
    /// 重设集合的使用长度，仅对应到 UsageSize，不会改动 Capacity。
    /// <para>如果输入值大于真实长度，会被强制截断为真实长度。</para>
    /// </summary>
    /// <param name="length"></param>
    public void ReLength(uint length)
    {
        if (!activated) return;

        if (length > Capacity)
            Prototype.size = Capacity;
        else
            Prototype.size = length;
    }
}