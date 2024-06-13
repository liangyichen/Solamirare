using System.Linq;

namespace Solamirare
{


    /// <summary>
    /// 字符串操作类
    /// </summary>
    /// <param name="useStringReplacewhen">在原始值小于该数字的情况下，enableDefaultReplace 为 true，并且查询值和新值长度不相等、替换次数仅一次的情况下，内部算法将使用原生 String.Replace，</param>
    /// <param name="enableDefaultReplace">现阶段为了更多测试算法的稳定性，暂把值设置默认为 false</param>
    public class IntellStringReplace(int useStringReplacewhen = 200, bool enableDefaultReplace = false) : IIntellStringReplace
    {
        /// <summary>
        /// 
        /// </summary>
        int useStringReplacewhen = useStringReplacewhen;




        /// <summary>
        /// 非等长字符串替换
        /// </summary>
        /// <param name="sourceString"></param>
        /// <param name="selectString"></param>
        /// <param name="newString"></param>
        unsafe void byDynamicLength(ref string sourceString, ref string selectString, ref string newString)
        {

            ReadOnlySpan<char> source_span = sourceString;

            ReadOnlySpan<char> select_span = selectString;

            int selectStringLength = select_span.Length;

            ReadOnlySpan<char> newTextSpan = newString;

            int source_length = source_span.Length;

            //-----  计算查找字符串的出现次数
            int select_count = source_span.Count(select_span);

            if (select_count <= 0) return;


            //检测到仅仅是简单替换，使用原生办法
            if (source_length < useStringReplacewhen && select_count >= 1 && enableDefaultReplace)
            {
                sourceString = sourceString.Replace(selectString, newString);
                return;
            }


            //------------------------------------------------

            //原始字符串删除所有查找对象后的剩余长度
            int oldLengthExcludeSelect = source_length - selectStringLength * select_count;

            //得到结果集合的长度
            int output_length = newTextSpan.Length * select_count + oldLengthExcludeSelect;

            char[] result = new char[output_length];

            

            int result_index = 0;

            var selectIndex = source_span.IndexOf(select_span);
            ReadOnlySpan<char> search_view = sourceString;

            
            
            //算法注解范例： 123_456_789_0 ，查找 _ , 替换为 AA

            //标记截取原始字符串的部分，用于记录关键词之前的部分， 此时 prev_End 指向第一个 _
            int prev_Start = 0, prev_End = selectIndex;


            

            fixed (char* p_result = result, p_source = sourceString, p_newTextSpan = newTextSpan)
            {
                begin:

                //复制非关键词部分 123
                for (; prev_Start < prev_End; prev_Start++)
                {
                    p_result[result_index] = p_source[prev_Start];
                        
                    result_index += 1;
                }
                //此时得到 123

                //下一个起始点是当前结束点+关键词长度，指向 4 的位置，值是 4
                prev_Start = prev_End + selectStringLength; 

                
                //复制替换第一个关键词
                for (int sp = 0; sp < newTextSpan.Length; sp++)
                {
                    p_result[result_index] = p_newTextSpan[sp];

                    result_index += 1;
                }
                //此时得到 123AA
                

                //后续尚未查找的部分 456_789_0 ，下次循环从这里面查找。 (下次循环会得到789_0)
                search_view = search_view.Slice(selectIndex + selectStringLength);

                //在尚未查找的部分中搜索关键字，得到 6 和 7 之间的 _ 的位置，3
                selectIndex = search_view.IndexOf(select_span);


                //剩余未查找部分从起始到新关键词的部分就是下一段非关键词部分
                //下一个结束点是原始字符串中的第 7 个字符
                prev_End = prev_Start + selectIndex;

                if (prev_End > prev_Start)
                {
                    goto begin;
                }

                //剩余的非关键词部分
                for (; prev_Start < source_length; prev_Start++)
                {
                    p_result[result_index] = p_source[prev_Start];
                    result_index += 1;
                }
                
            }


            sourceString = new string(result);

            return;
        }


       

        /// <summary>
        /// 高效字符串替换，已经假定查询值与新值等长，并且不做 null 判断。 
        /// </summary>
        /// <param name="sourceString"></param>
        /// <param name="selectString"></param>
        /// <param name="newString"></param>
        /// <param name="lengthOfSelectValue"></param>
        /// <returns></returns>
        public unsafe void UnsafeReplace(ref string sourceString, ref string selectString, ref string newString, int lengthOfSelectValue)
        {

            ReadOnlySpan<char> source_span = sourceString;

            ReadOnlySpan<char> selectSpan = selectString;


            //例如 ----ABB---- 中，需要把AB替换替换为 A, 预期的值是 ----AB----
            //第一次循环后会得到  ----AB---- ，第二次循环如果再次从头部搜索的话，
            //最终值会变成 ----A---- 
            //使用分段查找视图，每次循环的起始点会逐渐向后移动，一来可以避免以上回文问题，二是避免不必要的重复搜索
            //特此建造该视图，记录每次需要搜索的分段
            ReadOnlySpan<char> subStringForSource = source_span;

            int checkPoint = -1;

            fixed (char* p_source = sourceString, p_newString = newString)
            {

            begin:

                checkPoint = subStringForSource.IndexOf(selectSpan);

                if (checkPoint > -1)
                {
                    for (int sp = 0; sp < lengthOfSelectValue; sp++)
                    {
                        p_source[checkPoint] = p_newString[sp];

                        checkPoint += 1;
                    }

                    //在此重设查询视图的指向片段，下次不会做多余搜索，也会避免二次替换问题
                    subStringForSource = subStringForSource.Slice(checkPoint);

                    goto begin;
                }

            }


        }



        /// <summary>
        /// 高效字符串替换，在查询值与新值等长的情况下可以实现 0 分配内存， 如果两个输入值不等长，在做连续大量替换的时候也能发挥高性能。
        /// </summary>
        /// <param name="sourceString"></param>
        /// <param name="selectString"></param>
        /// <param name="newString"></param>
        /// <returns></returns>
        public unsafe void Replace(ref string sourceString, string selectString, string newString)
        {
            if (string.IsNullOrWhiteSpace(selectString)) return;

            if (newString is null) return;

            int selectStringLength = selectString.Length;

            int newStringLength = newString.Length;



            //如果查询值与新值长度不一致，会强制转换算法
            if (selectStringLength != newStringLength)
            {
                byDynamicLength(ref sourceString, ref selectString, ref newString);
            }
            else
            {
                UnsafeReplace(ref sourceString, ref selectString, ref newString, selectStringLength);
            }


        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceString"></param>
        /// <param name="inputs"></param>
        public unsafe void Replaces(ref string sourceString, (string tag, string value)[] inputs)
        {
            foreach (var input in inputs)
            {
                Replace(ref sourceString, input.tag, input.value);
            }
        }






    }
}
