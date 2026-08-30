namespace Solamirare;


/// <summary>
/// IO_URing 请求上下文
/// </summary>
public unsafe struct IO_URingRequestContext
{
    //维护提示：AsyncRequestContext 必须放在第一位，禁止移动。 因为外部通用回调函数必须把 IO_URingRequestContext 强制转换为 AsyncRequestContext
    public AsyncRequestContext AsyncRequestContext;


    /// <summary>
    /// sockaddr_in 结构体，生命周期必须覆盖整个异步连接过程。
    /// io_uring IORING_OP_CONNECT 提交后内核会持续访问此地址，
    /// 必须存在上下文里而不能是栈变量。
    /// </summary>
    public sockaddr_in Addr;


    /// <summary>发送缓冲区中有效数据的字节数。</summary>
    public int SendLen;

    /// <summary>Poll 唤醒后应回到的状态。</summary>
    public AsyncHttpClientRequestState PostPollState;
}
