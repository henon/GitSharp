/*
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 * Copyright (C) 2009, Rolenun <rolenun@gmail.com>
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
using GitSharp.Core;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;
using GitSharp.Tests.GitSharp.Core.Util;
using NUnit.Framework;
using System.IO;
using GitSharp.Core.Tests;

namespace GitSharp.Tests.GitSharp
{
    [TestFixture]
    public class CloneTests : RepositoryTestCase
    {
        [Test]
        public void Check_cloned_bare_repo()
        {
            Assert.Ignore("This test has not been implemented yet.");
        }

        [Test]
        public void Check_cloned_repo_git()
        {
            string toPath = Path.Combine(trash.FullName, "test");
            string fromUrl = "git://github.com/henon/test.git";

            using (Repository repo = Git.Clone(fromUrl, toPath))
            {
                Assert.IsTrue(Repository.IsValid(repo.Directory));
                //Verify content is in the proper location
                var readme = Path.Combine(repo.WorkingDirectory, "README.txt");
                Assert.IsTrue(new FileInfo(readme).Exists);
            }
        }

        [Test]
        public void Try_cloning_non_existing_repo_git()
        {
            string toPath = Path.Combine(trash.FullName, "test");
            string fromUrl = "git://github.com/henon/nonExistingRepo.git";
            AssertHelper.Throws<NoRemoteRepositoryException>(() => { using (Repository repo = Git.Clone(fromUrl, toPath)) { } }, "Repository shouldn't exist.");
        }

        [Test]
        [Ignore("TransportLocal is not completely ported yet.")]
        public void Checked_cloned_local_dotGit_suffixed_repo()
        {
            //setup of .git directory
            var resource =
                new DirectoryInfo(PathUtil.Combine(Path.Combine(Environment.CurrentDirectory, "Resources"),
                                               "OneFileRepository"));
            var tempRepository =
                new DirectoryInfo(Path.Combine(trash.FullName, "OneFileRepository" + Path.GetRandomFileName() + Constants.DOT_GIT_EXT));
            CopyDirectory(resource.FullName, tempRepository.FullName);

            var repositoryPath = new DirectoryInfo(Path.Combine(tempRepository.FullName, Constants.DOT_GIT));
            Directory.Move(repositoryPath.FullName + "ted", repositoryPath.FullName);


            using (var repo = new Repository(repositoryPath.FullName))
            {
                Assert.IsTrue(Repository.IsValid(repo.Directory));
                Commit headCommit = repo.Head.CurrentCommit;
                Assert.AreEqual("f3ca78a01f1baa4eaddcc349c97dcab95a379981", headCommit.Hash);
            }

            string toPath = Path.Combine(trash.FullName, "to.git");

            using (var repo = Git.Clone(repositoryPath.FullName, toPath))
            {
                Assert.IsTrue(Repository.IsValid(repo.Directory));
                Commit headCommit = repo.Head.CurrentCommit;
                Assert.AreEqual("f3ca78a01f1baa4eaddcc349c97dcab95a379981", headCommit.Hash);
            }
        }

        [Test]
        [Ignore]
        public void Check_cloned_repo_http()
        {
            string toPath = Path.Combine(trash.FullName, "test");
            string fromUrl = "http://github.com/henon/test.git";

            using (Repository repo = Git.Clone(fromUrl, toPath))
            {
                Assert.IsTrue(Repository.IsValid(repo.Directory));
                //Verify content is in the proper location
                var readme = Path.Combine(repo.WorkingDirectory, "README.txt");
                Assert.IsTrue(new FileInfo(readme).Exists);
            }
        }
    }
}