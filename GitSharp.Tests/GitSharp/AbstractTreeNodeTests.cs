/*
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
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
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp
{
	[TestFixture]
	public class AbstractTreeNodeTests : ApiTestCase
	{

		[Test]
		public void NameAndPath()
		{
			using (var repo = GetTrashRepository())
			{
				Assert.AreEqual("master.txt", repo.Get<Leaf>("master.txt").Name);
				Assert.AreEqual("master.txt", repo.Get<Leaf>("master.txt").Path);
				Assert.AreEqual("", repo.CurrentBranch.CurrentCommit.Tree.Name);
				Assert.AreEqual("", repo.CurrentBranch.CurrentCommit.Tree.Path);
				Assert.AreEqual("a", repo.Get<Tree>("a").Name);
				Assert.AreEqual("a", repo.Get<Tree>("a").Path);
				Assert.AreEqual("a1.txt", repo.Get<Leaf>("a/a1.txt").Name);
				Assert.AreEqual("a/a1.txt", repo.Get<Leaf>("a/a1.txt").Path);
			}
		}

		[Test]
		public void GetHistory()
		{
			using (var repo = GetTrashRepository())
			{
				// history of master.txt
				var master_txt = repo.Get<Leaf>("master.txt");
				var commits = master_txt.GetHistory().ToArray();
				var history = commits.Select(c => c.Hash).ToArray();
				Assert.AreEqual(new[] { "58be4659bb571194ed4562d04b359d26216f526e", "6c8b137b1c652731597c89668f417b8695f28dd7" }, history);

				// history of a/a1.txt
				commits = repo.Get<Leaf>("a/a1.txt").GetHistory().ToArray();
				history = commits.Select(c => c.Hash).ToArray();
				Assert.AreEqual(new[] { "d86a2aada2f5e7ccf6f11880bfb9ab404e8a8864", "ac7e7e44c1885efb472ad54a78327d66bfc4ecef" }, history);

				// history of a/a2.txt
				commits = repo.Get<Leaf>("a/a2.txt").GetHistory().ToArray();
				history = commits.Select(c => c.Hash).ToArray();
				Assert.AreEqual(new[] { "6db9c2ebf75590eef973081736730a9ea169a0c4", "2c349335b7f797072cf729c4f3bb0914ecb6dec9" }, history);

			}
		}

		[Ignore("this test fails because cgit (for some reason) selects a different commit out of two possible candidates")]
		[Test]
		public void GetHistory1()
		{
			using (var repo = GetTrashRepository())
			{
				// history of a/a1
				var commits = repo.Get<Leaf>("a/a1").GetHistory().ToArray();
				var history = commits.Select(c => c.Hash).ToArray();
				Assert.AreEqual(new[] { "f73b95671f326616d66b2afb3bdfcdbbce110b44" }, history);
			}
		}

		[Ignore("this test fails because cgit filters out one duplicate change which gitsharp doesn't yet")] // d0114ab8ac326bab30e3a657a0397578c5a1af88 and f73b95671f326616d66b2afb3bdfcdbbce110b44 are the same change merged together
		[Test]
		public void GetHistory2()
		{
			using (var repo = GetTrashRepository())
			{
				// history of a/
				var commits = repo.Get<Tree>("a/").GetHistory().ToArray();
				var history = commits.Select(c => c.Hash).ToArray();
				Assert.AreEqual(new[] { "f73b95671f326616d66b2afb3bdfcdbbce110b44",
					"6db9c2ebf75590eef973081736730a9ea169a0c4",
					"d86a2aada2f5e7ccf6f11880bfb9ab404e8a8864",
					"2c349335b7f797072cf729c4f3bb0914ecb6dec9",
					"ac7e7e44c1885efb472ad54a78327d66bfc4ecef", 
				}, history);

			}
		}


	}
}
