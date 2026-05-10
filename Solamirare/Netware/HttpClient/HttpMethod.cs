namespace Solamirare;

/// <summary>
/// 表示 HTTP 请求方法。
/// </summary>
public enum HttpMethod : byte
{
    /// <summary>GET 请求。</summary>
    Get = 0,
    /// <summary>POST 请求。</summary>
    Post = 1,
    /// <summary>PUT 请求。</summary>
    Put = 2,
    /// <summary>DELETE 请求。</summary>
    Delete = 3,
    /// <summary>HEAD 请求。</summary>
    Head = 4,
    /// <summary>OPTIONS 请求。</summary>
    Options = 5,
    /// <summary>PATCH 请求。</summary>
    Patch = 6
}
