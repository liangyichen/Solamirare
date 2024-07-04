using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Solamirare;
using System.Text;

namespace TestSimpleJsonPerformance
{
    [MemoryDiagnoser]
    public class Program
    {

        [Params(1,100,500,1000)]
        public int Count;

        DynamicData obj;

        ITextSerializer jsonGenerator;

        public Program() {


            obj = new DynamicData();

            jsonGenerator = new SolamirareJsonGenerator();

            obj.Set("name", "my name");
            obj.Set("age", "1");
            obj.Set("address", "my address");
        }


        [Benchmark]
        public void SimpleJson()
        {
            for (var i = 0; i < Count; i++)
                obj.ExportToJson(jsonGenerator);
        }


        [Benchmark]
        public void Json_Net()
        {

            for (var i = 0; i < Count; i++)
                System.Text.Json.JsonSerializer.Serialize(obj.Data);
        }



        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Program>();
            Console.ReadLine();
        }
    }
}
