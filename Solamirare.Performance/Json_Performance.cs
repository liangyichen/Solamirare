using Solamirare.Tests;

[MemoryDiagnoser]
public unsafe class Json_Performance
{



    [Benchmark]
    public void CollectionDecode()
    {
        Json_Test.CollectionDecode();
    }

    [Benchmark]
    public void ObjectStringToDictionary()
    {
        Json_Test.ObjectStringToDictionary();
    }


    [Benchmark]
    public void BaseDecode()
    {
        Json_Test.BaseDecode();
    }

    [Benchmark]
    public void DocumentToString()
    {
        Json_Test.DocumentToString();
    }

    [Benchmark]
    public void Objects()
    {
        UnamangedMemory_Test.Objects();
    }

    [Benchmark]
    public void EncodeAndDecodeCollection()
    {
        UnamangedMemory_Test.EncodeAndDecodeCollection();
    }

}