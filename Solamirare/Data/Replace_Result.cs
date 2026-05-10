using System.Runtime.CompilerServices;

namespace Solamirare
{
    /// <summary>
    /// 替换操作结果描述
    /// </summary>
    [SkipLocalsInit]
    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi, Pack = 4)]
    public ref struct Replace_Result
    {
        /// <summary>
        /// 执行结果状态码，对应 Replace_Result 静态字段
        /// </summary>
        [FieldOffset(0)]
        public byte Status;

        /// <summary>
        /// 是否执行成功
        /// </summary>
        public bool Success { get { return Status == 0; } }

        /// <summary>
        /// 成功
        /// </summary>
        public static readonly byte Success_Code = 0;

        /// <summary>
        /// 失败，因为栈内存不可扩容
        /// </summary>
        public static readonly byte Failed_StackResize = 1;

        /// <summary>
        /// 输入的查询出现空值
        /// </summary>
        public static readonly byte Faild_EmptyInput = 2;

        /// <summary>
        /// 栈溢出
        /// </summary>
        public static readonly byte Failed_StackOverflow = 3;

        /// <summary>
        /// 长度不匹配
        /// </summary>
        public static readonly byte LengthNotMatch = 4;

        /// <summary>
        /// 未找到
        /// </summary>
        public static readonly byte NotFound = 5;

        /// <summary>
        /// 数据源是空值
        /// </summary>
        public static readonly byte SourceIsEmptyOrNull = 6;

        /// <summary>
        /// 只读对象，不可修改
        /// </summary>
        public static readonly byte ReadOnly = 7;


        /// <summary>
        /// 长度不可修改
        /// </summary>
        public static readonly byte Failed_FixedSize = 8;

        /// <summary>
        /// 对象未激活
        /// </summary>
        public static readonly byte UnActivated = 9;
    }

}
