

namespace Solamirare.Extention
{
    /// <summary>
    /// CLR功能扩展集合
    /// </summary>
    public static class Extension
    {
        static Regex getTagArray;

        static JsonSerializerOptions jsonSerializerOptions;

        /// <summary>
        /// 一个表示为逻辑空的时间值 (1970-01-01 00:00:00)
        /// </summary>
        public static DateTime EmptyDatetimeValue {get; private set;}

 
        static Extension()
        {
            getTagArray = new Regex(@",|\s|，|\|", RegexOptions.Compiled);


            EmptyDatetimeValue = DateTime.Parse("1970-01-01 00:00:00");

            jsonSerializerOptions =  new JsonSerializerOptions()
            {
                WriteIndented = true,

                Converters =
                {
                    new JsonStringEnumConverter()
                },

                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// 键值对类型导出到动态类型
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Photon ExportToDynamicData(this IEnumerable<KeyValuePair<string, string>> source)
        { 
        
            var obj = new Photon();

            obj.Import(source);

            return obj;
        }

        
        /// <summary>
        /// 如果包含其中任何一个字符
        /// </summary>
        /// <param name="source"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static bool EqualsByChar(this ReadOnlySpan<char> source, Span<char> values)
        {
            var result = false;
            if (values.Length<1) return false;

            

            for (var i = 0; i < values.Length; i++)
            {
                var c = values[i];
                if (source.Contains(c))
                { 
                    result = true;
                    break;
                }
            }

            return result;
        }


        /// <summary>
        /// 如果等同于其中任何一个值，（忽略大小写）
        /// </summary>
        /// <param name="source"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static bool EqualsBy(this ReadOnlySpan<char> source, params string[] values)
        {
            var result = false;

            for (var i = 0; i < values.Length; i++)
            {
                if (string.IsNullOrEmpty(values[i])) break;

                var nodeSpan = values[i].AsSpan();

                if (source.Equals(nodeSpan, StringComparison.OrdinalIgnoreCase))
                { 
                    result = true;
                    break;
                }
            }

            return result;
        }


        /// <summary>
        /// 如果以其中任何一个值作为起始（忽略大小写）
        /// </summary>
        /// <param name="source"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static bool StartWithBy(this ReadOnlySpan<char> source, params string[] values)
        {
            var result = false;

            for (var i = 0; i < values.Length; i++)
            {
                if (string.IsNullOrEmpty(values[i])) break;

                var nodeSpan = values[i].AsSpan();

                if (source.StartsWith(nodeSpan, StringComparison.OrdinalIgnoreCase))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }


        /// <summary>
        /// 字符串更新自身值（如果新值与旧值等长可以实现0创建）
        /// </summary>
        /// <param name="source"></param>
        /// <param name="select"></param>
        /// <param name="newValue"></param>
        /// <param name="intellStringReplace">如果之前已经有激活的操作对象就传进来</param>
        /// <returns>返回操作对象，方便复用</returns>
        public static IIntellStringReplace ReplaceUpdate(this string source, string select, string newValue, IIntellStringReplace intellStringReplace = null)
        {
            if (intellStringReplace == null) intellStringReplace = new IntellStringReplace();

            if (intellStringReplace is not null)
            { 
                intellStringReplace.Replace(ref source, select, newValue);
            }

            return intellStringReplace;
        }


        /// <summary>
        /// 控制台输出指定颜色信息
        /// </summary>
        /// <param name="string">信息正文</param>
        /// <param name="fontColor">文字颜色</param>
        /// <param name="bg">可选背景色，默认黑色</param>
        public static void ConsoleOutputByColor(string @string, ConsoleColor fontColor, ConsoleColor bg = ConsoleColor.Black)
        {

            Console.BackgroundColor = bg;

            Console.ForegroundColor = fontColor;

            if (@string is not null)
            {
                Console.WriteLine(@string);
            }
            else
            {
                Console.WriteLine("null");
            }

            Console.ResetColor();

        }


        /// <summary>
        /// 通过单个字符串，得到ASCII码，空值返回 0（如果字符数量大于1，则取第一个字符）
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int ASCII(this string str)
        {
            if (!string.IsNullOrWhiteSpace(str))
            {
                var array = Encoding.ASCII.GetBytes(str);
                return (short)(array[0]);
            }
            else
            {
                return 0;
            }
        }


        /// <summary>
        /// 把该对象打印输出到控制台
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="object"></param>
        /// <param name="fontColor"></param>
        /// <param name="bg"></param>
        /// <returns></returns>
        public static T PrintToConsole<T>(this T @object, ConsoleColor fontColor, ConsoleColor bg = ConsoleColor.Black)
        {
          
            if (@object is not null)
            {
               
                ConsoleOutputByColor(@object!.ToString()!, fontColor, bg);
            }
            else
            {

                ConsoleOutputByColor("null", fontColor, bg);
            }

            return @object!;
        }



        /// <summary>
        /// 把该对象打印输出到控制台（异步）, 应用场景是 web 或 桌面窗口环境， 控制台输出不要阻碍性能
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="object"></param>
        /// <param name="fontColor"></param>
        /// <param name="bg"></param>
        /// <returns></returns>
        public static T PrintToConsoleAsync<T>(this T @object, ConsoleColor fontColor, ConsoleColor bg = ConsoleColor.Black)
        {
            Task.Run(()=>{

                if (@object is not null)
                {
                    //如果出现可能为空的提示，纯属瞎掰，不管
                    ConsoleOutputByColor(@object!.ToString()!, fontColor, bg);
                }
                else
                {

                    ConsoleOutputByColor("null", fontColor, bg);
                }

            });

            return @object;
        }





        /// <summary>
        /// 选定基于单列的不重复值
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element))) { yield return element; }
            }
        }


        public static async Task AppendToFileAsync(this IEnumerable<string> sources, string path)
        {
            await GeneralApplication.CheckTextFileAsync(path, string.Empty);
            using (TextWriter writer = File.AppendText(path))  
            {  
                foreach (string source in sources)
                    await writer.WriteAsync(source);
            }  
        }

        public static async Task AppendToFileAsync(this string source, string path)
        {
            await GeneralApplication.CheckTextFileAsync(path, string.Empty);
            using (TextWriter writer = File.AppendText(path))  
            {  
                await writer.WriteAsync(source);
            }  
        }



        /// <summary>
        /// 将字符串保存到文件，如果文件已经存在，则覆盖，如果目录和文件不存在，则会首先创建再添加
        /// </summary>
        /// <param name="source"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task<ExecuteResultBase> SaveToFileAsync(this string source, string path)
        {
            ExecuteResultBase result = new ExecuteResultBase { };


            var logPathDir = Path.GetDirectoryName(path);

            GeneralApplication.CheckDir(logPathDir!);

            if (!string.IsNullOrEmpty(source))
            { 
                try
                {
                    await File.WriteAllTextAsync(path, source);
                }
                catch (Exception e)
                {
                    result.Message = e.ToString();
                }
            }
            else
            {
                //在此不同于创建文件，因为追加空字符串是没有意义的
                result.Message = ConstValues.Source_Can_Not_Be_Empty;
            }

            return result;
        }


        /// <summary>
        /// 转换到Unix时间值（Milliseconds）
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static long ToUnixTimeMilliseconds(this DateTime dateTime)
        {
            return ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds();
        }

 

        /// <summary>
        /// 读取指定路径的文字内容，如果文件不存在，则得到空字符串
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static async Task<string> ReadAsStringAsync(this string filePath)
        {
            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath);
            }
            else
            {
                return string.Empty;
            }
        }



        /// <summary>
        /// 将字符串集合追加保存到文件，如果目录和文件不存在，则会首先创建再添加
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task<ExecuteResultBase> AppendSaveToFileAsync(this IEnumerable<string> sources, string path)
        {
            ExecuteResultBase result = new();



            await GeneralApplication.CheckTextFileAsync(path, string.Empty);

            if (sources.Any())
            {
                try
                {
                    await File.AppendAllLinesAsync(path, sources,Encoding.UTF8);
                }
                catch (Exception e)
                {
                    result.Message = e.ToString();
                }
            }
            else
            {
                //空字符串就没有必要浪费一次IO操作了
                result.Message = ConstValues.Source_Can_Not_Be_Empty;
            }

            return result;
        }





        /// <summary>
        /// 序列化为json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="object"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static async Task<string> SerializeToJsonString<T>(this T @object, JsonSerializerOptions? option = null)
        {
            if (@object is not null)
            {
                return await Task.Run(()=>{
                    if(option is not null)
                        return JsonSerializer.Serialize<T>(@object, option);

                    else
                        return JsonSerializer.Serialize<T>(@object, jsonSerializerOptions);
                });
            }
            else
            {
                return string.Empty;
            }
        }




        /// <summary>
        /// 反序列化json字符串为对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="string"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static async Task<T> DeserializeJsonToObjectAsync<T>(this string @string, JsonSerializerOptions? option = null)
        {
            if (!string.IsNullOrWhiteSpace(@string))
            {
                var opt = option is not null?option:jsonSerializerOptions;
                try
                {
                    return await Task.Run(() => 
                    {
                        var obj = JsonSerializer.Deserialize<T>(@string!, opt!);

                        return obj!;
                    });

                }
                catch(Exception e)
                {
                    Console.Write(e.ToString());
                    return default(T)!;
                }
            }
            else
            {
                return default!;
            }
        }





        /// <summary>
        /// 为字符串创建安全的格式化为Int方案， 逻辑失败则得到0
        /// </summary>
        /// <param name="string"></param>
        /// <returns></returns>
        public static int SafeParseInt(this string @string)
        {
            if(string.IsNullOrWhiteSpace(@string)) return 0; 

            int result;

            if(!int.TryParse(@string, out result))
            {
                result = 0;
            }

            return result;
        }


        


        /// <summary>
        /// 为 KeyValuePair(string, StringValues) 键值对的集合创建安全的返回值，如果逻辑空则得到string.Empty
        /// </summary>
        /// <param name="value"></param>
        /// <param name="name">Key匹配，忽略大小写</param>
        /// <returns></returns>
        public static string SafeString(this IEnumerable<KeyValuePair<string, StringValues>> value, string name)
        {
            var obj = value.FirstOrDefault(i=>i.Key.Equals(name,StringComparison.OrdinalIgnoreCase));
            
            var result = obj.Value;

            if(!StringValues.IsNullOrEmpty(result))
            {
                return result.ToString();
            }
            else
            {
                return string.Empty;
            }
      
        }

        

        /// <summary>
        /// 安全地把字符串转换成时间，如果逻辑失败，得到 1970年1月1日0时0分0秒
        /// </summary>
        /// <param name="string"></param>
        /// <returns></returns>
        public static DateTime ToSafeDateTime(this string @string)
        {
            if(@string == null) return EmptyDatetimeValue;

            if(DateTime.TryParse(@string,out var result))
            {
                return result;
            }
            else
            {
                return EmptyDatetimeValue;
            }
        }

        /// <summary>
        /// 安全地把字符串转换为 boolean, (0,string.empty,null) == false, 如果逻辑失败，得到 false
        /// </summary>
        /// <param name="string"></param>
        /// <returns></returns>
        public static bool ToSafeBoolan(this string @string)
        {
            if(string.IsNullOrWhiteSpace(@string)) return false;

            if(@string == "1" || @string == "0") return @string != "0"?true:false;

            if(bool.TryParse(@string, out var result))
            {
                return result;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 安全地把字符串转换为 int, 如果逻辑失败，则得到0
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int ToSafeInt32(this string value)
        {
            if(value == null) return 0;

            if (string.IsNullOrWhiteSpace(value))
            {
                int.TryParse(value, out var result);

                return result;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// 把字符串安全转换为Guid,如果字符串为空则得到Guid.Empty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Guid ToSafeGuid(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Guid.Empty;

            Guid.TryParse(value, out var result);

            return result;
        }

        /// <summary>
        /// 把字符串安全转换为Enum，如果字符串为空则得到Enum的默认值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T ToEnum<T>(this string value)  where T:Enum
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Enum.TryParse(typeof(T), value, out var result);

                return (T)result!;
            }
            else
            {
                return default(T)!;
            }
        }


        /// <summary>
        /// bool 安全转换为字符串 "0" 或 "1"， "0":false. "1": true
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToZeroOrOne(this bool value)
        {
            return value?"1":"0";
        }

        /// <summary>
        /// 从原始字符串中分离出以半角逗号、所有类型空白（空白、换行、制表）、全角逗号、分隔符(|)分隔的字符串数组，自动去除两边空白。
        /// 内部已经实现重复过滤
        /// </summary>
        /// <param name="string"></param>
        /// <returns></returns>
        public static string[] SpilitToArrayAndTrimDistinct(this string @string)
        {

            if (string.IsNullOrEmpty(@string))
            {
                return [];
            }
            else
            {

                string _s = @string.Trim();
                return getTagArray.Split(@string).Select(i => i.Trim()).Distinct().ToArray();
            }
        }

 

        /// <summary>
        /// 把该字符串以无参数方式Post到指定url。
        /// （如果字符串本身是json，则可以实现对象传递）
        /// </summary>
        /// <param name="string"></param>
        /// <param name="url"></param>
        public static async Task<ExecuteResultBase<string>> SendToNetwareByPost(this string @string,string url)
        {
            if (string.IsNullOrWhiteSpace(@string))
            {
                return new ExecuteResultBase<string>
                {
                    Message = ConstValues.Source_Can_Not_Be_Empty
                };
            }

            var result = await Http.PostBodyByJson(url, @string);

            return result;

        }



        /// <summary>
        /// 加密（如果使用对称加密类型不需要填写 key 和 iv）
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static (bool Success, string Value) AsEncrypt(this string input, EncryptType type, string key = "", string iv = "")
        {
            if (type == EncryptType.None)

                return (Success:false,Value:ConstValues.UnSupport_Method);

            if (string.IsNullOrWhiteSpace(input)) 
                
                return (Success:false, Value: ConstValues.Failure_Format_Of_Source_Value);

            //对称加密：需要 key 或 iv
            
            if (type == EncryptType.AES) return GeneralApplication.AESEncrypt(input, key, iv);


            if (type == EncryptType.HMAC_512) return GeneralApplication.HMAC_SHA512(input, key, Encoding.UTF8);
            

            //非对称加密，不需要key和iv:
            
            using (var provider = GeneralApplication.CreateCryptographyObject(type))
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = provider!.ComputeHash(bytes);
                string result = Convert.ToBase64String(hash);

                return (Success: true, Value: result);
            }

        }

        /// <summary>
        /// 把 IEnumerable<KeyValuePair<string,string>> 所有元素，按照 参数=参数值 的模式用 &amp; 字符拼接成字符串，并对参数值做 urlencode
        /// </summary>
        /// <param name="enums">需要拼接的数组</param>
        /// <returns>拼接完成以后的字符串</returns>
        public static string AsUrlEncodeParameters(this IEnumerable<KeyValuePair<string, string>> enums)
        {

            StringBuilder temp = new();

            foreach (KeyValuePair<string, string> node in enums)
            {
                temp.Append(string.Format("{0}={1}&", node.Key, Uri.EscapeDataString(node.Value)));
            }

            //去掉最後一個&字符
            int nLen = temp.Length;
            temp.Remove(nLen - 1, 1);

            return temp.ToString();
        }


        /// <summary>
        /// 统计当前词汇中指定字符串的出现次数
        /// </summary>
        /// <param name="sourcestr"></param>
        /// <param name="string"></param>
        /// <returns></returns>
        public static int AppearCount(this string sourcestr, ref string @string)
        {
            Regex regex = new Regex(@string, RegexOptions.IgnoreCase);
            var mymatch = regex.Matches(sourcestr);
            return mymatch.Count;
        }




    }
}
