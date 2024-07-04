

using Microsoft.Extensions.DependencyInjection;


namespace Solamirare.Net
{



    /// <summary>
    /// 网络应用库
    /// </summary>
    public static class Http
    {

        static ServiceProvider serviceProvider;

        static IHttpClientFactory? httpClientFactory;

        static Http()
        {
            serviceProvider = new ServiceCollection()
                .AddHttpClient()

       
                .BuildServiceProvider();

            httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        }


        static HttpClient CreateHttpClient(string? userAgent = null, bool keepalive = true)
        {
            var httpclient = httpClientFactory!.CreateClient();

            
            httpclient.DefaultRequestHeaders.Remove("User-Agent");
                httpclient.DefaultRequestHeaders.Add("User-Agent", ConstValues.DefaultUserAgent);


            httpclient.DefaultRequestHeaders.ConnectionClose = !keepalive;





            return httpclient;
        }

        /// <summary>
        /// 使用HTTP-GET获取远程字符串内容
        /// </summary>
        /// <param name="url"></param>
        /// <param name="onsuccess"></param>
        /// <param name="userAgent"></param>
        /// <param name="keepalive"></param>
        /// <returns></returns>
        public static async Task<ExecuteResultBase<string>> Get(string url,  Action<HttpResponseMessage>? onsuccess = null,  string? userAgent = null, bool keepalive = true)
        {


            var httpclient = CreateHttpClient(userAgent, keepalive);



            var result = new ExecuteResultBase<string>();

            try
            {
                result.Core = await httpclient.GetStringAsync(url);
            }
            catch (Exception e)
            {
                result.Message = e.ToString();
                
            }

            return result;
            
        }


        /// <summary>
        /// 发送Post数据，Json对象方式。（保证传入的字符串是 Json 格式）
        /// </summary>
        /// <param name="url"></param>
        /// <param name="json"></param>
        /// <param name="onsuccess"></param>
        /// <param name="userAgent"></param>
        /// <param name="httpclient"></param>
        /// <returns></returns>
        public static async Task<ExecuteResultBase<string>> PostBodyByJson(string url, string json, Action<HttpResponseMessage>? onsuccess = null, string? userAgent = null, HttpClient? httpclient = null)
        {

            HttpContent httpContent = new StringContent(json);

            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json")
            {
                CharSet = "utf-8"
            };

            

            return await HttpRequestBase(url, httpclient!, userAgent!, onsuccess!, httpContent!);
        }




        /// <summary>
        /// 发送Post数据，键值对方式
        /// </summary>
        /// <param name="url"></param>
        /// <param name="values"></param>
        /// <param name="onsuccess"></param>
        /// <param name="userAgent"></param>
        /// <param name="httpclient"></param>
        /// <returns></returns>
        public static async Task<ExecuteResultBase<string>> Post(string url, IEnumerable<KeyValuePair<string, string>> values, Action<HttpResponseMessage>? onsuccess = null, string? userAgent = null, HttpClient? httpclient = null)
        {

            HttpContent content = new FormUrlEncodedContent(values);

            return await HttpRequestBase(url, httpclient!, userAgent!, onsuccess!, content!,HttpMethod.Post);

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="httpclient"></param>
        /// <param name="userAgent"></param>
        /// <param name="onsuccess"></param>
        /// <param name="content"></param>
        /// <param name="method"></param>
        /// <param name="keepalive"></param>
        /// <returns></returns>
        public static async Task<ExecuteResultBase<string>> HttpRequestBase(string url,
            HttpClient httpclient,
            string userAgent,
            Action<HttpResponseMessage>? onsuccess = null,
            HttpContent? content = null,
            HttpMethod? method = null,
            bool keepalive = true)
        {

            var r = new ExecuteResultBase<string> { Core = string.Empty };

            if(string.IsNullOrEmpty(userAgent)) userAgent = ConstValues.DefaultUserAgent;

            if(httpclient is null) httpclient = CreateHttpClient(userAgent, keepalive);


            if (method is null) method = HttpMethod.Get;

            HttpRequestMessage request = new HttpRequestMessage(method, url);
            if (content is not null) request.Content = content;

            using HttpResponseMessage response = await httpclient.SendAsync(request);



            try
            {
                var responseResult = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    r.Core = responseResult;
                    if (onsuccess is not null)
                    {
                        onsuccess(response);
                    }
                }
                else
                {
                    r.Message = response.StatusCode.ToString();
                    
                }
            }
            catch (Exception e)
            {
                r.Message = e.ToString();
            }
            

            return r;

        }




    }
}
