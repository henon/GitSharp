using System;
using GitSharp.Core;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp.Core.Util
{
    public enum AssertedPlatform
    {
        Windows,
        Mono
    }

    public static class AssertHelper
    {
        public static TException Throws<TException, TParam>(TParam param, Action<TParam> codeBlock) where TException : Exception
        {
            return Throws<TException>(() => codeBlock(param));
        }

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
        
        public static bool IsRunningOn(AssertedPlatform assertedPlatform)
        {
            SystemReader systemReader = SystemReader.getInstance();
            
            bool isRunningOnUnknownOS = (systemReader.getOperatingSystem() == PlatformType.Unknown);
            if (isRunningOnUnknownOS)
            {
                return false;
            }
            
            
            bool isRunningOnWindows = (systemReader.getOperatingSystem() == PlatformType.Windows);
            if (isRunningOnWindows && assertedPlatform == AssertedPlatform.Windows)
            {
                return true;
            }

            if (!isRunningOnWindows && assertedPlatform == AssertedPlatform.Mono)
            {
                return true;
            }

            return false;
        }

        public static void IgnoreOn(AssertedPlatform assertedPlatform, Action codeBlock, string ignoreExplaination)
        {
            if (IsRunningOn(assertedPlatform))
            {
                Assert.Ignore(ignoreExplaination);
                return;
            }

            codeBlock();
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