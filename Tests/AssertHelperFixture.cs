using System;
using NUnit.Framework;

namespace GitSharp.Tests
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
        public void ThrownExceptionCannotBeDerivedFromExpected()
        {
            try
            {
                AssertHelper.Throws<Exception>(() => { throw new InvalidOperationException(); });
            }
            catch (AssertionException e)
            {
                StringAssert.Contains(typeof (Exception).FullName, e.Message);
                StringAssert.Contains(typeof (InvalidOperationException).FullName, e.Message);
            }
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
            }
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
            }
        }
    }
}