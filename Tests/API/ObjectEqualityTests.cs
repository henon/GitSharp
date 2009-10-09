using Git;
using NUnit.Framework;

namespace GitSharp.Tests.API
{
    [TestFixture]
    public class ObjectEqualityTests : RepositoryTestCase
    {
        [Test]
        public void ShouldBeAbleToCompareNullObjects()
        {
            AbstractObject obj = null;

            Assert.IsTrue(obj == null);
        }

        [Test]
        public void ShouldBeAbleToCompareNullObjectsInverse()
        {
            AbstractObject obj = null;

            Assert.IsTrue(null == obj);
        }

        [Test]
        public void SameInstanceShouldBeEqual()
        {
            var repos = new Repository(db);
            var obj = repos.CurrentBranch.CurrentCommit;

            Assert.IsTrue(obj == obj);
        }

        [Test]
        public void DifferentInstancesShouldntBeEqual()
        {
            var repos = new Repository(db);
            var obj = repos.CurrentBranch.CurrentCommit;
            var obj2 = repos.CurrentBranch.CurrentCommit.Parent;

            Assert.IsTrue(obj != obj2);
        }
    }
}