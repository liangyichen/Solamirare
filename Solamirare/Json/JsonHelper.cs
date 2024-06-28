
namespace Solamirare;

/// <summary>
/// 
/// </summary>
public static class JsonHelper
{


    static unsafe bool TrueQuaro(char* q, int length)
    {
        int count = 0;
        for(int i = 0;i<length;i++)
        {
            if(q[-i]=='\\')
            {
                count += 1;
            }
            else
            {
                break;
            }
        }

        var isTrue = !(count % 2 == 0); //偶数是假引号，奇数是真引号,  真引号：\", 假引号：\\"
        

        return isTrue;
    }





    /// <summary>
    /// 判断双引号是否属于 json 本身的双引号
    /// </summary>
    /// <param name="string">字符串指针 (已经剔除最左边的2位字符: { 和 ")</param>
    /// <param name="index">当前查找到的引号所位于 @string 里的下标</param>
    /// <param name="length">@string的长度</param>
    /// <returns>告诉调用方，在 @string 中从0开始，取 length 位，然后调用方需要把 @string 从 slice 处裁剪，最后调用方根据 end 判断当前是否最后一次循环。</returns>
    public unsafe static (int length, int slice, bool end) SliceLength(char* @string, int index, int length)
    {
        //if(index==0) return (0,0,false); //第0位不可能是引号，会违反规范，因为实际就会变成 {""name":"my name"}

       

        char* p = &@string[index]; //此时 s[index] 本身代表当前查找到的引号本身
        
            //-1,-2 不需要判断，因为左边至少会有2个符号， 
            //例如原始字符串 {"name":"my name"}，实际传入的是：name":"my name"}
            var o1 = p[-1]; 

            
            if(p[+1]!='}') //<-- 不能判断 +2， 因为那个位置可能就是真超出数组了
            {
                if(o1 == '\\') //初步判断可能是假引号
                {
                    if(index>0)
                    {
                        var trueQuaro = TrueQuaro(p,length);
                        if(trueQuaro)//真引号
                        {
                            return (index,index,false);
                        }
                        else
                            return (-1,index,false); //假引号，跳过
                    
                    }
                    else
                    {
                        return (-2,0,false);
                    }
                }
                else // r":"123","Ct":"","rl":"128"}
                {
                    //#1
                    if(p[+1] == ':' && p[+2] == '"' && p[+3] == '"')
                    {
                        return (index,index+2,false); // ":"" 左边 ， +2的目的是下一次跳到 #2
                    }
                    
                    //#2
                    if(p[-1] == '"' && p[+1] == ',' && p[+2] == '"')
                    {
                        return (0,4,false); // ""," 中间
                    }

                    //#3
                    if((p[+1] == ':' || p[+1] == ',') && p[+2] == '"')
                    {
                        return (index,index+3,false); // ":" or "," 左边
                    }

                    //#4
                    if((p[-1] == ':' || p[+1] == ',') && p[-2] == '"') 
                    {
                        return (-1,1,false); // ":" or "," 右边
                    }   

                    return (index,1,false);
                    
                }
            }
            else
            {
                if(o1 == '\\') return (-1,1,false);  // \"} 肯定是假引号，因为这不符合 json 规范
                
                if(p[-1]=='"' && p[-2]==':')
                {
                    return (0,0,true); // :""} 右边引号
                }
                else
                {
                    return (index,0,true);
                }
            }
        
    
    }


    

}