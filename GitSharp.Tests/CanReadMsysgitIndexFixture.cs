using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitSharp.Core;
using NUnit.Framework;

namespace GitSharp.Tests
{
    [TestFixture]
    public class CanReadMsysgitIndexFixture : RepositoryTestCase
    {
        [Test]
        public void CanReadMsysgitIndex()
        {
            //setup of .git directory
            var resource =
                new DirectoryInfo(Path.Combine(Path.Combine(Environment.CurrentDirectory, "Resources"),
                                               "OneFileRepository"));
            var tempRepository =
                new DirectoryInfo(Path.Combine(trash.FullName, "OneFileRepository" + Path.GetRandomFileName()));
            CopyDirectory(resource.FullName, tempRepository.FullName);

            var repositoryPath = new DirectoryInfo(Path.Combine(tempRepository.FullName, ".git"));
            Directory.Move(repositoryPath.FullName + "ted", repositoryPath.FullName);



            var repository = new Core.Repository(repositoryPath);
            GitIndex index = repository.Index;

            Assert.IsNotNull(index);
            List<GitIndex.Entry> entries = index.Members.ToList();
            Assert.AreEqual(1, entries.Count);

            GitIndex.Entry entry = entries[0];
            Assert.AreEqual("dummy.txt", entry.Name);

            Core.Ref headRef = repository.Head;
            Assert.AreEqual("refs/heads/master", headRef.Name);
            Assert.AreEqual("f3ca78a01f1baa4eaddcc349c97dcab95a379981", headRef.ObjectId.Name);

            object obj = repository.MapObject(headRef.ObjectId, headRef.OriginalName);
#pragma warning disable 0612
            Assert.IsInstanceOfType(typeof(Core.Commit), obj); // [henon] IsInstanceOfType is obsolete
#pragma warning restore 0612
            var commit = (Core.Commit) obj;

            Assert.AreEqual("f3ca78a01f1baa4eaddcc349c97dcab95a379981", commit.CommitId.Name);
            Assert.AreEqual(commit.Committer, commit.Author);
            Assert.AreEqual("nulltoken <emeric.fermas@gmail.com> 1255117188 +0200", commit.Committer.ToExternalString());

            Assert.AreEqual(0, commit.ParentIds.Length);
        }

        [Test]
        public void CanAddAFileToAMSysGitIndexWhereAFileIsAlreadyWaitingToBeCommitted()
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



            var repository = new Core.Repository(repositoryPath);
            GitIndex index = repository.Index;

            Assert.IsNotNull(index);

            writeTrashFile(Path.Combine(repository.WorkingDirectory.FullName, "c.txt"), "c");

            var tree = repository.MapTree(repository.Head.ObjectId);

            index.add(repository.WorkingDirectory, new FileInfo(Path.Combine(repository.WorkingDirectory.FullName, "c.txt")));

            var diff = new IndexDiff(tree, index);
            diff.Diff();

            index.write();


            Assert.AreEqual(2, diff.Added.Count);
            Assert.IsFalse(diff.Added.Contains("a.txt"), "Should not contain a.txt because it is already committed.");
            Assert.IsTrue(diff.Added.Contains("b.txt"), "Should contain b.txt since it was added by msysgit, but not committed");
            Assert.IsTrue(diff.Added.Contains("c.txt"), "Should contain c.txt since it was added by this test, but not committed");
            Assert.AreEqual(0, diff.Changed.Count);
            Assert.AreEqual(0, diff.Modified.Count);
            Assert.AreEqual(0, diff.Removed.Count);
        }


        [Test]
        public void Check_entries_of_msysgit_index()
        {
            var repo = new Core.Repository(trash_git);
            var index_path = Path.Combine(repo.Directory.FullName, "index");
            new FileInfo("Resources/index_originating_from_msysgit").CopyTo(index_path);

            var index = repo.Index;
            index.RereadIfNecessary();

            var paths = new[] {            
                "New Folder/New Ruby Program.rb",
                "for henon.txt",
                "test.cmd", 
            };

            var dict = index.Members.ToDictionary(entry => entry.Name);
            Assert.AreEqual(3, dict.Count);
            foreach (var path in paths)
                Assert.IsTrue(dict.ContainsKey(path));
        }

    }
}



