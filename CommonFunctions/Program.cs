using Solamirare;
using Solamirare.Extention;

namespace CommonFunctions
{

    //Solamirare 功能演示

    internal unsafe class Program
    {

        static void useJsonResponse()
        {
            ITextSerializer itext = new SolamirareJsonGenerator();


var html  = """


    {"AllowBlogComment":"False","name":"SystemSetting","GoogleMapKey":"AIzaSyA4u7eA_JZwoTcp1fbakEzk-m63r7l6hsg","ContentTitleLength":"128","ContentsDisplayEveyPageCount":"15","AdminPasswordOnlyInit":"Qazwsx123!","HeadKeywords":"liang yichen","HeadDescription":"Liang Yichen, A Photographer Around the World.","TimeZone":"0","InnerHead":"<link rel=\"apple-touch-icon\" href=\"/favicon.ico\" />

<meta property=\"og:title\" content=\"liang yichen\" />

<hr />


""";



            var data = new Dictionary<string,string>();

            var name = "<name>";
            var value = "<my \\tname>";

            data.Add(name, value);
            data.Add("na>me2", "va<lue2");

            // var ms_json = System.Text.Json.JsonSerializer.Serialize(new KeyValuePair<string,string>(name,value));
            
            
            // var test_length = "{\"\\u003cname\":\"my name\"}".Length;


            // data.AppendIfNotExist("lastname", "my lastname");
            // data.AppendIfNotExist("html",html);

            var json = itext.SerializeCollection(data);
            
            Console.WriteLine(json);
        }


        static char[] FailureChars = new[] { 'a', 'd',};


        static int FailureCharsCount(ref ReadOnlySpan<char> source)
        {
            int count = 0;
            for (int i = 0;i<FailureChars.Length;i++)
            {
                if(source.IndexOf(FailureChars[i]) > -1) count += 1;
            }

            return count;
        }

        static void testFailureChars()
        {
           

            var source = "abcdefg".AsSpan();

            var count = FailureCharsCount(ref source);


            Console.WriteLine(count);

        }

        static void testMSJson()
        {
            var data = new DynamicData();
            data.AppendIfNotExist("code", "\\");

            var json = System.Text.Json.JsonSerializer.Serialize(data.Values());

            Console.WriteLine(json);
        }

        static void Main(string[] args)
        {
            useJsonResponse();
            Console.ReadLine();
        }
    }
}
