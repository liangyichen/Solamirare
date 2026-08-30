using Solamirare.Tests;

namespace TestSimpleJsonPerformance;




[MemoryDiagnoser]
public unsafe class ValueLinkedList_Performance
{


    [Benchmark]
    public void MixedAppend()
    {
        ValueLiskedList_Test.MixedAppend();
    }



    [Benchmark]
    public void Commons()
    {
        ValueLiskedList_Test.Commons();
    }


    [Benchmark]
    public void Append()
    {
        ValueLiskedList_Test.Append();
    }

    [Benchmark]
    public void ForEach()
    {
        ValueLiskedList_Test.ForEachMethod();
    }
    

    [Benchmark]
    public void ContainsSpan()
    {
        ValueLiskedList_Test.ContainsSpan();
    }


    [Benchmark]
    public void IsEmpty()
    {
        ValueLiskedList_Test.IsEmpty();
    }

    [Benchmark]
    public void Get()
    {
        ValueLiskedList_Test.Get();
    }


    [Benchmark]
    public void IndexOfAny()
    {
        ValueLiskedList_Test.IndexOfAny();
    }


    [Benchmark]
    public void ReUseReady()
    {
        ValueLiskedList_Test.ReUseReady();
    }



    [Benchmark]
    public void IndexOf()
    {
        ValueLiskedList_Test.IndexOf();
    }


    [Benchmark]
    public void LastIndexOf()
    {
        ValueLiskedList_Test.LastIndexOf();
    }


    [Benchmark]
    public void Contains()
    {
        ValueLiskedList_Test.Contains();
    }


    [Benchmark]
    public void Dispose()
    {
        ValueLiskedList_Test.Dispose();
    }




}