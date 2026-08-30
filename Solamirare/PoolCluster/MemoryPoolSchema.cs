namespace Solamirare;

/// <summary>
/// 描述一个子内存池的规格：每个块的字节长度与块的总数量。
/// <para>
/// 注意：该结构体的大小固定为 8 字节，参与内存布局计算，不可更改。
/// </para>
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
public struct MemoryPoolSchema
{
    /// <summary>每个内存块的字节长度。</summary>
    public uint NodeSize;

    /// <summary>该子池中内存块的总数量。</summary>
    public uint Count;

    /// <summary>
    /// 创建一个内存池规格描述。
    /// </summary>
    public MemoryPoolSchema(uint nodeSize, uint count)
    {
        NodeSize = nodeSize;
        Count = count;
    }
}