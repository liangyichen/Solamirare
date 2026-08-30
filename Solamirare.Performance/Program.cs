using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Solamirare.Performance;

namespace TestSimpleJsonPerformance;





public unsafe class Program
{
    static void Main(string[] args)
    {
        BenchmarkDotNet.Reports.Summary summary_MemoryPool = BenchmarkRunner.Run<MemoryPool_Performance>();
        

        BenchmarkDotNet.Reports.Summary summary_Json = BenchmarkRunner.Run<Json_Performance>();


        BenchmarkDotNet.Reports.Summary UDictionary = BenchmarkRunner.Run<UDictionary_Performance>();


        BenchmarkDotNet.Reports.Summary summary_UnManagedMemory = BenchmarkRunner.Run<UnManagedMemory_Performance>();


        BenchmarkDotNet.Reports.Summary summary_ValueLinkedList = BenchmarkRunner.Run<ValueLinkedList_Performance>();


//=======  以上是正确完成的



        //BenchmarkDotNet.Reports.Summary summary_UnManagedIO = BenchmarkRunner.Run<UnManagedIO_Performance>();
 

        
        

        Console.ReadLine();
    }
}

