namespace Solamirare;


/// <summary>
/// HTTP 服务器启动器
/// </summary>
public unsafe static class HttpServerStartup
{
    /// <summary>
    /// 启动服务器
    /// </summary>
    /// <param name="port"></param>
    /// <param name="responseCallback"></param>
    /// <returns></returns>
    public static ValueHttpServer* Start(ushort port, delegate*<UHttpContext*, bool> responseCallback)
    {
        Span<MemoryPoolSchema> schemas = stackalloc MemoryPoolSchema[]
        {
            new MemoryPoolSchema(8,    256),
            new MemoryPoolSchema(64,   1024),
            new MemoryPoolSchema(96,   1024),
            new MemoryPoolSchema(160,  256),
            new MemoryPoolSchema(384,  256),
            new MemoryPoolSchema(2048, 256),
            new MemoryPoolSchema(4096, 256),
            new MemoryPoolSchema(8192, 256),
        };



        HTTPSeverConfig* config = (HTTPSeverConfig*)NativeMemory.AllocZeroed((nuint)sizeof(HTTPSeverConfig));

        *config = new HTTPSeverConfig
        {
            Port = port,
            READ_BUFFER_CAPACITY = 2048,
            RESPONSE_BUFFER_CAPACITY = 4096,
            MAX_CONNECTIONS = 128,
            ResponseCallback = responseCallback,
            Instances = new UnManagedMemory<nint>(8)
        };

        config->MemoryPool = new MemoryPoolCluster();

        config->MemoryPool.Init(schemas);

        ValueHttpServer* httpServerRunning = (ValueHttpServer*)NativeMemory.AllocZeroed((nuint)sizeof(ValueHttpServer));

        httpServerRunning->Init(config);

        config->Starter = httpServerRunning;

        httpServerRunning->Start();

        return config->Starter;
    }
}
