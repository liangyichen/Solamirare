
namespace Solamirare
{

    /// <summary>
    /// 对应  DynamicData.S ，在此制定一些常用规范。
    /// 约定负数部分是系统备用或特殊状态，0以及正整数对应具体业务，如需使用自定义值不应该设置负数。
    /// 约定如需自定义未列出的值时，应该大于正整数 1000。
    /// </summary>
    public static class DynamicDataStatus
    {
        
        /// <summary>
        /// 默认值， 表示普通的正常状态。
        /// </summary>
        public static readonly int Enable = 0;

        /// <summary>
        /// 备用，
        /// （非强制要求：建议用于表示这是一条备用数据，不会长期保留）
        /// </summary>
        public static readonly int Temp = 1;

        //====================================

        /// <summary>
        /// 关闭，
        /// （非强制要求：建议用于表示某条数据暂停使用，或等待删除）。
        /// </summary>
        public static readonly int Disabled = -1;
        
        /// <summary>
        /// 争议，
        /// （非强制要求：建议用于表示与另外的数据存在争议或冲突）。
        /// </summary>
        public static readonly int Disputed = -2;
    }



    /// <summary>
    /// 动态应用类别，绑定到动态数据的 T
    /// </summary>
    public static class DynamicApplicationType
    {


        //================================
        //==== 负数部分是系统或者root使用

        /// <summary>
        /// 系统设置 (-4)
        /// </summary>
        public static readonly int SystemSetting = -4;

        /// <summary>
        /// html文本
        /// </summary>
        public static readonly int HTMLFiles  = -1;

        /// <summary>
        /// javascript文本
        /// </summary>
        public static readonly int JavascriptFiles  = -3;

        /// <summary>
        /// CSS文本
        /// </summary>
        public static readonly int CSSFiles  = -2;


        /// <summary>
        /// 服务器级别的设置，当前服务器下的所有域共用设置，root级别 (-5)
        /// </summary>
        public static readonly int ServerSetting = -5;


        /// <summary>
        /// 标识为空数据（-999），可以理解为逻辑上的 null
        /// </summary>
        public static readonly int IsEmpty = -999;




        //================================
        //=== 正数部分是常规应用类别




        /// <summary>
        /// 面向公共的通用类型（0），可以用于包揽常见的及未列出的业务类型
        /// </summary>
        public static readonly int CommonPublic = 0;


        /// <summary>
        /// 常规内容 (1)(HTML)
        /// </summary>
        public static readonly int Content = 1;


        /// <summary>
        /// 地理空间数据(2)(XML)
        /// </summary>
        public static readonly int GeographyXML = 2;

        /// <summary>
        /// 用户数据 (3)
        /// </summary>
        public static readonly int User = 3;


        /// <summary>
        /// 邮件 (4)
        /// </summary>
        public static readonly int Mail = 4;

        /// <summary>
        /// HTML模版内容（5）
        /// </summary>
        public static readonly int HtmlCache = 5;


        /// <summary>
        /// 动态类型数据（6）
        /// </summary>
        public static readonly int Dynamic = 6;

        /// <summary>
        /// RSS or Atom
        /// </summary>
        public static readonly int Feed = 7;


        /// <summary>
        /// 地理空间数据(8)(JSON)
        /// </summary>
        public static readonly int GeographyJSON = 8;


        /// <summary>
        /// 机器学习文本(9)(CSV)
        /// </summary>
        public static readonly int MLTextCSV = 9;

    }
}
