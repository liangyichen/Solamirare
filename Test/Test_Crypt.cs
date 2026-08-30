using System;
using System.Collections.Generic;
using System.Text;
using Xunit;


namespace Solamirare.Tests;

public class Test_Crypt
{

    [Fact]
    public void AES256GCM()
    {
        Assert.True(Crypt_Test.AES256GCM());
    }

    [Fact]
    public void AES128GCM()
    {
        Assert.True(Crypt_Test.AES256GCM());
    }


    [Fact]
    public void HMACSha256()
    {
        Assert.True(Crypt_Test.HMACSha256());
    }


    [Fact]
    public void HmacSha384()
    {
        Assert.True(Crypt_Test.HmacSha384());
    }


    [Fact]
    public void Sha384()
    {
        Assert.True(Crypt_Test.Sha384());
    }


    [Fact]
    public void Sha256()
    {
        Assert.True(Crypt_Test.Sha256());
    }

    [Fact]
    public void EcdheP256()
    {
        Assert.True(Crypt_Test.EcdheP256());
    }
}

