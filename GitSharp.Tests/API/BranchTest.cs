using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;

namespace GitSharp.Tests.API
{
    [TestFixture]
    class BranchTest : ApiTestCase
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

                filepath = Path.Combine(repo.WorkingDirectory, "Bintang Kecil.txt");
                File.WriteAllText(filepath, "Bintang Kecil, di langit yang biru, amat banyak menghias angkasa.");
                repo.Index.Add(filepath);
                var commit2 = repo.Commit("Nyanyian anak bangsa", new Author("Legend", "hist@jakarta.id"));

                repo.CurrentBranch.ResetHard(commit.Hash);

                Assert.AreEqual(commit.Hash, repo.CurrentBranch.CurrentCommit.Hash);

                filepath = Path.Combine(repo.WorkingDirectory, "for me.txt");
                File.WriteAllText(filepath, "This should be fine if reset hard was working fine.");
                repo.Index.Add(filepath);
                var commit3 = repo.Commit("commit after hard reset", new Author("paupaw", "paupaw@home.jp"));

                Assert.AreEqual(commit3.Hash, repo.CurrentBranch.CurrentCommit.Hash);
            }
        }
    }
}
