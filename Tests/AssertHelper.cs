using System;
using Xunit;

namespace GitSharp.Tests
{
    public static class AssertHelper
    {
		/*
        public static TException Throws<TException>(Action codeBlock) where TException : Exception
        {
            const string expectedFormat = "Exception of type '{0}' was expected.";
            const string insteadFormat = "Instead, exception of type '{0}' was thrown.";

            string expectedMessage = string.Format(expectedFormat, typeof(TException).FullName);

            Exception exception = GetExceptionFrom(codeBlock);

            if (exception == null)
            {
                Assert.False(true, expectedMessage);
                return null;
            }

            if (exception.GetType() != typeof(TException))
            {
                string insteadMessage = string.Format(insteadFormat, exception.GetType());
                Assert.False(true, string.Format("{0} {1}", expectedMessage, insteadMessage));
            }

            return (TException)exception;
        }

        private static Exception GetExceptionFrom(Action code)
        {
            try
            {
                code();
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }
		 * */
    }
}