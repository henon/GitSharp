using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.Commands;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp
{
	[TestFixture]
	public class MergeTests : ApiTestCase
	{
		[Test]
		public void MergeStrategyOurs()
		{
			using (var repo = GetTrashRepository())
			{
				var a = repo.Get<Branch>("a");
				var c = repo.Get<Branch>("c");
				var result = a.Merge(c, MergeStrategy.Ours);
				Assert.IsTrue(result.Success);
				Assert.AreEqual(repo.Get<Branch>("a^").CurrentCommit.Tree, a.CurrentCommit.Tree);
				Assert.AreEqual(2, a.CurrentCommit.Parents.Count());
				Assert.AreEqual("Merge branch 'c' into a", result.Commit.Message);
			}
		}

		[Test]
		public void MergeStrategyTheirs()
		{
			using (var repo = GetTrashRepository())
			{
				var a = repo.Get<Branch>("a");
				var c = repo.Get<Branch>("c");
				var result = a.Merge(c, MergeStrategy.Theirs);
				Assert.IsTrue(result.Success);
				Assert.AreEqual(repo.Get<Branch>("c").CurrentCommit.Tree, a.CurrentCommit.Tree);
				Assert.AreEqual(2, a.CurrentCommit.Parents.Count());
				Assert.AreEqual("Merge branch 'c' into a", result.Commit.Message);
			}
		}
	}
}
