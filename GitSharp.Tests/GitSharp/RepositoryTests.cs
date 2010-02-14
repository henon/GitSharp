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
using GitSharp.Tests.GitSharp;
using NUnit.Framework;
using System.IO;

namespace GitSharp.API.Tests
{
	[TestFixture]
	public class RepositoryTests : ApiTestCase
	{

		[Test]
		public void ImplicitConversionToCoreRepo()
		{
			using (var repo = this.GetTrashRepository())
			{
				Assert.IsTrue(repo is Repository);
				GitSharp.Core.Repository core_repo = repo;
				Assert.IsTrue(core_repo is GitSharp.Core.Repository);
			}
		}

		[Test]
		public void FindRepository()
		{
			var was_here = Directory.GetCurrentDirectory();
			try
			{
				using (var repo = this.GetTrashRepository())
				{
					Assert.AreEqual(repo.Directory, Repository.FindRepository(repo.WorkingDirectory));
					Assert.AreEqual(repo.Directory, Repository.FindRepository(repo.Directory));
					var root = repo.WorkingDirectory;
					var hmm = Path.Combine(root, "hmm");
					Directory.CreateDirectory(hmm);
					Assert.AreEqual(repo.Directory, Repository.FindRepository(hmm));
					Directory.SetCurrentDirectory(hmm);
					Assert.AreEqual(repo.Directory, Repository.FindRepository(null));
				}
			}
			finally
			{
				Directory.SetCurrentDirectory(was_here);
			}
		}

		[Test]
		public void FindRepositoryReturnsNullInNonGitTree()
		{
			using (var repo = this.GetTrashRepository())
			{
				var path = Path.GetRandomFileName();
				var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), path));

				Assert.AreEqual(null, Repository.FindRepository(directory.FullName));

				directory.Delete();
			}
		}

		[Test]
		public void AccessGitObjects()
		{
			// standard access of git objects, supported by gitsharp.core
			using (var repo = this.GetTrashRepository())
			{
				Assert.IsTrue(repo.Get<Commit>("master").IsCommit);
				Assert.NotNull(repo.Get<Branch>("master"));
				Assert.NotNull(repo.Get<Branch>("master").Target);
				Assert.IsTrue(repo.Get<Commit>("HEAD^^").IsCommit);
				Assert.IsTrue(repo.Get<Tag>("A").IsTag);
				Assert.IsTrue(repo.Get<Branch>("a").Target.IsCommit);
				Assert.IsTrue(repo.Get<Commit>("a").IsCommit);
				Assert.IsTrue(repo.Get<Commit>("prefix/a").IsCommit);
				Assert.IsTrue(repo.Get<Commit>("68cb1f232964f3cd698afc1dafe583937203c587").IsCommit);
				Assert.NotNull(repo.Get<Blob>("a")); // <--- returns a blob containing the raw representation of tree "a" on master
				Assert.IsTrue(repo.Get<Tree>("a").IsTree); // <--- there is a directory "a" on master
				Assert.IsTrue(repo.Get<Tree>("a/").IsTree);
				Assert.NotNull(repo.Get<Blob>("a/a1"));
				Assert.NotNull(repo.Get<Leaf>("a/a1"));
			}
		}

		[Ignore]
		[Test]
		public void AccessGitObjectsMagic()
		{
			// not currently supported by gitsharp.core. requires some magic to resolve these cases
			using (var repo = this.GetTrashRepository())
			{
				Assert.IsTrue(repo.Get<Commit>("49322bb1").IsCommit); // abbrev. hashes are not yet supported!
				Assert.IsTrue(repo.Get<Commit>("68cb1f2").IsCommit);
				Assert.IsTrue(repo.Get<Tree>("HEAD^^").IsTree); // some magic is required for this
				Assert.IsNotNull(repo.Get<Blob>("68cb1f232964f3cd698afc1dafe583937203c587")); // <--- returns the commit as blob (i.e. for inspection of the raw contents)
				Assert.IsTrue(repo.Get<Tree>("68cb1f232964f3cd698afc1dafe583937203c587").IsTree); // <--- returns the commit as blob (i.e. for inspection of the raw contents)
			}
		}
	}
}