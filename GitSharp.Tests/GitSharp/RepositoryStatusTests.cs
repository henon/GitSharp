/*
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.Core;
using GitSharp.Tests.GitSharp;
using NUnit.Framework;
using System.IO;

namespace GitSharp.API.Tests
{
	[TestFixture]
	public class RepositoryStatusTests : ApiTestCase
	{

		[Test]
		public void StatusEvenWorksWithHeadLessRepo()
		{
			using (var repo = Repository.Init(Path.Combine(trash.FullName, "test")))
			{
				RepositoryStatus status = null;
				Assert.DoesNotThrow(() => status = repo.Status);
				Assert.IsFalse(repo.Status.AnyDifferences);
				Assert.AreEqual(0,
									 status.Added.Count + status.Staged.Count + status.Missing.Count + status.Modified.Count +
									 status.Removed.Count);
			}
		}

		[Test]
		public void TracksAddedFiles()
		{
			//setup of .git directory
			var resource =
				 new DirectoryInfo(Path.Combine(Path.Combine(Environment.CurrentDirectory, "Resources"),
														  "CorruptIndex"));
			var tempRepository =
				 new DirectoryInfo(Path.Combine(trash.FullName, "CorruptIndex" + Path.GetRandomFileName()));
			CopyDirectory(resource.FullName, tempRepository.FullName);

			var repositoryPath = new DirectoryInfo(Path.Combine(tempRepository.FullName, Constants.DOT_GIT));
			Directory.Move(repositoryPath.FullName + "ted", repositoryPath.FullName);

			using (var repository = new Repository(repositoryPath.FullName))
			{
				var status = repository.Status;

				Assert.IsTrue(status.AnyDifferences);
				Assert.AreEqual(1, status.Added.Count);
				Assert.IsTrue(status.Added.Contains("b.txt")); // the file already exists in the index (eg. has been previously git added)
				Assert.AreEqual(0, status.Staged.Count);
				Assert.AreEqual(0, status.Missing.Count);
				Assert.AreEqual(0, status.Modified.Count);
				Assert.AreEqual(0, status.Removed.Count);
				Assert.AreEqual(0, status.Untracked.Count);

				string filepath = Path.Combine(repository.WorkingDirectory, "c.txt");
				writeTrashFile(filepath, "c");
				repository.Index.Add(filepath);

				status = repository.Status;

				Assert.IsTrue(status.AnyDifferences);
				Assert.AreEqual(2, status.Added.Count);
				Assert.IsTrue(status.Added.Contains("b.txt"));
				Assert.IsTrue(status.Added.Contains("c.txt"));
				Assert.AreEqual(0, status.Staged.Count);
				Assert.AreEqual(0, status.Missing.Count);
				Assert.AreEqual(0, status.Modified.Count);
				Assert.AreEqual(0, status.Removed.Count);
				Assert.AreEqual(0, status.Untracked.Count);

				repository.Commit("after that no added files should remain", Author.Anonymous);
				status = repository.Status;

				Assert.AreEqual(0, status.Added.Count);
				Assert.AreEqual(0, status.Staged.Count);
				Assert.AreEqual(0, status.Missing.Count);
				Assert.AreEqual(0, status.Modified.Count);
				Assert.AreEqual(0, status.Removed.Count);
				Assert.AreEqual(0, status.Untracked.Count);
				Assert.AreEqual(0, status.Untracked.Count);
			}
		}

		[Test]
		public void UntrackedFiles()
		{
			using (var repo = new Repository(trash.FullName))
			{
				var a = writeTrashFile("untracked.txt", "");
				var b = writeTrashFile("someDirectory/untracked2.txt", "");
				repo.Index.Add(writeTrashFile("untracked2.txt", "").FullName); // <-- adding a file with same name in higher directory to test the file name comparison.
				var status = repo.Status;
				Assert.AreEqual(2, status.Untracked.Count);
				Assert.IsTrue(status.Untracked.Contains("untracked.txt"));
				Assert.IsTrue(status.Untracked.Contains("someDirectory/untracked2.txt"));
				Assert.IsTrue(status.Added.Contains("untracked2.txt"));
				Assert.AreEqual(0, status.Staged.Count);
				Assert.AreEqual(0, status.Missing.Count);
				Assert.AreEqual(0, status.Modified.Count);
				Assert.AreEqual(8, status.Removed.Count);
			}
		}


		[Test]
		public void Modified_and_Staged()
		{
			using (var repo = GetTrashRepository())
			{
				var index = repo.Index;
				index.Add(writeTrashFile("file2", "file2").FullName, writeTrashFile("dir/file3", "dir/file3").FullName);
				repo.Commit("committing file2 and dir/file2", Author.Anonymous);
				index.Add(writeTrashFile("file2", "file2 changed").FullName, writeTrashFile("dir/file3", "dir/file3 changed").FullName);
				writeTrashFile("dir/file3", "modified");

				var status = repo.Status;

				Assert.AreEqual(2, status.Staged.Count);
				Assert.IsTrue(status.Staged.Contains("file2"));
				Assert.IsTrue(status.Staged.Contains("dir/file3"));
				Assert.AreEqual(1, status.Modified.Count);
				Assert.IsTrue(status.Modified.Contains("dir/file3"));
				Assert.AreEqual(0, status.Added.Count);
				Assert.AreEqual(0, status.Removed.Count);
				Assert.AreEqual(0, status.Missing.Count);
				Assert.AreEqual(0, status.Untracked.Count);

				repo.Commit("committing staged changes, modified files should still be modified", Author.Anonymous);
				status = repo.Status;

				Assert.AreEqual(0, status.Staged.Count);
				Assert.AreEqual(1, status.Modified.Count);
				Assert.IsTrue(status.Modified.Contains("dir/file3"));
			}
		}

		[Test]
		public void Removed()
		{
			using (var repo = GetTrashRepository())
			{
				var index = repo.Index;
				index.Stage(writeTrashFile("file2", "file2").FullName, writeTrashFile("dir/file3", "dir/file3").FullName);
				index.CommitChanges("...", Author.Anonymous);
				index.Remove(Path.Combine(trash.FullName, "file2"), Path.Combine(trash.FullName, "dir"));

				var diff = index.Status;
				Assert.AreEqual(2, diff.Removed.Count);
				Assert.IsTrue(diff.Removed.Contains("file2"));
				Assert.IsTrue(diff.Removed.Contains("dir/file3"));
				Assert.AreEqual(0, diff.Staged.Count);
				Assert.AreEqual(0, diff.Modified.Count);
				Assert.AreEqual(0, diff.Added.Count);
				// Assert.AreEqual(2, diff.Untracked.Count); TODO: verify!
				//Assert.IsTrue(diff.Untracked.Contains("file2"));
				//Assert.IsTrue(diff.Untracked.Contains("dir/file3"));
				Assert.AreEqual(0, diff.Untracked.Count);

				repo.Commit("committing staged changes, this does not delete removed files from the working directory. they should be untracked now.", Author.Anonymous);
				diff = repo.Status;

				Assert.AreEqual(0, diff.Removed.Count);
				Assert.AreEqual(2, diff.Untracked.Count);
				Assert.IsTrue(diff.Untracked.Contains("file2"));
				Assert.IsTrue(diff.Untracked.Contains("dir/file3"));
			}
		}

		[Test]
		public void Missing()
		{
			using (var repo = GetTrashRepository())
			{
				var index = repo.Index;
				index.Stage(writeTrashFile("file2", "file2").FullName, writeTrashFile("dir/file3", "dir/file3").FullName);
				index.CommitChanges("...", Author.Anonymous);
				index.Stage(writeTrashFile("file4", "file4").FullName, writeTrashFile("dir/file4", "dir/file4").FullName);

				new FileInfo(Path.Combine(repo.WorkingDirectory, "file2")).Delete();
				new FileInfo(Path.Combine(repo.WorkingDirectory, "file4")).Delete();
				new DirectoryInfo(Path.Combine(repo.WorkingDirectory, "dir")).Delete(true);

				var diff = index.Status;
				Assert.AreEqual(2, diff.Added.Count);
				Assert.AreEqual(0, diff.Staged.Count);
				Assert.AreEqual(0, diff.Modified.Count);
				Assert.AreEqual(0, diff.Untracked.Count);
				Assert.AreEqual(4, diff.Missing.Count);
				Assert.IsTrue(diff.Missing.Contains("file2"));
				Assert.IsTrue(diff.Missing.Contains("file4"));
				Assert.IsTrue(diff.Missing.Contains("dir/file3"));
				Assert.IsTrue(diff.Missing.Contains("dir/file4"));


				repo.Commit("committing the added files which are missing in the working directory", Author.Anonymous);
				diff = repo.Status;

				Assert.AreEqual(0, diff.Added.Count);
				Assert.AreEqual(0, diff.Staged.Count);
				Assert.AreEqual(0, diff.Modified.Count);
				Assert.AreEqual(0, diff.Untracked.Count);
				Assert.AreEqual(4, diff.Missing.Count);
			}
		}

		[Test]
		public void DirectoryTreeTest() // <--  tests the working directory iterator substitution used for untrackt file detection and ignore handling ...
		{
			using (var repo = GetTrashRepository())
			{
				repo.CurrentBranch.Reset(ResetBehavior.Hard);
				var dir = new DirectoryTree(repo);
				Assert.AreEqual(null, dir.Name);
				var members = dir.Members.ToDictionary(m => m.Name);
				Assert.IsTrue(members.ContainsKey("master.txt"));
				Assert.AreEqual("master.txt", members["master.txt"].FullName);
				Assert.IsTrue(members.ContainsKey("a"));
				Assert.AreEqual("a", members["a"].FullName);
				var a = members["a"] as DirectoryTree;
				Assert.NotNull(a);
				var a_members = a.Members.ToDictionary(m => m.Name);
				Assert.AreEqual("a/a1", a_members["a1"].FullName);
				var iter = new TreeIterator(dir, TreeIterator.Order.POSTORDER);
				Assert.IsTrue(iter.hasNext());
				var entry = iter.next();
				Assert.NotNull(iter.next());
				Assert.NotNull(iter.next());
				Assert.NotNull(iter.next());
				Assert.NotNull(iter.next());
				Assert.NotNull(iter.next());
				Assert.NotNull(iter.next());
				Assert.NotNull(iter.next());
			}
		}
	}
}
