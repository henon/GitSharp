using System;
using GitSharp.Tests.GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Core.Tests.Util
{
    [TestFixture]
    public class AssertHelperFixture
    {
        [Test]
        public void TestPassWhenThrowingTheCorrectException()
        {
            AssertHelper.Throws<InvalidOperationException>(() => { throw new InvalidOperationException(); });
        }

        [Test]
        public void WhenOfTheCorrectTypeThrownExceptionCanBeFurtherExamined()
        {
            var e = AssertHelper.Throws<InvalidOperationException>(() => { throw new InvalidOperationException("Hi from below"); });
            Assert.AreEqual("Hi from below", e.Message);
        }

        [Test]
        public void ThrownExceptionCanBeDerivedFromExpected()
        {
            var e = AssertHelper.Throws<Exception>(() => { throw new InvalidOperationException("Was invalid."); });

            var castE = (InvalidOperationException)e;
            Assert.AreEqual("Was invalid.", castE.Message);
        }

        [Test]
        public void ThrownExceptionHasToBeOfTheExactType()
        {
            try
            {
                AssertHelper.Throws<ArgumentOutOfRangeException>(() => { throw new InvalidOperationException(); });
            }
            catch (AssertionException e)
            {
                StringAssert.Contains(typeof (ArgumentOutOfRangeException).FullName, e.Message);
                StringAssert.Contains(typeof (InvalidOperationException).FullName, e.Message);
                return;
            }

            Assert.Fail();
        }

        [Test]
        public void NotThrowingExceptionLeadsTheTestToFail()
        {
            try
            {
                AssertHelper.Throws<ArgumentOutOfRangeException>(() => { return; });
            }
            catch (AssertionException e)
            {
                StringAssert.Contains(typeof (ArgumentOutOfRangeException).FullName, e.Message);
                return;
            }

            Assert.Fail();
        }
    }
}