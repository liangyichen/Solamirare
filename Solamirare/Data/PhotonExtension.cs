namespace Solamirare;


/// <summary>
/// Photon 的功能扩展
/// </summary>
public static class PhotonExtension
{
    


    public static Photon ImportFromJsonString(this Photon photon, in string json)
    {
       var span_json = json.AsSpan(2,json.Length-2);
       
        int length = span_json.Length;

        Begin:

            var endOfKey = span_json.IndexOf("\":\"");

            if(endOfKey<0) goto RETURN;

            var key = span_json.Slice(0,endOfKey);
            
            
            ReadOnlySpan<char> value;
            var endOfValue = span_json.IndexOf("\",\"");
            if(endOfValue>-1)
            {
                value = span_json.Slice(endOfKey+3,endOfValue-(endOfKey+3));
                span_json = span_json.Slice(endOfValue+3);
            }
            else
            {
                endOfValue = span_json.IndexOf("\"}");
                value = span_json.Slice(endOfKey+3,endOfValue-(endOfKey+3));
                span_json = span_json.Slice(endOfValue+2);
            }

            
            photon.AppendIfNotExist(PhotonExtension.safeSymbols(key,key.Length), PhotonExtension.safeSymbols(value,value.Length));

            goto Begin;

        RETURN:


        return photon;
    }



    /// <summary>
    /// 把 json 字符串中由两个字符标识的 \n , \r , \t, \", \\ 还原成单一字符
    /// </summary>
    /// <param name="input"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static unsafe string safeSymbols(ReadOnlySpan<char> input, int length)
    {
        var tempArray = input.ToArray();
       

        int symbol_slice_odd = 0; //如果执行了清洗，最末端减去多少个字符
        fixed(char* p_json = tempArray)
        {
            
            
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
                        //Buffer.MemoryCopy(&p_json[i], &p_json[i-1], (length-1) * 2, (length-1) * 2);

                        for(int n = i;n<length;n++)
                        {
                            p_json[n-1] = p_json[n];
                        }
                        
                        symbol_slice_odd += 1;
                    }
                }
            }


            var resultArray = new Span<char>(p_json,length-symbol_slice_odd);
            var resultString = resultArray.ToString();
            return resultString;
        }
        
    }


    // /// <summary>
    // /// 把 json 字符串中由两个字符标识的 \n , \r , \t, \", \\ 还原成单一字符
    // /// </summary>
    // /// <param name="input"></param>
    // /// <param name="length"></param>
    // /// <returns></returns>
    // public static unsafe string safeSymbols(char* input, int length)
    // {
    //     Span<char> resultArray = new char[length]; //这里不能用 stackalloc ，500字左右就会导致栈内存容纳不下

 
        

    //     int symbol_slice_odd = 0; //如果执行了清洗，最末端减去多少个字符
    //     fixed(char* p_json = resultArray)
    //     {
            
    //         Buffer.MemoryCopy(input, p_json, length * 2, length * 2);
    //         for(int i = 1;i<length;i++)
    //         {
    //             var c = p_json[i];
    //             var o1 = p_json[i-1];
    //             if(o1 == '\\')
    //             {
    //                 var con1 = i>1 && (c=='n' || c=='r' || c=='t' || c == '"' || c == '\\') && p_json[i - 1] == '\\' && p_json[i - 2] != '\\';
    //                 if(!con1) con1 = i==1 && (c=='n' || c=='r' || c=='t' || c == '"' || c == '\\') && p_json[i - 1] == '\\';
    //                 if(!con1) con1 = i==0 && (c=='n' || c=='r' || c=='t' || c == '"' || c == '\\');

    //                 if(con1)
    //                 {
    //                     switch(p_json[i])
    //                     {
    //                         case 'n':
    //                             p_json[i] = '\n';
    //                         break;
    //                         case 'r':
    //                             p_json[i] = '\r';
    //                         break;
    //                         case 't':
    //                             p_json[i] = '\t';
    //                         break;
    //                     }
                    
    //                     //如果 \ 和 ", 直接就左移了，不需要上面再判断赋值
    //                     Buffer.MemoryCopy(&p_json[i], &p_json[i-1], (length-1) * 2, (length-1) * 2);
                        
    //                     symbol_slice_odd += 1;
    //                 }
    //             }
    //         }
    //     }

    //     resultArray = resultArray.Slice(0,length-symbol_slice_odd);
    //     var resultString = resultArray.ToString();

    //     return resultString;
    // }
}





