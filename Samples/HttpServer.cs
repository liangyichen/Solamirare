
public static unsafe class TestHttpServer
{ 
    /// <summary>
    /// 启动服务器
    /// </summary>
    public static void Start(ushort port)
    {
 
        ValueHttpServer* running = HttpServerStartup.Start(port, &ResponseOut);

        Console.ReadLine();

        running->Stop();

        Console.ReadLine();

    }

    /// <summary>
    /// 用户业务逻辑回调函数
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    static bool ResponseOut(UHttpContext* context)
    {

        context->Response.ServerHeader = "Solamirare Server Edited"u8;

        context->Response.AddOrUpdateHeader("Author"u8, "Solamirare Project"u8);

        context->Response.ResponseContentType = HttpMimeTypes.TextPlain;


        context->Response.Write("----- Request Connection  ------\r\n");
        context->Response.Write(context->Connection.IpAddress);
        context->Response.Write("\r\n");


        context->Response.Write(context->Connection.ClientPort);
        context->Response.Write("\r\n");


        context->Response.Write(context->Request.Method, 7); // 7 是因为 UnKnown 或 CONNECT 是7个字
        context->Response.Write("\r\n");


        context->Response.Write(context->Request.Path);
        context->Response.Write("\r\n");


        context->Response.Write(context->Request.HostHeader);
        context->Response.Write("\r\n");


        context->Response.Write(context->Request.Protocol);
        context->Response.Write("\r\n");


        context->Response.Write(context->Request.HttpVersion);
        context->Response.Write("\r\n");


        context->Response.Write("----- Request QueryString ------\r\n");

        context->Request.Query.ForEach(&loopsKeyAndValue, context);


        context->Response.Write("----- Request Form ------\r\n");


        context->Request.Form.ForEach(&loopsAndDecodeKeyValue, context);



        context->Response.Write("------ Fixed Fileds -----\r\n");
        context->Response.Write(context->Request.HostHeader);
        context->Response.Write("\r\n");
        context->Response.Write(context->Request.UserAgentHeader);
        context->Response.Write("\r\n");
        context->Response.Write(context->Request.ConnectionHeader);
        context->Response.Write("\r\n");
        context->Response.Write(context->Request.ContentLengthHeader);
        context->Response.Write("\r\n");


        context->Response.Write("------ Request Headers -----\r\n");

        context->Request.Headers.ForEach(&loopsKeyAndValue, context);


        context->Response.Write("------ Request Cookies -----\r\n");

        context->Request.Cookies.ForEach(&loopsKeyAndValue, context);

        context->Response.Write("\r\n");

        ValueDictionary<UnManagedMemory<char>, UnManagedMemory<char>>* dic = stackalloc ValueDictionary<UnManagedMemory<char>, UnManagedMemory<char>>[1];
         
        dic->Init(8,true,null);

        ReadOnlySpan<char> spanJson = HttpSeerverResources.JsonString;

        SolamirareJsonGenerator.DecodeObjectString_AppendToDictionary(spanJson, dic);

        char* memJson = stackalloc char[128];

        UnManagedMemory<char> json = new UnManagedMemory<char>(memJson, 128,0);


        SolamirareJsonGenerator.SerializeObject(dic, &json);
        
        dic->DisposeAll();
 
        
        context->Response.Write(&json);
        context->Response.Write("\r\n");

         



        return true;

    }

    static bool loopsKeyAndValue(int index, UnManagedCollection<byte>* key, UnManagedCollection<byte>* value, void* caller)
    {
        UHttpContext* context = (UHttpContext*)caller;

        context->Response.Write(key);
        context->Response.Write(": "u8);
        context->Response.Write(value);
        context->Response.Write("\r\n"u8);

        return true;
    }

    static bool loopsAndDecodeKeyValue(int index, UnManagedCollection<byte>* key, UnManagedCollection<byte>* value, void* caller)
    {
        UHttpContext* context = (UHttpContext*)caller;

        UnManagedMemory<char> _key = UrlEncodeAndDecoder.DecodeToChars(key);
        UnManagedMemory<char> _value = UrlEncodeAndDecoder.DecodeToChars(value);


        context->Response.Write(_key);
        context->Response.Write(": "u8);
        context->Response.Write(_value);
        context->Response.Write("\r\n"u8);

        _key.Dispose();
        _value.Dispose();


        return true;
    }

}