using Xunit;

namespace Solamirare.Tests;


public unsafe class Test_UDictionary
{
    [Fact]
    public void KindsOfAppend()
    {
        Assert.True(UDictionary_Test.Append());
    }




    [Fact]
    public void Remove()
    {
        Assert.True(UDictionary_Test.Remove());
    }

    [Fact]
    public void UnManagedString()
    {
        Assert.True(UDictionary_Test.UnManagedString());
    }

    [Fact]
    public void FindByBytes()
    {
        Assert.True(UDictionary_Test.FindByBytes());
    }


    [Fact]
    public void FindBySpan()
    {
        Assert.True(UDictionary_Test.FindBySpan());
    }

    [Fact]
    public void ToJson()
    {
        Assert.True(UDictionary_Test.ToJson());
    }

    [Fact]
    public void ForEach()
    {
        Assert.True(UDictionary_Test.ForEach());
    }


    [Fact]
    public void UpdateMethods()
    {
        Assert.True(UDictionary_Test.AddOrUpdate());
    }


    [Fact]
    public void BasicOperations()
    {
        Assert.True(UDictionary_Test.BasicOperations());
    }


    [Fact]
    public void CapacityManagement()
    {
        Assert.True(UDictionary_Test.CapacityManagement());
    }

    [Fact]
    public void Collections()
    {
        Assert.True(UDictionary_Test.TestCollections());
    }


    [Fact]
    public void IteratorAndRemoveCurrent()
    {
        Assert.True(UDictionary_Test.IteratorAndRemoveCurrent());
    }


    [Fact]
    public void TryMethods()
    {
        Assert.True(UDictionary_Test.TryMethods());
    }


    [Fact]
    public void UtilityMethods()
    {
        Assert.True(UDictionary_Test.UtilityMethods());
    }








}
