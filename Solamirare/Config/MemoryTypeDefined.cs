namespace Solamirare;

/// <summary>
/// 手动标识内存类型，属于栈或堆
/// </summary>
public enum MemoryTypeDefined : byte
{
    /// <summary>
    /// 未知
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 栈内存
    /// </summary>
    Stack = 1,

    /// <summary>
    /// 堆内存
    /// </summary>
    Heap = 2,
}