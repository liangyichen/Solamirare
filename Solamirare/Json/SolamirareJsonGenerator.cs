
namespace Solamirare
{

    /// <summary>
    /// Solamirare 自实现 json 序列化
    /// </summary>
    public unsafe sealed class SolamirareJsonGenerator: ITextSerializer
    {

        static readonly char[] symbolsReplace;
        
        static readonly char[] pref;

        static SolamirareJsonGenerator()
        {
            symbolsReplace  = new char['\\' + 2];

            symbolsReplace['"' - 1] = '2';
            symbolsReplace['"' + 1] = '2';

            symbolsReplace['<' - 1] = '3';
            symbolsReplace['<' + 1] = 'C';

            symbolsReplace['\'' - 1] = '2';
            symbolsReplace['\'' + 1] = '7';

            pref = new char[4] { '\\', 'u', '0', '0' };
        }

        
        /// <summary>
        /// 序列化对象，输出 {"name":"my name",......} 形式
        /// </summary>
        /// <param name="data"></param>
        /// <param name="countIf"></param>
        /// <param name="searchSymbols"></param>
        /// <returns></returns>
        public string SerializeObject(IEnumerable<KeyValuePair<string, string>> data, int countIf = -1, bool searchSymbols = true)
        {

            if(searchSymbols)
                return serializeObjectRebuild(data, countIf);
            else
                return SerializeObjectsNative(data, countIf);

        }

        /// <summary>
        /// 序列化集合，输出 ["myname","my age",......] 形式
        /// </summary>
        /// <param name="data"></param>
        /// <param name="countIf">如果外部能够事先知晓集合的长度，传进来可以改善性能</param>
        /// <param name="searchSymbols"></param>
        /// <returns></returns>
        public string SerializeCollection(IEnumerable<string> data, int countIf = -1, bool searchSymbols = true)
        {

            if (searchSymbols)
                return serializeCollectionRebuild(data, countIf);
            else
                return SerializeCollectionNative(data, countIf);
        }



            
        unsafe string SerializeObjectsNative(IEnumerable<KeyValuePair<string, string>> data, int length = -1)
        {
            if (data is null) return "{}";

            int loopCount = 0;

            if (length <= 0)
            {
                if (data is Array)
                    loopCount = (data as KeyValuePair<string, string>[])!.Length; //数组的长度是天然可以获取的
                else
                    loopCount = data.Count(); //外部既不指定长度， 这边内部也检测到它不是数组，这一步就逃不脱了
            }
            else
            {
                loopCount = length; //外部已知长度
            }

            if (loopCount <= 0) return "{}";


            int* schema = stackalloc int[loopCount * 3];



            int charsCount = 0; //字符总数量

            foreach (var s in data)
            {

                *schema = s.Key.Length;
                schema += 1;

                *schema = s.Value.Length;
                schema += 1;

                *schema = charsCount + 1;
                schema += 1;

                // 2个3分别是 两边的双引号和右边再加一个结束符号( 右括号 或 逗号 )， 以及区分 key 和 value 的 两个双引号和一个冒号
                charsCount += s.Key.Length + 3 + s.Value.Length + 3;


            }


            // 强制限制使用的内存容量
            // 2000000 = 1000 * 1000 * 2 , 按照 unix 计算方法的 2M， 64位系统的栈内存是 4M，
            // 因为其它进程也需要使用栈内存，这里设定为最大只能使用一半
            // 不考虑32位系统

            if (charsCount > 2000000) return "{}";


            charsCount += 1; //左符号 { 或 [


            schema -= loopCount * 3; //把指向给复位回去，后面还需要通过自增的形式再次使用这个指针

            Span<char> resultString = stackalloc char[charsCount];

            fixed (char* p_result_chars = &resultString[0])
            {

                foreach (var sub in data)
                {
                    int subKeysCount = *schema;
                    schema += 1;

                    int subValuesCount = *schema;
                    schema += 1;

                    int subCharsIndex = *schema;
                    schema += 1;

                    char* p_result_chars_each = &p_result_chars[subCharsIndex];

                    *p_result_chars_each = '"';
                    p_result_chars_each += 1;


                    fixed (char* p_subString = sub.Key)
                    {
                        // *2 是因为 char 占用2个字节
                        Buffer.MemoryCopy(p_subString, p_result_chars_each, subKeysCount * 2, subKeysCount * 2);
                        p_result_chars_each += subKeysCount;
                    }

                    *p_result_chars_each = '"';
                    p_result_chars_each += 1;

                    *p_result_chars_each = ':';
                    p_result_chars_each += 1;

                    *p_result_chars_each = '"';
                    p_result_chars_each += 1;


                    fixed (char* p_subString = sub.Value)
                    {
                        // *2 是因为 char 占用2个字节
                        Buffer.MemoryCopy(p_subString, p_result_chars_each, subValuesCount * 2, subValuesCount * 2);
                        p_result_chars_each += subValuesCount;
                    }

                    *p_result_chars_each = '"';
                    p_result_chars_each += 1;

                    *p_result_chars_each = ',';
                }

                p_result_chars[0] = '{';
                p_result_chars[charsCount - 1] = '}';

            }

            return resultString.ToString();
        }


        unsafe string SerializeCollectionNative(IEnumerable<string> subStrings, int length = -1)
        {

            if (subStrings is null) return "[]";

            int loopCount = 0;

            if (length <= 0)
            {
                if (subStrings is Array)
                    loopCount = (subStrings as string[])!.Length; //数组的长度是天然可以获取的
                else
                    loopCount = subStrings.Count(); //外部既不指定长度， 这边内部也检测到它不是数组，这一步就逃不脱了
            }
            else
            {
                loopCount = length; //外部已知长度
            }

            if (loopCount <= 0) return "[]";


            int* schema = stackalloc int[loopCount*2];

        

            int charsCount = 0; //字符总数量

            foreach (var s in subStrings)
            {
            

                *schema = s.Length;
                schema += 1;

                *schema = charsCount + 1;
                schema += 1;

                // 3分别是 两边的双引号和右边再加一个结束符号( 右括号 或 逗号 )
                charsCount += s.Length + 3;

            
            }

            // 强制限制使用的内存容量
            // 2000000 = 1000 * 1000 * 2 , 按照 unix 计算方法的 2M， 64位系统的栈内存是 4M，
            // 因为其它进程也需要使用栈内存，这里设定为最大只能使用一半
            // 不考虑32位系统

            if (charsCount > 2000000) return "[]";


            charsCount += 1; //左符号 { 或 [


            schema -= loopCount * 2; //把指向给复位回去，后面还需要通过自增的形式再次使用这个指针

            Span<char> resultString = stackalloc char[charsCount];

            fixed (char* p_result_chars = &resultString[0])
            {

                foreach (var sub in subStrings)
                {
                    
                    fixed (char* p_subString = sub)
                    {
                        int subCharsCount = *schema;
                        schema += 1;
                        int subCharsIndex = *schema;
                        schema += 1;

                        char* p_result_chars_each = &p_result_chars[subCharsIndex];
                        
                        *p_result_chars_each = '"';
                        p_result_chars_each += 1;

                        // *2 是因为 char 占用2个字节
                        Buffer.MemoryCopy(p_subString, p_result_chars_each, subCharsCount * 2, subCharsCount * 2);
                        p_result_chars_each += subCharsCount;
                        

                        *p_result_chars_each = '"';
                        p_result_chars_each += 1;
                        *p_result_chars_each = ',';

                    }
                }

                p_result_chars[0] = '[';
                p_result_chars[charsCount - 1] = ']';

            }

            return resultString.ToString();

        }


        /// <summary>
        /// 构建单一对象
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <param name="symbolsDefaults"></param>
        /// <returns></returns>
        unsafe string serializeObjectRebuild(IEnumerable<KeyValuePair<string, string>> data, int length = -1, int symbolsDefaults = 96)
        {

            if (data is null) return "{}";

            int loopCount = 0;

            if (length <= 0)
            {
                // AOT 可以执行 is 和 as 
                if (data is Array)
                    loopCount = (data as KeyValuePair<string, string>[])!.Length; //数组的长度是天然可以获取的
                else
                    loopCount = data.Count(); //外部既不指定长度， 这边内部也检测到它不是数组，这一步就逃不脱了
            }
            else
            {
                loopCount = length; //外部已知长度
            }

            if (loopCount <= 0) return "{}";

            int setSymbolsDefaultsCount = symbolsDefaults;

            NodeSchema* keySchemas = stackalloc NodeSchema[loopCount];
            NodeSchema* ValueSchemas = stackalloc NodeSchema[loopCount];

            int* specialSymbolsIndexsOnKey;
            int* specialSymbolsIndexsOnValue;
            
            
            
        Restart:

            var subString_current = data.GetEnumerator();

            int schemaIndex = 0; //为下面的 foreach 循环建立一个下标

            int SpecialKeySymbolsCount = 0; //特殊符号总数量
            int SpecialValueSymbolsCount = 0;

            int charsCount = 0; //字符总数量, 起始值 1 是因为第 0 位作为左起始符号, { 或 [

            int* p = stackalloc int[setSymbolsDefaultsCount];
            int* p2 = stackalloc int[setSymbolsDefaultsCount];

            specialSymbolsIndexsOnKey = p;
            specialSymbolsIndexsOnValue = p2;

            while (subString_current.MoveNext())
            {

                int subKeyLength = subString_current.Current.Key.Length;

                int subValueLength = subString_current.Current.Value.Length;

                int subKeyNewLength = subKeyLength;

                int subValueNewLength = subValueLength;


                ReadOnlySpan<char> span_key_sub = subString_current.Current.Key;
                ReadOnlySpan<char> span_value_sub = subString_current.Current.Value;



                int special_chars_count_on_key = 0;

                for (int i = 0; i < subKeyLength; i++)
                {
                    char k = span_key_sub[i];
                    

                    if (k == 39 || k == 34 || k == 60 || k == 10  || k == 13 || k == 9 ||  k == 92)
                    {

                        if (SpecialKeySymbolsCount > setSymbolsDefaultsCount)
                        {
                            subString_current.Reset();
                            setSymbolsDefaultsCount = setSymbolsDefaultsCount * 2;
                            keySchemas -= schemaIndex;
                            ValueSchemas -= schemaIndex;
                            goto Restart;
                        }


                        special_chars_count_on_key += 1;
                        SpecialKeySymbolsCount += 1;

                        *specialSymbolsIndexsOnKey = i;
                        specialSymbolsIndexsOnKey += 1;

                        subKeyNewLength -= 1; //原始特殊字符要减去

                        if(k == 39 || k == 34 || k == 60)
                        {
                            //6是特殊符号的utf8表示字符串长度， 例如 \u0022 ，它们都是6位长度
                            subKeyNewLength += 6; //把新utf8字符串长度加进来
                        }
                        else
                        {
                            //2是特殊符号的转义字符串长度， 例如 \" ，是2位长度
                            subKeyNewLength += 2; //把新utf8字符串长度加进来
                        }
                        
                    }

                }


                int special_chars_count_on_value = 0;

                for (int i = 0; i < subValueLength; i++)
                {
                    char v = span_value_sub[i];
                
                    if (v == 39 || v == 34 || v == 60 || v == 10  || v == 13 || v == 9 ||  v == 92)
                    {

                        if (SpecialValueSymbolsCount > setSymbolsDefaultsCount)
                        {
                           
                            setSymbolsDefaultsCount = setSymbolsDefaultsCount * 2;
                            keySchemas -= schemaIndex;
                            ValueSchemas -= schemaIndex;
                            goto Restart;
                        }

                        special_chars_count_on_value += 1;
                        SpecialValueSymbolsCount += 1;

                        *specialSymbolsIndexsOnValue = i;
                        specialSymbolsIndexsOnValue += 1;

                        subValueNewLength -= 1; //原始特殊字符要减去

                        if(v == 39 || v == 34 || v == 60)
                        {
                            //6是特殊符号的utf8表示字符串长度， 例如 \u0022 ，它们都是6位长度
                            subValueNewLength += 6; //把新utf8字符串长度加进来
                        }
                        else
                        {
                            //2是特殊符号的转义字符串长度， 例如 \" ，是2位长度
                            subValueNewLength += 2; 
                        }
                    }

                }


                //===============================================

                keySchemas->SpecialSymbolsCount = special_chars_count_on_key;

                //是否存在特殊字符
                keySchemas->ExistSpecialSymbols = special_chars_count_on_key > 0;

                keySchemas->subSourceLength = subKeyLength;

                // 往后循环累加下方的 charsCount 新值
                keySchemas->indexEach = charsCount + 1;


                //================================================


                ValueSchemas->SpecialSymbolsCount = special_chars_count_on_value;

                //是否存在特殊字符
                ValueSchemas->ExistSpecialSymbols = special_chars_count_on_value > 0;

                ValueSchemas->subSourceLength = subValueLength;

                // 往后循环累加下方的 charsCount 新值
                ValueSchemas->indexEach = keySchemas->indexEach + subKeyNewLength +3;

                //===============================================


                // 2个3分别是 两边的双引号和右边再加一个结束符号( 右括号 或 逗号 ) 以及 key 和 value 之间的两个双引号以及一个冒号
                charsCount += subKeyNewLength + subValueNewLength + 3 + 3;

                keySchemas += 1;
                ValueSchemas += 1;
                schemaIndex += 1;
                
            }

            // 强制限制使用的内存容量
            // 2000000 = 1000 * 1000 * 2 , 按照 unix 计算方法的 2M， 64位系统的栈内存是 4M，
            // 因为其它进程也需要使用栈内存，这里设定为最大只能使用一半，否则会造成整个进程崩溃
            // 不考虑32位系统

            if (charsCount > 2000000) return "{}";

            charsCount += 1; //左符号 { 或 [

            specialSymbolsIndexsOnKey -= SpecialKeySymbolsCount; //把指向给复位回去，后面还需要通过自增的形式再次使用这个指针
            specialSymbolsIndexsOnValue -= SpecialValueSymbolsCount;

            keySchemas -= schemaIndex; //把指向给复位回去，后面还需要通过自增的形式再次使用这个指针
            ValueSchemas -= schemaIndex;

            Span<char> resultString = stackalloc char[charsCount];

            fixed (char* p_result_chars = &resultString[0])
            {

                int sub_strings_loop_index = 0;

                foreach (var sub in data)
                {
                    NodeSchema* keySchema = &keySchemas[sub_strings_loop_index];

                    NodeSchema* valueSchema = &ValueSchemas[sub_strings_loop_index];

                    delegate*<char*, char*, int*, NodeSchema*, Span<char>, char, int*> d_process = &processSchema;

                    fixed (char* p_subKey = sub.Key)
                    {
                        specialSymbolsIndexsOnKey = d_process(p_subKey, p_result_chars, specialSymbolsIndexsOnKey, keySchema, symbolsReplace, ':');
                    }

                    fixed (char* p_subValue = sub.Value)
                    {
                        specialSymbolsIndexsOnValue = d_process(p_subValue, p_result_chars, specialSymbolsIndexsOnValue, valueSchema, symbolsReplace, ',');

                    }


                    sub_strings_loop_index += 1;
                }

                p_result_chars[0] = '{';
                p_result_chars[charsCount - 1] = '}';

            }

            return resultString.ToString();

        }


        
        static int* processSchema(char* subString, char* p_result_chars, int* specialSymbolsIndexs, NodeSchema* schema, Span<char> symbolsReplace, char endChar)
        {

            schema->SubString = subString;

                char* p_result_chars_each = &p_result_chars[schema->indexEach];

                *p_result_chars_each = '"';
                p_result_chars_each += 1;


                if (!schema->ExistSpecialSymbols)
                {
                    // *2 是因为 char 占用2个字节
                    Buffer.MemoryCopy(schema->SubString, p_result_chars_each, schema->subSourceLength * 2, schema->subSourceLength * 2);
                    p_result_chars_each += schema->subSourceLength;
                    schema->SubString += schema->subSourceLength;
                }
                else
                {

                    
                    int subStringOddCount = 0; //统计已经处理多少个字符

                    for (int i = 0; i < schema->SpecialSymbolsCount; i++)
                    {
                        //如果特殊字符之前存在正常字符，把它们一次性处理完毕
                        int slice_unsearch = *specialSymbolsIndexs - subStringOddCount; //how much length to the next special symbol
                        specialSymbolsIndexs += 1;


                        if (slice_unsearch > 0)
                        {
                        
                            Buffer.MemoryCopy(schema->SubString, p_result_chars_each, slice_unsearch * 2, slice_unsearch * 2);
                        
                            
                        
                            p_result_chars_each += slice_unsearch; //上面复制了多少字符，主结果指针跟着移动
                            schema->SubString += slice_unsearch; //当前段落字符串指针也跟着移动（没关系，这时候只是原始字符串）
                            subStringOddCount += slice_unsearch;//复制正常字符完毕
                        }

                        // < ' "
                        if(*schema->SubString == 34 || *schema->SubString == 39 || *schema->SubString == 60)
                        {
                            fixed(char* p_pref = pref)
                            Buffer.MemoryCopy(p_pref, p_result_chars_each, 8, 8);   //4个char, 每个char占用2个字节
                            p_result_chars_each += 4;

                            *p_result_chars_each = symbolsReplace[*schema->SubString - 1];

                            p_result_chars_each += 1;

                            *p_result_chars_each = symbolsReplace[*schema->SubString + 1];

                            p_result_chars_each += 1;


                            schema->SubString += 1;
                            subStringOddCount += 1;
                        }
                        //  10:\n  13:\r  9:\t  92:\\
                        else if( *schema->SubString == 10 ||  *schema->SubString == 13 || *schema->SubString == 9 ||   *schema->SubString == 92)
                        {
                            
                            *p_result_chars_each = '\\';
                            p_result_chars_each += 1;

                            switch (*schema->SubString)
                            {
                                case '\n':
                                    *p_result_chars_each = 'n';
                                break;
                                case '\r':
                                    *p_result_chars_each = 'r';
                                break;
                                case '\t':
                                    *p_result_chars_each = 't';
                                break;
                                case '\\':
                                    *p_result_chars_each = '\\';
                                break;
                            }
                            p_result_chars_each += 1;
                            


                            schema->SubString += 1;
                            subStringOddCount += 1;
                        }
                    }

                    //在处理完所有特殊字符后，还剩余多少正常字符未处理
                    var least = schema->subSourceLength - subStringOddCount;
                    if (least > 0)
                    {
                        //最后剩余未处理的字符
                        Buffer.MemoryCopy(schema->SubString, p_result_chars_each, least * 2, least * 2);
                        p_result_chars_each += least; //上面复制了多少字符，主结果指针跟着移动
                        schema->SubString += least; //当前段落字符串指针也跟着移动（没关系，这时候只是原始字符串）
                        subStringOddCount += least;//复制正常字符完毕
                    }

                }

                *p_result_chars_each = '"';
                p_result_chars_each += 1;
                *p_result_chars_each = endChar;
                
                p_result_chars_each += 1;

            return specialSymbolsIndexs;

        }


        /// <summary>
        /// 构建集合
        /// </summary>
        /// <param name="subStrings"></param>
        /// <param name="length"></param>
        /// <param name="symbolsDefaults"></param>
        /// <returns></returns>
        unsafe string serializeCollectionRebuild(IEnumerable<string> subStrings, int length = -1, int symbolsDefaults = 96)
        {
            
            if (subStrings is null) return "[]";

            int loopCount = 0;

            if (length <= 0)
            {
                // AOT 可以执行 is 和 as 
                if (subStrings is Array)
                    loopCount = (subStrings as string[])!.Length; //数组的长度是天然可以获取的
                else
                    loopCount = subStrings.Count(); //外部既不指定长度， 这边内部也检测到它不是数组，这一步就逃不脱了
            }
            else
            { 
                loopCount = length; //外部已知长度
            }

            if(loopCount <= 0) return "[]";

            int setSymbolsDefaultsCount = symbolsDefaults;
            NodeSchema* schema = stackalloc NodeSchema[loopCount];
            int* specialSymbolsIndexs;
            
            
            




        Restart:

            var subString_current = subStrings.GetEnumerator();

            int schemaIndex = 0; //为下面的 foreach 循环建立一个下标

            int SpecialSymbolsCount = 0; //特殊符号总数量

            int charsCount = 0; //字符总数量, 起始值 1 是因为第 0 位作为左起始符号, { 或 [

            int* p = stackalloc int[setSymbolsDefaultsCount];

            specialSymbolsIndexs = p;
            

            while (subString_current.MoveNext())
            {

                int subLength = subString_current.Current.Length;
                int subStringNewLength = subLength;
                ReadOnlySpan<char> span_sub = subString_current.Current;



                int special_chars_count_on_current_line = 0;

                for (int i = 0; i < subLength; i++) 
                {
                    char c = span_sub[i];
                    
                    // < ' "
                    if (c == 39 || c == 34 || c == 60 || c == 10 || c == 13 || c == 9 ||  c == 92)
                    {

                        if (SpecialSymbolsCount > setSymbolsDefaultsCount)
                        {
                            //subString_current.Reset();
                            setSymbolsDefaultsCount = setSymbolsDefaultsCount * 2;
                            schema -= schemaIndex;
                            goto Restart;
                        }
                        
                        special_chars_count_on_current_line += 1;
                        SpecialSymbolsCount += 1;

                        *specialSymbolsIndexs = i;
                        specialSymbolsIndexs += 1;

                        subStringNewLength -= 1; //原始特殊字符要减去

                        if(c == 39 || c == 34 || c == 60)
                        {
                            //6是特殊符号的utf8表示字符串长度， 例如 \u0022 ，它们都是6位长度
                            subStringNewLength += 6; //把新utf8字符串长度加进来
                        }
                        else
                        {
                            //2是特殊符号的转义字符串长度， 例如 \" ，是2位长度
                            subStringNewLength += 2;
                        }
                    }

                }


                schema->SpecialSymbolsCount = special_chars_count_on_current_line;


                //是否存在特殊字符
                schema->ExistSpecialSymbols = special_chars_count_on_current_line > 0;
                
    


                schema->subSourceLength = subLength;

                // 往后循环累加下方的 charsCount 新值
                schema->indexEach = charsCount + 1;

                // 3分别是 两边的双引号和右边再加一个结束符号( 右括号 或 逗号 )
                charsCount += subStringNewLength + 3;

                schema += 1;
                schemaIndex += 1;
            }



            // 强制限制使用的内存容量
            // 2000000 = 1000 * 1000 * 2 , 按照 unix 计算方法的 2M， 64位系统的栈内存是 4M，
            // 因为其它进程也需要使用栈内存，这里设定为最大只能使用一半，否则会造成整个进程崩溃
            // 不考虑32位系统

            if (charsCount > 2000000) return "[]";

            charsCount += 1; //左符号 { 或 [

            specialSymbolsIndexs -= SpecialSymbolsCount; //把指向给复位回去，后面还需要通过自增的形式再次使用这个指针
            schema -= schemaIndex; //把指向给复位回去，后面还需要通过自增的形式再次使用这个指针

            Span<char> resultString = stackalloc char[charsCount];


            delegate*<char*,char*, int*, NodeSchema*, Span<char>, char, int*> d_process = &processSchema;


            fixed (char* p_result_chars = &resultString[0])
            {
                
                    
                int sub_strings_loop_index = 0;

                foreach (var sub in subStrings)
                {
                    NodeSchema* subSchema = &schema[sub_strings_loop_index];

                    fixed (char* p_subString = sub)
                    {
                        specialSymbolsIndexs  = d_process(p_subString, p_result_chars, specialSymbolsIndexs, subSchema, symbolsReplace, ',');
                    }

                    sub_strings_loop_index += 1;
                }

                p_result_chars[0] = '[';
                p_result_chars[charsCount - 1] = ']';

            }

            return resultString.ToString();

        }
        
    }
}
