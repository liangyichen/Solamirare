using Solamirare.Test;
using Xunit;

namespace Solamirare.Tests;

public unsafe class Test_CircularDeque
{
    [Fact]
    public void PushFrontAndPopBack()
    {
        Assert.True(CircularDequeTests.TestPushFrontAndPopBack());
    }

    [Fact]
    public void PushBackAndPopFront()
    {
        Assert.True(CircularDequeTests.TestPushBackAndPopFront());
    }

    [Fact]
    public void CapacityExpansion()
    {
        Assert.True(CircularDequeTests.TestCapacityExpansion());
    }

    [Fact]
    public void Clear()
    {
        Assert.True(CircularDequeTests.TestClear());
    }

    

    [Fact]
    public void WrapAround()
    {
        Assert.True(CircularDequeTests.TestWrapAround());
    }

    

 


    [Fact]
    public void TrimExcess()
    {
        Assert.True(CircularDequeTests.TestTrimExcess());
    }

    

}