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

		/// <summary>
		/// When not allowing a fast forward merge, a new commit is created the merged commits as parent.
		/// </summary>
		[Test]
		public void MergeStrategyRecursive_FastForwardOff()
		{
			using (var repo = GetTrashRepository())
			{
				var a = repo.Get<Branch>("a");
				var c = repo.Get<Branch>("c");
				var result = Git.Merge(new MergeOptions { NoFastForward = true, Branches = new[]{ a, c }, MergeStrategy = MergeStrategy.Recursive }); 
				Assert.IsTrue(result.Success);
				Assert.AreEqual(repo.Get<Branch>("c").CurrentCommit.Tree, a.CurrentCommit.Tree);
				var a_commit = repo.Get<Branch>("a").CurrentCommit;
				var c_commit = repo.Get<Branch>("c").CurrentCommit;
				Assert.IsTrue(a_commit.Parents.Contains(c_commit));
				Assert.IsTrue(a_commit.Parents.Contains(repo.Tags["A"].Target as Commit));
				Assert.AreEqual(2, a.CurrentCommit.Parents.Count());
				Assert.AreEqual("Merge branch 'c' into a", result.Commit.Message);
			}
		}

	}
}
