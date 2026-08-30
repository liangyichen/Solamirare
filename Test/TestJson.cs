
using Solamirare.Test;
using Xunit;

namespace Solamirare.Tests;

public unsafe class Test_Json
{


    [Fact]
    public void JsonObjectStringToDictionary()
    {
        Assert.True(Json_Test.ObjectStringToDictionary());
    }

    [Fact]
    public void JsonCollectionDecode()
    {
        Assert.True(Json_Test.CollectionDecode());
    }

    [Fact]
    public void BaseDecode()
    {
        Assert.True(Json_Test.BaseDecode());
    }



    [Fact]
    public void JsonDocumentToString()
    {
        Assert.True(Json_Test.DocumentToString());
    }



    /// <summary>
    /// 测试大型文件的序列化与反序列化还原（单字符串）
    /// </summary>
    [Fact]
    public void LargeFiles()
    {
        Assert.True(Json_Test.LargeFiles());
    }




    [Fact]
    public void Json_Objects()
    {
        Assert.True(UnamangedMemory_Test.Objects());
    }





    [Fact]
    public void JSON_EncodeAndDecodeCollection()
    {
        Assert.True(UnamangedMemory_Test.EncodeAndDecodeCollection());
    }



}