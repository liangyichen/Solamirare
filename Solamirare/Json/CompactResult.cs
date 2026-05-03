namespace Solamirare;

public unsafe partial struct JsonDocument
{
    /// <summary>
    /// 表示 JSON 压缩操作的结果。
    /// </summary>
    public ref struct CompactResult
    {
        /// <summary>压缩后的 JSON 数据。</summary>
        public UnManagedCollection<char> CompactedData;

        /// <summary>指示压缩操作是否成功。</summary>
        public bool IsSuccess;

        /// <summary>创建一个失败结果。</summary>
        public static CompactResult Failure => new CompactResult { CompactedData = UnManagedCollection<char>.Empty, IsSuccess = false };

        /// <summary>
        /// 创建一个成功结果。
        /// </summary>
        /// <param name="data">压缩后的 JSON 数据。</param>
        public static CompactResult Success(UnManagedCollection<char> data) => new CompactResult { CompactedData = data, IsSuccess = true };
    }
}
