using Xunit;

namespace Solamirare.Tests;

// 这里是核心功能算法测试

public unsafe class Test_Core
{

    [Fact]
    public void Count_Ultra_Large_Char()
    {
        Assert.True(Core_Tests.Test_Count_Ultra_Large_Char());
    }

    [Fact]
    public void Count_Int_Alignment_Stress()
    {
        Assert.True(Core_Tests.Test_Count_Int_Alignment_Stress());
    }

    [Fact]
    public void Count_Overlapping_Stress()
    {
        Assert.True(Core_Tests.Test_Count_Overlapping_Stress());
    }

    [Fact]
    public void Count_Cross_Window_Boundary()
    {
        Assert.True(Core_Tests.Test_Count_Cross_Window_Boundary());
    }

    [Fact]
    public void Count_Complex_Overlap()
    {
        Assert.True(Core_Tests.Test_Count_Complex_Overlap());
    }


    [Fact]
    public void VerifyAlignmentLogic()
    {
        Assert.True(Core_Tests.VerifyAlignmentLogic());
    }

    [Fact]
    public void IndexOf_Basic()
    {
        Assert.True(Core_Tests.Test_IndexOf_Basic());
    }

    [Fact]
    public void IndexOf_Short_CacheLine_Stress()
    {
        Assert.True(Core_Tests.Test_IndexOf_Short_CacheLine_Stress());
    }
    [Fact]
    public void IndexOf_Short_Edge_Constraint()
    {
        Assert.True(Core_Tests.Test_IndexOf_Short_Edge_Constraint());
    }


    [Fact]
    public void IndexOf_Ultra_Misaligned_LongMatch()
    {
        Assert.True(Core_Tests.Test_IndexOf_Ultra_Misaligned_LongMatch());
    }


    [Fact]
    public void IndexOf_Ultra_Int_Stress()
    {
        Assert.True(Core_Tests.Test_IndexOf_Ultra_Int_Stress());
    }


    [Fact]
    public void Empty_And_Single_Byte()
    {
        Assert.True(Core_Tests.Test_Empty_And_Single_Byte());
    }


    [Fact]
    public void StartsWith_EndsWith()
    {
        Assert.True(Core_Tests.Test_StartsWith_EndsWith());
    }


    [Fact]
    public void IndexOf_Ultra_Unaligned_Entry()
    {
        Assert.True(Core_Tests.Test_IndexOf_Ultra_Unaligned_Entry());
    }

    [Fact]
    public void IndexOf_Ultra_Heavy_Collision()
    {
        Assert.True(Core_Tests.Test_IndexOf_Ultra_Heavy_Collision());
    }


    [Fact]
    public void IndexOf_Ultra_PageBoundary_Safety()
    {
        Assert.True(Core_Tests.Test_IndexOf_Ultra_PageBoundary_Safety());
    }

    [Fact]
    public void IndexOf_Ultra_Prefix_Overlap_Stress()
    {
        Assert.True(Core_Tests.Test_IndexOf_Ultra_Prefix_Overlap_Stress());
    }

    [Fact]
    public void IndexOf_Ultra_Char_Alignment()
    {
        Assert.True(Core_Tests.Test_IndexOf_Ultra_Char_Alignment());
    }


    [Fact]
    public void IndexOf_Ultra_CrossVectorBoundary()
    {
        Assert.True(Core_Tests.Test_IndexOf_Ultra_CrossVectorBoundary());
    }

    [Fact]
    public void IndexOf_Ultra_LargeScale()
    {
        Assert.True(Core_Tests.Test_IndexOf_Ultra_LargeScale());
    }

    [Fact]
    public void IndexOf_Ultra_NotFound()
    {
        Assert.True(Core_Tests.Test_IndexOf_Ultra_NotFound());
    }

    [Fact]
    public void IndexOf_Ultra_TailOverlap()
    {
        Assert.True(Core_Tests.Test_IndexOf_Ultra_TailOverlap());
    }


    [Fact]
    public void LastIndexOf_Ultra_CrossBlock()
    {
        Assert.True(Core_Tests.Test_LastIndexOf_Ultra_CrossBlock());
    }

    [Fact]
    public void LastIndexOf_Ultra_Int_Alignment()
    {
        Assert.True(Core_Tests.Test_LastIndexOf_Ultra_Int_Alignment());
    }

    [Fact]
    public void LastIndexOf_Ultra_HighFreq_Distraction()
    {
        Assert.True(Core_Tests.Test_LastIndexOf_Ultra_HighFreq_Distraction());
    }
    [Fact]
    public void LastIndexOf_Ultra_ReversePageBoundary()
    {
        Assert.True(Core_Tests.Test_LastIndexOf_Ultra_ReversePageBoundary());
    }

    [Fact]
    public void LastIndexOf_Ultra_MSB_Priority()
    {
        Assert.True(Core_Tests.Test_LastIndexOf_Ultra_MSB_Priority());
    }

    [Fact]
    public void LastIndexOf_Ultra_MultipleMatches()
    {
        Assert.True(Core_Tests.Test_LastIndexOf_Ultra_MultipleMatches());
    }


    [Fact]
    public void LastIndexOf_Basic()
    {
        Assert.True(Core_Tests.Test_LastIndexOf_Basic());
    }


}
