using System.Linq;
using GitSharp.Tests.GitSharp;
using NUnit.Framework;
using System.IO;
using System.Text;

namespace GitSharp.API.Tests
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

        [Test]
        public void Export_Commit() 
        {
            using (var repo = GetTrashRepository())
            {
                var dir = Directory.CreateDirectory(repo.WorkingDirectory + "/test").FullName;
                repo.Head.CurrentCommit.Checkout(dir);
                Assert.IsTrue(new FileInfo(dir + "/a/a1").Exists);
                Assert.IsTrue(new FileInfo(dir + "/a/a1.txt").Exists);
                Assert.IsTrue(new FileInfo(dir + "/a/a2.txt").Exists);
                Assert.IsTrue(new FileInfo(dir + "/b/b1.txt").Exists);
                Assert.IsTrue(new FileInfo(dir + "/b/b2.txt").Exists);
                Assert.IsTrue(new FileInfo(dir + "/c/c1.txt").Exists);
                Assert.IsTrue(new FileInfo(dir + "/c/c2.txt").Exists);
                Assert.IsTrue(new FileInfo(dir + "/master.txt").Exists);
                var dir1 = Directory.CreateDirectory(repo.WorkingDirectory + "/test1").FullName;
                repo.Head.CurrentCommit.Parent.Parent.Checkout(dir1);
                Assert.IsTrue(new FileInfo(dir1 + "/a/a1.txt").Exists);
                Assert.IsTrue(new FileInfo(dir1 + "/a/a2.txt").Exists);
                Assert.IsTrue(new FileInfo(dir1 + "/c/c1.txt").Exists);
                Assert.IsTrue(new FileInfo(dir1 + "/c/c2.txt").Exists);
                Assert.IsTrue(new FileInfo(dir1 + "/master.txt").Exists);
            }
        }
    }
}
