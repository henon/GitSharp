using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;

namespace Git.Tests
{
    [TestFixture]
    public class FindGitDirectoryTests : GitSharp.Tests.RepositoryTestCase
    {
        [Test]
        public void Honors_EnvVar_GIT_DIR()
        {
            //Store GIT_DIR value temporarily
            string tempGitDir = System.Environment.GetEnvironmentVariable("GIT_DIR");

            //Verify Environment Variable GIT_DIR
            var path = Path.Combine(Directory.GetCurrentDirectory(), "test1");
            System.Environment.SetEnvironmentVariable("GIT_DIR", path);
            var result = Git.Commands.FindGitDirectory(path, false, false);
            Assert.AreEqual(result, Path.Combine(path, ".git"));

            //Reset GIT_DIR value to initial value before the test
            System.Environment.SetEnvironmentVariable("GIT_DIR", tempGitDir);
        }

        [Test]
        public void Honors_CLI_Option_GIT_DIR()
        {
            //Verify specified directory
            var path = Path.Combine(Directory.GetCurrentDirectory(), "test");
            Git.Commands.GitDirectory = path; // <--- cli option --git_dir sets this global variable
            var result = Git.Commands.FindGitDirectory(path, false, false);
            Assert.AreEqual(Path.Combine(path, ".git"), result);
        }

        [Test]
        public void Honors_CurrentDirectory()
        {
            //Verify current directory (default, if the other three tests are empty)
            Git.Commands.GitDirectory = null;
            var path = Directory.GetCurrentDirectory();
            var result = Git.Commands.FindGitDirectory(path, false, false);
            Assert.AreEqual(Path.Combine(path, ".git"), result);
        }
    }
}
