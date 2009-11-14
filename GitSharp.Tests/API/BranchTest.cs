/*
 * Copyright (C) 2009, Paupaw <paupawsan@gmail.com>
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
    public class BranchTest : ApiTestCase
    {
        [Test]
        public void ResetHard()
        {
            using (Repository repo = GetTrashRepository())
            {
                string filepath = Path.Combine(repo.WorkingDirectory, "a present for paupaw.txt");
                File.WriteAllText(filepath, "hey, paupaw gets new shoes!");
                repo.Index.Add(filepath);
                var commit = repo.Commit("You feeling lucky punk!?", new Author("IronHide", "transformers@cybertron.com"));

                // now changing file from first commit
                File.AppendAllText(filepath, "... and a new hat too.");
                // and add new file
                string filepath1 = Path.Combine(repo.WorkingDirectory, "Bintang Kecil.txt");
                File.WriteAllText(filepath1, "Bintang Kecil, di langit yang biru, amat banyak menghias angkasa.");
                repo.Index.Add(filepath, filepath1);
                var commit2 = repo.Commit("Nyanyian anak bangsa", new Author("Legend", "hist@jakarta.id"));

                // adding an untracked file which should not be removed by reset hard
                var filepath2 = Path.Combine(repo.WorkingDirectory, "some untracked file");
                File.WriteAllText(filepath2, "untracked content");

                // git reset --hard
                repo.CurrentBranch.Reset(commit.Hash, ResetBehavior.Hard);

                Assert.AreEqual(commit.Hash, repo.CurrentBranch.CurrentCommit.Hash);
                Assert.IsFalse(new FileInfo(filepath1).Exists);
                Assert.AreEqual("hey, paupaw gets new shoes!", File.ReadAllText(filepath));
                var status = repo.Status;
                Assert.AreEqual(0, status.Added.Count);
                Assert.AreEqual(0, status.Modified.Count);
                Assert.AreEqual(0, status.Missing.Count);
                Assert.AreEqual(0, status.Removed.Count);
                Assert.AreEqual(0, status.Staged.Count);
                Assert.AreEqual(1, status.Untracked.Count);
                Assert.IsTrue(new FileInfo(filepath2).Exists);

                var filepath3 = Path.Combine(repo.WorkingDirectory, "for me.txt");
                File.WriteAllText(filepath3, "This should be fine if reset hard was working fine.");
                repo.Index.Add(filepath3);
                var commit3 = repo.Commit("commit after hard reset", new Author("paupaw", "paupaw@home.jp"));

                Assert.AreEqual(commit3.Hash, repo.CurrentBranch.CurrentCommit.Hash);
                Assert.AreEqual(commit3.Parent, commit);
            }
        }


        [Test]
        public void ResetHard1()
        {
            using (Repository repo = GetTrashRepository())
            {
                Assert.AreEqual(8, repo.Status.Removed.Count);
                repo.Head.Reset(ResetBehavior.Hard);
                Assert.AreEqual(0, repo.Status.Removed.Count);
                Assert.AreEqual(0, repo.Status.Untracked.Count);
            }
        }


        [Test]
        public void ResetSoft()
        {
            using (Repository repo = GetTrashRepository())
            {
                string filepath = Path.Combine(repo.WorkingDirectory, "a present for paupaw.txt");
                File.WriteAllText(filepath, "hey, paupaw gets new shoes!");
                repo.Index.Add(filepath);
                var commit = repo.Commit("You feeling lucky punk!?", new Author("IronHide", "transformers@cybertron.com"));

                // now changing file from first commit
                File.AppendAllText(filepath, "... and a new hat too.");
                // and add new file
                string filepath1 = Path.Combine(repo.WorkingDirectory, "Bintang Kecil.txt");
                File.WriteAllText(filepath1, "Bintang Kecil, di langit yang biru, amat banyak menghias angkasa.");
                repo.Index.Add(filepath, filepath1);
                var commit2 = repo.Commit("Nyanyian anak bangsa", new Author("Legend", "hist@jakarta.id"));

                // adding an untracked file which should not be removed by reset soft
                var filepath2 = Path.Combine(repo.WorkingDirectory, "some untracked file");
                File.WriteAllText(filepath2, "untracked content");

                // git reset --soft ...
                repo.CurrentBranch.Reset(commit.Hash, ResetBehavior.Soft);

                Assert.AreEqual(commit.Hash, repo.CurrentBranch.CurrentCommit.Hash);
                Assert.IsTrue(new FileInfo(filepath1).Exists);
                var status=repo.Status;
                Assert.IsTrue(status.Added.Contains("Bintang Kecil.txt"));
                Assert.IsTrue(status.Staged.Contains("a present for paupaw.txt"));
                Assert.AreEqual(1, status.Added.Count);
                Assert.AreEqual(0, status.Modified.Count);
                Assert.AreEqual(0, status.Missing.Count);
                Assert.AreEqual(0, status.Removed.Count);
                Assert.AreEqual(1, status.Staged.Count);
                Assert.AreEqual(1, status.Untracked.Count);
                Assert.IsTrue(new FileInfo(filepath2).Exists);

                var filepath3 = Path.Combine(repo.WorkingDirectory, "for me.txt");
                File.WriteAllText(filepath3, "This should be fine if reset soft was working fine.");
                repo.Index.Add(filepath3);
                var commit3 = repo.Commit("commit after soft reset", new Author("paupaw", "paupaw@home.jp"));

                Assert.AreEqual(commit3.Hash, repo.CurrentBranch.CurrentCommit.Hash);
                Assert.AreEqual(commit3.Parent, commit);
            }
        }

        [Test]
        public void ResetSoft1()
        {
            using (Repository repo = GetTrashRepository())
            {
                var c1 = repo.Head.CurrentCommit;
                Assert.AreEqual(8, repo.Status.Removed.Count);
                repo.Head.Reset(c1.Parent.Parent, ResetBehavior.Soft);
                Assert.AreEqual(5, repo.Status.Removed.Count);
                Assert.AreEqual(0, repo.Status.Untracked.Count);

                Assert.AreEqual(c1.Parent.Parent, repo.Head.CurrentCommit);
            }
        }
    }
}
