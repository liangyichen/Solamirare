using Solamirare.Tests;
namespace Solamirare.Performance;

[MemoryDiagnoser]
public unsafe class UDictionary_Performance
{
    [Benchmark]
    public void Append()
    {
        UDictionary_Test.Append();
    }

    [Benchmark]
    public void ToJson()
    {
        UDictionary_Test.ToJson();
    }

    [Benchmark]
    public void UnManagedString()
    {
        UDictionary_Test.UnManagedString();
    }

    [Benchmark]
    public void ForEach()
    {
        UDictionary_Test.ForEach();
    }

    [Benchmark]
    public void AddOrUpdate()
    {
        UDictionary_Test.AddOrUpdate();
    }
    [Benchmark]
    public void BasicOperations()
    {
        UDictionary_Test.BasicOperations();
    }
    [Benchmark]
    public void CapacityManagement()
    {
        UDictionary_Test.CapacityManagement();
    }
    [Benchmark]
    public void Collections()
    {
        UDictionary_Test.TestCollections();
    }
    [Benchmark]
    public void IteratorAndRemoveCurrent()
    {
        UDictionary_Test.IteratorAndRemoveCurrent();
    }
    [Benchmark]
    public void TryMethods()
    {
        UDictionary_Test.TryMethods();
    }
    [Benchmark]
    public void UtilityMethods()
    {
        UDictionary_Test.UtilityMethods();
    }

}
