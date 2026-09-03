using Solamirare.Tests;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Solamirare.Tests;


public unsafe class UnManagedMemory
{
    [Fact]
    public void Sample()
    {
        Assert.True(Sample_UnManagedMemory.Constructor());
    }


    [Fact]
    public void SpiltCopyToValueFrozenDictionary()
    {
        Assert.True(UnamangedMemory_Test.SpiltCopyToValueFrozenDictionary());
    }


    [Fact]
    public void myMemoryPool()
    {
        Assert.True(UnamangedMemory_Test.myMemoryPool());
    }


    [Fact]
    public void Init()
    {
        Assert.True(UnamangedMemory_Test.Init());
    }


    [Fact]
    public void HashEquals()
    {
        Assert.True(UnamangedMemory_Test.HashEquals());
    }


    [Fact]
    public void SpiltMapToValueFrozenDictionary()
    {
        Assert.True(UnamangedMemory_Test.SpiltMapToValueFrozenDictionary());
    }


    [Fact]
    public void SpiltCopyToCollection()
    {
        Assert.True(UnamangedMemory_Test.SpiltCopy());
    }

    [Fact]
    public void Transformance()
    {
        Assert.True(UnamangedMemory_Test.Transformance());
    }


    [Fact]
    public void SpiltMapToCollection()
    {
        Assert.True(UnamangedMemory_Test.SpiltMap());
    }


    [Fact]
    public void CopyTo()
    {
        Assert.True(UnamangedMemory_Test.CopyTo());
    }


    [Fact]
    public void Reverse()
    {
        Assert.True(UnamangedMemory_Test.Reverse());
    }


    [Fact]
    public void Sort()
    {
        Assert.True(UnamangedMemory_Test.Sort());
    }

    [Fact]
    public void ToBytes()
    {
        Assert.True(UnamangedMemory_Test.ToBytes());
    }






    [Fact]
    public void Heap_LastIndexOf()
    {
        Assert.True(UnamangedMemory_Test.Heap_LastIndexOf());
    }

    [Fact]
    public void ReadOnly()
    {
        Assert.True(UnamangedMemory_Test.ReadOnly());
    }

    [Fact]
    public void EnsureCapacity()
    {
        Assert.True(UnamangedMemory_Test.EnsureCapacity());
    }


    [Fact]
    public void Concat()
    {
        Assert.True(UnamangedMemory_Test.Concat());
    }


    [Fact]
    public void IndexOf_Short_Chars()
    {
        Assert.True(UnamangedMemory_Test.IndexOf_Short_Chars());
    }


    [Fact]
    public void IntToUnmanagedString()
    {
        Assert.True(UnamangedMemory_Test.IntToUnmanagedString());
    }

    [Fact]
    public void Operator()
    {
        Assert.True(UnamangedMemory_Test.Operator());
    }

    [Fact]
    public void Replace()
    {
        Assert.True(UnamangedMemory_Test.Replace());
    }


    [Fact]
    public void ParseFromDateTime()
    {
        Assert.True(UnamangedMemory_Test.ParseFromDateTime());
    }


    [Fact]
    public void ParseFromLong()
    {
        Assert.True(UnamangedMemory_Test.ParseFromLong());
    }


    [Fact]
    public void ParseFromInt()
    {
        Assert.True(UnamangedMemory_Test.ParseFromInt());
    }



    [Fact]
    public void ParseFromDecimal()
    {
        Assert.True(UnamangedMemory_Test.ParseFromDecimal());
    }




    [Fact]
    public void StartsWith()
    {
        Assert.True(UnamangedMemory_Test.StartsWith());
    }

    [Fact]
    public void IndexOfAny()
    {
        Assert.True(UnamangedMemory_Test.IndexOfAny());
    }



    [Fact]
    public void Contains_Single()
    {
        Assert.True(UnamangedMemory_Test.Contains_Single());
    }



    [Fact]
    public void Override_Operate_Equals()
    {
        Assert.True(UnamangedMemory_Test.Override_Operate_Equals());
    }




    [Fact]
    public void @forach()
    {
        Assert.True(UnamangedMemory_Test.foraech());
    }




    [Fact]
    public void ForEachMethod()
    {
        Assert.True(UnamangedMemory_Test.ForEachMethod());
    }



    [Fact]
    public void Resize_Min()
    {
        Assert.True(UnamangedMemory_Test.Resize_Min());
    }



    [Fact]
    public void JSON_EncodeAndDecodeStrings()
    {
        Assert.True(UnamangedMemory_Test.EncodeAndDecodeStrings());
    }


    [Fact]
    public void Contains_Collection()
    {
        Assert.True(UnamangedMemory_Test.Contains_Collection());
    }



    [Fact]
    public void Count()
    {
        Assert.True(UnamangedMemory_Test.Count());
    }



    [Fact]
    public void Index()
    {
        Assert.True(UnamangedMemory_Test.Index());
    }



    [Fact]
    public void RemoveAt()
    {
        Assert.True(UnamangedMemory_Test.RemoveAt());
    }




    [Fact]
    public void SetValue()
    {
        Assert.True(UnamangedMemory_Test.SetValue());
    }




    [Fact]
    public void IndexOf_Single_String()
    {
        Assert.True(UnamangedMemory_Test.IndexOf_Single_String());
    }


    [Fact]
    public void IndexOf_Single_Char()
    {
        Assert.True(UnamangedMemory_Test.IndexOf_Single_Char());
    }


    [Fact]
    public void AsRealSizeSpan()
    {
        Assert.True(UnamangedMemory_Test.AsRealSizeSpan());
    }

    [Fact]
    public void AsSpan()
    {
        Assert.True(UnamangedMemory_Test.AsSpan());
    }

    [Fact]
    public void IndexOf_Short_Bytes()
    {
        Assert.True(UnamangedMemory_Test.IndexOf_Short_Bytes());
    }

    [Fact]
    public void IndexOf_BYTE()
    {
        Assert.True(UnamangedMemory_Test.IndexOf_BYTE());
    }


    [Fact]
    public void Slice()
    {
        Assert.True(UnamangedMemory_Test.Slice());
    }


    [Fact]
    public void IndexOf_struct()
    {
        Assert.True(UnamangedMemory_Test.IndexOf_struct());
    }

    [Fact]
    public void IndexOf_INT()
    {
        Assert.True(UnamangedMemory_Test.IndexOf_INT());
    }


    [Fact]
    public void IndexsOf_Chars()
    {
        Assert.True(UnamangedMemory_Test.IndexsOf_Chars());
    }


    [Fact]
    public void InsertAt()
    {
        Assert.True(UnamangedMemory_Test.InsertAt());
    }


    [Fact]
    public void InsertCollectionAt()
    {
        Assert.True(UnamangedMemory_Test.InsertCollectionAt());
    }



    [Fact]
    public void RemoveRange()
    {
        Assert.True(UnamangedMemory_Test.RemoveRange());
    }



    [Fact]
    public void ReSize()
    {
        Assert.True(UnamangedMemory_Test.ReSize());
    }






    [Fact]
    public void GetPointer()
    {
        bool result = UnamangedMemory_Test.GetPointer();

        Assert.True(result);
    }

    [Fact]
    public void AutoIndexMemory()
    {
        bool result = UnamangedMemory_Test.AutoIndexMemory();

        Assert.True(result);
    }

    [Fact]
    public void fromSpan()
    {
        bool result = UnamangedMemory_Test.From_Span();

        Assert.True(result);
    }

    [Fact]
    public void from_ExtMemory()
    {
        bool result = UnamangedMemory_Test.From_ExtMemory();

        Assert.True(result);
    }

    [Fact]
    public void create_Empty()
    {
        bool result = UnamangedMemory_Test.Create_Empty();

        Assert.True(result);
    }

    [Fact]
    public void empty_to_Allocted()
    {
        bool result = UnamangedMemory_Test.Empty_to_Allocted();

        Assert.True(result);
    }



    [Fact]
    public void reset_From_ExternalMemory()
    {
        bool result = UnamangedMemory_Test.Reset_From_ExternalMemory();

        Assert.True(result);
    }

    [Fact]
    public void check_on_stack()
    {
        bool result = UnamangedMemory_Test.Check_on_stack();

        Assert.True(result);
    }

    [Fact]
    public void clone()
    {
        bool result = UnamangedMemory_Test.Clone();

        Assert.True(result);
    }


    [Fact]
    public void FirstOrDefault()
    {
        bool result = UnamangedMemory_Test.FirstOrDefault();

        Assert.True(result);
    }


    [Fact]
    public void Where()
    {
        bool result = UnamangedMemory_Test.Where();

        Assert.True(result);
    }

    [Fact]
    public void HashCode()
    {
        bool result = UnamangedMemory_Test.HashCode();

        Assert.True(result);
    }


    //=============

}