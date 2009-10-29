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

namespace GitSharp.Tests.API
{
    [TestFixture]
    public class RepositoryTests : ApiTestCase
    {

        [Test]
        public void StatusEvenWorksWithHeadLessRepo()
        {
            using (var repo = Repository.Init(Path.Combine(trash.FullName, "test")))
            {
                RepositoryStatus status = null;
                Assert.DoesNotThrow(() => status = repo.Status);
                Assert.IsFalse(repo.Status.AnyDifferences);
                Assert.AreEqual(0,
                                status.Added.Count + status.Changed.Count + status.Missing.Count + status.Modified.Count +
                                status.Removed.Count);
            }
        }

        [Test]
        public void ImplicitConversionToCoreRepo()
        {
            using (var repo = this.GetTrashRepository())
            {
                Assert.IsTrue(repo is Repository);
                GitSharp.Core.Repository core_repo = repo;
                Assert.IsTrue(core_repo is GitSharp.Core.Repository);
            }
        }

        [Test]
        public void RepositoryStatusTracksAddedFiles()
        {
            //setup of .git directory
            var resource =
                new DirectoryInfo(Path.Combine(Path.Combine(Environment.CurrentDirectory, "Resources"),
                                               "CorruptIndex"));
            var tempRepository =
                new DirectoryInfo(Path.Combine(trash.FullName, "CorruptIndex" + Path.GetRandomFileName()));
            CopyDirectory(resource.FullName, tempRepository.FullName);

            var repositoryPath = new DirectoryInfo(Path.Combine(tempRepository.FullName, ".git"));
            Directory.Move(repositoryPath.FullName + "ted", repositoryPath.FullName);

            using (var repository = new Repository(repositoryPath.FullName))
            {
                var status = repository.Status;

                Assert.IsTrue(status.AnyDifferences);
                Assert.AreEqual(1, status.Added.Count);
                Assert.IsTrue(status.Added.Contains("b.txt")); // the file already exists in the index (eg. has been previously git added)
                Assert.AreEqual(0, status.Changed.Count);
                Assert.AreEqual(0, status.Missing.Count);
                Assert.AreEqual(0, status.Modified.Count);
                Assert.AreEqual(0, status.Removed.Count);

                string filepath = Path.Combine(repository.WorkingDirectory, "c.txt");
                writeTrashFile(filepath, "c");
                repository.Index.Add(filepath);

                status = repository.Status;

                Assert.IsTrue(status.AnyDifferences);
                Assert.AreEqual(2, status.Added.Count);
                Assert.IsTrue(status.Added.Contains("b.txt"));
                Assert.IsTrue(status.Added.Contains("c.txt"));
                Assert.AreEqual(0, status.Changed.Count);
                Assert.AreEqual(0, status.Missing.Count);
                Assert.AreEqual(0, status.Modified.Count);
                Assert.AreEqual(0, status.Removed.Count);

            }
        }
    }
}