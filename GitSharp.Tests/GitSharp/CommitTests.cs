using System;
using System.Linq;
using GitSharp.Tests.GitSharp;
using NUnit.Framework;
using System.IO;
using System.Text;

namespace GitSharp.API.Tests
{
	[TestFixture]
	public class CommitTests : ApiTestCase
	{
		[Test]
		public void Commit_into_empty_repository()
		{
			var workingDirectory = Path.Combine(trash.FullName, "test");
			using (Repository repo = Repository.Init(workingDirectory))
			{
				string filepath = Path.Combine(workingDirectory, "for henon.txt");
				File.WriteAllText(filepath, "Weißbier");
				repo.Index.Add(filepath);
				string filepath1 = Path.Combine(workingDirectory, "for nulltoken.txt");
				File.WriteAllText(filepath1, "Rotwein");
				repo.Index.Add(filepath1);
				var commit = repo.Commit("Hello World!", new Author("A. U. Thor", "au@thor.com"));
				Assert.NotNull(commit);
				Assert.IsTrue(commit.IsCommit);
				Assert.IsNull(commit.Parent);
				Assert.AreEqual("A. U. Thor", commit.Author.Name);
				Assert.AreEqual("au@thor.com", commit.Author.EmailAddress);
				Assert.AreEqual("Hello World!", commit.Message);
				// TODO: check if tree contains for henon and for nulltoken, get the blobs and check  the content.
				Assert.AreEqual(commit, repo.Head.CurrentCommit);
				var changes = commit.Changes.ToDictionary(change => change.Name);
				Assert.AreEqual(ChangeType.Added, changes["for henon.txt"].ChangeType);
				Assert.AreEqual(ChangeType.Added, changes["for nulltoken.txt"].ChangeType);
				Assert.AreEqual("Weißbier", (changes["for henon.txt"].ComparedObject as Blob).Data);
				Assert.AreEqual("Rotwein", (changes["for nulltoken.txt"].ComparedObject as Blob).Data);
				Assert.AreEqual(2, changes.Count);
			}
		}

		[Test]
		public void Commit_changes_to_existing_commit()
		{
			using (var repo = GetTrashRepository())
			{
				var workingDirectory = repo.WorkingDirectory;
				string filepath = Path.Combine(workingDirectory, "README");
				File.WriteAllText(filepath, "This is a really short readme file\n\nWill write up some text here.");
				repo.Index.Add(filepath);
				//repo.Index.Remove(Path.Combine(workingDirectory, "a/a1"));
				var commit = repo.Commit("Adding ä README\n\n", new Author("müller", "müller@gitsharp.org"));
				Assert.NotNull(commit);
				Assert.IsTrue(commit.IsCommit);
				Assert.AreEqual(commit.Parent.Hash, "49322bb17d3acc9146f98c97d078513228bbf3c0");
				Assert.AreEqual("müller", commit.Author.Name);
				Assert.AreEqual("müller@gitsharp.org", commit.Author.EmailAddress);
				Assert.AreEqual("Adding ä README\n\n", commit.Message);
				// check if tree contains the new file, get the blob and check  the content.
				Assert.AreEqual(commit, repo.Head.CurrentCommit);
				var previous = new Commit(repo, "HEAD^");
				Assert.IsTrue(previous.IsCommit);
				Assert.AreEqual(previous, commit.Parent);
				var changes = commit.CompareAgainst(previous).ToDictionary(change => change.Name);
				Assert.AreEqual(ChangeType.Added, changes["README"].ChangeType);
				Assert.AreEqual("This is a really short readme file\n\nWill write up some text here.",
									 (changes["README"].ComparedObject as Blob).Data);
				Assert.AreEqual(ChangeType.Deleted, changes["a1"].ChangeType);
				Assert.AreEqual(ChangeType.Deleted, changes["a1.txt"].ChangeType);
				Assert.AreEqual(ChangeType.Deleted, changes["a2.txt"].ChangeType);
				Assert.AreEqual(ChangeType.Deleted, changes["b1.txt"].ChangeType);
				Assert.AreEqual(ChangeType.Deleted, changes["b2.txt"].ChangeType);
				Assert.AreEqual(ChangeType.Deleted, changes["c1.txt"].ChangeType);
				Assert.AreEqual(ChangeType.Deleted, changes["c2.txt"].ChangeType);
				Assert.AreEqual(ChangeType.Deleted, changes["master.txt"].ChangeType);
				Assert.AreEqual(9, changes.Count);

			}
		}

		[Test]
		public void Commit_changes_to_existing_commit1()
		{
			using (var repo = GetTrashRepository())
			{
				var commit = repo.Get<Commit>("f73b95671f326616d66b2afb3bdfcdbbce110b44");
				var changes = commit.Changes.ToDictionary(change => change.Name);
				Assert.AreEqual(ChangeType.Added, changes["a1"].ChangeType);
				Assert.AreEqual(1, changes.Count);

			}
		}

		[Test]
		public void Commit_changes_against_multiple_parents()
		{
			using (var repo = GetTrashRepository())
			{ 
				// this commit has three parents (three branches were merged together but actually nothing has changed!
				var commit = repo.Get<Branch>("master").CurrentCommit;
				Assert.AreEqual(3, commit.Parents.Count());
				var changes = commit.Changes.ToArray();
				Assert.AreEqual(0, changes.Length);

				// two parents
				commit = repo.Get<Commit>("0966a434eb1a025db6b71485ab63a3bfbea520b6");
				Assert.AreEqual(2, commit.Parents.Count());
				changes = commit.Changes.ToArray();
				Assert.AreEqual(0, changes.Length);
			}
		}

		[Test]
		public void Checkout_Commit()
		{
			using (var repo = GetTrashRepository())
			{
				repo.Head.CurrentCommit.Checkout();
				AssertFileExistsInWD("a/a1");
				AssertFileExistsInWD("a/a1.txt");
				AssertFileExistsInWD("a/a2.txt");
				AssertFileExistsInWD("b/b1.txt");
				AssertFileExistsInWD("b/b2.txt");
				AssertFileExistsInWD("c/c1.txt");
				AssertFileExistsInWD("c/c2.txt");
				AssertFileExistsInWD("master.txt");
				repo.Head.CurrentCommit.Parent.Parent.Checkout();
				AssertFileNotExistentInWD("a/a1");
				AssertFileExistsInWD("a/a1.txt");
				AssertFileExistsInWD("a/a2.txt");
				AssertFileNotExistentInWD("b/b1.txt");
				AssertFileNotExistentInWD("b/b2.txt");
				AssertFileExistsInWD("c/c1.txt");
				AssertFileExistsInWD("c/c2.txt");
				AssertFileExistsInWD("master.txt");
			}
		}

		[Test]
		public void Checkout_Paths()
		{
			using (var repo = GetTrashRepository())
			{
				//repo.Head.CurrentCommit.Checkout();
				var c = repo.Head.CurrentCommit;
				c.Checkout("a/a1", "a/a1.txt", "a/a2.txt", "master.txt");
				AssertFileExistsInWD("a/a1");
				AssertFileExistsInWD("a/a1.txt");
				AssertFileExistsInWD("a/a2.txt");
				AssertFileExistsInWD("master.txt");
				var c1 = repo.Head.CurrentCommit.Parent.Parent;
				Assert.Throws(typeof(ArgumentException), () => c1.Checkout("b/b1.txt"));
				Assert.Throws(typeof(ArgumentException), () => c1.Checkout("blahblah"));
				c1.Checkout("c/c1.txt");
				AssertFileExistsInWD("c/c1.txt");
			}
		}

		[Test]
		public void Commit_ancestors()
		{
			using (var repo = GetTrashRepository())
			{
				var commit = repo.Get<Branch>("master").CurrentCommit;
				var ancestors = commit.Ancestors;
				Assert.AreNotEqual(ancestors.First().Hash, commit.Hash);
			}
		}
	}
}
