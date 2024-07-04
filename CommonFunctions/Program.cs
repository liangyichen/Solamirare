using System.Diagnostics;
using Solamirare;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using BenchmarkDotNet.Running;

namespace CommonFunctions
{

    //Solamirare 功能演示

    [MemoryDiagnoser]
    public unsafe class Program
    {
        string[] rndArray;

        Dictionary<string, string> objects;


        public Program() {

            List<string>  rndBuild = new List<string>();

            rndBuild.Add(GeneralApplication.RandomStrings(10) + "<");
            rndBuild.Add(GeneralApplication.RandomStrings(10) + "<");
            rndBuild.Add(GeneralApplication.RandomStrings(10) + "<");

            rndArray = rndBuild.ToArray();


            objects = new Dictionary<string, string>();
            objects.Add("d<", "v>a>l>ue>_abc>");
            objects.Add("abc>", "<value_def");
            objects.Add("xcv4>", "<value_def");
            objects.Add("xcddv4>", "<value_def");
            objects.Add("xercv4>", "<value_def");

        }

        static ITextSerializer itext;

        static  Program()
        {

            

            itext = new SolamirareJsonGenerator();
        }




        [Params(1, 100)]
        public int BenchmarkLoops;


        [Benchmark]
        public void Solamirare()
        {
            var itextResult = string.Empty;

            for (int i = 0; i < BenchmarkLoops; i++)
                itextResult = itext.SerializeCollection(rndArray, rndArray.Length, true);
            
        }



        [Benchmark]
        public void MS()
        {
            var result = string.Empty;

            for (int i = 0; i < BenchmarkLoops; i++)
                result = System.Text.Json.JsonSerializer.Serialize(rndArray);
        }



        public void debugJsonResult()
        {

            int loop = 100000;

            var itextResult = string.Empty;

            rndArray = [
                "A<BCD",
                "<12<345>abc<d<e<f<g<",
                "<12<345>abc<d<e<"
            ];



            var st1 = Stopwatch.StartNew();



            // for (int i = 0; i < loop; i++)
            // {
            //     itextResult = itext.SerializeObject(objects, objects.Count, true);
            // }

            for (int i = 0; i < loop; i++)
            {
               itextResult = itext.SerializeCollection(rndArray, -1, true);
            }

            st1.Stop();

            
            
            var result = string.Empty;

            var st2 = Stopwatch.StartNew();

            // for (int i = 0; i < loop; i++)
            // {
            //     result = System.Text.Json.JsonSerializer.Serialize(objects);
            // }


            for (int i = 0; i < loop; i++)
            {
               result = System.Text.Json.JsonSerializer.Serialize(rndArray);
            }

            st2.Stop();


            Console.WriteLine($"json2:{st1.ElapsedMilliseconds}  , ms json:{st2.ElapsedMilliseconds}");

        }



        void testImportFromJson()
        {
            var itext = new SolamirareJsonGenerator();

            var json = """{"kkkk":"val\"e\"-1\"","name-2":"value-2","name-3":"value-3","name-4":"value-4"}""";
            
            var testdata = """{"AllowBlogComment":"False","name":"SystemSetting","GoogleMapKey":"AIzaSyA4u7eA_JZwoTcp1fbakEzk-m63r7l6hsg","ContentTitleLength":"128","ContentsDisplayEveyPageCount":"15","AdminPasswordOnlyInit":"Qazwsx123!","HeadKeywords":"liang yichen","HeadDescription":"Liang Yichen, A Photographer Around the World.","TimeZone":"0","InnerHead":"<link rel=\"apple-touch-icon\" href=\"/favicon.ico\" />\n<meta property=\"og:url\" content=\"https://liangyichen.net\" />\n<meta property=\"og:type\" content=\"website\" />\n<meta property=\"og:title\" content=\"liang yichen\" />\n<meta property=\"og:description\" content=\"a large part of these videos and photos comes from the Himalayas, recording local people and their lives, history, and landscapes.\" />\n<meta property=\"og:image\" content=\"[?link-image-server?]public/liangyichen1280.jpg\" />","EnableSendValidateMailByRegisterNewUser":"False","CustomScript":"","UrlNameLength":"128","CookiesExpiresDay":"360","FacebookAuthKey":"","HtmlFoot":"","GlobalUpdateContentsKey":"88888888","WebSiteFirstTitle":"Liang Yichen","innerKey":"Hbc6#sk0?2kx","ValidateClientInputWords":"False","Domain":"liangyichen.net,www.liangyichen.net,s3.liangyichen.net,192.168.0.102,192.168.0.102:5123,d78.liangyichen.net,d80.liangyichen.net,i.liangyichen.net,d1.liangyichen.net,localhost:5123,192.168.0.106:5000,i4.liangyichen.net,localhost:7000","ShieldedIP":"","CloudFlare_CF_Key_CF_Email_CF_Account_ID":"","AdminName":"admin","CustomDefaultIndexPage":"","EnableFallload":"False","CookiesName":"ClientValidate","HtmlCopyright":"<div class=\"viewCenter\">\n<hr />\n<h5>&copy;<a href=\"/\">Liang Yichen</a> | <a href=\"/about\">about</a> | <a href=\"/privacy\">privacy</a></h5>\n</div>\n<script defer src='https://static.cloudflareinsights.com/beacon.min.js' data-cf-beacon='{\"token\": \"8d38caca44a644009edd8bec66c64eb8\"}'></script>","HtmlHead":"\n<div class=\"viewCenter\">[img?/public/liangyichen1280.jpg|1280|636|liangyichen?]</div>\n<h1 class=\"header viewCenter\">Liang Yichen</h1>\n<p class=\"viewCenter\">\n<a href=\"/\" data-src=\"/\" class=\"buttom_index_page\">Photographic & Cinematograpic</a> | <a  href=\"https://io.liangyichen.net\">Programing C# & Javascript</a> | <a data-ajax=\"true\" href=\"/about\" data-src=\"/about\">About</a>\n</p>","key":""}""";

            var testdata2 = """{"r":"123","CustomScript":"","UrlNameLength":"128"}""";

            var testdata3 = """{"InnerHead":"<link rel=\"apple-touch-icon\" href=\"/favicon.ico\" />\n<meta property=\"og:url\""}""";

            var testdata5 = """{"name":""}""";

            var testdata6 = """{"code":"d4eb8\"}'></script>"}""";

            var testdata7 = """{"name":"my \\\\\" name","name2":"value2"}""";

            var testBase = """{"name":"my name"}""";

            var obj = new Photon();
            obj.ImportFromJsonString(testdata);

            var enumes = obj.Values2();

            foreach(var i in enumes)
            {
                Console.WriteLine($"key:{i.Key}, value:{i.Value}");
            }
            
        }


        static void testVsMsJson()
        {

            var dic3 = new Dictionary<string,string>();
            dic3.Add("v1","\"");
            dic3.Add("v2","\n");
            dic3.Add("v3","\r");
            dic3.Add("v4","\t");
            dic3.Add("v5","<");
            dic3.Add("v6","\\");
            dic3.Add("v7","\'");


            var c = '\"';
            var value = new string[]{c.ToString()};
            var itext = new SolamirareJsonGenerator();
            var ms = System.Text.Json.JsonSerializer.Serialize(dic3);
            var sl = itext.SerializeObject(dic3);

            Console.WriteLine($"ms: {ms}");
            Console.WriteLine($"sl: {sl}");
            Console.WriteLine((int)c);
        }



        static void TestDesries()
        {

            var itext = new SolamirareJsonGenerator();

            var json = """{"test":"\"n\na\"m\\e"}""";
            var json2 = """{"test":"\"name"}""";
            



            var ms = System.Text.Json.JsonSerializer.Serialize(json2);

            Console.WriteLine($"ms: {ms}");


            var obj = new Photon();
            obj.ImportFromJsonString(json);
            var sl = obj.ExportToJson(itext,true);
            
            
            Console.WriteLine($"sl: {sl}");

        }




        public static void Main(string[] args)
        {
            new Program()
                //.testImportFromJson();
                .debugJsonResult();

            //testVsMsJson();
            
            //TestDesries();


            //var summary = BenchmarkRunner.Run<Program>();
             Console.ReadLine();
        }
    }
}
