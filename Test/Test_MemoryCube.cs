namespace Solamirare.Tests;

using Solamirare.Test;
using Xunit;




public unsafe class Test_MemoryCube
{
    [Fact]
    public void Test_01_MinSizeBoundaryCycle()
    {
        Assert.True(MemoryCubeManagerTests.Test_01_MinSizeBoundaryCycle());

    }

    [Fact]
    public void Test_02_MaxSizeBoundaryCycle()
    {
        Assert.True(MemoryCubeManagerTests.Test_02_MaxSizeBoundaryCycle());

    }

    [Fact]
    public void Test_03_UndersizeFailure()
    {
        Assert.True(MemoryCubeManagerTests.Test_03_UndersizeFailure());

    }



    [Fact]
    public void Test_05_PoolStateVerificationAndNav()
    {
        Assert.True(MemoryCubeManagerTests.Test_05_PoolStateVerificationAndNav());

    }

    [Fact]
    public void Test_06_FullCapacityOverflowAndReallocate()
    {
        Assert.True(MemoryCubeManagerTests.Test_06_FullCapacityOverflowAndReallocate());

    }

    [Fact]
    public void Test_07_ReturnInvalidPointer()
    {
        Assert.True(MemoryCubeManagerTests.Test_07_ReturnInvalidPointer());

    }





    [Fact]
    public void Test_08_BaseMemoryPool_Scale()
    {
        Assert.True(MemoryCubeManagerTests.BaseMemoryPool_Scale());

    }

    [Fact]
    public void Test_09_ClusterMemoryPool_Scale()
    {
        Assert.True(MemoryCubeManagerTests.Test_09_ClusterMemoryPool_Scale());

    }



}