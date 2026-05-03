namespace Solamirare;

/// <summary>
/// 内存分配结果
/// </summary>
public unsafe ref struct MemoryPollAllocatedResult
{
    /// <summary>
    /// 内存起始地址
    /// </summary>
    public byte* Address;

    /// <summary>
    /// 内存段的真实长度，单位是 Byte（有可能因为分配时存在内存取整等操作，真实分配值会与实际请求值不一样，所以真实值以这里为准）
    /// </summary>
    public uint BytesSize;

}