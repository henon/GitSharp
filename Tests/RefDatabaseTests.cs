using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Gitty.Core.Tests
{
    [TestFixture]
    public class RefDatabaseTests
    {
        public Repository Repository { get; private set; }

        public RefDatabaseTests()
        {
            this.Repository = Repository.Open("sample");
        }

        [TestFixtureSetUp]
        public void Setup()
        {
            Assert.IsNotNull(this.Repository);
        }

        [Test]
        public void GetAllRefsTest()
        {
            var db = new RefDatabase(this.Repository);
            var refs = db.GetAllRefs();
            Assert.AreEqual(2, refs.Count);
            Assert.IsTrue(refs.ContainsKey("refs/heads/first"), "GetAllRefsTest#010");
            Assert.IsTrue(refs.ContainsKey("refs/heads/master"), "GetAllRefsTest#020");
            Assert.AreEqual("a13973bc29346193c4a023fc83cc5b0645784262", refs["refs/heads/master"].ObjectId.ToString(), "GetAllRefsTest#030");

        }
    }
}
