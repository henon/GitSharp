using System.IO;
using GitSharp.Core;
using NUnit.Framework;

namespace GitSharp.Tests
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

            Assert.IsFalse(entry.IsModified(file, true));
        }

        [Test]
        public void ShouldSupportNotModifiedExtensionlessFilesWithoutContentChecking()
        {
            var index = new GitIndex(db);

            writeTrashFile("extensionless-file", "contents");

            var file = new FileInfo(Path.Combine(trash.FullName, "extensionless-file"));

            index.add(trash, file);

            var entry = index.GetEntry("extensionless-file");

            Assert.IsFalse(entry.IsModified(file));
        }
    }
}