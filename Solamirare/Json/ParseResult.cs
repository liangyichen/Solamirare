namespace Solamirare;

public unsafe partial struct JsonDocument
{
    /// <summary>
    /// 表示字符串解析为 JSON 字符串值后的结果状态。
    /// </summary>
    public ref struct ParseResult
    {
        /// <summary>解析得到的数据；失败或空字符串时为 <see cref="UnManagedMemory{T}.Empty"/>。</summary>
        public UnManagedString Data;

        /// <summary>指示本次解析是否失败。</summary>
        public bool IsFailure;

        /// <summary>指示本次解析是否成功且结果为空字符串。</summary>
        public bool IsEmptyString;

        /// <summary>创建一个失败结果。</summary>
        public static ParseResult Failure => new ParseResult { Data = UnManagedString.Empty, IsFailure = true, IsEmptyString = false };

        /// <summary>创建一个“空字符串解析成功”的结果。</summary>
        public static ParseResult EmptyStringSuccess => new ParseResult { Data = UnManagedString.Empty, IsFailure = false, IsEmptyString = true };

        /// <summary>
        /// 创建一个成功结果。
        /// </summary>
        /// <param name="data">解析得到的字符串数据。</param>
        public static ParseResult Success(UnManagedString data) => new ParseResult { Data = data, IsFailure = false, IsEmptyString = false };
    }
}
