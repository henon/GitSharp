using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.Commands;
using GitSharp.Core;
using NUnit.Framework;
using System.IO;

namespace GitSharp.API.Tests
{
    [TestFixture]
    public class FindGitDirectoryTests : GitSharp.Core.Tests.RepositoryTestCase
    {
        [Test]
        public void Honors_environment_variable_GIT_DIR()
        {
            //Store GIT_DIR value temporarily
            string tempGitDir = System.Environment.GetEnvironmentVariable("GIT_DIR");
            try
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "test1");
                System.Environment.SetEnvironmentVariable("GIT_DIR", path);
                Git.DefaultGitDirectory = null; // override fallback
                Assert.AreEqual(path + Constants.DOT_GIT_EXT, AbstractCommand.FindGitDirectory(null, false, true));
                Assert.AreEqual(Path.Combine(path, Constants.DOT_GIT), AbstractCommand.FindGitDirectory(null, false, false));
            }
            finally
            {
                //Reset GIT_DIR value to initial value before the test
                System.Environment.SetEnvironmentVariable("GIT_DIR", tempGitDir);
            }
        }

        [Test]
        public void Honors_CurrentDirectory()
        {
            string tempGitDir = System.Environment.GetEnvironmentVariable("GIT_DIR");
            try
            {
                //current directory is returned only if path, global fallback and envvar are all null
                Git.DefaultGitDirectory = null; // override fallback
                System.Environment.SetEnvironmentVariable("GIT_DIR", null); // override environment
                var path = Directory.GetCurrentDirectory();
                Assert.IsFalse(path.EndsWith("git")); // <--- this should be the case anyway, but if not the next assertion would not pass correctly
                Assert.AreEqual(Directory.GetCurrentDirectory() + Constants.DOT_GIT_EXT, AbstractCommand.FindGitDirectory(null, false, true));
                Assert.AreEqual(Path.Combine(Directory.GetCurrentDirectory(), Constants.DOT_GIT), AbstractCommand.FindGitDirectory(null, false, false));
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("GIT_DIR", tempGitDir);
            }
        }

        [Test]
        public void Explicit_path_is_preferred()
        {
            // it should override global fallback
            Git.DefaultGitDirectory = "abc/def/ghi";
            Assert.AreEqual("xyz.git", AbstractCommand.FindGitDirectory("xyz", false, true));
            Assert.AreEqual(Path.Combine("xyz",Constants.DOT_GIT), AbstractCommand.FindGitDirectory("xyz", false, false));

            // it should override env var
            Git.DefaultGitDirectory = null;
            string tempGitDir = System.Environment.GetEnvironmentVariable("GIT_DIR");
            try
            {
                System.Environment.SetEnvironmentVariable("GIT_DIR", "uvw");
                Assert.AreEqual("xyz.git", AbstractCommand.FindGitDirectory("xyz", false, true));
                Assert.AreEqual(Path.Combine("xyz", Constants.DOT_GIT), AbstractCommand.FindGitDirectory("xyz", false, false));
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("GIT_DIR", tempGitDir);
            }
        }
    }
}