
namespace Test
{
    public class TestReplace
    {
        IntellStringReplace nativeString;


        public TestReplace() {

            nativeString = new IntellStringReplace();
        }




        [Fact(DisplayName = "扩展方法替换")]
        public void TestExtensionReplace()
        {
            var old = "abcdefg";

            nativeString.Replace(ref old, "c", "9");

            Assert.True(old.Equals("ab9defg"));
        }

        [Fact(DisplayName = "扩展方法替换2")]
        public void TestExtensionReplace2()
        {
            var old = "abcdefg";

            nativeString.Replace(ref old, "c", "99");

            Assert.True(old.Equals("ab99defg"));
        }




        [Fact(DisplayName = "中部等长替换")]
        public void TestReplaceString1()
        {
            var old = "aaabbbcccdddeeefff";

            nativeString.Replace(ref old, "ccc", "999");

            Assert.True(old.Equals("aaabbb999dddeeefff"));
        }

        [Fact(DisplayName = "中部非等长多次替换")]
        public void TestReplaceString2()
        {
            var old = "aaabbbcccdddeeefff";

            nativeString.Replace(ref old, "bc", "Q");
            nativeString.Replace(ref old, "cd", "Q");

            var equal = old.Equals("aaabbQcQddeeefff");

            Assert.True(equal);
        }

        [Fact(DisplayName = "尾部等长替换")]
        public void TestReplaceString3()
        {
            var old = "aaabbbcccdddeeefff";

            nativeString.Replace(ref old, "fff", "___");

            Assert.True(old.Equals("aaabbbcccdddeee___"));
        }


        /// <summary>
        /// 首部等长替换
        /// </summary>
        [Fact(DisplayName = "首部等长替换")]
        public void TestReplaceString5()
        {
            var old = "aaabbbcccdddeeefff";

            nativeString.Replace(ref old, "aa", "cv");

            var equal = old.Equals("cvabbbcccdddeeefff");

            Assert.True(equal);
        }

        /// <summary>
        /// 首部非等长替换
        /// </summary>
        [Fact(DisplayName = "首部非等长替换")]
        public void TestReplaceString6()
        {
            var old = "aaabbbcccdddeeefff";

            nativeString.Replace(ref old, "aa", "cv00");

            var equal = old.Equals("cv00abbbcccdddeeefff");

            Assert.True(equal);
        }

        /// <summary>
        /// 首部空值测试
        /// </summary>
        [Fact(DisplayName = "首部空值替换")]
        public void TestReplaceString7()
        {
            var old = "aaabbbcccdddeeefff";

            nativeString.Replace(ref old, "aa", "");

            var equal = old.Equals("abbbcccdddeeefff");

            Assert.True(equal);
        }


        /// <summary>
        /// 中部空值测试
        /// </summary>
        [Fact(DisplayName = "中部空值替换")]
        public void TestReplaceString8()
        {
            var old = "aaabbbcccdddeeefff";

            nativeString.Replace(ref old, "cddde", "");

            var equal = old.Equals("aaabbbcceefff");

            Assert.True(equal);
        }


        [Fact(DisplayName = "中部非等长单次替换")]
        public void TestReplaceString()
        {
            var old = "aaabbbcccdddeeefff";

            nativeString.Replace(ref old, "eee", "____");

            var equal = old.Equals("aaabbbcccddd____fff");

            Assert.True(equal);

        }



        [Fact(DisplayName = "特殊字符替换")]
        public void TestSpeshesymbol()
        {
            var old = """<meta property="og:image" server="[?link-image-server?]" content="[?link-image-server?]public/xxx.jpg" />""";

            nativeString.Replace(ref old, "[?link-image-server?]", "/");

            var equal = old.Equals("""<meta property="og:image" server="/" content="/public/xxx.jpg" />""");

            Assert.True(equal);

        }



    }
}
