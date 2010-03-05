using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using GitSharp.CLI;

namespace Git.Tests.CLI
{
    [TestFixture]
    class CustomOptionTests
    {
        [Test]
        public void CanParseOptions()
        {
            string[] args = {"--quiet", "--unused", "--verbose", "--", "path1", "path2"};

            //Test without multi-path option
            //Simulates method that uses: argumentsRemaining = ParseOptions(args);
            GitSharp.CLI.UnitTest cmd = new UnitTest();
            cmd.ProcessMultiplePaths = false;
            cmd.Run(args);
            Assert.AreEqual(new List<String> { "--unused" }, cmd.ArgumentsRemaining);
            Assert.IsNull(cmd.FilePaths);
            
            //Test with multi-path option
            //Simulates method that uses: ParseOptions(args, out filePaths, out argumentsRemaining)
            cmd = new UnitTest();
            cmd.ProcessMultiplePaths = true;
            cmd.Run(args);
            Assert.AreEqual(new List<String> { "--unused" }, cmd.ArgumentsRemaining);
            Assert.AreEqual(new List<String> { "path1", "path2" }, cmd.FilePaths);
        }
    }
}
