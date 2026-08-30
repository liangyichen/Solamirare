using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;


public static class ASPNET_Start
{
    public static void Start(int port)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(port);
        });

        builder.Logging.ClearProviders();

        WebApplication app = builder.Build();

        app.MapPost("/", async (HttpContext context) =>
        {
            StringValues p1 = context.Request.Form["p1"];
            StringValues p2 = context.Request.Form["p2"];


            StringBuilder builder = new StringBuilder(128);

            builder.Append("----- Request Query ------\r\n");
            foreach (KeyValuePair<string, StringValues> i in context.Request.Query)
            {
                builder.Append(i.Key);
                builder.Append(": ");
                builder.Append(i.Value);
                builder.Append("\r\n");
            }

            builder.Append("----- Request Form ------\r\n");
            foreach (KeyValuePair<string, StringValues> i in context.Request.Form)
            {
                builder.Append(i.Key);
                builder.Append(": ");
                builder.Append(i.Value);
                builder.Append("\r\n");
            }


            builder.Append("----- Request Headers ------\r\n");
            foreach (KeyValuePair<string, StringValues> i in context.Request.Headers)
            {
                builder.Append(i.Key);
                builder.Append(": ");
                builder.Append(i.Value);
                builder.Append("\r\n");
            }


            builder.Append("----- Request Cookies ------\r\n");
            foreach (KeyValuePair<string, string> i in context.Request.Cookies)
            {
                builder.Append(i.Key);
                builder.Append(": ");
                builder.Append(i.Value);
                builder.Append("\r\n");
            }



            JsonModel0? model = JsonSerializer.Deserialize<JsonModel0>(HttpSeerverResources.JsonString);

            string modelJsonString = JsonSerializer.Serialize(model);

            builder.Append(modelJsonString);



            context.Response.ContentType = "text/plain";

            await context.Response.WriteAsync(builder.ToString());
        });


        app.Run();
    }
}