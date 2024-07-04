

namespace Solamirare;




/// <summary>
/// 为AOT反序列化定制的操作结果
/// </summary>
public class DynamicResult : ExecuteResultBase
{
    /// <summary>
    /// 为AOT反序列化定制的操作结果
    /// </summary>
    public DynamicResult()
    {
        Core = new ExpandoObject();
    }

    /// <summary>
    /// 数据核心
    /// </summary>
    public dynamic Core {get;set;}
    
}