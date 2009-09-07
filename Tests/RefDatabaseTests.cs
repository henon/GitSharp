using System.IO;
using NUnit.Framework;

namespace GitSharp.Tests
{
    [TestFixture]
    public class RefDatabaseTests
    {
        public Repository Repository { get; private set; }

        public RefDatabaseTests()
        {
            Repository = new Repository(new DirectoryInfo("sample.git"), new DirectoryInfo("sample"));
        }

        [TestFixtureSetUp]
        public void Setup()
        {
            Assert.IsNotNull(Repository);
        }

        [Test]
        public void GetAllRefsTest()
        {
            var db = new RefDatabase(Repository);
            var refs = db.GetAllRefs();
            Assert.AreEqual(3, refs.Count);
            Assert.IsTrue(refs.ContainsKey("refs/heads/first"), "GetAllRefsTest#010");
            Assert.IsTrue(refs.ContainsKey("refs/heads/master"), "GetAllRefsTest#020");
            Assert.IsTrue(refs.ContainsKey("refs/tags/my_tag"), "GetAllRefsTest#025");
            Assert.AreEqual("a13973bc29346193c4a023fc83cc5b0645784262", refs["refs/heads/master"].ObjectId.ToString(), "GetAllRefsTest#030");
        }
    }
}
