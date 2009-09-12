using System;
using GitSharp.Util;
using NUnit.Framework;

namespace GitSharp.Tests
{
    [TestFixture]
    public class StringExtensionsFixture
    {
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