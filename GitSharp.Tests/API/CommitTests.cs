using System.Linq;
using NUnit.Framework;
using System.IO;

namespace GitSharp.Tests.API
{
    [TestFixture]
    public class CommitTests : ApiTestCase
    {
        [Test]
        public void Commit_into_empty_repository()
        {
            var workingDirectory = Path.Combine(trash.FullName, "test");
            using (Repository repo = Repository.Init(workingDirectory))
            {
                string filepath = Path.Combine(workingDirectory, "for henon.txt");
                File.WriteAllText(filepath, "Weißbier");
                repo.Index.Add(filepath);
                string filepath1 = Path.Combine(workingDirectory, "for nulltoken.txt");
                File.WriteAllText(filepath1, "Rotwein");
                repo.Index.Add(filepath1);
                var commit = repo.Commit("Hello World!", new Author("A. U. Thor", "au@thor.com"));
                Assert.NotNull(commit);
                Assert.IsTrue(commit.IsCommit);
                Assert.IsNull(commit.Parent);
                Assert.AreEqual("A. U. Thor", commit.Author.Name);
                Assert.AreEqual("au@thor.com", commit.Author.EmailAddress);
                Assert.AreEqual("Hello World!", commit.Message);
                // TODO: check if tree contains for henon and for nulltoken, get the blobs and check  the content.
                Assert.AreEqual(commit, repo.Head.CurrentCommit);
                var changes = commit.Changes.ToDictionary(change => change.Name);
                Assert.AreEqual(ChangeType.Added, changes["for henon.txt"].ChangeType);
                Assert.AreEqual(ChangeType.Added, changes["for nulltoken.txt"].ChangeType);
                Assert.AreEqual("Weißbier", (changes["for henon.txt"].ComparedObject as Blob).Data);
                Assert.AreEqual("Rotwein", (changes["for nulltoken.txt"].ComparedObject as Blob).Data);
                Assert.AreEqual(2, changes.Count);
            }
        }

        [Test]
        public void Commit_into_empty_repository_forShiftJist1()
        {
            var workingDirectory = Path.Combine(trash.FullName, "test1");
            using (Repository repo = Repository.Init(workingDirectory))
            {
                //GitSharp.Core.Constants.setCHARSET("Shift_JIS");
                string filepath = Path.Combine(workingDirectory, "for henon.txt");
                File.WriteAllText(filepath, "Weißbier");
                repo.Index.Add(filepath);
                string filepath1 = Path.Combine(workingDirectory, "for nulltoken.txt");
                File.WriteAllText(filepath1, "Rotwein");
                repo.Index.Add(filepath1);
                string filepath2 = Path.Combine(workingDirectory, "俺のためだ.txt");
                File.WriteAllText(filepath2, "西東京市");
                repo.Index.Add(filepath2);
                var commit = repo.Commit("Hello World!日本からShift_JISのためをコミットしました", new Author("ポウルス", "paupaw@tokyo-dome.com"));
                Assert.NotNull(commit);
                Assert.IsTrue(commit.IsCommit);
                Assert.IsNull(commit.Parent);
                Assert.AreEqual("ポウルス", commit.Author.Name);
                Assert.AreEqual("paupaw@tokyo-dome.com", commit.Author.EmailAddress);
                Assert.AreEqual("Hello World!日本からShift_JISのためをコミットしました", commit.Message);
                // TODO: check if tree contains for henon and for nulltoken, get the blobs and check  the content.
                Assert.AreEqual(commit, repo.Head.CurrentCommit);
                var changes = commit.Changes.ToDictionary(change => change.Name);
                Assert.AreEqual(ChangeType.Added, changes["for henon.txt"].ChangeType);
                Assert.AreEqual(ChangeType.Added, changes["for nulltoken.txt"].ChangeType);
                Assert.AreEqual(ChangeType.Added, changes["俺のためだ.txt"].ChangeType);
                Assert.AreEqual("Weißbier", (changes["for henon.txt"].ComparedObject as Blob).Data);
                Assert.AreEqual("Rotwein", (changes["for nulltoken.txt"].ComparedObject as Blob).Data);
                Assert.AreEqual("西東京市", (changes["俺のためだ.txt"].ComparedObject as Blob).Data);
                Assert.AreEqual(3, changes.Count);
            }
        }

        [Test]
        public void Commit_into_empty_repository_forShiftJist2()
        {
            var workingDirectory = Path.Combine(trash.FullName, "test2");
            using (Repository repo = Repository.Init(workingDirectory))
            {
                //GitSharp.Core.Constants.setCHARSET("Shift_JIS");
                
                string filepath = Path.Combine(workingDirectory, @"Resources\encodingTestData\Shift_JIS\ウサギちゃん\Rabbitはウサギです.txt");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filepath));
                System.IO.File.Copy(@"Resources\encodingTestData\Shift_JIS\ウサギちゃん\Rabbitはウサギです.txt",filepath);
                repo.Index.Add(filepath); //Add using UTF-8 params, but the file itself is Shift_JIS.. heh!?

                string filepath1 = Path.Combine(workingDirectory, @"Resources\encodingTestData\Shift_JIS\東京都\新宿三丁目.txt");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filepath1));
                System.IO.File.Copy(@"Resources\encodingTestData\Shift_JIS\東京都\新宿三丁目.txt", filepath1);
                repo.Index.Add(filepath1); //Add using UTF-8 params, but the file itself is Shift_JIS.. heh!?

                var commit = repo.Commit("Hello World!日本からShift_JISのためをコミットしました", new Author("ポウルス", "paupaw@tokyo-dome.com"));
                Assert.NotNull(commit);
                Assert.IsTrue(commit.IsCommit);
                Assert.IsNull(commit.Parent);
                Assert.AreEqual("ポウルス", commit.Author.Name);
                Assert.AreEqual("paupaw@tokyo-dome.com", commit.Author.EmailAddress);
                Assert.AreEqual("Hello World!日本からShift_JISのためをコミットしました", commit.Message);
                // TODO: check if tree contains for henon and for nulltoken, get the blobs and check  the content.
                Assert.AreEqual(commit, repo.Head.CurrentCommit);
                var changes = commit.Changes.ToDictionary(change => change.Name);
                Assert.AreEqual(ChangeType.Added, changes["Rabbitはウサギです.txt"].ChangeType);
                Assert.AreEqual(ChangeType.Added, changes["新宿三丁目.txt"].ChangeType);
                Assert.AreEqual("ラビットis usagi desu.", (changes["Rabbitはウサギです.txt"].ComparedObject as Blob).Data);
                Assert.AreEqual("電車で行きます。", (changes["新宿三丁目.txt"].ComparedObject as Blob).Data);
                Assert.AreEqual(2, changes.Count);
            }
        }

        [Test]
        public void Commit_changes_to_existing_commit()
        {
            using (var repo = GetTrashRepository())
            {
                var workingDirectory = repo.WorkingDirectory;
                string filepath = Path.Combine(workingDirectory, "README");
                File.WriteAllText(filepath, "This is a really short readme file\n\nWill write up some text here.");
                repo.Index.Add(filepath);
                //repo.Index.Remove(Path.Combine(workingDirectory, "a/a1"));
                var commit = repo.Commit("Adding ä README\n\n", new Author("müller", "müller@gitsharp.org"));
                Assert.NotNull(commit);
                Assert.IsTrue(commit.IsCommit);
                Assert.AreEqual(commit.Parent.Hash, "49322bb17d3acc9146f98c97d078513228bbf3c0");
                Assert.AreEqual("müller", commit.Author.Name);
                Assert.AreEqual("müller@gitsharp.org", commit.Author.EmailAddress);
                Assert.AreEqual("Adding ä README\n\n", commit.Message);
                // check if tree contains the new file, get the blob and check  the content.
                Assert.AreEqual(commit, repo.Head.CurrentCommit);
                var previous = new Commit(repo, "HEAD^");
                Assert.IsTrue(previous.IsCommit);
                Assert.AreEqual(previous, commit.Parent);
                var changes = previous.CompareAgainst(commit).ToDictionary(change => change.Name);
                Assert.AreEqual(ChangeType.Added, changes["README"].ChangeType);
                Assert.AreEqual("This is a really short readme file\n\nWill write up some text here.",
                                (changes["README"].ComparedObject as Blob).Data);
                Assert.AreEqual(ChangeType.Deleted, changes["a1"].ChangeType);
                Assert.AreEqual(ChangeType.Deleted, changes["a1.txt"].ChangeType);
                Assert.AreEqual(ChangeType.Deleted, changes["a2.txt"].ChangeType);
                Assert.AreEqual(ChangeType.Deleted, changes["b1.txt"].ChangeType);
                Assert.AreEqual(ChangeType.Deleted, changes["b2.txt"].ChangeType);
                Assert.AreEqual(ChangeType.Deleted, changes["c1.txt"].ChangeType);
                Assert.AreEqual(ChangeType.Deleted, changes["c2.txt"].ChangeType);
                Assert.AreEqual(ChangeType.Deleted, changes["master.txt"].ChangeType);
                Assert.AreEqual(9, changes.Count);
            }
        }
    }
}
