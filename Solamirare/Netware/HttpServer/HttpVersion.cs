namespace Solamirare;


/// <summary>
/// Http协议版本
/// </summary>
public enum HttpVersion : byte
{
    /// <summary>
    /// 1.0 或者不明版本（按照1.0处理）
    /// </summary>
    HTTP10 = 10,  // 1.0

    /// <summary>
    /// 1.1
    /// </summary>
    HTTP11 = 11,  // 1.1

    /// <summary>
    /// 2.0
    /// </summary>
    HTTP20 = 20,  // 2.0

    /// <summary>
    /// 3
    /// </summary>
    HTTP30 = 30,  // 3.0
}