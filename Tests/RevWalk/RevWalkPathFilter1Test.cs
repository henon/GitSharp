/*
 * Copyright (C) 2009, Google Inc.
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

using GitSharp.RevWalk;
using GitSharp.TreeWalk.Filter;
using Xunit;

namespace GitSharp.Tests.RevWalk
{
	public class RevWalkPathFilter1Test : RevWalkTestCase
	{
		private void Filter(string path)
		{
			Rw.setTreeFilter(AndTreeFilter
				.create(PathFilterGroup.createFromStrings(new[] { path }), TreeFilter.ANY_DIFF));
		}

		[Fact]
		public void testEmpty_EmptyTree()
		{
			RevCommit a = Commit();
			Filter("a");
			MarkStart(a);
			Assert.Null(Rw.next());
		}

		[Fact]
		public void testEmpty_NoMatch()
		{
			RevCommit a = Commit(Tree(File("0", Blob("0"))));
			Filter("a");
			MarkStart(a);
			Assert.Null(Rw.next());
		}

		[Fact]
		public void testSimple1()
		{
			RevCommit a = Commit(Tree(File("0", Blob("0"))));
			Filter("0");
			MarkStart(a);
			AssertCommit(a, Rw.next());
			Assert.Null(Rw.next());
		}

		[Fact]
		public void testEdits_MatchNone()
		{
			RevCommit a = Commit(Tree(File("0", Blob("a"))));
			RevCommit b = Commit(Tree(File("0", Blob("b"))), a);
			RevCommit c = Commit(Tree(File("0", Blob("c"))), b);
			RevCommit d = Commit(Tree(File("0", Blob("d"))), c);
			Filter("a");
			MarkStart(d);
			Assert.Null(Rw.next());
		}

		[Fact]
		public void testEdits_MatchAll()
		{
			RevCommit a = Commit(Tree(File("0", Blob("a"))));
			RevCommit b = Commit(Tree(File("0", Blob("b"))), a);
			RevCommit c = Commit(Tree(File("0", Blob("c"))), b);
			RevCommit d = Commit(Tree(File("0", Blob("d"))), c);
			Filter("0");
			MarkStart(d);
			AssertCommit(d, Rw.next());
			AssertCommit(c, Rw.next());
			AssertCommit(b, Rw.next());
			AssertCommit(a, Rw.next());
			Assert.Null(Rw.next());
		}

		[Fact]
		public void testStringOfPearls_FilePath1()
		{
			RevCommit a = Commit(Tree(File("d/f", Blob("a"))));
			RevCommit b = Commit(Tree(File("d/f", Blob("a"))), a);
			RevCommit c = Commit(Tree(File("d/f", Blob("b"))), b);
			Filter("d/f");
			MarkStart(c);

			AssertCommit(c, Rw.next());
			Assert.Equal(1, c.ParentCount);
			AssertCommit(a, c.GetParent(0)); // b was skipped

			AssertCommit(a, Rw.next());
			Assert.Equal(0, a.ParentCount);
			Assert.Null(Rw.next());
		}

		[Fact]
		public void testStringOfPearls_FilePath2()
		{
			RevCommit a = Commit(Tree(File("d/f", Blob("a"))));
			RevCommit b = Commit(Tree(File("d/f", Blob("a"))), a);
			RevCommit c = Commit(Tree(File("d/f", Blob("b"))), b);
			RevCommit d = Commit(Tree(File("d/f", Blob("b"))), c);
			Filter("d/f");
			MarkStart(d);

			// d was skipped
			AssertCommit(c, Rw.next());
			Assert.Equal(1, c.ParentCount);
			AssertCommit(a, c.GetParent(0)); // b was skipped

			AssertCommit(a, Rw.next());
			Assert.Equal(0, a.ParentCount);
			Assert.Null(Rw.next());
		}

		[Fact]
		public void testStringOfPearls_DirPath2()
		{
			RevCommit a = Commit(Tree(File("d/f", Blob("a"))));
			RevCommit b = Commit(Tree(File("d/f", Blob("a"))), a);
			RevCommit c = Commit(Tree(File("d/f", Blob("b"))), b);
			RevCommit d = Commit(Tree(File("d/f", Blob("b"))), c);
			Filter("d");
			MarkStart(d);

			// d was skipped
			AssertCommit(c, Rw.next());
			Assert.Equal(1, c.ParentCount);
			AssertCommit(a, c.GetParent(0)); // b was skipped

			AssertCommit(a, Rw.next());
			Assert.Equal(0, a.ParentCount);
			Assert.Null(Rw.next());
		}

		[Fact]
		public void testStringOfPearls_FilePath3()
		{
			RevCommit a = Commit(Tree(File("d/f", Blob("a"))));
			RevCommit b = Commit(Tree(File("d/f", Blob("a"))), a);
			RevCommit c = Commit(Tree(File("d/f", Blob("b"))), b);
			RevCommit d = Commit(Tree(File("d/f", Blob("b"))), c);
			RevCommit e = Commit(Tree(File("d/f", Blob("b"))), d);
			RevCommit f = Commit(Tree(File("d/f", Blob("b"))), e);
			RevCommit g = Commit(Tree(File("d/f", Blob("b"))), f);
			RevCommit h = Commit(Tree(File("d/f", Blob("b"))), g);
			RevCommit i = Commit(Tree(File("d/f", Blob("c"))), h);
			Filter("d/f");
			MarkStart(i);

			AssertCommit(i, Rw.next());
			Assert.Equal(1, i.ParentCount);
			AssertCommit(c, i.GetParent(0)); // h..d was skipped

			AssertCommit(c, Rw.next());
			Assert.Equal(1, c.ParentCount);
			AssertCommit(a, c.GetParent(0)); // b was skipped

			AssertCommit(a, Rw.next());
			Assert.Equal(0, a.ParentCount);
			Assert.Null(Rw.next());
		}
	}
}
