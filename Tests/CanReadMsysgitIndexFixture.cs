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
    }
}



