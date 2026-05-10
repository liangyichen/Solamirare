using System.Runtime.CompilerServices;

namespace Solamirare;

/// <summary>
/// 表示一次 HTTP 请求返回的响应数据。
/// </summary>
public unsafe struct ValueHttpResponse
{
    /// <summary>响应体数据。</summary>
    public UnManagedCollection<byte> Body;

    /// <summary>HTTP 状态码。</summary>
    public int StatusCode;


    /// <summary>
    /// 指示本次响应是否成功。
    /// </summary>
    public bool Success
    {
        get
        {
            return StatusCode == 200;
        }
    }

    /// <summary>
    /// 初始化一个空的响应结构。
    /// </summary>
    public ValueHttpResponse()
    {
       
    }

    /// <summary>
    /// 释放当前响应实例占用的结构体存储空间。
    /// </summary>
    public void Dispose()
    {
        fixed(void* self = &this)
        
        NativeMemory.Free(self);
        
    }
}
