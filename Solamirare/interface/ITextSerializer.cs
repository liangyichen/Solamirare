
namespace Solamirare
{
    /// <summary>
    /// 字符串序列化接口
    /// </summary>
    public unsafe interface ITextSerializer
    {



        /// <summary>
        /// 序列化单一对象，输出的是对象格式，例如： { data各个节点 } 。data 的实际源通常是 Dictionary 等 K-V 类型
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        string SerializeObject(IEnumerable<KeyValuePair<string, string>> data);

        /// <summary>
        /// 序列化集合，输出的是集合格式，例如： [ data各个节点 ] 。data 的实际源通常是任意集合类型
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        string SerializeCollection(IEnumerable<KeyValuePair<string, string>> data);

    }
}