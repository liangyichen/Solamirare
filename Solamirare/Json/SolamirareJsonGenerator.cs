namespace Solamirare
{

    /// <summary>
    /// Solamirare 自实现 json 序列化
    /// </summary>
    public unsafe class SolamirareJsonGenerator: ITextSerializer
    {
        static FrozenDictionary<char,char[]> symbols;

        static char[] FailureChars;
        
        
        /// <summary>
        /// Solamirare 自实现 json 序列化
        /// </summary>
        static SolamirareJsonGenerator() { 

            var _symbols = new Dictionary<char, char[]>
            {
                { '"', ['2', '2'] },
                { '/', ['2', 'F'] },
                { '<', ['3', 'C'] },
                { '>', ['3', 'E'] },
                { '&', ['2', '6'] },
                { '\\', ['5', 'C'] },
                { '\n', ['0', 'A'] },
                { '\r', ['0', 'D'] },
                { '\t', ['0', '9'] },
                { '\f', ['0', 'C'] },
                { '\xA0', ['A', '0'] },
                { '\v', ['0', 'B'] }
            };

            symbols = _symbols.ToFrozenDictionary();
            FailureChars = symbols.Keys.ToArray();
        }
        


        static int NewLength(ReadOnlySpan<char> source)
        {
         
            int source_old_length = source.Length;

            for (int i = 0;i<FailureChars.Length;i++)
            {
                if(source.IndexOf(FailureChars[i]) > -1) 
                {
                    var failureChars_count = source.Count(FailureChars[i]);//当前遍历到的这个特殊符号的出现次数
                    source_old_length -= failureChars_count; //减去这些原始特殊字符的数量，因为它们将会被消失，所以要减去
                    

                    var new_str_chars_count = failureChars_count * 6; //6是特殊符号的utf8表示字符串长度， 例如 \u0022 ，它们都是6位长度
                    source_old_length += new_str_chars_count; //把计算得到的新utf8字符串长度加进来
                }
            }
            

            return source_old_length;
        }

        static int appendChar(char* p_source, Span<char> codeChars)
        {
            return PointerOperator.SetPointerValue(p_source, '\\','u','0','0',codeChars[0],codeChars[1]);
        }

      
      

        /// <summary>
        /// 序列化对象
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        string serializeObject(IEnumerable<KeyValuePair<string, string>> data)
        {
            //以下算法可以只需要组合 enumsLength 次 char 数组，
            //如果使用字符串相加，需要 5 * enumsLength 段字符串碎片

            if (data is null)
            {
                return "{}";
            }

            
            //这次计算必须做，好在 AOT 和 Jit Release 中依然在百万级别领先微软自带 json， 差异不大
            var enumsLength = data.Count();
            

            var schema = new int[enumsLength, 5];
            var schme_index = 0;
            int charsCount = 0;
            int quotesCount = 0;
            bool hasQuotes = false;


            foreach(var d in data)
            {

                var spanKey = d.Key.AsSpan();

                var spanValue = d.Value.AsSpan();

                //key中用于替换非法字符的新字符总数量 - 非法字符数量，（因为下面需要与key的原始长度相加）
                var keyNewLength = NewLength(spanKey);

                //value中用于替换非法字符的新字符总数量 - 非法字符数量，（因为下面需要与value的原始长度相加）
                var valueNewLength = NewLength(spanValue);

                //以下的数字长度依次是： 双引号 + key + key的非法字符数量 + (双引号 + 逗号 + 双引号) + value + value的非法字符数量 + 双引号 + 结束符号( 右括号 或 逗号 )
                var eachCount = 1 + keyNewLength + 3 + valueNewLength + 1 + 1;


                schema[schme_index, 0] = spanKey.Length; //key的原始长度
                schema[schme_index, 1] = spanValue.Length; //value的原始长度
                schema[schme_index, 2] = eachCount; //每段总长
                schema[schme_index, 3] = keyNewLength; //key的新长度
                schema[schme_index, 4] = valueNewLength; //value的新长度

                quotesCount += keyNewLength + valueNewLength;

                schme_index += 1;

                charsCount += eachCount;

            }
            

            if (charsCount < 1) 
            {
                return "{}";
            }


            charsCount += 1; //这个1是最左边的起始符号，[ 或 {

            hasQuotes = quotesCount > 0; //往后的操作是否需要查找双引号

            schme_index = 0;

            int loop = 0;





            var i = data.GetEnumerator();

            var loop_count_index = 0;

            int indexOfEnd = charsCount - 1; //最后一个字符的索引位置

            char* p_result = stackalloc char[charsCount];

            Span<char> p_FailureChars = FailureChars;

            p_result[0] = '{';
            p_result[indexOfEnd] = '}';
            

            while (i.MoveNext())
            {
                
                loop_count_index += loop == 0 ? 1 : schema[loop - 1, 2]; //后段备注：获取上一次循环的长度

                //前半段起始点，也是本次循环的起始点
                int each_key_index = loop_count_index;

                //后半段起始点： 本次循环的起始点 + 双引号 + key长度 + 双引号
                int each_last_index = loop_count_index + 1 + schema[schme_index, 3] + 1;




                //以下两个独立逻辑可以在两个线程中成功运作，并且得到正确结果
                //但是目前受制于创建独立线程以及等待线程完成的开销过大，造成性能损失 （Thread, ThreadPool, Parallels, Task 均不行）
                //如果能够解决系统常驻一个激活的独立空线程作为实时辅助参与进来的话，例如  task(()=>逻辑1) ，问题很可能就可以解决，性能再翻倍



                //----------  独立逻辑1  -----------
                



                    fixed (char* p_key = i.Current.Key)
                    {
                        char* p_result_key = &p_result[each_key_index];

                        

                        *p_result_key = '"';
                        p_result_key += 1;


                        for (var n = 0; n < schema[schme_index, 0]; n++)
                        {
                            char* p_keyChar = &p_key[n];
                            if (hasQuotes &&  p_FailureChars.IndexOf(*p_keyChar) > -1)
                            {
                                var nextIndex = appendChar(p_result_key, symbols[*p_keyChar]);
                                p_result_key += nextIndex;
                            }
                            else
                            {
                                *p_result_key = *p_keyChar;
                                p_result_key += 1;
                            }
                        }


                        *p_result_key = '"';
                    }
                
                //----------  独立逻辑1结束 --------



                //----------  独立逻辑2 -------------
                
                    fixed (char* p_value = i.Current.Value)
                    {

                        char* p_result_value = &p_result[each_last_index];

                        int nextIndex = PointerOperator.SetPointerValue(p_result_value,':','"');
                        p_result_value += nextIndex;
                        each_last_index += nextIndex;

                        for (var n = 0; n < schema[schme_index, 1]; n++)
                        {
                            char* p_valueChar = &p_value[n];

                            if (hasQuotes && p_FailureChars.IndexOf(*p_valueChar) > -1)
                            {
                                var newIndex = appendChar(p_result_value, symbols[*p_valueChar]);
                                p_result_value += newIndex;
                                each_last_index += newIndex;
                            }
                            else
                            {
                                *p_result_value = *p_valueChar;
                                p_result_value += 1;
                                each_last_index += 1;
                            }
                        }


                        *p_result_value = '\"';
                        p_result_value += 1;
                        each_last_index += 1;
                    }
                
                
                //----------  独立逻辑2结束 ---------



                //----------  本次 key - value 节点赋值结束 -----

                if (each_last_index < indexOfEnd)
                {
                    
                    p_result[each_last_index] = ',';
                    
                    each_last_index += 1;

                    schme_index += 1;

                    loop += 1;
                }

            }

            var result = new Span<char>(p_result,charsCount);
            
            return result.ToString();

        }


        /// <summary>
        /// 序列化单一对象，输出的是对象格式，例如： { data各个节点 } 。data 的实际源通常是 Dictionary 等 K-V 类型
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public string SerializeObject(IEnumerable<KeyValuePair<string, string>> data)
        {
            return serializeObject(data);
        }

        /// <summary>
        /// 序列化集合，输出的是集合格式，例如： [ data各个节点 ] 。data 的实际源通常是任意集合类型
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public string SerializeCollection(IEnumerable<KeyValuePair<string, string>> data)
        {
            return serializeObject(data);
        }
    }
}
