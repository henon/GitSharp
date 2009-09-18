using System;
using System.IO;
using GitSharp.Util;
using Xunit;

namespace GitSharp.Tests
{
    public class StringExtensionsFixture
    {
        [StrictFactAttribute]
        public void GetBytesShouldNotGenerateABOMWhenWorkingInUTF8()
        {
            string filePath = Path.GetTempFileName();

            File.WriteAllBytes(filePath, "a".getBytes("UTF-8"));

            Assert.Equal(1, new FileInfo(filePath).Length);
        }

        [StrictFactAttribute]
        public void GetBytesShouldThrowIfPassedAnUnknownEncodingAlias()
        {
            Assert.Throws<ArgumentException>(() => "a".getBytes("Dummy"));
        }

        [StrictFactAttribute]
        public void SliceShouldReturnExpectedResult()
        {
            Assert.Equal("urge", "hamburger".Slice(4, 8));
            Assert.Equal("mile", "smiles".Slice(1, 5));
        }

        [StrictFactAttribute]
        public void SliceShouldThrowIfBeginIndexIsNegative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => "hamburger".Slice(-1, 8));
        }

        [StrictFactAttribute]
        public void SliceShouldThrowIfEndIndexIsGreaterThanTheLengthOfTheString()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => "hamburger".Slice(4, 42));
        }

        [StrictFactAttribute]
        public void SliceShouldThrowIfBeginIndexIsGreaterThanEndIndex()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => "hamburger".Slice(8, 4));
        }
    }
}