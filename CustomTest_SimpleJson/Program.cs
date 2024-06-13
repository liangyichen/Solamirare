using Solamirare;
using System.Diagnostics;
using System.Text;
using Solamirare.Extention;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CustomTest_SimpleJson
{

    [JsonSerializable(typeof(Dictionary<string,string>))]
    public partial class KV_JSON_OutputContext : JsonSerializerContext
    {

    }

    internal class Program
    {
        static Dictionary<string, string> dic;

        static JsonSerializerOptions options;

       

        static Program()
        {

            dic = new Dictionary<string, string>();


            //用于AOT的源生成器，同时在Jit环境下也能让.Net自带的 JsonSerializer.Serialize 脱离反射，发挥最大性能，执行结果的比较才足够客观。
            options = new JsonSerializerOptions();
            options.TypeInfoResolverChain.Add(KV_JSON_OutputContext.Default);
        }


        static void createData(int count)
        {
            dic.Clear();

            for (var i = 0; i < count; i++)
                dic.Add(GeneralApplication.RandomStrings(10), GeneralApplication.RandomStrings(20));

        }

        static void Loop(int loop)
        {
            StringBuilder build = new StringBuilder();
            var jsonGeneratorWithBuilder = new SolamirareJsonGeneratorWithStringBuilder();
            var jsonGeneratorWithPointer = new SolamirareJsonGenerator();

            $"============ ({loop} times) : ".PrintToConsole(ConsoleColor.Green);

            Stopwatch st1 = Stopwatch.StartNew();
            st1.Start();
            var t0 = "";
            for (int i = 0; i < loop; i++)
                t0 = JsonSerializer.Serialize(dic, options);


            st1.Stop();
            ($"Microsoft.Serialize: " + st1.ElapsedMilliseconds + " ms").PrintToConsole(ConsoleColor.Blue);

            Stopwatch st2 = Stopwatch.StartNew();
            st2.Start();

            var t1 = "";
            for (int i = 0; i < loop; i++)
                t1 = jsonGeneratorWithBuilder.SerializeObject(dic, build);

            st2.Stop();

            ($"SolamirareJson.WithStringBuilder: " + st2.ElapsedMilliseconds + " ms").PrintToConsole(ConsoleColor.Yellow);


            Stopwatch st3 = Stopwatch.StartNew();
            st3.Start();

            var t2 = "";
            for (int i = 0; i < loop; i++)
                t2 = jsonGeneratorWithPointer.SerializeObject(dic);

            st3.Stop();

            ($"SolamirareJson.WithPointer: " + st3.ElapsedMilliseconds + " ms").PrintToConsole(ConsoleColor.Cyan);
        }


        static void Execute(params int[] propertiesCount)
        {
            foreach (var count in propertiesCount)
            {

                createData(count);
                Console.WriteLine();
                ($"=====================================================").PrintToConsole(ConsoleColor.Green);

                ($"====================== PropertiesCount: {count} ======================").PrintToConsole(ConsoleColor.Green);


                //3个方案都事先执行一遍，给它们预热，顺便把需要测试的数据输出到客户端做预览

                var dic2 = dic.Select(i => new KeyValuePair<string, string>(i.Key, i.Value)).ToArray();


                var jsonGenerator = new SolamirareJsonGenerator();
                var jsonGenerator2 = new SolamirareJsonGeneratorWithStringBuilder();

                var text = jsonGenerator.SerializeObject(dic2);
                text = jsonGenerator2.SerializeObject(dic2);

                var ms = JsonSerializer.Serialize(dic, options);



                

                text.PrintToConsole(ConsoleColor.Green);



                ($"=====================================================").PrintToConsole(ConsoleColor.Green);

               

                Loop(100);

                Thread.Sleep(1000);
                Loop(1000);

                Thread.Sleep(1000);
                Loop(10000);

                Thread.Sleep(1000);
                Loop(100000);

                // Thread.Sleep(1000);
                // Loop(1000000);

            }
        }


        static async Task Main(string[] args)
        {

            //以 AOT 运行结果为准，不要在 Debug 模式观察（误差非常大）
            //命令行模式下面，在项目根目录运行 dotnet publish 即可编译到 AOT
            //在 bin\Release\net9.0\平台名\native 可以找到运行程序



            //如果在vs中通过项目界面编译 Realese ，得到的文件是 Jit 模式，运行时的性能与AOT模式差异也不大，可以作为快速开发时的参考


            Execute(5, 10, 20, 100);

            Console.ReadLine();
        }
    }
}
