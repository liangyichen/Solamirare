namespace Solamirare;

/// <summary>
/// 表示 HTTP 请求体的内容类型。
/// </summary>
public enum HttpContentType : byte
{
    /// <summary>表单 URL 编码内容。</summary>
    FormUrlEncoded = 0,
    /// <summary>JSON 内容。</summary>
    Json = 1,
    /// <summary>XML 内容。</summary>
    Xml = 2,
    /// <summary>纯文本内容。</summary>
    PlainText = 3,
    /// <summary>HTML 内容。</summary>
    Html = 4,
    /// <summary>二进制内容。</summary>
    Binary = 5
}
