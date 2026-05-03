namespace Solamirare;

/// <summary>
/// 表示一次 HTTP 请求处理过程中使用的上下文。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe partial struct UHttpContext
{
    /// <summary>
    /// 当前请求对象。
    /// </summary>
    public UHttpRequest Request;

    /// <summary>
    /// 当前响应对象。
    /// </summary>
    public UHttpResponse Response;

    /// <summary>
    /// 当前连接对象。
    /// </summary>
    public UHttpConnection Connection;

    /// <summary>
    /// 动态接收缓冲区，用于在读取阶段累计原始 HTTP 请求头数据。
    /// </summary>
    public UnManagedMemory<byte> RequestHeader;

    /// <summary>
    /// 响应输出缓冲区。
    /// </summary>
    public UnManagedMemory<byte> ResponseBuffer;

    /// <summary>
    /// 当前已读取或已发送的字节数。
    /// </summary>
    public uint ReadBytes;

    /// <summary>
    /// 当前响应总长度。
    /// </summary>
    public uint TotalResponseLength;

    /// <summary>
    /// 当前上下文状态值。
    /// </summary>
    public byte State;

    /// <summary>
    /// 初始化一个新的 HTTP 上下文。
    /// </summary>
    public UHttpContext()
    {
        resetFileds();
    }

    void resetFileds()
    {
        ReadBytes = 0;
        TotalResponseLength = 0;
        State = 0;
    }

    /// <summary>
    /// 清空上下文内容，以便复用。
    /// </summary>
    public void Clear()
    {
        resetFileds();
        RequestHeader.Reset();
        Request.Clear();
        Response.Clear();
        Connection.Clear();
    }

    /// <summary>
    /// 释放上下文持有的资源。
    /// </summary>
    public void Dispose()
    {
        resetFileds();
        RequestHeader.Reset();
        Request.Dispose();
        Response.Dispose();
        Connection.Clear();
    }

    internal static bool DictionaryDisposeWithInnerLoop<T1, T2>(int index, UnManagedMemory<T1>* key, UnManagedMemory<T2>* value, void* caller)
        where T1 : unmanaged
        where T2 : unmanaged
    {
        if (key is not null)
        {
            key->Dispose();
        }

        if (value is not null)
        {
            value->Dispose();
        }

        return true;
    }

        
    /// <summary>
    /// 描述当前对象状态的哈希码。
    /// </summary>
    public ulong StatusCode
    {
        get
        {
            fixed(UHttpContext* p = &this)
            {
                ulong result = Fingerprint.MemoryFingerprint(p);

                return result;
            }
        }
    }
}
