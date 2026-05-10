namespace Solamirare;


/// <summary>
/// 表示 HTTP 请求方法。
/// </summary>
public enum ConnectionMethod : byte
{
    /// <summary>GET 请求。</summary>
    GET = 0,

    /// <summary>POST 请求。</summary>
    POST = 1,

    /// <summary>PUT 请求。</summary>
    PUT = 2,

    /// <summary>DELETE 请求。</summary>
    DELETE = 3,

    /// <summary>HEAD 请求。</summary>
    HEAD = 4,

    /// <summary>OPTIONS 请求。</summary>
    OPTIONS = 5,

    /// <summary>PATCH 请求。</summary>
    PATCH = 6,

    /// <summary>CONNECT 请求。</summary>
    CONNECT = 7,

    /// <summary>TRACE 请求。</summary>
    TRACE = 8,

    /// <summary>推送请求（HTTP/2 PUSH_PROMISE）。</summary>
    PUSH = 9,

    /// <summary>未知请求方法。</summary>
    UnKnown = 255
}

