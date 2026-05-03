namespace Solamirare;


/// <summary>
/// 内存块信息，用户端
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe ref struct BaseMemoryPoolUserBlockInfo
{

    /// <summary>
    /// 用户可用内存大小
    /// </summary>
    public ulong SizeOnUsage;

    /// <summary>
    /// 遍历时的序号
    /// </summary>
    public uint Index;

    /// <summary>
    /// 内存块是否为空闲状态
    /// </summary>
    public bool IsFree;

    fixed byte padding[3]; //填充到16字节
}
