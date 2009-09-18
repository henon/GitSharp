using GitSharp.Util;
using Xunit;

namespace GitSharp.Tests
{
    public class Crc32Tests
    {
        [StrictFactAttribute]
        public void Tests()
        {
            var crc = new Crc32();
            Assert.Equal((uint)0, crc.Value);
            crc.Update(145);
			Assert.Equal((uint)1426738271, crc.Value);
			crc.Update(123456789);
			Assert.Equal((uint)1147030863, crc.Value);
            var data = new byte[] { 145, 234, 156 };
            crc.Update(data);
            Assert.Equal(3967437022, crc.Value);
        }
    }
}