using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using GitSharp.Tests;
using System.IO;

namespace Git.Tests
{
    [TestFixture]
    public class IndexTest : RepositoryTestCase
    {
        [Test]
        public void IndexAdd()
        {
            bool bare = false;
            var workingDirectory = Path.Combine(trash.FullName, "test");
            var repo = Repository.Init(workingDirectory, bare);
            string filepath = Path.Combine(workingDirectory, "for henon.txt");
            File.WriteAllText(filepath, "Weißbier");
            repo.Index.Add(filepath);
            repo.Index.Write();
        }
    }
}
