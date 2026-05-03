namespace Solamirare;


/// <summary>
/// 序列化执行结果状态
/// </summary>
public enum SerializeResultEnum : byte
{

    /// <summary>
    /// 执行成功
    /// </summary>
    OK = 0,

    /// <summary>
    /// 因为栈内存不可重设大小
    /// </summary>
    Failed_StackReSize = 1,

    /// <summary>
    /// 因为超出栈内存限制
    /// </summary>
    Failed_StackLimit = 2,

    /// <summary>
    /// 数据源是空值
    /// </summary>
    Null_Or_Empty = 3,

    /// <summary>
    /// 类型错误
    /// </summary>
    FailedTypes = 4,

    /// <summary>
    /// 初始化未定义
    /// </summary>
    UnDefined = 5
}