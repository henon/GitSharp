using System.Linq;
using GitSharp.Core;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests.Util
{
    [TestFixture]
    public class ByteArrayExtensionsFixture
    {
        [Test]
        public void ReadLine_CanExtractWithNoLineEnding()
        {
            byte[] input = "no newline".getBytes();
            var parsedLined = input.ReadLine(0);
            Assert.IsNotNull(parsedLined);
            Assert.AreEqual(input.Length, parsedLined.NextIndex);
            Assert.IsTrue(input.SequenceEqual(parsedLined.Buffer));
        }

        [Test]
        public void ReadLine_CanNotExtractWithOutOfRangePosition()
        {
            byte[] input = "no newline".getBytes();
            var parsedLined = input.ReadLine(10);
            Assert.IsNotNull(parsedLined);
            Assert.AreEqual(-1, parsedLined.NextIndex);
            Assert.IsNull(parsedLined.Buffer);
        }

        [Test]
        public void ReadLine_CanNotExtractAnEmptyByteArray()
        {
            var input = new byte[0];
            var parsedLined = input.ReadLine(0);
            Assert.IsNotNull(parsedLined);
            Assert.AreEqual(-1, parsedLined.NextIndex);
            Assert.IsNull(parsedLined.Buffer);
        }

        [Test]
        public void ReadLine_CanExtractAByteArrayContainingOnlyALF()
        {
            byte[] input = "\n".getBytes();
            var parsedLined = input.ReadLine(0);
            Assert.IsNotNull(parsedLined);
            Assert.AreEqual(1, parsedLined.NextIndex);
            Assert.IsTrue(new byte[0].SequenceEqual(parsedLined.Buffer));
        }

        [Test]
        public void ReadLine_CanExtractAByteArrayContainingOnlyACRLF()
        {
            byte[] input = "\r\n".getBytes();
            var parsedLined = input.ReadLine(0);
            Assert.IsNotNull(parsedLined);
            Assert.AreEqual(2, parsedLined.NextIndex);
            Assert.IsTrue(new byte[0].SequenceEqual(parsedLined.Buffer));
        }

        [Test]
        public void ReadLine_CanExtractWithACRAtTheEnd()
        {
            byte[] input = "no newline\r".getBytes();
            var parsedLined = input.ReadLine(0);
            Assert.IsNotNull(parsedLined);
            Assert.AreEqual(input.Length, parsedLined.NextIndex);
            Assert.IsTrue(input.SequenceEqual(parsedLined.Buffer));
        }

        [Test]
        public void ReadLine_CanExtractWithACRAtTheBegining()
        {
            byte[] input = "\rno newline".getBytes();
            var parsedLined = input.ReadLine(0);
            Assert.IsNotNull(parsedLined);
            Assert.AreEqual(input.Length, parsedLined.NextIndex);
            Assert.IsTrue(input.SequenceEqual(parsedLined.Buffer));
        }

        [Test]
        public void ReadLine_CanExtractFirstLineDelimitedWithALF()
        {
            const string firstLine = "first line";
            byte[] input = (firstLine + "\nsecondline").getBytes();
            var parsedLined = input.ReadLine(0);
            Assert.IsNotNull(parsedLined);
            Assert.AreEqual(firstLine.Length + 1, parsedLined.NextIndex);
            Assert.IsTrue((firstLine.getBytes()).SequenceEqual(parsedLined.Buffer));
        }

        [Test]
        public void ReadLine_CanExtractFirstLineDelimitedWithACRLF()
        {
            const string firstLine = "first line";
            byte[] input = (firstLine + "\r\nsecondline").getBytes();
            var parsedLined = input.ReadLine(0);
            Assert.IsNotNull(parsedLined);
            Assert.AreEqual(firstLine.Length + 2, parsedLined.NextIndex);
            Assert.IsTrue((firstLine.getBytes()).SequenceEqual(parsedLined.Buffer));
        }

        [Test]
        public void ReadLine_CanExtractSecondLineDelimitedWithALF()
        {
            const string firstLine = "first line";
            const string secondLine = "second line";
            byte[] input = (firstLine + "\n" + secondLine).getBytes();
            var parsedLined = input.ReadLine(firstLine.Length + 1);
            Assert.IsNotNull(parsedLined);
            Assert.AreEqual(input.Length, parsedLined.NextIndex);
            Assert.IsTrue((secondLine.getBytes()).SequenceEqual(parsedLined.Buffer));
        }

        [Test]
        public void ReadLine_CanExtractSecondLineDelimitedWithACRLF()
        {
            const string firstLine = "first line";
            const string secondLine = "second line";
            byte[] input = (firstLine + "\r\n" + secondLine).getBytes();
            var parsedLined = input.ReadLine(firstLine.Length + 2);
            Assert.IsNotNull(parsedLined);
            Assert.AreEqual(input.Length, parsedLined.NextIndex);
            Assert.IsTrue((secondLine.getBytes()).SequenceEqual(parsedLined.Buffer));
        }

        [Test]
        public void ReadLine_CanExtractSecondLineDelimitedWithALF2()
        {
            const string firstLine = "first line";
            const string secondLine = "second line";
            byte[] input = (firstLine + "\n" + secondLine + "\n").getBytes();
            var parsedLined = input.ReadLine(firstLine.Length + 1);
            Assert.IsNotNull(parsedLined);
            Assert.AreEqual(input.Length, parsedLined.NextIndex);
            Assert.IsTrue((secondLine.getBytes()).SequenceEqual(parsedLined.Buffer));
        }

        [Test]
        public void ReadLine_CanExtractSecondLineDelimitedWithACRLF2()
        {
            const string firstLine = "first line";
            const string secondLine = "second line";
            byte[] input = (firstLine + "\r\n" + secondLine + "\r\n").getBytes();
            var parsedLined = input.ReadLine(firstLine.Length + 2);
            Assert.IsNotNull(parsedLined);
            Assert.AreEqual(input.Length, parsedLined.NextIndex);
            Assert.IsTrue((secondLine.getBytes()).SequenceEqual(parsedLined.Buffer));
        }

        [Test]
        public void StartsWith_ReturnTrueeWhenInputStartsWithPrefix()
        {
            const string input = "hello world!";
            const string prefix = "hell";

            Assert.IsTrue(input.getBytes().StartsWith(prefix.getBytes()));
        }
        [Test]
        public void StartsWith_ReturnFalseWhenInputDoesNotStartWithPrefix()
        {
            const string input = "hello world!";
            const string prefix = "help";

            Assert.IsFalse(input.getBytes().StartsWith(prefix.getBytes()));
        }
        [Test]
        public void StartsWith_ReturnFalseWhenPrefixIsLongerThanInput()
        {
            const string input = "hello world!";
            const string prefix = "hello world! this is too long.";

            Assert.IsFalse(input.getBytes().StartsWith(prefix.getBytes()));
        }

    }
}
