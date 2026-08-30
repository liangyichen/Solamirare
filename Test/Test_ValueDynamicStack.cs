using Xunit;

namespace Solamirare.Tests;


public unsafe class Test_ValueDynamicStack
{
    [Fact]
    public void AppendReferences()
    {
        Assert.True(ValueStack_Test.Base());
    }

    
    [Fact]
    public void PopAndPeekAndEmpty()
    {
        Assert.True(ValueStack_Test.PopAndPeekAndEmpty());
    }


    [Fact]
    public void ClearAndContains()
    {
        Assert.True(ValueStack_Test.ClearAndContains());
    }


    [Fact]
    public void MultipleSegments()
    {
        Assert.True(ValueStack_Test.MultipleSegments());
    }


    [Fact]
    public void StructTypeTest()
    {
        Assert.True(ValueStack_Test.StructTypeTest());
    }


    [Fact]
    public void EnumeratorTest()
    {
        Assert.True(ValueStack_Test.EnumeratorTest());
    }


    [Fact]
    public void TryCopyToSpanTest()
    {
        Assert.True(ValueStack_Test.TryCopyToTest());
    }
    
}