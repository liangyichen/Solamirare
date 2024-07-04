
namespace Solamirare
{
    /// <summary>
    /// 字符串格式验证类
    /// </summary>
    public static class Validator
    {

        private static readonly Regex _iSImgUrl, _regexEmail, _isUrlFormat, _regexNickName,
            _isTagOrCatgoryString, 

            _UrlName, _isDouble,
            _isNumerOfPrice, _anchor, _isIPAddress,
            isIntorFloat, isCharsAndInt;  

        /// <summary>
        /// 仅数字类型
        /// </summary>
        static Regex IsDigital;

        static Validator()
        {
            IsDigital = new Regex(@"^[0-9]\d*$");
            isCharsAndInt = new Regex("^[A-Za-z0-9]+$", RegexOptions.Compiled);
            isIntorFloat = new Regex(@"(^[0-9]*[1-9][0-9]*$)|(^([0-9]{1,}[.][0-9]*)$)", RegexOptions.Compiled);

            _isIPAddress = new Regex(@"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$");
            _regexEmail = new Regex(@"^((?'name'.+?)\s*<)?(?'email'(?>[a-zA-Z\d!#$%&'*+\-/=?^_`{|}~]+\x20*|""(?'user'(?=[\x01-\x7f])[^""\\]|\\[\x01-\x7f])*""\x20*)*(?'angle'<))?(?'user'(?!\.)(?>\.?[a-zA-Z\d!#$%&'*+\-/=?^_`{|}~]+)+|""((?=[\x01-\x7f])[^""\\]|\\[\x01-\x7f])*"")@(?'domain'((?!-)[a-zA-Z\d\-]+(?<!-)\.)+[a-zA-Z]{2,}|\[(((?(?<!\[)\.)(25[0-5]|2[0-4]\d|[01]?\d?\d)){4}|[a-zA-Z\d\-]*[a-zA-Z\d]:((?=[\x01-\x7f])[^\\\[\]]|\\[\x01-\x7f])+)\])(?'angle')(?(name)>)$");
            _iSImgUrl = new Regex(@"https?://.+\.(jpg|gif|png|bmp|tif|svg|webp|jpeg|tiff)");
            _regexNickName = new Regex(@"^(?!_)(?!.*?_$)[a-zA-Z0-9_\u4e00-\u9fa5]+$");
          

            _isUrlFormat = new Regex(@"(http[s]{0,1})://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?");
            _isTagOrCatgoryString = new Regex(@"[\w\u4e00-\u9fa5]");

            
            _UrlName = new Regex(@"^[\w\-]+$");
           
            _isNumerOfPrice = new Regex(@"\d{1,10}\.*\d{0,2}");
            _anchor = new Regex("<a[^>]+href=[^>]+>[^<]*</a>");
           
            _isDouble = new Regex(@"^[1-9]/d*/./d*|0/./d*[1-9]/d*$");
        }


        /// <summary>
        /// 52个字母与整数的组合 A-Za-z0-9
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsCharsAndInt(in string source)
        {
            return isCharsAndInt.IsMatch(source);
        }


        /// <summary>
        /// 整数或者浮点数类型
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsIntorFloat(in string source)
        {
            return isIntorFloat.IsMatch(source);
        }



        /// <summary>
        /// 匹配远程图像地址(http|https开始, jpg、png、gif、bmp结尾)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsRemoteImageUrl(in string source)
        {
            return _iSImgUrl.IsMatch(source);
        }

        /// <summary>
        /// 是否电子邮件地址格式
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <returns></returns>
        public static bool RegexEmail(in string emailAddress)
        {
            return _regexEmail.IsMatch(emailAddress);
        }

        /// <summary>
        /// 昵称格式
        /// </summary>
        /// <param name="nicknameString"></param>
        /// <returns></returns>
        public static bool RegexNickName(in string nicknameString)
        {
            return _regexNickName.IsMatch(nicknameString);
        }

        /// <summary>
        /// 价格格式，小数点可有可无，有的话精确到后二位
        /// </summary>
        /// <param name="priceStr"></param>
        /// <returns></returns>
        public static bool IsNumberOfPrice(in string priceStr)
        {
            return _isNumerOfPrice.IsMatch(priceStr);
        }

        /// <summary>
        /// URLName匹配，仅允许26个英文字母、数字、连字符(-)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsUrlName(in string source)
        {
           

            return _UrlName.IsMatch(source);
        }

        /// <summary>
        /// 是否为整数
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsDigitalString(in string str)
        {
            return IsDigital.IsMatch(str);
        }

        /// <summary>
        /// 是否ipV4地址
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsIPV4Address(in string str)
        {
            return _isIPAddress.IsMatch(str);
        }


        /// <summary>
        /// 正浮点数
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsDoubleString(in string str)
        {
            return _isDouble.IsMatch(str);
        }



        /// <summary>
        /// 是否是标签和分类允许的名称
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsTagOrCatgoryString(in string str)
        {
            return _isTagOrCatgoryString.IsMatch(str);
        }








        /// <summary>
        /// 是否url格式
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool CheckUrlFormat(in string source)
        {
            return !string.IsNullOrEmpty(source) ? _isUrlFormat.IsMatch(source) : false;
        }





        /// <summary>
        /// 检测存在链接
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static bool HasLink(in string content)
        {

            return _isUrlFormat.Match(content).Success;
        }



        /// <summary>
        /// 过滤超链接为空
        /// </summary>
        /// <param name="content"></param>
        /// <param name="optionContent">可选的替换内容，默认是空白</param>
        /// <returns></returns>
        public static string ReplaceAnchorToEmpty(in string content, in string optionContent="")
        {
            //目前过滤掉所有的超链接的内容

            //Regex re2 = new Regex("<a.*?>|</a>");//过滤超链接中的<a ....>xxx</a>标签，标签中xxx内容保留
            if(optionContent=="")
                return _anchor.Replace(content, string.Empty);
            else
                return _anchor.Replace(content, optionContent);
        }


    }
}
