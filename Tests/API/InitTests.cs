/*
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

using System.IO;

namespace Git.Tests
{
    [TestFixture]
    public class InitTests : GitSharp.Tests.RepositoryTestCase
    {
        [Test]
        public void Init_honors_environment_variable_GIT_DIR()
        {
            //Store GIT_DIR value temporarily
            string tempGitDir = System.Environment.GetEnvironmentVariable("GIT_DIR");
            try
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "test1");
                System.Environment.SetEnvironmentVariable("GIT_DIR", path);
                var init = new InitCommand();
                Commands.GitDirectory = null; // override fallback
                Assert.AreEqual(Path.Combine(path, ".git"), init.ActualPath);
            }
            finally
            {
                //Reset GIT_DIR value to initial value before the test
                System.Environment.SetEnvironmentVariable("GIT_DIR", tempGitDir);
            }
        }

        [Test]
        public void Init_honors_global_fallback_gitdir()
        {
            //Verify specified directory
            var path = Path.Combine(Directory.GetCurrentDirectory(), "test");
            Git.Commands.GitDirectory = path; // <--- cli option --git_dir sets this global variable. it is a fallback value for all commands
            var init = new InitCommand();
            Assert.AreEqual(Path.Combine(path, ".git"), init.ActualPath);
        }

        [Test]
        public void Init_Honors_CurrentDirectory()
        {
            string tempGitDir = System.Environment.GetEnvironmentVariable("GIT_DIR");
            try
            {
                //current directory is returned only if global fallback and envvar are null
                Git.Commands.GitDirectory = null; // override fallback
                System.Environment.SetEnvironmentVariable("GIT_DIR", null); // override environment
                var path = Directory.GetCurrentDirectory();
                var init = new InitCommand();
                Assert.AreEqual(Path.Combine(Directory.GetCurrentDirectory(), ".git"), init.ActualPath);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("GIT_DIR", tempGitDir);
            }
        }

        [Test]
        public void Explicitely_set_path_overrides_everything()
        {
            // override global fallback
            Git.Commands.GitDirectory = "abc/def/ghi";
            var init = new InitCommand() { Path = "xyz" };
            Assert.AreEqual(Path.Combine(init.Path, ".git"), init.ActualPath);

            // override env var
            Git.Commands.GitDirectory = null;
            string tempGitDir = System.Environment.GetEnvironmentVariable("GIT_DIR");
            try
            {
                Assert.AreEqual(Path.Combine(init.Path, ".git"), init.ActualPath);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("GIT_DIR", tempGitDir);
            }
        }

        [Test]
        public void IsBare()
        {
            //Test bare repository
            bool bare = true;
            var path = Path.Combine(trash.FullName, "test.git");
            var repo = Repository.Init(path, bare);
            Assert.IsTrue(repo.IsBare);
        }

        [Test]
        public void IsNonBare()
        {
            //Test non-bare repository
            bool bare = false;
            var path = Path.Combine(trash.FullName, "test");
            var repo = Repository.Init(path, bare);
            Assert.IsFalse(repo.IsBare);
        }


        [Test]
        public void IsBareValid()
        {
            Assert.Ignore("Bare repo validity check is not yet implemented");
            //Test bare repository
            bool bare = true;
            var path = Path.Combine(trash.FullName, "test.git");
            var repo = Repository.Init(path, bare);
            Assert.IsTrue(repo.IsBare);
            Assert.IsTrue(Repository.IsValid(repo.Directory, bare));
        }

        [Test]
        public void IsNonBareValid()
        {
            //Test non-bare repository
            bool bare = false;
            var path = Path.Combine(trash.FullName, "test");
            var repo = Repository.Init(path, bare);
            Assert.IsFalse(repo.IsBare);
            Assert.IsTrue(Repository.IsValid(repo.Directory, bare));
        }
    }
}
