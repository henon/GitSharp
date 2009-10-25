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



            var repository = new Repository(repositoryPath);
            GitIndex index = repository.Index;

            Assert.IsNotNull(index);
            List<GitIndex.Entry> entries = index.Members.ToList();
            Assert.AreEqual(1, entries.Count);

            GitIndex.Entry entry = entries[0];
            Assert.AreEqual("dummy.txt", entry.Name);

            Ref headRef = repository.Head;
            Assert.AreEqual("refs/heads/master", headRef.Name);
            Assert.AreEqual("f3ca78a01f1baa4eaddcc349c97dcab95a379981", headRef.ObjectId.Name);

            object obj = repository.MapObject(headRef.ObjectId, headRef.OriginalName);
#pragma warning disable 0612
            Assert.IsInstanceOfType(typeof(Commit), obj); // [henon] IsInstanceOfType is obsolete
#pragma warning restore 0612
            var commit = (Commit) obj;

            Assert.AreEqual("f3ca78a01f1baa4eaddcc349c97dcab95a379981", commit.CommitId.Name);
            Assert.AreEqual(commit.Committer, commit.Author);
            Assert.AreEqual("nulltoken <emeric.fermas@gmail.com> 1255117188 +0200", commit.Committer.ToExternalString());

            Assert.AreEqual(0, commit.ParentIds.Length);
        }

        public static void CopyDirectory(string sourceDirectoryPath, string targetDirectoryPath)
        {
            if (!targetDirectoryPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                targetDirectoryPath += Path.DirectorySeparatorChar;
            }

            if (!Directory.Exists(targetDirectoryPath))
            {
                Directory.CreateDirectory(targetDirectoryPath);
            }

            string[] files = Directory.GetFileSystemEntries(sourceDirectoryPath);

            foreach (string fileSystemElement in files)
            {
                if (Directory.Exists(fileSystemElement))
                {
                    CopyDirectory(fileSystemElement, targetDirectoryPath + Path.GetFileName(fileSystemElement));
                    continue;
                }

                File.Copy(fileSystemElement, targetDirectoryPath + Path.GetFileName(fileSystemElement), true);
            }
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



            var repository = new Repository(repositoryPath);
            GitIndex index = repository.Index;

            Assert.IsNotNull(index);

            writeTrashFile(Path.Combine(repository.WorkingDirectory.FullName, "c.txt"), "c");

            var tree = new Tree(repository);

            index.add(repository.WorkingDirectory, new FileInfo(Path.Combine(repository.WorkingDirectory.FullName, "c.txt")));

            var diff = new IndexDiff(tree, index);
            diff.Diff();

            index.write();


            Assert.AreEqual(3, diff.Added.Count);
            Assert.IsTrue(diff.Added.Contains("a.txt"));
            Assert.IsTrue(diff.Added.Contains("b.txt"));
            Assert.IsTrue(diff.Added.Contains("c.txt"));
            Assert.AreEqual(0, diff.Changed.Count);
            Assert.AreEqual(0, diff.Modified.Count);
            Assert.AreEqual(0, diff.Removed.Count);
        }

    }
}



