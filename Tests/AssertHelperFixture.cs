using System;
using Xunit;

namespace GitSharp.Tests
{
    public class AssertHelperFixture
    {
		/*
        [Fact]
        public void TestPassWhenThrowingTheCorrectException()
        {
            AssertHelper.Throws<InvalidOperationException>(() => { throw new InvalidOperationException(); });
        }

        [Fact]
        public void WhenOfTheCorrectTypeThrownExceptionCanBeFurtherExamined()
        {
            var e = AssertHelper.Throws<InvalidOperationException>(() => { throw new InvalidOperationException("Hi from below"); });
            Assert.Equal("Hi from below", e.Message);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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
		 * */
    }
}