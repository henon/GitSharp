using System;
using System.IO;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests
{
    [TestFixture]
    public class StringExtensionsFixture
    {
        [Test]
        public void GetBytesShouldNotGenerateABOMWhenWorkingInUTF8()
        {
            string filePath = Path.GetTempFileName();

            File.WriteAllBytes(filePath, "a".getBytes("UTF-8"));

            Assert.AreEqual(1, new FileInfo(filePath).Length);
        }

        [Test]
        public void GetBytesShouldThrowIfPassedAnUnknownEncodingAlias()
        {
            AssertHelper.Throws<ArgumentException>(() => "a".getBytes("Dummy"));
        }

        [Test]
        public void SliceShouldReturnExpectedResult()
        {
            Assert.AreEqual("urge", "hamburger".Slice(4, 8));
            Assert.AreEqual("mile", "smiles".Slice(1, 5));
        }

        [Test]
        public void SliceShouldThrowIfBeginIndexIsNegative()
        {
            AssertHelper.Throws<ArgumentOutOfRangeException>(() => "hamburger".Slice(-1, 8));
        }

        [Test]
        public void SliceShouldThrowIfEndIndexIsGreaterThanTheLengthOfTheString()
        {
            AssertHelper.Throws<ArgumentOutOfRangeException>(() => "hamburger".Slice(4, 42));
        }

        [Test]
        public void SliceShouldThrowIfBeginIndexIsGreaterThanEndIndex()
        {
            AssertHelper.Throws<ArgumentOutOfRangeException>(() => "hamburger".Slice(8, 4));
        }
    }
}