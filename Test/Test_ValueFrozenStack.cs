using Solamirare.Test;
using Xunit;

namespace Solamirare.Tests;


public unsafe class Test_ValueFrozenStack
{

    [Fact]
    public void Construction()
    {
        Assert.True(ValueFrozenStackTests.TestConstruction());
    }

    [Fact]
    public void ExternalMemory()
    {
        Assert.True(ValueFrozenStackTests.TestExternalMemory());
    }

    [Fact]
    public void ZeroCapacity()
    {
        Assert.True(ValueFrozenStackTests.TestZeroCapacity());
    }

    [Fact]
    public void PushPop()
    {
        Assert.True(ValueFrozenStackTests.TestPushPop());
    }

    [Fact]
    public void Clear()
    {
        Assert.True(ValueFrozenStackTests.TestClear());
    }

}