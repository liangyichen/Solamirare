


public unsafe class TestHttpClientSync
{

    public unsafe static void Run()
    {

        // 分配响应缓冲区 (100KB)
        const int RESPONSE_SIZE = 100 * 1024;

        UnManagedMemory<byte> responseBuffer = new UnManagedMemory<byte>(RESPONSE_SIZE, RESPONSE_SIZE);


        ValueHttpClient client = new ValueHttpClient();

        client.headers.AddOrUpdate("Accept", "*/*");
        // client.headers.AddOrUpdate("Accept-Encoding", "gzip, deflate");
        client.headers.AddOrUpdate("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 15_7_3) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/26.0 Safari/605.1.15");

        Console.WriteLine("测试 1: HTTP 请求到 localhost:8059");


        UnManagedMemory<char> body = "field1=value1&field2=value2".CopyToChars();

        ValueHttpResponse* httpReponseResult =
        client.RequestPOST("http://localhost:8059/", &responseBuffer, &body, HttpContentType.FormUrlEncoded);


        body.Dispose();

        UnManagedMemory<char> responseChars = httpReponseResult->Body.CopyToChars();

        Console.WriteLine(responseChars.AsSpan());

        httpReponseResult->Dispose();

        responseChars.Dispose();

        Console.WriteLine("主线程继续执行");


        // 如果 OpenSSL 可用，测试 HTTPS 
        Console.WriteLine("测试 2: HTTPS 请求 (需要 OpenSSL)");

        try
        {
            ValueHttpResponse* httpReponseResult2 = client.RequestGET("https://www.cnblogs.com/", &responseBuffer);

            if (httpReponseResult2->Success)
            {
                UnManagedCollection<byte> result2 = httpReponseResult2->Body;

                UnManagedMemory<char> responseChars2 = result2.CopyToChars();

                Console.WriteLine(responseChars2.AsSpan());
                
                responseChars2.Dispose();
            }

            httpReponseResult2->Dispose();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"跳过 HTTPS 测试: {ex.Message}");
            Console.WriteLine("\n要启用 HTTPS 支持，请安装 OpenSSL:");
            Console.WriteLine("  brew install openssl@3");
        }

        client.Dispose();
        responseBuffer.Dispose();

        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("所有测试完成!");
        Console.WriteLine(new string('=', 80));
    }

}