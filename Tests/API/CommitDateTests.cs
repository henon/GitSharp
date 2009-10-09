using System;
using Git;
using NUnit.Framework;

namespace GitSharp.Tests.API
{
    [TestFixture]
    public class CommitDateTests : RepositoryTestCase
    {
        private const string KnownCommit = "49322bb17d3acc9146f98c97d078513228bbf3c0";
        private static readonly DateTimeOffset _expectedDate = new DateTimeOffset(2005, 04, 07, 15, 32, 13, new TimeSpan(-7, 0, 0));

        [Test]
        public void ShouldBeAbleToReadAuthorDate()
        {
            var expectedDate = DateTimeOffset.Parse("2005-04-07 15:32:13 -07:00");

            var repos = new Repository(db);
            var commit = new Commit(repos, KnownCommit);

            Assert.AreEqual(expectedDate, commit.AuthorDate);
        }

        [Test]
        public void ShouldBeAbleToReadAuthorDate2()
        {
            var repos = new Repository(db);
            var commit = new Commit(repos, KnownCommit);

            Assert.AreEqual(_expectedDate, commit.AuthorDate);
        }

        [Test]
        public void ShouldBeAbleToReadCommitDate()
        {
            var repos = new Repository(db);
            var commit = new Commit(repos, KnownCommit);

            Assert.AreEqual(_expectedDate, commit.CommitDate);
        }
    }
}