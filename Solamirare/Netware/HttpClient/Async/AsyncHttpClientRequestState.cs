namespace Solamirare;


/// <summary>
/// 异步HttpClient请求状态，由 IO_URing、KQueue 和 IOCP 共用，某些状态仅适用于特定平台，但为了简化设计，统一使用一个枚举来表示请求的不同阶段。
/// </summary>
public enum AsyncHttpClientRequestState : byte
{
    /// <summary>
    /// 正在连接
    /// </summary>
    Connecting = 0,

    /// <summary>
    /// SSL 握手阶段
    /// </summary>
    Handshaking = 1,

    /// <summary>
    /// 发送阶段
    /// </summary>
    Sending = 2,

    /// <summary>
    /// 正在流式发送 POST Body
    /// </summary>
    SendingBody = 3,

    /// <summary>
    ///  正在等待 POLL 事件以继续 SSL 操作（IO_URing专用）
    /// </summary>
    WaitingForPoll = 4,

    /// <summary>
    /// 接收阶段
    /// </summary>
    Receiving = 5,

    /// <summary>
    /// 完成阶段（无论成功还是失败，都会进入这个状态，回调函数可以根据其他字段判断结果）
    /// </summary>
    Completed = 6
}