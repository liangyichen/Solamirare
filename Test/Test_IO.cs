using Solamirare;
using Solamirare.Test;
using Xunit;



namespace Solamirare.Tests;


public unsafe class Test_IO
{
    

    public void IO_ReadTextFile()
    {
        bool result = IO_Test.ReadTextFile();


        Assert.True(result);
    }

    [Fact]
    public void IO_AppendTextFile()
    {
        Assert.True(IO_Test.AppendContent());
    }


    [Fact]
    public void IO_WriteTextFile()
    {
        Assert.True(IO_Test.WriteTextToFile());
    }


    [Fact]
    public void IO_DeleteFile()
    {
        Assert.True(IO_Test.DeleteFile());
    }



    [Fact]
    public void IO_FileBytesSize()
    {
        Assert.True(IO_Test.FileBytesSize());
    }

    [Fact]
    public void IO_FileExists()
    {
        Assert.True(IO_Test.FileExists());
    }

}