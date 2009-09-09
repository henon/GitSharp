using Xunit;
using GitSharp.Util;

namespace GitSharp.Tests.Util
{
    public class VariousUtilityTests
    {
        [Fact]
        public void TestBitCount()
        {
            Assert.Equal(1, (2 << 5).BitCount());
            Assert.Equal(1, 1.BitCount());
            Assert.Equal(2, 3.BitCount());
        }

        [Fact]
        public void TestNumberOfTrailingZeros()
        {
            Assert.Equal(0, 1.NumberOfTrailingZeros());
            Assert.Equal(1, 2.NumberOfTrailingZeros());
            Assert.Equal(6, (2 << 5).NumberOfTrailingZeros());
            Assert.Equal(0, ((2 << 5)+1).NumberOfTrailingZeros());
        }

    }
}
