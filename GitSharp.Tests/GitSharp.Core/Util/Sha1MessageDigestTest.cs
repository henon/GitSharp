using System.Linq;
using System.Text;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp.Core.Util
{
    [TestFixture]
    public class Sha1MessageDigestTest
    {
        [Test]
        public void EmptyString()
        {
            var expected = new byte[] { 256 - 38, 57, 256 - 93, 256 - 18, 94, 107, 75, 13, 50, 85, 256 - 65, 256 - 17, 256 - 107, 96, 24, 256 - 112, 256 - 81, 256 - 40, 7, 9 };

            MessageDigest md = CreateSUT();

            byte[] result = md.Digest();

            Assert.AreEqual(20, result.Length);
            Assert.IsTrue(expected.SequenceEqual(result));
        }

        [Test]
        public void ShortStringOneUpdate()
        {
            var expected = new byte[] { 48, 15, 76, 31, 256 - 27, 18, 256 - 16, 66, 256 - 67, 256 - 20, 8, 70, 256 - 23, 114, 104, 256 - 49, 113, 97, 55, 256 - 65 };

            MessageDigest md = CreateSUT();

            md.Update("nulltoken".getBytes());
            byte[] result = md.Digest();

            Assert.AreEqual(20, result.Length);
            Assert.IsTrue(expected.SequenceEqual(result));
        }

        [Test]
        public void ShortStringTwoUpdates()
        {
            var expected = new byte[] { 48, 15, 76, 31, 256 - 27, 18, 256 - 16, 66, 256 - 67, 256 - 20, 8, 70, 256 - 23, 114, 104, 256 - 49, 113, 97, 55, 256 - 65 };

            MessageDigest md = CreateSUT();

            md.Update("null".getBytes());
            md.Update("token".getBytes());
            byte[] result = md.Digest();

            Assert.AreEqual(20, result.Length);
            Assert.IsTrue(expected.SequenceEqual(result));
        }

        [Test]
        public void LongStringOneUpdate()
        {
            var expected = new byte[] { 256 - 25, 115, 256 - 78, 84, 256 - 32, 116, 38, 256 - 76, 256 - 96, 85, 256 - 69, 256 - 88, 89, 256 - 81, 256 - 41, 35, 256 - 99, 39, 256 - 52, 86 };

            MessageDigest md = CreateSUT();

            var sb = new StringBuilder();
            for (int i = 0; i < 20; i++)
            {
                sb.Append("nulltoken");
            }
            md.Update(sb.ToString().getBytes());

            byte[] result = md.Digest();

            Assert.AreEqual(20, result.Length);
            Assert.IsTrue(expected.SequenceEqual(result));
        }

        private static MessageDigest CreateSUT()
        {
            return MessageDigest.getInstance("SHA-1");
        }
    }
}
