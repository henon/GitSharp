using System;
using NUnit.Framework;

namespace GitSharp.Tests
{
    public static class AssertHelper
    {
        public static TException Throws<TException>(Action codeBlock) where TException : Exception
        {
            const string expectedFormat = "Exception of type '{0}' was expected.";
            const string insteadFormat = "Instead, exception of type '{0}' was thrown.";

            string expectedMessage = string.Format(expectedFormat, typeof(TException).FullName);


            Exception exception = GetExceptionFrom(codeBlock);

            if (exception == null)
            {
                Assert.Fail(expectedMessage);
                return null;
            }

            if (!(typeof(TException).IsAssignableFrom(exception.GetType())))
            {
                string insteadMessage = string.Format(insteadFormat, exception.GetType());
                Assert.Fail(string.Format("{0} {1}", expectedMessage, insteadMessage));
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
    }
}