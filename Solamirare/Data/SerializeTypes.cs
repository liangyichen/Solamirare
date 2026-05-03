namespace Solamirare;

/// <summary>
/// 序列化类型
/// </summary>
public enum JsonSerializeTypes : byte
{

    /// <summary>
    /// 任意类型
    /// </summary>
    Any = 0,

    /// <summary>
    /// 数字
    /// </summary>
    Number = 1,

    /// <summary>
    /// 字符串
    /// </summary>
    String = 2,

    /// <summary>
    /// 布尔
    /// </summary>
    Boolean = 3,

    /// <summary>
    /// 对象
    /// </summary>
    Object = 4,

    /// <summary>
    /// 集合
    /// </summary>
    Array = 5,

    /// <summary>
    /// 空
    /// </summary>
    Null = 6,

    /// <summary>
    /// 未定义
    /// </summary>
    Undefined = 7

}


/// <summary>
/// 序列化类型通用库
/// </summary>
internal static class JsonSerializes
{
    /// <summary>
    /// 判断非托管类型
    /// </summary>
    /// <param name="tCode"></param>
    /// <returns></returns>
    internal static JsonSerializeTypes GetTypeCode(TypeCode tCode)
    {
        JsonSerializeTypes SerializeType;

        switch (tCode)
        {
            case TypeCode.Char:
                SerializeType = JsonSerializeTypes.String;
                break;

            case TypeCode.Boolean:

                SerializeType = JsonSerializeTypes.Boolean;

                break;

            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
                SerializeType = JsonSerializeTypes.Number;
                break;
                
            case TypeCode.String:
                SerializeType = JsonSerializeTypes.String;
                break;

            case TypeCode.DateTime:
                SerializeType = JsonSerializeTypes.String;
                break;

            default:
                SerializeType = JsonSerializeTypes.Undefined;
                break;
        }

        return SerializeType;
    }
}