

using System.Diagnostics;
using System.Text;

namespace Test
{
    public class TestSimpleJson
    {

        [Fact]
        public async void TestJsonPeformance()
        {
            var jsonGenerator = new SolamirareJsonGenerator();

            DynamicData obj = new DynamicData();
            int loop = 10000;

            obj.Set("name", "my name");
            obj.Set("age", "1");
            obj.Set("address", "my address");

            Stopwatch st1 = Stopwatch.StartNew();
            st1.Start();

            for (int i = 0; i < loop; i++) 
                System.Text.Json.JsonSerializer.Serialize(obj.Data);


            st1.Stop();
            var st1_result = st1.ElapsedMilliseconds;

            Stopwatch st2 = Stopwatch.StartNew();
            st2.Start();

            var t = "";
            for (int i = 0; i < loop; i++)
                t = obj.ExportToSimpleJson(jsonGenerator);

            st2.Stop();

            var st2_result = st2.ElapsedMilliseconds;


            Assert.True(st1_result > st2_result);
        
        }





        [Fact]
        public async void TestDynamicDataExport()
        {
           
            DynamicData obj = new DynamicData();

            obj.Set("name", "my name");
            obj.Set("age", "1");
            obj.Set("address", "my address");


            ITextSerializer jsonGenerator = new SolamirareJsonGenerator();
            var result =  obj.ExportToSimpleJson(jsonGenerator);


            var count =  result.Count();

            Assert.Equal(result, "{\"age\":\"1\",\"address\":\"my address\",\"name\":\"my name\"}");
        
        }

        [Fact]
        public async void TestDynamicDataExport2()
        {
            
            DynamicData obj = new DynamicData();

            obj.Set("name", "my name");
            obj.Set("age", "1\"\"");
            obj.Set("address", "my address");
            ITextSerializer jsonGenerator = new SolamirareJsonGenerator();



            var result =  obj.ExportToSimpleJson(jsonGenerator, "name", "age");

            Assert.Equal(result, "{\"age\":\"1\\\"\\\"\",\"name\":\"my name\"}");

        }

        [Fact]
        public async void TestDynamicDataExport3()
        {
            ITextSerializer jsonGenerator = new SolamirareJsonGenerator();
            DynamicData obj = new DynamicData();

            obj.Set("name", "\"my name");
            obj.Set("age", "1");
            obj.Set("address", "my address");


            var result =  obj.ExportToSimpleJson(jsonGenerator, "age");

            Assert.Equal(result, "{\"age\":\"1\"}");

        }

        [Fact]
        public async void TestCollects()
        {
            ITextSerializer jsonGenerator = new SolamirareJsonGenerator();


            
            var dic = new Dictionary<string, string>() {

                { "name\"", "value"},
                { "age","10"}

            };



            var result = jsonGenerator.SerializeCollection(dic);

            Assert.Equal(result, "[\"name\\\"\":\"value\",\"age\":\"10\"]");

        }
    }
}
