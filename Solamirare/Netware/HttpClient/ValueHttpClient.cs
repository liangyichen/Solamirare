namespace Solamirare;



/// <summary>
/// 跨平台 HTTP/HTTPS 客户端（无托管内存分配）
/// <para>Unified Cross-Platform HTTP Client (Zero Allocation)</para>
/// </summary>
public unsafe struct ValueHttpClient
{
    // ============= 安全常量 =============
    private const int REQUEST_BUFFER_SIZE = 8192;


    // 超时与重试
    private const int DEFAULT_SOCKET_TIMEOUT = 10;


    private const int DEFAULT_MAX_RETRIES = 3;


    /// <summary>
    /// HTTP 请求头集合
    /// </summary>
    public ValueDictionary<UnManagedString, UnManagedString> headers;


    // ============= 配置参数 =============
    private int socketTimeoutSeconds;


    private int maxRetries;


    private bool disposed = false;


    static bool supportsHttps;


    static ValueHttpClient()
    {
        supportsHttps = GenericHttpClientFunctions.IsHttpsSupported;
    }



    /// <summary>
    /// 初始化 ValueHttpClient 实例
    /// </summary>
    public ValueHttpClient()
        : this(
            DEFAULT_SOCKET_TIMEOUT,
            DEFAULT_MAX_RETRIES)
    {
        // 使用默认配置：
        // Socket 超时 10 秒
        // 最大重试 3 次
    }


    /// <summary>
    /// 使用指定配置初始化 ValueHttpClient 实例
    /// </summary>
    /// <param name="socketTimeoutSeconds">
    /// Socket 超时时间，单位为秒。
    /// </param>
    /// <param name="maxRetries">
    /// 最大重试次数。
    /// 0 表示不重试。
    /// </param>
    public ValueHttpClient(
        int socketTimeoutSeconds,
        int maxRetries)
    {
        // 确保超时时间不会小于 1 秒
        if (socketTimeoutSeconds < 1)
        {
            socketTimeoutSeconds = 1;
        }

        // 确保重试次数不会为负数
        if (maxRetries < 0)
        {
            maxRetries = 0;
        }


        headers =
            new ValueDictionary<
                UnManagedString,
                UnManagedString>(4);


        headers.AddOrUpdate(
            "User-Agent",
            "SolamirareHttpClient");


        this.socketTimeoutSeconds =
            socketTimeoutSeconds;


        this.maxRetries =
            maxRetries;
    }


    /// <summary>
    /// 修改当前 ValueHttpClient 的运行时配置
    /// </summary>
    /// <param name="socketTimeoutSeconds">
    /// Socket 超时时间，单位为秒。
    /// </param>
    /// <param name="maxRetries">
    /// 最大重试次数。
    /// 0 表示不重试。
    /// </param>
    public void Configure(
        int socketTimeoutSeconds,
        int maxRetries)
    {
        // 确保超时时间不会小于 1 秒
        if (socketTimeoutSeconds < 1)
        {
            socketTimeoutSeconds = 1;
        }

        // 确保重试次数不会为负数
        if (maxRetries < 0)
        {
            maxRetries = 0;
        }


        this.socketTimeoutSeconds =
            socketTimeoutSeconds;


        this.maxRetries =
            maxRetries;
    }


    /// <summary>
    /// 执行GET请求
    /// <para>如果是 Windows 环境，必须安装有 OpenSSL 库</para>
    /// </summary>
    /// <param name="url"></param>
    /// <param name="responseBuffer"></param>
    /// <returns></returns>
    public ValueHttpResponse* RequestGET(
        UnManagedCollection<char> url,
        UnManagedMemory<byte>* responseBuffer)
    {
        return ExecuteRequest(
            url,
            responseBuffer,
            HttpMethod.Get,
            default,
            default);
    }


    /// <summary>
    /// 执行Post请求
    /// <para>如果是 Windows 环境，必须安装有 OpenSSL 库</para>
    /// </summary>
    /// <param name="url"></param>
    /// <param name="responseBuffer"></param>
    /// <param name="body"></param>
    /// <param name="contentType"></param>
    /// <returns></returns>
    public ValueHttpResponse* RequestPOST(
        UnManagedCollection<char> url,
        UnManagedMemory<byte>* responseBuffer,
        UnManagedString* body,
        HttpContentType contentType)
    {
        return ExecuteRequest(
            url,
            responseBuffer,
            HttpMethod.Post,
            body,
            contentType);
    }


    private ValueHttpResponse* ExecuteRequest(
        UnManagedCollection<char> url,
        UnManagedMemory<byte>* responseBuffer,
        HttpMethod method,
        UnManagedString* body,
        HttpContentType contentType)
    {
        ValueRequestData requ;


        fixed (
            ValueDictionary<
                UnManagedString,
                UnManagedString>* p_headers =
                &headers)
        {
            requ =
                new ValueRequestData
                {
                    url = url,

                    responseBuffer =
                        responseBuffer,

                    headers =
                        p_headers,

                    body =
                        body,

                    socketTimeoutSeconds =
                        socketTimeoutSeconds,

                    method =
                        method,

                    contentType =
                        contentType,

                    maxRetries =
                        maxRetries,

                    disposed =
                        disposed,

                    result =
                        null
                };
        }


        executeRequest(&requ);


        return requ.result;
    }


    static void executeRequest(
        void* dat)
    {
        ValueRequestData* data =
            (ValueRequestData*)dat;


        if (data->disposed ||
            data is null ||
            data->responseBuffer is null ||
            data->url.IsEmpty)
        {
            return;
        }


        data->responseBuffer->Clear();


        UrlParts parts;


        if (!GenericHttpClientFunctions.ParseUrl(
                data->url,
                &parts))
        {
            return;
        }


        if (parts.IsHttps &&
            !supportsHttps)
        {
            return;
        }


        if (data->responseBuffer is null)
        {
            return;
        }


        ValueHttpResponse lastResponse;


        byte* requestBuffer =
            stackalloc byte[REQUEST_BUFFER_SIZE];


        for (
            int attempt = 0;
            attempt <= data->maxRetries;
            attempt++)
        {
            int sockfd = -1;


            IntPtr sslCtx =
                IntPtr.Zero;


            IntPtr ssl =
                IntPtr.Zero;


            try
            {
                sockfd =
                    GenericHttpClientFunctions.Connect(
                        &parts,
                        data->socketTimeoutSeconds);


                if (sockfd > 0)
                {
                    if (parts.IsHttps &&
                        sockfd > 0)
                    {
                        GenericHttpClientFunctions.SetupSSL(
                            sockfd,
                            out sslCtx,
                            out ssl);
                    }


                    UnManagedMemory<byte> buffer =
                        new UnManagedMemory<byte>(
                            requestBuffer,
                            REQUEST_BUFFER_SIZE,
                            0,
                            MemoryTypeDefined.Stack);



                    uint requestLength =
                        GenericHttpClientFunctions.BuildRequest(
                            &parts,
                            &buffer,
                            REQUEST_BUFFER_SIZE,
                            data->headers,
                            data->method,
                            data->body,
                            data->contentType);


                    if (requestLength > 0)
                    {
                        // 1. 发送 Headers
                        GenericHttpClientFunctions.Send(
                            sockfd,
                            requestBuffer,
                            requestLength,
                            parts.IsHttps,
                            ssl);


                        // 2. 发送 Body (如果存在)
                        // 优化：流式发送，支持大文件且不占用额外栈空间
                        if (data->method ==
                                HttpMethod.Post &&
                            data->body != null &&
                            !data->body->IsEmpty)
                        {
                            GenericHttpClientFunctions.SendBody(
                                sockfd,
                                data->body,
                                parts.IsHttps,
                                ssl);
                        }


                        if (parts.IsHttps)
                        {
                            lastResponse =
                                GenericHttpClientFunctions
                                    .ReceiveSSLResponse(
                                        ssl,
                                        data->responseBuffer);
                        }
                        else
                        {
                            lastResponse =
                                GenericHttpClientFunctions
                                    .ReceiveResponse(
                                        sockfd,
                                        data->responseBuffer);
                        }


                        data->result =
                            (ValueHttpResponse*)
                                NativeMemory.AllocZeroed(
                                    (nuint)
                                    sizeof(ValueHttpResponse));


                        data->result->Body =
                            lastResponse.Body;


                        data->result->StatusCode =
                            lastResponse.StatusCode;


                        /*
                         * 已经成功收到 HTTP 响应。
                         *
                         * 不再继续执行 retry。
                         *
                         * 这一点对于聊天系统尤其重要：
                         * 上层业务请求可能具有实际副作用，
                         * HTTP 层不应该在已经获得有效响应之后
                         * 再次自动发送相同请求。
                         */
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
            }
            finally
            {
                GenericHttpClientFunctions.CleanupSSL(
                    ssl,
                    sslCtx);


                GenericHttpClientFunctions.CloseSocket(
                    sockfd);


                NativeMemory.Clear(
                    requestBuffer,
                    REQUEST_BUFFER_SIZE);
            }
        }


        return;
    }


    /// <summary>
    /// 释放当前客户端持有的请求头集合及其相关资源。
    /// </summary>
    public void Dispose()
    {
        if (!disposed)
        {
            headers.ForEach(
                &GenericHttpClientFunctions
                    .headersDisposeEach,
                null);


            headers.Dispose();


            disposed = true;
        }
    }
}