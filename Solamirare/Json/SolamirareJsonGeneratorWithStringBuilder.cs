


//基于span的算法暂时屏蔽不使用

/*
namespace Solamirare
{




    /// <summary>
    /// Solamirare 自实现 json 序列化 （通过 StringBuilder）(仅备用开发阶段使用，以后很大可能这个类会被删除)
    /// </summary>
    public class SolamirareJsonGeneratorWithStringBuilder    // : ITextSerializer  ，暂时不继承接口，因为该类的使用依赖过多
    {

        /// <summary>
        /// Solamirare 自实现 json 序列化 （通过 StringBuilder）
        /// </summary>
        public SolamirareJsonGeneratorWithStringBuilder()
        { 
        
        }

        StringBuilder string_build;

        /// <summary>
        /// 执行 Json 序列化（开发中，勿直接调用，非安全的指针操作方式，性能待改善）
        /// </summary>
        /// <param name="charsCount"></param>
        /// <param name="firstChar"></param>
        /// <param name="endChar"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        unsafe string serialize_by_point(int charsCount, char firstChar, char endChar, ref IEnumerable<KeyValuePair<string, string>> data)
        {

            

            string tempString = new string('\0', charsCount);

            ReadOnlySpan<char> chars = tempString;




           
            int index = 0;

            var i = data.GetEnumerator();

            fixed (char* p_result = chars)
            {
                p_result[index] = firstChar;
                p_result[charsCount - 1] = endChar;
                index += 1;



                if (i.MoveNext())
                {

                loop:


                    var spanKey = i.Current.Key.AsSpan();

                    var spanValue = i.Current.Value.AsSpan();



                    p_result[index] = '"';
                    index += 1;


                    fixed (char* p_key = spanKey)
                    {
                        
                        for (var n = 0; n < spanKey.Length; n++)
                        {

                            p_result[index] = p_key[n];
                            index += 1;
                        }
                    }

                    p_result[index] = '"';
                    index += 1;
                    p_result[index] = ':';
                    index += 1;
                    p_result[index] = '"';
                    index += 1;


                    fixed (char* p_value = spanValue)
                        for (var n = 0; n < spanValue.Length; n++)
                        {

                            p_result[index] = p_value[n];
                            index += 1;
                        }

                    p_result[index] = '\"';
                    index += 1;

                    if (i.MoveNext())
                    {
                        p_result[index] = ',';
                        index += 1;


                       
                        goto loop;
                    }

                }

            }


            return tempString;

        }

        /// <summary>
        /// 计算所有节点的字符串长度总和，每个节点的取值范围是："name":"value" ， 不包含多个节点之间的逗号数量。
        /// result[0] 表示 字符总和，result[1] 表示集合包含的数量
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        int[] procCharsLength(ref IEnumerable<KeyValuePair<string, string>> data)
        {

            //以下算法可以只需要组合一次字符串，一次已经是底限，因为是方法功能性质是构造 json ，不可能 0 次
            //如果使用字符串相加，需要 5 * collectionCount 段字符串碎片

            int collectionCount = 0;

            int charsCount = 0;

            var quotesCount = 0;

            int[] result = new int[2];

            foreach (var i in data) {

                collectionCount += 1;

                var spanKey = i.Key.AsSpan();

                var spanValue = i.Value.AsSpan();

                
                    quotesCount += spanKey.Count('"'); // key 如果遇到双引号，需要增加一个斜杠

                    quotesCount += spanValue.Count('"'); // value 如果遇到双引号，需要增加一个斜杠
                

                //以下的数字长度依次是：
                //双引号 + key的长度 + (双引号 + 逗号 + 双引号) + value的长度 + 双引号 
                charsCount += 1 + spanKey.Length + 3 + spanValue.Length + 1;
            }

            charsCount += quotesCount; // + key 和 value 的双引号数量总和


            if (collectionCount > 1) //只有节点数量大于1的情况才会需要逗号
            {
                int commaCount = collectionCount - 1; //节点数量减少1， 例如：两个节点之间有一个逗号、 五个节点之间有四个逗号

                charsCount += commaCount;
            }

            result[0] = charsCount += 2; //首尾两个 [ 或 {

            result[1] = quotesCount;


            return result;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="firstChar"></param>
        /// <param name="endChar"></param>
        /// <param name="data"></param>
        /// <param name="build"></param>
        /// <returns></returns>
        string serialize(char firstChar, char endChar, IEnumerable<KeyValuePair<string, string>> data, StringBuilder? build = null)
        {

            //以下算法可以只需要组合一次字符串，一次已经是底限，因为方法功能性质是构造 json ，不可能 0 次
            //如果使用字符串相加，需要 5 * collectionCount 段字符串碎片

            var proc = procCharsLength(ref data);

            int charsCount = proc[0];
            int quotesCount = proc[1];

            if (charsCount == 0) return firstChar.ToString() + endChar;

            if (quotesCount == 0) return serialize_by_point(charsCount, firstChar, endChar, ref data);


            if (string_build == null)
            {
                string_build = build;

                if (string_build == null)
                    string_build = new StringBuilder();
            }


            string_build.Capacity = charsCount;

            int loop = 0;

            var i = data.GetEnumerator();

            string_build.Append(firstChar);


            if (i.MoveNext())
            {

            loop:


                var spanKey = i.Current.Key.AsSpan();

                var spanValue = i.Current.Value.AsSpan();



                string_build.Append('"');


                for (var n = 0; n < spanKey.Length; n++)
                {
                    if (spanKey[n] == '"')
                    {
                        string_build.Append('\\');

                    }

                    string_build.Append(spanKey[n]);
                }


                string_build.Append('"');

                string_build.Append(':');

                string_build.Append('"');


                for (var n = 0; n < spanValue.Length; n++)
                {
                    if (spanValue[n] == '"')
                    {
                        string_build.Append('\\');

                    }

                    string_build.Append(spanValue[n]);
                }

                string_build.Append('\"');

                if (i.MoveNext())
                {
                    string_build.Append(',');

                    loop += 1;
                    goto loop;
                }

            }


            string_build.Append(endChar);
            var result = string_build.ToString();
            string_build.Clear();



            return result;

        }



        /// <summary>
        /// 序列化单一对象，输出的是对象格式，例如： { data各个节点 } 。data 的实际源通常是 Dictionary 等 K-V 类型
        /// </summary>
        /// <param name="data">数据源</param>
        /// <param name="build">如果有事先创建的 StringBuilder ,传进来可以复用，在高频场合最好是保证传入</param>
        /// <returns></returns>
        public string SerializeObject(IEnumerable<KeyValuePair<string, string>> data, StringBuilder? build = null)
        {
             return serialize('{','}',data,  build);
        }

        /// <summary>
        /// 序列化集合，输出的是集合格式，例如： [ data各个节点 ] 。data 的实际源通常是任意集合类型
        /// </summary>
        /// <param name="data">数据源</param>
        /// <param name="build">如果有事先创建的 StringBuilder ,传进来可以复用，在高频场合最好是保证传入</param>
        /// <returns></returns>
        public string SerializeCollection(IEnumerable<KeyValuePair<string, string>> data, StringBuilder? build = null)
        {
            return serialize('[', ']', data, build);

        }
    }
}

*/