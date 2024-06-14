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
        

        /// <summary>
        /// 重新计算字符串在进行特殊符号重写后的所需长度
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
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


        public string SerializeObject(IEnumerable<KeyValuePair<string, string>> data, int countIf = -1)



        /// <summary>
        /// 序列化集合
        /// </summary>
        /// <param name="data"></param>
        /// <param name="countIf">如果外部能够事先知晓集合的长度，传进来可以改善性能</param>
        /// <returns></returns>
        public string SerializeCollection(IEnumerable<string> data, int countIf = -1)
        {

            if (data is null)
            {
                return "[]";
            }


            //这次计算必须做，好在测试后发现 AOT 和 Jit Release 差异不大, 都能在百万级别领先微软自带 json，
            var enumsLength = countIf > 0? countIf: data.Count();

            if (enumsLength <= 0) return "[]";


            var schema = new int[enumsLength, 2];
            var schme_index = 0;
            int charsCount = 0;


            foreach (var d in data)
            {

                var spanKey = d.AsSpan();

                //key中用于替换非法字符的新字符总数量 - 非法字符数量，（因为下面需要与key的原始长度相加）
                var keyNewLength = NewLength(spanKey);


                //以下的数字长度依次是： 双引号 + key + 双引号 + 结束符号( 右括号 或 逗号 )
                var eachCount = 1 + keyNewLength  + 1 + 1;


                schema[schme_index, 0] = spanKey.Length; //key的原始长度
                schema[schme_index, 1] = keyNewLength; //key的新长度

               

                schme_index += 1;

                charsCount += eachCount;

            }


            if (charsCount < 1)
            {
                return "[]";
            }


            charsCount += 1; //这个1是最左边的起始符号，[

            schme_index = 0;

            int loop = 0;

            var i = data.GetEnumerator();

            var loop_count_index = 0;

            int indexOfEnd = charsCount - 1; //最后一个字符的索引位置

            char* p_result = stackalloc char[charsCount];

            Span<char> p_FailureChars = FailureChars;

            p_result[0] = '[';
            p_result[indexOfEnd] = ']';


            while (i.MoveNext())
            {

                loop_count_index += loop == 0 ? 1 : schema[loop - 1,1]; //后段备注：获取上一次循环的长度


                //起始点： 本次循环的起始点
                int each_last_index = loop_count_index;



                fixed (char* p_value = i.Current)
                {

                    char* p_result_value = &p_result[each_last_index];

                    *p_result_value = '"';
                    p_result_value += 1;
                    each_last_index += 1;


                    if (schema[schme_index,1] > schema[schme_index, 0])
                    {
                        for (var n = 0; n < schema[schme_index, 0]; n++)
                        {
                            char* p_valueChar = &p_value[n];

                            if (p_FailureChars.IndexOf(*p_valueChar) > -1)
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
                    }
                    else
                    {
                        for (var n = 0; n < schema[schme_index, 0]; n++)
                        {
                            char* p_valueChar = &p_value[n];

                            *p_result_value = *p_valueChar;
                            p_result_value += 1;
                            each_last_index += 1;
                        }
                    }


                    *p_result_value = '\"';
                    p_result_value += 1;
                    each_last_index += 1;
                }


                //----------  本次 key - value 节点赋值结束 -----

                if (each_last_index < indexOfEnd)
                {

                    p_result[each_last_index] = ',';

                    each_last_index += 1;

                    schme_index += 1;

                    loop += 1;
                }

            }

            var result = new Span<char>(p_result, charsCount);

            return result.ToString();
        }


        /// <summary>
        /// 序列化对象
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public char* serializLine(string line) 
        { 
            

            ReadOnlySpan<char> lineSpan = line;
            int sourceLength = line.Length;
            int tempLength = lineSpan.Length + 2;


            //结果长度与输入值的长度肯定不一样（最少也要加2），所以不能通过指针修改输入源， 必须做一次临时生成
            //所幸这是堆内存，会被立即释放
            char* temp = stackalloc char[tempLength]; //2是首尾两个双引号

            *temp = '"';
            temp += 1;

            fixed (char* p_line = &lineSpan[0])
            {
                for (int i = 0; i < sourceLength; i++)
                {
                    char* p_line_current = p_line;
                    
                    *temp = *p_line_current;
                    
                    p_line_current += 1;
                    temp += 1;
                }
            }

            *temp = '"';

            //var resultSpan = new Span<char>(temp, tempLength);
            //resultSpan.ToString();

            return temp;
        }


    }
}
