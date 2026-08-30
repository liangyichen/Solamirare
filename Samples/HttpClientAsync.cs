

/// <summary>
/// AsyncMacOSHttpClient 使用例子
/// </summary>
public static unsafe class TestHttpClientAsync
{

    /// <summary>
    /// 
    /// </summary>
    public static void Run()
    {
        // 初始化客户端
        AsyncHttpClient client = new AsyncHttpClient();
        client.Initialize(timeoutSeconds: 10, retries: 3);

        // 注意，必须先执行 TestHttpServer.Start(8059) 启动服务器， 或者启动其它开启了8059端口的本地服务器也可
        // 准备请求数据
        string urlStr = "http://localhost:8059/?geta=1&getb=2";

        fixed (char* urlPtr = urlStr)
        {
            UnManagedCollection<char> url = new UnManagedCollection<char>(urlPtr, (uint)urlStr.Length);

            UnManagedMemory<byte>* responseBuffer = (UnManagedMemory<byte>*)Marshal.AllocHGlobal(sizeof(UnManagedMemory<byte>));
            responseBuffer->Init(4096,4096);

            // 定义回调函数
            delegate* unmanaged<void*, void> callback = &OnRequestCompleted;

            // 发起异步 GET 请求
            //client.RequestGETAsync(url, responseBuffer, callback);


            UnManagedMemory<char> body = new UnManagedMemory<char>("b1=b1007&b2=b200");
            client.RequestPOSTAsync(url, responseBuffer, &body, HttpContentType.FormUrlEncoded,callback);

            
            Console.ReadLine();
            body.Dispose();

            // 清理
            client.Dispose();
            responseBuffer->Dispose();
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe void OnRequestCompleted(void* context)
    {
        AsyncRequestContext* ctx = (AsyncRequestContext*)context;

        DebugHelper.PrintUtf8Buffer(ctx->ResponseBuffer);
    }
}