using Xunit;


namespace Solamirare.Tests;

    public unsafe class Test_UnmanagedCollection
    {
        [Fact]
        public void IndexOf_SIMD()
        {
            Assert.True(UnamangedCollection_Test.IndexOf_SIMD());
        }

        [Fact]
        public void @foreach()
        {
            Assert.True(UnamangedCollection_Test.foraech());
        }
    }
