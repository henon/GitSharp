/*
 * Copyright (C) 2009, Rolenun <rolenun@gmail.com>
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

using System.IO;
using GitSharp.Core.Tests;
using GitSharp.Commands;
using GitSharp.Tests.GitSharp;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using System.Collections;

namespace GitSharp.API.Tests
{
	[TestFixture]
	public class StatusTests : ApiTestCase
	{
		const string filename = "newfile.txt";
		const string filenameSubdir1 = "subdir1/newfile.txt";
		const string filenameSubdir2 = "subdir1/subdir2/newfile.txt";
		
		int repCount;
		
		delegate RepositoryStatus StatusOperation (Repository repo, List<string> outFilesToCheck);

		void RunStatusTests(StatusOperation oper)
		{
            //Due to the cumulative nature of these tests, rather than recreate the same 
            // conditions multiple times, all StatusResult testing has been rolled into one test.
			
			var path = Path.Combine(trash.FullName, "test" + ++repCount);
			if (Directory.Exists (path)) {
				Directory.Delete (path, true);
				Directory.CreateDirectory (path);
			}
            using (var repo = Repository.Init(path, false))
            {
				List<string> filesToCheck = new List<string> ();
				
                //Verify the file has not already been created
				filesToCheck.Clear ();
				RepositoryStatus results = oper (repo, filesToCheck);
                AssertStatus (results, filesToCheck, filename);
                AssertStatus (results, filesToCheck, filenameSubdir1);
                AssertStatus (results, filesToCheck, filenameSubdir2);

	            //Create the files and verify the files are untracked
				if (!Directory.Exists (repo.FromGitPath ("subdir1/subdir2")))
					Directory.CreateDirectory (repo.FromGitPath ("subdir1/subdir2"));
	            File.WriteAllText(repo.FromGitPath (filename), "Just a simple test.");
	            File.WriteAllText(repo.FromGitPath (filenameSubdir1), "Just a simple test.");
	            File.WriteAllText(repo.FromGitPath (filenameSubdir2), "Just a simple test.");
				
				filesToCheck.Clear ();
				results = oper (repo, filesToCheck);
                AssertStatus (results, filesToCheck, filename, results.Untracked);
                AssertStatus (results, filesToCheck, filenameSubdir1, results.Untracked);
                AssertStatus (results, filesToCheck, filenameSubdir2, results.Untracked);
				
				// Modify a file
				File.WriteAllText(repo.FromGitPath (filenameSubdir1), "Just a simple modified test.");
				
				filesToCheck.Clear ();
				results = oper (repo, filesToCheck);
                AssertStatus (results, filesToCheck, filename, results.Untracked);
                AssertStatus (results, filesToCheck, filenameSubdir1, results.Untracked);
                AssertStatus (results, filesToCheck, filenameSubdir2, results.Untracked);
				
				//Add an unmodified file to the index
	            File.WriteAllText(repo.FromGitPath (filenameSubdir1), "Just a simple test.");
				Index index = new Index(repo);
				index.Add(filenameSubdir1);
				
				filesToCheck.Clear ();
				results = oper (repo, filesToCheck);
                AssertStatus (results, filesToCheck, filename, results.Untracked);
                AssertStatus (results, filesToCheck, filenameSubdir1, results.Added);
                AssertStatus (results, filesToCheck, filenameSubdir2, results.Untracked);

				//Modify file in the index
				File.WriteAllText(repo.FromGitPath (filenameSubdir1), "Just a simple modified test.");
				
				filesToCheck.Clear ();
				results = oper (repo, filesToCheck);
                AssertStatus (results, filesToCheck, filename, results.Untracked);
                AssertStatus (results, filesToCheck, filenameSubdir1, results.Modified, results.Added);
                AssertStatus (results, filesToCheck, filenameSubdir2, results.Untracked);

				// Commit the added file
				repo.Commit ("test 1");
				
				filesToCheck.Clear ();
				results = oper (repo, filesToCheck);
                AssertStatus (results, filesToCheck, filename, results.Untracked);
                AssertStatus (results, filesToCheck, filenameSubdir1, results.Modified);
                AssertStatus (results, filesToCheck, filenameSubdir2, results.Untracked);

				// Commit the modification
				index.Add (filenameSubdir1);
				repo.Commit ("test 2");
				
				filesToCheck.Clear ();
				results = oper (repo, filesToCheck);
                AssertStatus (results, filesToCheck, filename, results.Untracked);
                AssertStatus (results, filesToCheck, filenameSubdir1);
                AssertStatus (results, filesToCheck, filenameSubdir2, results.Untracked);

				// Modify the committed file
				File.WriteAllText(repo.FromGitPath (filenameSubdir1), "Modified after commit.");
				
				filesToCheck.Clear ();
				results = oper (repo, filesToCheck);
                AssertStatus (results, filesToCheck, filename, results.Untracked);
                AssertStatus (results, filesToCheck, filenameSubdir1, results.Modified);
                AssertStatus (results, filesToCheck, filenameSubdir2, results.Untracked);

				// Remove the committed file
				File.Delete (repo.FromGitPath (filenameSubdir1));
				
				filesToCheck.Clear ();
				results = oper (repo, filesToCheck);
                AssertStatus (results, filesToCheck, filename, results.Untracked);
                AssertStatus (results, filesToCheck, filenameSubdir1, results.Removed);
                AssertStatus (results, filesToCheck, filenameSubdir2, results.Untracked);

				// Stage the file removal
				index.Remove (filenameSubdir1);
				
				filesToCheck.Clear ();
				results = oper (repo, filesToCheck);
                AssertStatus (results, filesToCheck, filename, results.Untracked);
                AssertStatus (results, filesToCheck, filenameSubdir1, results.Removed);
                AssertStatus (results, filesToCheck, filenameSubdir2, results.Untracked);
				
				// Commit changes
				repo.Commit ("test 3");
				filesToCheck.Clear ();
				results = oper (repo, filesToCheck);
                AssertStatus (results, filesToCheck, filename, results.Untracked);
                AssertStatus (results, filesToCheck, filenameSubdir1);
                AssertStatus (results, filesToCheck, filenameSubdir2, results.Untracked);
				
				// Modify the committed file, stage it and delete it
				File.WriteAllText(repo.FromGitPath (filenameSubdir1), "Modified before delete.");
				index.Add (filenameSubdir1);
				File.Delete (repo.FromGitPath (filenameSubdir1));
				filesToCheck.Clear ();
				results = oper (repo, filesToCheck);
                AssertStatus (results, filesToCheck, filename, results.Untracked);
                AssertStatus (results, filesToCheck, filenameSubdir1, results.Added, results.Removed);
                AssertStatus (results, filesToCheck, filenameSubdir2, results.Untracked);
			}
		}
		
		void AssertStatus (RepositoryStatus results, List<string> filesToCheck, string file, params HashSet<string>[] fileStatuses)
		{
			Assert.IsNotNull(results);
			
			var allStatus = new HashSet<string> [] {
				results.Added,
				results.MergeConflict,
				results.Missing,
				results.Modified,
				results.Removed,
				results.Staged,
				results.Untracked
			};
			
			var allStatusName = new string[] {
				"Added",
				"MergeConflict",
				"Missing",
				"Modified",
				"Removed",
				"Staged",
				"Untracked"
			};
			
			if (!filesToCheck.Contains (file))
				fileStatuses = new HashSet<string>[0];
			
			for (int n=0; n<allStatus.Length; n++) {
				var status = allStatus [n];
				if (((IList)fileStatuses).Contains (status))
					Assert.IsTrue (status.Contains (file), "File " + file + " not found in " + allStatusName[n] + " collection");
				else
					Assert.IsFalse (status.Contains (file), "File " + file + " should no be in " + allStatusName[n] + " collection");
			}
		}
		
		[Test]
		public void TestRecursiveDirectoryRoot ()
		{
			RunStatusTests (delegate (Repository repo, List<string> outFilesToCheck) {
				outFilesToCheck.Add (filename);
				outFilesToCheck.Add (filenameSubdir1);
				outFilesToCheck.Add (filenameSubdir2);
				return repo.GetDirectoryStatus ("", true);
			});
		}

		[Test]
		public void TestRecursiveDirectorySubdir1 ()
		{
			RunStatusTests (delegate (Repository repo, List<string> outFilesToCheck) {
				outFilesToCheck.Add (filenameSubdir1);
				outFilesToCheck.Add (filenameSubdir2);
				return repo.GetDirectoryStatus ("subdir1", true);
			});
		}

		[Test]
		public void TestRecursiveDirectorySubdir2 ()
		{
			RunStatusTests (delegate (Repository repo, List<string> outFilesToCheck) {
				outFilesToCheck.Add (filenameSubdir2);
				return repo.GetDirectoryStatus ("subdir1/subdir2", true);
			});
		}

		[Test]
		public void TestNonrecursiveDirectoryRoot ()
		{
			RunStatusTests (delegate (Repository repo, List<string> outFilesToCheck) {
				outFilesToCheck.Add (filename);
				return repo.GetDirectoryStatus ("", false);
			});
		}

		[Test]
		public void TestNonrecursiveDirectorySubdir1 ()
		{
			RunStatusTests (delegate (Repository repo, List<string> outFilesToCheck) {
				outFilesToCheck.Add (filenameSubdir1);
				return repo.GetDirectoryStatus ("subdir1", false);
			});
		}

		[Test]
		public void TestNonrecursiveDirectorySubdir2 ()
		{
			RunStatusTests (delegate (Repository repo, List<string> outFilesToCheck) {
				outFilesToCheck.Add (filenameSubdir2);
				return repo.GetDirectoryStatus ("subdir1/subdir2", false);
			});
		}

		[Test]
		public void TestFileStatus ()
		{
			RunStatusTests (delegate (Repository repo, List<string> outFilesToCheck) {
				outFilesToCheck.Add (filename);
				return repo.GetFileStatus (filename);
			});
			RunStatusTests (delegate (Repository repo, List<string> outFilesToCheck) {
				outFilesToCheck.Add (filenameSubdir1);
				return repo.GetFileStatus (filenameSubdir1);
			});
			RunStatusTests (delegate (Repository repo, List<string> outFilesToCheck) {
				outFilesToCheck.Add (filenameSubdir2);
				return repo.GetFileStatus (filenameSubdir2);
			});
		}
	}
}
