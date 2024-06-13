
global using Solamirare;
global using Solamirare.Extention;
global using Xunit;
global using Solamirare.Net;
using System.Security.Cryptography;

namespace Test
{
    public class TestEncrypt
    {


        [Fact]
        public void TestAES()
        {
            var source = "ECANNGXYDGNlkmdRO3  PXVOYFDVBWYBXlbkwfbdxyjnfjbycvy3ecohxAIRJKWATRHDRAGONRW";
            var key = "nnnn";
            var iv = "xxxbhlgllvx";


            var enc = source.AsEncrypt(Solamirare.Encrypt.EncryptType.AES, key, iv);


            (bool Success, string Value) dec = GeneralApplication.AESDecrypt(enc.Value, key, iv);

            bool equal = source.Equals(dec.Value);

            Assert.True(equal);
        }

        [Fact]
        public void TestMac512()
        {
            var source = "huy7t6giuh";
            var key = "nnnn";

            //通过第三方 hmac-sha512 在线计算得出： https://www.freeformatter.com/hmac-generator.html


            var test = "aa8cbf014d16997e9a0a6b9f484a32b1c98e03c997fa0cffc50b5957c0c2a04a22d9c5a70bfc8231a2fdd403820e2b64c3c94a82ee556ef8ee938625cb5d09ce";


            var enc = source.AsEncrypt(Solamirare.Encrypt.EncryptType.HMAC_512, key);

            var equal = test.Equals(enc.Value, StringComparison.OrdinalIgnoreCase);

            Assert.True(equal);
        }

        //[Fact]
        public async Task TestHttpGet()
        {
            var req = await Http.Get("http://127.0.0.1:5123/api/Online");

            var equal = req.Core!.Contains("ok");

            Assert.True(equal);
        }

        //[Fact]
        public async Task TestPostJson()
        {
            var checkValue = "iiicfew43r422tg443r2q34dt63y76fu75vj8b5t4w5zxcvtbinknib7u6hjrtve5i";

            var data = new { a = 100, b = checkValue };

            var json = await data.SerializeToJsonString();

            var req = await Http.PostBodyByJson("http://127.0.0.1:5123/api/GetBody", json);

            var equal = req.Core!.Contains(checkValue);

            Assert.True(equal);
        }

        //[Fact]
        public async Task TestPostData()
        {
            var checkValue = "iiicfew43r422tg443r2q34dt63y76fu75vj8b5t4w5zxcvtbinknib7u6hjrtve5i";
            var dic = new Dictionary<string, string>() {

                { "a","999"},
                { "b",checkValue}
            };

            var req = await Http.Post("http://127.0.0.1:5123/api/getposts", dic);

            var equal = req.Core.Contains(checkValue);

            Assert.True(equal);

        }



    }
}
