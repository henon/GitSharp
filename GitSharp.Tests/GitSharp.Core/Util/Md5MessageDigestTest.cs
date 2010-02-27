using System.Linq;
using System.Text;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp.Core.Util
{
    [TestFixture]
    public class Md5MessageDigestTest
    {
        [Test]
        public void EmptyString()
        {
            var expected = new byte[] { 256 - 44, 29, 256 - 116, 256 - 39, 256 - 113, 0, 256 - 78, 4, 256 - 23, 256 - 128, 9, 256 - 104, 256 - 20, 256 - 8, 66, 126 };

            MessageDigest md = CreateSUT();

            byte[] result = md.Digest();

            Assert.AreEqual(16, result.Length);
            Assert.IsTrue(expected.SequenceEqual(result));
        }

        [Test]
        public void ShortStringOneUpdate()
        {
            var expected = new byte[] { 101, 256 - 19, 26, 256 - 3, 85, 256 - 19, 125, 33, 256 - 20, 256 - 96, 256 - 100, 256 - 24, 256 - 54, 69, 256 - 87, 14 };

            MessageDigest md = CreateSUT();

            md.Update("nulltoken".getBytes());
            byte[] result = md.Digest();

            Assert.AreEqual(16, result.Length);
            Assert.IsTrue(expected.SequenceEqual(result));
        }

        [Test]
        public void ShortStringTwoUpdates()
        {
            var expected = new byte[] { 101, 256 - 19, 26, 256 - 3, 85, 256 - 19, 125, 33, 256 - 20, 256 - 96, 256 - 100, 256 - 24, 256 - 54, 69, 256 - 87, 14 };

            MessageDigest md = CreateSUT();

            md.Update("null".getBytes());
            md.Update("token".getBytes());
            byte[] result = md.Digest();

            Assert.AreEqual(16, result.Length);
            Assert.IsTrue(expected.SequenceEqual(result));
        }

        [Test]
        public void LongStringOneUpdate()
        {
            var expected = new byte[] { 256 - 3, 33, 256 - 57, 74, 256 - 58, 256 - 26, 72, 20, 85, 113, 119, 21, 256 - 74, 81, 120, 83 };

            MessageDigest md = CreateSUT();

            var sb = new StringBuilder();
            for (int i = 0; i < 20; i++)
            {
                sb.Append("nulltoken");
            }
            md.Update(sb.ToString().getBytes());

            byte[] result = md.Digest();

            Assert.AreEqual(16, result.Length);
            Assert.IsTrue(expected.SequenceEqual(result));
        }

        private static MessageDigest CreateSUT()
        {
            return MessageDigest.getInstance("MD5");
        }
    }
}