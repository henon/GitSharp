using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace GitSharp.Tests
{
    [TestFixture]
    public class Crc32Tests
    {
        [Test]
        public void Tests()
        {
            var crc = new Crc32();
            Assert.AreEqual(0, crc.Value);
            crc.Update(145);
            Assert.AreEqual(1426738271, crc.Value);
            crc.Update(123456789);
            Assert.AreEqual(1147030863, crc.Value);
            byte[] data = new byte[] { 145, 234, 156 };
            crc.Update(data);
            Assert.AreEqual(3967437022, crc.Value);
        }
    }
}
