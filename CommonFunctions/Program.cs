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




            var data = new Dictionary<string,string>();

            var name = "<name>";
            var value = "<my \\tname>";

            data.Add(name, value);
            data.Add("name2", "value2");


            var json = itext.SerializeObject(data);


            //-----------------

            var emus = new string[] { "<aaaa" };


            var json_emus = itext.SerializeCollection(emus);

            
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
