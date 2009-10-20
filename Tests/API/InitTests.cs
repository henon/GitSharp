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
        public void IsBare()
        {
            //Test bare repository
            bool bare = true;
            DirectoryInfo path = new DirectoryInfo(Path.Combine(trash.FullName, "test.git"));
            Git.Commands.GitDirectory = Git.Commands.FindGitDirectory(path, false, bare);
        	Git.Repository repo = Git.Repository.Init(bare);
            Assert.IsTrue(Repository.IsValid(Git.Commands.GitDirectory.FullName, bare));
            Assert.IsTrue(repo.IsBare);
            Git.Commands.GitDirectory = null;
        }

        [Test]
        public void IsNonBare()
        {
            //Test non-bare repository
            bool bare = false;
            DirectoryInfo path = new DirectoryInfo(Path.Combine(trash.FullName, "test"));
            Git.Commands.GitDirectory = Git.Commands.FindGitDirectory(path, false, bare);
            Git.Repository repo = Git.Repository.Init(bare);
            Assert.IsTrue(Repository.IsValid(Git.Commands.GitDirectory.FullName, bare));
            Assert.IsFalse(repo.IsBare);
            Git.Commands.GitDirectory = null;
        }

        [Test]
        public void IsBareValid()
        {
            //Test bare repository
            bool bare = true;
            DirectoryInfo path = new DirectoryInfo(Path.Combine(trash.FullName, "test.git"));
        	Git.Commands.GitDirectory = Git.Commands.FindGitDirectory(path, false, bare);
            Git.Repository repo = Git.Repository.Init(bare);
            Assert.IsTrue(repo.IsBare);
            Assert.IsTrue(Repository.IsValid(Git.Commands.GitDirectory.FullName, bare));
        }

        [Test]
        public void IsNonBareValid()
        {
            //Test non-bare repository
            bool bare = false;
            DirectoryInfo path = new DirectoryInfo(Path.Combine(trash.FullName, "test"));
        	Git.Commands.GitDirectory = Git.Commands.FindGitDirectory(path, false, bare);
            Git.Repository repo = Git.Repository.Init(bare);
            Assert.IsFalse(repo.IsBare);
            Assert.IsTrue(Repository.IsValid(Git.Commands.GitDirectory.FullName, bare));
        }
	}
}
