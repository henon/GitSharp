using System;
using Git;
using NUnit.Framework;

namespace GitSharp.Tests.API
{
    [TestFixture]
    public class CommitDateTests : RepositoryTestCase
    {
        private const string KnownCommit = "49322bb17d3acc9146f98c97d078513228bbf3c0";
        private static readonly DateTimeOffset ExpectedDate = DateTimeOffset.Parse("2005-04-07 15:32:13 -07:00");

        [Test]
        public void ShouldBeAbleToReadAuthorDate()
        {
            var repos = new Repository(db);
            var commit = new Commit(repos, KnownCommit);

            Assert.AreEqual(ExpectedDate, commit.AuthorDate);
        }

        [Test]
        public void ShouldBeAbleToReadCommitDate()
        {
            var repos = new Repository(db);
            var commit = new Commit(repos, KnownCommit);

            Assert.AreEqual(ExpectedDate, commit.CommitDate);
        }
    }
}