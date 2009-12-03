using System;
using System.IO;
using System.Threading;
using GitSharp.Core;
using GitSharp.Core.Tests.Util;
using GitSharp.Tests.GitSharp.Core.Util;
using NUnit.Framework;
using FileMode=GitSharp.Core.FileMode;

namespace GitSharp.Core.Tests
{
    [TestFixture]
    public class IndexModifiedTests : RepositoryTestCase
    {
        [Test]
        public void ShouldSupportExtensionlessFiles()
        {
            var index = new GitIndex(db);

            writeTrashFile("extensionless-file", "contents");

            var file = new FileInfo(Path.Combine(trash.FullName, "extensionless-file"));

            index.add(trash, file);

            var entry = index.GetEntry("extensionless-file");

            Assert.IsFalse(entry.IsModified(trash, true));
        }

        [Test]
        public void ShouldSupportNotModifiedExtensionlessFilesWithoutContentChecking()
        {
            var index = new GitIndex(db);

            writeTrashFile("extensionless-file", "contents");

            var file = new FileInfo(Path.Combine(trash.FullName, "extensionless-file"));

            index.add(trash, file);

            var entry = index.GetEntry("extensionless-file");

            Assert.IsFalse(entry.IsModified(trash));
        }
        
        [Test]
        public void ShouldAllowComparingOfAlreadyOpenedFile()
        {
            var index = new GitIndex(db);
            var file = writeTrashFile("extensionless-file", "contents");

            index.add(trash, file);

            var entry = index.GetEntry("extensionless-file");

            if (AssertHelper.IsRunningOn(AssertedPlatform.Mono))
            {
                // File timestamps on Unix based systems are only precise to the second
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            // replace contents of file (with same size so it passes the size check)
            using (var writer = file.CreateText())
                writer.Write("stnetnoc");

            // opening the file for reading shoudn't block us from checking the contents
            using (file.OpenRead())
                Assert.IsTrue(entry.IsModified(trash, true));
        }
    }
}