using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using GitSharp.Util;

namespace GitSharp.Tests
{
    [TestFixture]
    public class VariousUtilityTests
    {
        [Test]
        public void TestBitCount()
        {
            Assert.AreEqual(1, (2 << 5).BitCount());
            Assert.AreEqual(1, 1.BitCount());
            Assert.AreEqual(2, 3.BitCount());
        }

        [Test]
        public void TestNumberOfTrailingZeros()
        {
            Assert.AreEqual(0, 1.NumberOfTrailingZeros());
            Assert.AreEqual(1, 2.NumberOfTrailingZeros());
            Assert.AreEqual(6, (2 << 5).NumberOfTrailingZeros());
            Assert.AreEqual(0, ((2 << 5)+1).NumberOfTrailingZeros());
        }

    }
}
