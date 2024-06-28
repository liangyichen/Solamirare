namespace Solamirare;


/// <summary>
/// Photon 的功能扩展
/// </summary>
public static class PhotonExtension
{
    /// <summary>
    /// 由 Json 字符串导入数据，key-value 可以任意，但是原则性的错误不要犯（例如null,""）,
    /// Json 字符串必须严格遵守 Json 规范，除了字符串本身内部之外，不允许单引号、不允许空格回车换行以及各种不可见字符,
    /// Json 的值必须是 string 类型，不支持 int,bool 等无引号的类型表示法，
    /// 例如这样是不允许的 {"enable":false} , {"age":10} 
    /// 必须以引号包围，以 string 的形态表示： {"enable":"false"} , {"age":"10"} 
    /// </summary>
    /// <param name="photon"></param>
    /// <param name="json"></param>
    /// <returns></returns>
    public static unsafe Photon ImportFromJsonString(this Photon photon, in string json)
    {
        if(string.IsNullOrWhiteSpace(json)) return photon;

        var span_json = json.AsSpan();
        


        span_json = span_json.Slice(2,json.Length-2); //跳过 {“
        int span_source_length = span_json.Length;
        bool isKey = true;
        ReadOnlySpan<char> keyString = ReadOnlySpan<char>.Empty;
        int keyLength = 0;
            
        int count = span_source_length;
        int n = 0;
        
        char* p_key = stackalloc char[1];
        
        fixed(char* p_span_json = span_json)
        {
            char* p_span = &p_span_json[0];

            while(true)
            {
                    
                if(p_span[n] == '"')
                {
                    (int length, int slice, bool end) sliceLength;

                    
                    sliceLength = JsonHelper.SliceLength(p_span,n,span_source_length);
                    
                    if(sliceLength.length > -1) //如果-1表示不处理，继续读下一个引号
                    {
                        var current = &p_span[0];
                        var current_length = sliceLength.length;

                        string view = new string(p_span,0,current_length);
                        
                        if(isKey)
                        {
                            p_key = current;
                            keyLength = current_length;
                            isKey = false;
                        }
                        else
                        {
                            
                            var k = safeSymbols(p_key,keyLength);
                            var v = safeSymbols(current,current_length);
                            
                            photon.AppendIfNotExist(k,v);
                            
                            isKey = true;
                        }
                        
                        
                        if(!sliceLength.end)
                        {
                            p_span += sliceLength.slice;
                            n = 0;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                n += 1;
                count -= 1;

                if(count<=0) break; //这是另一个防护条件，无论怎么循环都不可能超出原始字符串的长度，可以避免输入无引号的字符串引发无限循环
            }
        }  
        return photon;
    }


    /// <summary>
    /// 把 json 字符串中由两个字符标识的 \n , \r , \t, \", \\ 还原成单一字符
    /// </summary>
    /// <param name="input"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static unsafe string safeSymbols(char* input, int length)
    {
        Span<char> resultArray = new char[length]; //这里不能用 stackalloc ，500字左右就会导致栈内存容纳不下

 
        

        int symbol_slice_odd = 0; //如果执行了清洗，最末端减去多少个字符
        fixed(char* p_json = resultArray)
        {
            
            Buffer.MemoryCopy(input, p_json, length * 2, length * 2);
            for(int i = 1;i<length;i++)
            {
                var c = p_json[i];
                var o1 = p_json[i-1];
                if(o1 == '\\')
                {
                    var con1 = i>1 && (c=='n' || c=='r' || c=='t' || c == '"' || c == '\\') && p_json[i - 1] == '\\' && p_json[i - 2] != '\\';
                    if(!con1) con1 = i==1 && (c=='n' || c=='r' || c=='t' || c == '"' || c == '\\') && p_json[i - 1] == '\\';
                    if(!con1) con1 = i==0 && (c=='n' || c=='r' || c=='t' || c == '"' || c == '\\');

                    if(con1)
                    {
                        switch(p_json[i])
                        {
                            case 'n':
                                p_json[i] = '\n';
                            break;
                            case 'r':
                                p_json[i] = '\r';
                            break;
                            case 't':
                                p_json[i] = '\t';
                            break;
                        }
                    
                        //如果 \ 和 ", 直接就左移了，不需要上面再判断赋值
                        Buffer.MemoryCopy(&p_json[i], &p_json[i-1], (length-1) * 2, (length-1) * 2);
                        
                        symbol_slice_odd += 1;
                    }
                }
            }
        }

        resultArray = resultArray.Slice(0,length-symbol_slice_odd);
        var resultString = resultArray.ToString();

        return resultString;
    }
}