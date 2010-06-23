using System;
using System.IO;
using GitSharp.Core.Util;
using GitSharp.Tests.GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Core.Tests.Util
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

        [Test]
        public void DifferentLength_compareTo_1()
        {
            Assert.AreEqual(-1, "".compareTo("a"));
        }

        [Test]
        public void DifferentLength_compareTo_2()
        {
            Assert.AreEqual(-2, "".compareTo("aa"));
        }

        [Test]
        public void DifferentLength_compareTo_3()
        {
            Assert.AreEqual(1, "a".compareTo(""));
        }

        [Test]
        public void DifferentLength_compareTo_4()
        {
            Assert.AreEqual(2, "aa".compareTo(""));
        }

        [Test]
        public void DifferentLength_compareTo_5()
        {
            Assert.AreEqual(2, "bb".compareTo(""));
        }

        [Test]
        public void DifferentLength_compareTo_6()
        {
            Assert.AreEqual(-1, "AB".compareTo("B"));
        }

        [Test]
        public void DifferentLength_compareTo_7()
        {
            Assert.AreEqual(1, "B".compareTo("AB"));
        }

        [Test]
        public void SameLength_compareTo_1()
        {
            Assert.AreEqual(0, "A".compareTo("A"));
        }
        [Test]
        public void SameLength_compareTo_2()
        {
            Assert.AreEqual(32, "a".compareTo("A"));
        }
        [Test]
        public void SameLength_compareTo_3()
        {
            Assert.AreEqual(-32, "A".compareTo("a"));
        }

        [Test]
        public void SameLength_compareTo_4()
        {
            Assert.AreEqual(32, "aaa".compareTo("aaA"));
        }
        [Test]
        public void SameLength_compareTo_5()
        {
            Assert.AreEqual(-32, "aaA".compareTo("aaa"));
        }

        [Test]
        public void SameLength_compareTo_6()
        {
            Assert.AreEqual(32, "aaaa".compareTo("aaAB"));
        }
        [Test]
        public void SameLength_compareTo_7()
        {
            Assert.AreEqual(31, "aaAb".compareTo("aaAC"));
        }
        [Test]
        public void SameLength_compareTo_8()
        {
            Assert.AreEqual(2, "aaCb".compareTo("aaAa"));
        }

    }
}