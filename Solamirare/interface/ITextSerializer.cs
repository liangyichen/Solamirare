
namespace Solamirare
{
    /// <summary>
    /// 字符串序列化接口
    /// </summary>
    public unsafe interface ITextSerializer
    {



        /// <summary>
        /// 序列化单一对象，输出的是对象格式，例如： { "name":"my name", ...... } 。data 的实际源通常是 Dictionary 等 K-V 类型
        /// </summary>
        /// <param name="data"></param>
        /// <param name="countIf">如果外部能够事先知晓集合的长度，传进来可以改善性能</param>
        /// <param name="searchSymbols"></param>
        /// <returns></returns>
        string SerializeObject(IEnumerable<KeyValuePair<string, string>> data, int countIf = -1, bool searchSymbols = true);

        /// <summary>
        /// 序列化集合，输出的是集合格式，例如： [ "item1","item2"...... ] 。data 的实际源通常是任意集合类型
        /// </summary>
        /// <param name="data"></param>
        /// <param name="countIf">如果外部能够事先知晓集合的长度，传进来可以改善性能</param>
        /// <param name="searchSymbols"></param>
        /// <returns></returns>
        string SerializeCollection(IEnumerable<string> data, int countIf = -1, bool searchSymbols = true);
        

    }
}