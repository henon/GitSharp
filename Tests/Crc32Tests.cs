using System;
using GitSharp.Util;
using Xunit;

namespace GitSharp.Tests
{
    public class Crc32Tests
    {
        [Fact]
        public void Tests()
        {
            var crc = new Crc32();
			Assert.Equal(0, Convert.ToInt32(crc.Value));
            crc.Update(145);
            Assert.Equal(1426738271, Convert.ToInt32(crc.Value));
            crc.Update(123456789);
			Assert.Equal(1147030863, Convert.ToInt32(crc.Value));
            var data = new byte[] { 145, 234, 156 };
            crc.Update(data);
            Assert.Equal(3967437022, crc.Value);
        }
    }
}