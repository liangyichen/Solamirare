

using Solamirare.Tests;






[MemoryDiagnoser]
public class UnManagedMemory_Performance
{


    [Benchmark]
    public void Json_EncodeAndDecodeCollection()
    {
        UnamangedMemory_Test.EncodeAndDecodeCollection();
    }


    [Benchmark]
    public void Json_Objects()
    {
        UnamangedMemory_Test.Objects();
    }


    [Benchmark]
    public void Json_EncodeAndDecodeStrings()
    {
        UnamangedMemory_Test.EncodeAndDecodeStrings();
    }



    [Benchmark]
    public void SpiltCopy()
    {
        UnamangedMemory_Test.SpiltCopy();
    }


    [Benchmark]
    public void SpiltMap()
    {
        UnamangedMemory_Test.SpiltMap();
    }


    [Benchmark]
    public void SpiltCopyToValueFrozenDictionary()
    {
        UnamangedMemory_Test.SpiltCopyToValueFrozenDictionary();
    }


    [Benchmark]
    public void SpiltMapToValueFrozenDictionary()
    {
        UnamangedMemory_Test.SpiltMapToValueFrozenDictionary();
    }



    [Benchmark]
    public void ReadOnly()
    {
        UnamangedMemory_Test.ReadOnly();
    }


    [Benchmark]
    public void CopyTo()
    {
        UnamangedMemory_Test.CopyTo();
    }

    [Benchmark]
    public void ToBytes()
    {
        UnamangedMemory_Test.ToBytes();
    }


    [Benchmark]
    public void Sort()
    {
        UnamangedMemory_Test.Sort();
    }



    [Benchmark]
    public void Reverse()
    {
        UnamangedMemory_Test.Reverse();
    }




    [Benchmark]
    public void Heap_LastIndexOf()
    {
        UnamangedMemory_Test.Heap_LastIndexOf();
    }




    [Benchmark]
    public void EnsureCapacity()
    {
        UnamangedMemory_Test.EnsureCapacity();
    }



    [Benchmark]
    public void Concat()
    {
        UnamangedMemory_Test.Concat();
    }



    [Benchmark]
    public void SetValue()
    {
        UnamangedMemory_Test.SetValue();
    }




    [Benchmark]
    public void IndexOf_Short_Chars()
    {
        UnamangedMemory_Test.IndexOf_Short_Chars();
    }



    [Benchmark]
    public void IntToUnmanagedString()
    {
        UnamangedMemory_Test.IntToUnmanagedString();
    }



    [Benchmark]
    public void ForEech()
    {
        UnamangedMemory_Test.foraech();
    }




    [Benchmark]
    public void Replace()
    {
        UnamangedMemory_Test.Replace();
    }



    [Benchmark]
    public void ParseFromDateTime()
    {
        UnamangedMemory_Test.ParseFromDateTime();
    }



    [Benchmark]
    public void ParseFromLong()
    {
        UnamangedMemory_Test.ParseFromLong();
    }



    [Benchmark]
    public void ParseFromInt()
    {
        UnamangedMemory_Test.ParseFromInt();
    }


    [Benchmark]
    public void ParseFromDecimal()
    {
        UnamangedMemory_Test.ParseFromDecimal();
    }



    [Benchmark]
    public void IndexOfAny()
    {
        UnamangedMemory_Test.IndexOfAny();
    }


    [Benchmark]
    public void Override_Operate_Equals()
    {
        UnamangedMemory_Test.Override_Operate_Equals();
    }


    [Benchmark]
    public void Contains_Single()
    {
        UnamangedMemory_Test.Contains_Single();
    }


    [Benchmark]
    public void ForEachMethod()
    {
        UnamangedMemory_Test.ForEachMethod();
    }



    [Benchmark]
    public void Resize_Min()
    {
        UnamangedMemory_Test.Resize_Min();
    }



    [Benchmark]
    public void Check_on_stack()
    {
        UnamangedMemory_Test.Check_on_stack();
    }


    [Benchmark]
    public void Contains_Collection()
    {
        UnamangedMemory_Test.Contains_Collection();
    }


    [Benchmark]
    public void Count()
    {
        UnamangedMemory_Test.Count();
    }


    [Benchmark]
    public void Index()
    {
        UnamangedMemory_Test.Index();
    }


    [Benchmark]
    public void RemoveAt()
    {
        UnamangedMemory_Test.RemoveAt();
    }


    [Benchmark]
    public void IndexOf_Single_String()
    {
        UnamangedMemory_Test.IndexOf_Single_String();
    }



    [Benchmark]
    public void IndexOf_Single_Char()
    {
        UnamangedMemory_Test.IndexOf_Single_Char();
    }



    [Benchmark]
    public void AsRealSizeSpan()
    {
        UnamangedMemory_Test.AsRealSizeSpan();
    }



    [Benchmark]
    public void AsSpan()
    {
        UnamangedMemory_Test.AsSpan();
    }



    [Benchmark]
    public void IndexOf_Short_Bytes()
    {
        UnamangedMemory_Test.IndexOf_Short_Bytes();
    }



    [Benchmark]
    public void IndexOf_BYTE()
    {
        UnamangedMemory_Test.IndexOf_BYTE();
    }



    [Benchmark]
    public void Slice()
    {
        UnamangedMemory_Test.Slice();
    }



    [Benchmark]
    public void IndexOf_struct()
    {
        UnamangedMemory_Test.IndexOf_struct();
    }



    [Benchmark]
    public void IndexOf_INT()
    {
        UnamangedMemory_Test.IndexOf_INT();
    }



    [Benchmark]
    public void IndexsOf_Chars()
    {
        UnamangedMemory_Test.IndexsOf_Chars();
    }



    [Benchmark]
    public void InsertAt()
    {
        UnamangedMemory_Test.InsertAt();
    }



    [Benchmark]
    public void InsertCollectionAt()
    {
        UnamangedMemory_Test.InsertCollectionAt();
    }



    [Benchmark]
    public void RemoveRange()
    {
        UnamangedMemory_Test.RemoveRange();
    }



    [Benchmark]
    public void ReSize()
    {
        UnamangedMemory_Test.ReSize();
    }


    [Benchmark]
    public void GetPointer()
    {
        UnamangedMemory_Test.GetPointer();
    }



    [Benchmark]
    public void AutoIndexMemory()
    {
        UnamangedMemory_Test.AutoIndexMemory();
    }


    [Benchmark]
    public void From_Span()
    {
        UnamangedMemory_Test.From_Span();
    }



    [Benchmark]
    public void From_ExtMemory()
    {
        UnamangedMemory_Test.From_ExtMemory();
    }


    [Benchmark]
    public void Create_Empty()
    {
        UnamangedMemory_Test.Create_Empty();
    }


    [Benchmark]
    public void Empty_to_Allocted()
    {
        UnamangedMemory_Test.Empty_to_Allocted();
    }



    [Benchmark]
    public void Reset_From_ExternalMemory()
    {
        UnamangedMemory_Test.Reset_From_ExternalMemory();
    }


    [Benchmark]
    public void Clone()
    {
        UnamangedMemory_Test.Clone();
    }


}