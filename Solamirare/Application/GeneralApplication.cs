namespace Solamirare;

/// <summary>
/// 通用应用程序
/// </summary>
public class GeneralApplication
{


    static Regex _regImg;



    static GeneralApplication()
    {
       

        //img标签中查找src的值
        _regImg = new Regex(@"<img\b[^<>]*?\bsrc[\s\t\r\n]*=[\s\t\r\n]*[""']?[\s\t\r\n]*(?<imgUrl>[^\s\t\r\n""'<>]*)[^<>]*?/?[\s\t\r\n]*>", RegexOptions.IgnoreCase);

    }

    /// <summary>   
    /// 取得HTML中所有的图像URL。   
    /// </summary>   
    /// <param name="html">HTML代码</param>   
    /// <returns>图片的URL列表</returns>   
    public static string GetImageUrls(string html)
    {

        MatchCollection matches = _regImg.Matches(html);

        string url = string.Empty;

        if (matches.Count > 0)
        {

            url = matches[0].Groups["imgUrl"].Value;
        }

        return url;
    }

    /// <summary>
    /// 产生随机字符串
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    public static string RandomStrings(int length)
    {
        var cs = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789~!@#$%^&*()_+{}:\"?<>.";
        var chars = new char[length];
        var random = new Random();

        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = cs[random.Next(cs.Length)];
        }

        var result = new string(chars);

        return result;
    }



    /// <summary>
    /// 通过ASCII码，得到单个字符串
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public static string ASCII_ToString(int code)
    {
        var array = new byte[] { (byte)code };
        return Encoding.ASCII.GetString(array);
    }





    /// <summary>
    /// 计算代码执行时间并且输出到控制台
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="functionName"></param>
    /// <param name="func"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public static T WatchProcessTimeOut<T>(string functionName, Func<T> func, ConsoleColor color = ConsoleColor.Green)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var result = func();

        stopwatch.Stop();

        // 返回执行时间
        $"{functionName},{stopwatch.ElapsedMilliseconds}".PrintToConsole(color);

        return result;

    }





    /// <summary>
    /// 转换为 urlname 格式(先去除两头的空白，然后把任意位置的一个或者多个空格转换为一个连字符)，
    /// 并且强制转换为小写
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static string ReplaceToUrlName(string source)
    {
        string result;

        if (!string.IsNullOrEmpty(source))
        {
            var _urlNameReplace = new Regex(@"<[^>  /\\?&.>]+", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            result = _urlNameReplace.Replace(source.Trim().ToLower(), "-");
        }
        else
        {
            result = string.Empty;
        }

        return result;

    }





    /// <summary>
    /// 检测目录组是否存在,如果不存在则创建
    /// </summary>
    /// <param name="paths"></param>
    /// <returns>description beford status: Core: directory exist(true) or not(false)</returns>
    public static ExecuteResultBase<bool> CheckDir(params string[] paths)
    {
        var fullpath = Path.Combine(paths);
        var result = new ExecuteResultBase<bool> { Core = Directory.Exists(fullpath) };

        if (!result.Core)
        {
            Directory.CreateDirectory(fullpath);
        }

        return result;
    }


    /// <summary>
    /// 检测目录是否存在,如果不存在则创建
    /// </summary>
    /// <param name="path"></param>
    /// <returns>description beford status: Core: directory exist(true) or not(false)</returns>
    public static ExecuteResultBase CheckDir(string path)
    {
        var result = new ExecuteResultBase();

        if (string.IsNullOrWhiteSpace(path)) { result.Message = ConstValues.Path_Can_Not_Be_Empty; }


        if (result.Success && !Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        return result;
    }


    /// <summary>
    /// (已过时，应该使用 CheckTextFileAsync)检测文本文件是否存在，如果不存在，则建立
    /// </summary>
    /// <param name="path"></param>
    /// <param name="defaultValue"></param>
    /// <returns>description before status: Core: file exist(true) or not(false)</returns>
    [Obsolete("move use to CheckTextFileAsync")]
    public static ExecuteResultBase<bool> CheckTextFile(string path, string defaultValue = "")
    {
        return CheckTextFileAsync(path, defaultValue).Result;
    }


    /// <summary>
    /// 检测文本文件是否存在，如果不存在，则建立
    /// </summary>
    /// <param name="path"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static async Task<ExecuteResultBase<bool>> CheckTextFileAsync(string path, string defaultValue = "")
    {

        var dir = Path.GetDirectoryName(path);
        CheckDir(dir!);

        var result = new ExecuteResultBase<bool> { Core = File.Exists(path) };


        if (!result.Core)
        {
            await File.AppendAllTextAsync(path, defaultValue);
            result.Core = false;
        }

        return result;
    }



    /// <summary>
    /// 读取指定路径的文字内容，如果文件不存在，则得到空字符串
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    [Obsolete("move use to ReadFileAsTextAsync.")]
    public static string ReadFileTextContent(string path)
    {
        return ReadFileAsTextAsync(path).Result;
    }


    /// <summary>
    /// 读取指定路径的文字内容，如果文件不存在，则得到空字符串
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static async Task<string> ReadFileAsTextAsync(string path)
    {
        var check = await CheckTextFileAsync(path);

        if (check.Success)
        {
            return File.ReadAllText(path, Encoding.UTF8);
        }
        else
        {
            return check.Message;
        }
    }


    static (byte[] KEY, byte[] IV) create_AES_Key_IV(string keyString, string ivString)
    {
        var key = new byte[32];
        var iv = new byte[16];

        byte[] byteKey = Encoding.UTF8.GetBytes(keyString.PadRight(key.Length));

        Array.Copy(byteKey, key, key.Length);

        byte[] byteIV = Encoding.UTF8.GetBytes(ivString.PadRight(key.Length));

        Array.Copy(byteIV, iv, iv.Length);

        return (KEY: key, IV: iv);
    }


    /// <summary>
    /// HMAC-SHA512 加密
    /// </summary>
    /// <param name="input"> 要加密的字符串 </param>
    /// <param name="key"> 密钥 </param>
    /// <param name="encoding"> 字符编码 </param>
    /// <returns></returns>
    internal static (bool Success, string Value) HMAC_SHA512(string input, string key, Encoding encoding)
    {

        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(key))

            return (Success: false, Value: ConstValues.Failure_Format_Of_Source_Value);

        var hMACSHA512 = new HMACSHA512(encoding.GetBytes(key));

        var data = hMACSHA512.ComputeHash(encoding.GetBytes(input));

        var result = BitConverter.ToString(data).Replace("-", "");

        return (Success: true, Value: result);
    }





    /// <summary>
    /// AES 加密
    /// </summary>
    /// <param name="string"></param>
    /// <param name="keyString"></param>
    /// <param name="ivString"></param>
    /// <returns></returns>
    internal static (bool Success, string Value) AESEncrypt(string @string, string keyString, string ivString)
    {

        if (string.IsNullOrWhiteSpace(keyString) || string.IsNullOrWhiteSpace(ivString))

            return (Success: false, Value: ConstValues.KEY_Or_IV_Can_Not_Be_Empty);


        var key_iv = create_AES_Key_IV(keyString, ivString);

        int keyLength = key_iv.KEY.Length;

        var encrypt = AESOperator.EncryptStringToBytes(@string, key_iv.KEY, key_iv.IV);

        var result = Convert.ToBase64String(encrypt.Value);

        return (Success: true, Value: result);

    }






    /// <summary>
    /// AES 解密
    /// </summary>
    /// <param name="base64String"></param>
    /// <param name="keyString"></param>
    /// <param name="ivString"></param>
    /// <returns></returns>
    public static (bool Success, string Value) AESDecrypt(string base64String, string keyString, string ivString)
    {


        if (string.IsNullOrEmpty(base64String) || base64String.Length % 4 != 0)

            return (Success: false, Value: ConstValues.Failure_Format_Of_Source_Value);


        if (string.IsNullOrWhiteSpace(keyString) || string.IsNullOrWhiteSpace(ivString))

            return (Success: false, Value: ConstValues.KEY_Or_IV_Can_Not_Be_Empty);

        var key_iv = create_AES_Key_IV(keyString, ivString);

        int keyLength = key_iv.KEY.Length;

        try
        {
            byte[] ciphertext = Convert.FromBase64String(base64String);

            var result = AESOperator.DecryptStringFromBytes(ciphertext, key_iv.KEY, key_iv.IV);

            return result;
        }
        catch (Exception ex)
        {

            return (Success: false, Value: ex.Message);
        }
    }

    /// <summary>
    /// 创建对称加密提供器
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static HashAlgorithm? CreateCryptographyObject(EncryptType type)
    {
        HashAlgorithm provider = null;

        switch (type)
        {

            case EncryptType.SHA1:

                provider = SHA1.Create();

                break;

            case EncryptType.SHA3_512:

                provider = SHA3_512.Create();

                break;

            case EncryptType.SHA256:

                provider = SHA256.Create();

                break;

            case EncryptType.SHA512:

                provider = SHA512.Create();

                break;

            case EncryptType.MD5_32:

                provider = MD5.Create();

                break;

        }

        return provider;
    }







}

