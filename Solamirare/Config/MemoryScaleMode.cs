namespace Solamirare;

/// <summary>
/// 内存段扩容模式
/// </summary>
public enum MemoryScaleMode
{
    /// <summary>
    /// 二倍扩容，直到大于指定的容量
    /// </summary>
    X2 = 0,

    /// <summary>
    /// 指定容量（但是会保证不会小于原始容量）
    /// </summary>
    AppendEquals = 1,

    /// <summary>
    /// (原始容量 + 指定容量) + (原始容量 + 指定容量) / 2
    /// </summary>
    X3
}