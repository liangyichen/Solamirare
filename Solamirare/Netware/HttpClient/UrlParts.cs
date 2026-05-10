namespace Solamirare;

/// <summary>
/// 表示 URL 解析后的关键组成部分。
/// </summary>
public unsafe struct UrlParts
{
    /// <summary>指示当前 URL 是否使用 HTTPS。</summary>
    public bool IsHttps;

    /// <summary>保存主机名的缓冲区。</summary>
    public fixed byte Host[256];

    /// <summary>主机名的实际长度。</summary>
    public int HostLength;

    /// <summary>保存路径的缓冲区。</summary>
    public fixed byte Path[1024];

    /// <summary>路径的实际长度。</summary>
    public int PathLength;

    /// <summary>目标端口号。</summary>
    public int Port;
}
