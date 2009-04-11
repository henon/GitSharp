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
            this.Repository = Repository.Open("sample.git");
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
            Assert.IsTrue(refs.ContainsKey("refs/heads/first"));
            Assert.IsTrue(refs.ContainsKey("refs/heads/master"));
            

        }
    }
}
