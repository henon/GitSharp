/*
 * Copyright (C) 2009, nulltoken <emeric.fermas@gmail.com>
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
using GitSharp.Tests.GitSharp;
using NUnit.Framework;
using GitSharp.Core.Tests;
using System.IO;

namespace GitSharp.API.Tests
{
	[TestFixture]
	public class IndexTest : ApiTestCase
	{
		[Test]
		public void IndexAdd()
		{
			var workingDirectory = Path.Combine(trash.FullName, "test");
			using (var repo = Repository.Init(workingDirectory))
			{
				var index_path = Path.Combine(repo.Directory, "index");
				var old_index = Path.Combine(repo.Directory, "old_index");
				var index = repo.Index;
				index.Write(); // write empty index
				new FileInfo(index_path).CopyTo(old_index);
				string filepath = Path.Combine(workingDirectory, "for henon.txt");
				File.WriteAllText(filepath, "Weißbier");
				repo.Index.Add(filepath);
				// now verify
				Assert.IsTrue(new FileInfo(index_path).Exists);
				Assert.AreNotEqual(File.ReadAllBytes(old_index), File.ReadAllBytes(index_path));

				// make another addition
				var index_1 = Path.Combine(repo.Directory, "index_1");
				new FileInfo(index_path).CopyTo(index_1);
				string filepath1 = Path.Combine(workingDirectory, "for nulltoken.txt");
				File.WriteAllText(filepath1, "Rotwein");
				index = new Index(repo);
				index.Add(filepath1);
				Assert.AreNotEqual(File.ReadAllBytes(index_1), File.ReadAllBytes(index_path));
				Assert.DoesNotThrow(() => repo.Index.Read());

				var status = repo.Status;
				Assert.IsTrue(status.Added.Contains("for henon.txt"));
				Assert.IsTrue(status.Added.Contains("for nulltoken.txt"));
				Assert.AreEqual(2, status.Added.Count);
				Assert.AreEqual(0, status.Staged.Count);
				Assert.AreEqual(0, status.Missing.Count);
				Assert.AreEqual(0, status.Modified.Count);
				Assert.AreEqual(0, status.Removed.Count);
			}
		}

		[Test]
		public void Read_write_empty_index()
		{
			using (var repo = GetTrashRepository())
			{
				var index_path = Path.Combine(repo.Directory, "index");
				var old_index = Path.Combine(repo.Directory, "old_index");
				var index = repo.Index;
				index.Write(); // write empty index
				Assert.IsTrue(new FileInfo(index_path).Exists);
				new FileInfo(index_path).MoveTo(old_index);
				Assert.IsFalse(new FileInfo(index_path).Exists);

				using (var repo2 = new Repository(repo.Directory))
				{
					Index new_index = repo2.Index;
					new_index.Write(); // see if the read index is rewritten identitcally
				}

				Assert.IsTrue(new FileInfo(index_path).Exists);
				Assert.AreEqual(File.ReadAllBytes(old_index), File.ReadAllBytes(index_path));
			}
		}

		[Test]
		public void Diff_special_msysgit_index()
		{
			using (var repo = GetTrashRepository())
			{
				var index_path = Path.Combine(repo.Directory, "index");
				new FileInfo("Resources/index_originating_from_msysgit").CopyTo(index_path);

				var status = repo.Status;
				var added = new HashSet<string>
                                {
                                    "New Folder/New Ruby Program.rb",
                                    "for henon.txt",
                                    "test.cmd",
                                };
				var removed = new HashSet<string>
                                  {
                                      "a/a1",
                                      "a/a1.txt",
                                      "a/a2.txt",
                                      "b/b1.txt",
                                      "b/b2.txt",
                                      "c/c1.txt",
                                      "c/c2.txt",
                                      "master.txt"
                                  };

				Assert.IsTrue(added.SetEquals(status.Added));
				Assert.AreEqual(0, status.Staged.Count);
				Assert.IsTrue(added.SetEquals(status.Missing));
				Assert.AreEqual(0, status.Modified.Count);
				Assert.IsTrue(removed.SetEquals(status.Removed));
			}
		}

		/// <summary>
		/// This testcase shows how to interact with the index without a working directory
		/// </summary>
		[Test]
		public void Add_and_Commit_directly()
		{
			using (var repo = GetTrashRepository())
			{
				var index = repo.Index;
				index.AddContent("I/am/a.file", "and this is the content\nin me.");
				Assert.AreEqual("and this is the content\nin me.", index.GetContent("I/am/a.file"));
				Assert.AreEqual(new[] { "I/am/a.file" }, index.Entries);
				Assert.IsTrue(index["I/am/a.file"].IsBlob);

				string iAmAFile = string.Format("I{0}am{0}a.file", Path.DirectorySeparatorChar);
				Assert.AreEqual(index["I/am/a.file"], index[iAmAFile]); // internal git slash conversion

				repo.Commit("committing our new file which is not actually present in the working directory.");
				Assert.AreEqual(new[] { "I/am/a.file" }, index.Entries);
				repo.SwitchToBranch(repo.CurrentBranch);
				Assert.IsTrue(new FileInfo(Path.Combine(repo.WorkingDirectory, "I/am/a.file")).Exists);
			}
		}

		[Test]
		public void Access_Index_Members()
		{
			using (var repo = GetTrashRepository())
			{
				repo.SwitchToBranch("master");
				var index = repo.Index;
				Assert.AreEqual(new[] { "a/a1", "a/a1.txt", "a/a2.txt", "b/b1.txt", "b/b2.txt", "c/c1.txt", "c/c2.txt", "master.txt" }, index.Entries);
				Assert.IsTrue(index["a/a1"].IsBlob);
				Assert.IsTrue(index["master.txt"].IsBlob);
			}
		}

		[Test]
		public void Stage_Unstage()
		{
			using (var repo = GetTrashRepository())
			{
				var foo_bar = "foo/bar" + Path.GetRandomFileName();
				var index = repo.Index;
				index.AddContent(foo_bar, "baz");
				Assert.AreEqual("baz", index.GetContent(foo_bar));
				repo.Commit("committing ... ", Author.Anonymous);
				index.StageContent(foo_bar, "buzz");
				Assert.AreEqual("buzz", index.GetContent(foo_bar));
				index.Unstage(foo_bar);
				Assert.AreEqual("baz", index.GetContent(foo_bar));
				index.Unstage(foo_bar); // <--- unstage on committed file has no effect. to remove a file completely from the index we need to use remove
				Assert.AreEqual("baz", index.GetContent(foo_bar));
				index.Remove(foo_bar);
				Assert.IsNull(index.GetContent(foo_bar));
				var added_file = "some/new/content/Added.txt";
				index.AddContent(added_file, "");
				index.Unstage(added_file);
				Assert.IsNull(index.GetContent(added_file));
			}
		}

		[Test]
		public void Checkout_From_Index()
		{
			using (var repo = GetTrashRepository())
			{
				var index = repo.Index;
				// first adding content to the index without relying on the file system
				index.AddContent("foo/bar", "baz");
				index.StageContent("foo/bar", "buzz");
				index.AddContent("hmm.txt", "");
				// then check out and see if the files exist in the working directory
				index.Checkout();
				AssertFileExistsInWD("foo/bar");
				AssertFileContentInWDEquals("foo/bar", "buzz");
				AssertFileExistsInWD("hmm.txt");
				AssertFileContentInWDEquals("hmm.txt", "");
				// check out again, nothing should change
				index.Checkout();
				AssertFileExistsInWD("foo/bar");
				AssertFileContentInWDEquals("foo/bar", "buzz");
				AssertFileExistsInWD("hmm.txt");
				AssertFileContentInWDEquals("hmm.txt", "");
			}
		}


		[Test]
		public void Checkout_Single_Files_From_Index()
		{
			using (var repo = GetTrashRepository())
			{
				var index = repo.Index;
				// first adding content to the index without relying on the file system
				index.AddContent("foo/bar", "baz");
				index.StageContent("foo/bar", "buzz");
				index.AddContent("hmm.txt", "");
				index.Checkout("foo/bar");
				AssertFileExistsInWD("foo/bar");
				AssertFileNotExistentInWD("hmm.txt");
				AssertFileContentInWDEquals("foo/bar", "buzz");
				index.Checkout("hmm.txt");
				AssertFileExistsInWD("hmm.txt");
				AssertFileContentInWDEquals("hmm.txt", "");
			}
		}

		// AddAll tests

		[Test]
		public void AddAll_No_New_Files()
		{
			// test AddAll with empty directory results in no differences
			var workingDirectory = Path.Combine(trash.FullName, "test");
			using (var repo = Repository.Init(workingDirectory))
			{
				var status = new RepositoryStatus(repo);
				Assert.IsFalse(status.AnyDifferences);
				// verify AddAll does nothing as there have been no changes made
				repo.Index.AddAll();
				Assert.IsFalse(status.AnyDifferences);
				Assert.AreEqual(0, status.Added.Count);
				Assert.AreEqual(0, status.Untracked.Count);
				Assert.AreEqual(0, status.Staged.Count);
				Assert.AreEqual(0, status.Missing.Count);
				Assert.AreEqual(0, status.Modified.Count);
				Assert.AreEqual(0, status.Removed.Count);
			}
		}

		[Test]
		public void AddAll_New_Files()
		{
			// test AddAll with one new file in the directory retults in one added file and is flagged as a difference
			var workingDirectory = Path.Combine(trash.FullName, "test");
			using (var repo = Repository.Init(workingDirectory))
			{
				var filePath = Path.Combine(repo.WorkingDirectory, "testfile.txt");
				File.WriteAllText(filePath, "Unadded file");
				repo.Index.AddAll();
				var status = new RepositoryStatus(repo);
				Assert.IsTrue(status.AnyDifferences);
				// there should be 1 added file
				Assert.AreEqual(1, status.Added.Count);
				// the added file must be the file that was just added
				status.Added.Contains(filePath);
				// no other changes
				Assert.AreEqual(0, status.Untracked.Count);
				Assert.AreEqual(0, status.Staged.Count);
				Assert.AreEqual(0, status.Missing.Count);
				Assert.AreEqual(0, status.Modified.Count);
				Assert.AreEqual(0, status.Removed.Count);
			}
		}

		[Test]
		public void AddAll_New_Directory()
		{
			// test AddAll with one new direcotry is flagged as a new add
			var workingDirectory = Path.Combine(trash.FullName, "test");
			using (var repo = Repository.Init(workingDirectory))
			{
				var dirPath = Path.Combine(repo.WorkingDirectory, "testdir");
				Directory.CreateDirectory(dirPath);
				repo.Index.AddAll();
				var status = new RepositoryStatus(repo);
				Assert.IsFalse(status.AnyDifferences);
				// there should be 0 added file
				Assert.AreEqual(0, status.Added.Count);
				// the added direcotry must be the directory that was just added
				status.Added.Contains(dirPath);
				// no other changes
				Assert.AreEqual(0, status.Untracked.Count);
				Assert.AreEqual(0, status.Staged.Count);
				Assert.AreEqual(0, status.Missing.Count);
				Assert.AreEqual(0, status.Modified.Count);
				Assert.AreEqual(0, status.Removed.Count);
			}
		}

		[Test]
		public void AddAll_New_Directory_With_File()
		{
			// test AddAll with one new directory and a new file inside the directory
			var workingDirectory = Path.Combine(trash.FullName, "test");
			using (var repo = Repository.Init(workingDirectory))
			{
				var dirPath = Path.Combine(repo.WorkingDirectory, "testdir");
				Directory.CreateDirectory(dirPath);
				var filePath = Path.Combine(dirPath, "testfile.txt");
				File.WriteAllText(filePath, "Unadded file");
				repo.Index.AddAll();
				var status = new RepositoryStatus(repo);
				Assert.IsTrue(status.AnyDifferences);
				// there should be 1 added files
				Assert.AreEqual(1, status.Added.Count);
				// the added file must be the file that was just added
				status.Added.Contains(filePath);
				status.Added.Contains(dirPath);
				// no other changes
				Assert.AreEqual(0, status.Untracked.Count);
				Assert.AreEqual(0, status.Staged.Count);
				Assert.AreEqual(0, status.Missing.Count);
				Assert.AreEqual(0, status.Modified.Count);
				Assert.AreEqual(0, status.Removed.Count);
			}
		}

		[Test]
		public void AddAll_New_File_And_New_Directory_With_File()
		{
			// test AddAll with one file, one new directory and a new file inside that directory
			var workingDirectory = Path.Combine(trash.FullName, "test");
			using (var repo = Repository.Init(workingDirectory))
			{
				var filePath = Path.Combine(repo.WorkingDirectory, "testfile.txt");
				var dirPath = Path.Combine(repo.WorkingDirectory, "testdir");
				var filePath2 = Path.Combine(dirPath, "testfile.txt");
				File.WriteAllText(filePath, "Unadded file");
				Directory.CreateDirectory(dirPath);
				File.WriteAllText(filePath2, "Unadded file");
				repo.Index.AddAll();
				var status = new RepositoryStatus(repo);
				Assert.IsTrue(status.AnyDifferences);
				// there should be 2 added files
				Assert.AreEqual(2, status.Added.Count);
				// the added file must be the file that was just added
				status.Added.Contains(filePath);
				status.Added.Contains(filePath2);
				status.Added.Contains(dirPath);
				// no other changes
				Assert.AreEqual(0, status.Untracked.Count);
				Assert.AreEqual(0, status.Staged.Count);
				Assert.AreEqual(0, status.Missing.Count);
				Assert.AreEqual(0, status.Modified.Count);
				Assert.AreEqual(0, status.Removed.Count);
			}
		}

		[Test]
		public void AddAll_Directory_Recursive()
		{
			// test AddAll with one new directory with a directory within it and each these directories containing a new file within it
			// root folder also contains one new file
			var workingDirectory = Path.Combine(trash.FullName, "test");
			using (var repo = Repository.Init(workingDirectory))
			{
				var dirPath = Path.Combine(repo.WorkingDirectory, "testdir");
				var dirPath2 = Path.Combine(dirPath, "testdir2");
				var filePath = Path.Combine(repo.WorkingDirectory, "testfile.txt");
				var filePath2 = Path.Combine(dirPath, "testfile.txt");
				var filePath3 = Path.Combine(dirPath2, "testfile.txt");
				Directory.CreateDirectory(dirPath);
				Directory.CreateDirectory(dirPath2);
				File.WriteAllText(filePath, "Unadded file");
				File.WriteAllText(filePath2, "Unadded file");
				File.WriteAllText(filePath3, "Unadded file");
				repo.Index.AddAll();
				var status = new RepositoryStatus(repo);
				Assert.IsTrue(status.AnyDifferences);
				// there should be 3 added files
				Assert.AreEqual(3, status.Added.Count);
				// the added file must be the file that was just added
				status.Added.Contains(dirPath);
				status.Added.Contains(dirPath2);
				status.Added.Contains(filePath);
				status.Added.Contains(filePath2);
				status.Added.Contains(filePath3);
				// no other changes
				Assert.AreEqual(0, status.Untracked.Count);
				Assert.AreEqual(0, status.Staged.Count);
				Assert.AreEqual(0, status.Missing.Count);
				Assert.AreEqual(0, status.Modified.Count);
				Assert.AreEqual(0, status.Removed.Count);
			}
		}

		[Test]
		public void AddAll_Ignore_Files()
		{
			// test AddAll files added to the DOT_IGNORE file
			var workingDirectory = Path.Combine(trash.FullName, "test");
			using (var repo = Repository.Init(workingDirectory))
			{
				var ignoreFilePath = Path.Combine(repo.WorkingDirectory, GitSharp.Core.Constants.GITIGNORE_FILENAME);
				// add the name of the test file to the ignore list
				File.WriteAllText(ignoreFilePath, "*.txt");
				repo.Index.Add(ignoreFilePath);
				repo.Commit("Committing .gitignore file so it won't show up as untracked file");
				var filePath = Path.Combine(repo.WorkingDirectory, "testfile.txt");
				File.WriteAllText(filePath, "Unadded file");
				repo.Index.AddAll();
				var status = new RepositoryStatus(repo);
				// no changes are the file should be ignored
				Assert.IsFalse(status.AnyDifferences);
				// there should be 0 added file
				Assert.AreEqual(0, status.Added.Count);
				// no other changes
				Assert.AreEqual(0, status.Untracked.Count);
				Assert.AreEqual(0, status.Staged.Count);
				Assert.AreEqual(0, status.Missing.Count);
				Assert.AreEqual(0, status.Modified.Count);
				Assert.AreEqual(0, status.Removed.Count);
			}
		}

		// TODO: test add's behavior on wrong input data
	}
}