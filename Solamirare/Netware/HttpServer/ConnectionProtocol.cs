namespace Solamirare;


/// <summary>
/// 表示连接或应用层协议类型。
/// </summary>
public enum ConnectionProtocol : byte
{
    /// <summary>HTTP 协议。</summary>
    HTTP = 0,

    /// <summary>TCP 协议。</summary>
    TCP = 1,

    /// <summary>UDP 协议。</summary>
    UDP = 2,

    /// <summary>FTP 协议。</summary>
    FTP = 3,

    /// <summary>SFTP 协议。</summary>
    SFTP = 4,

    /// <summary>HTTPS 协议。</summary>
    HTTPS = 5,

    /// <summary>未知协议。</summary>
    UnKnown = 6
}
