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

using GitSharp.Tests.Util;
using GitSharp.RevWalk;
namespace GitSharp.Tests.RevWalk
{
    using NUnit.Framework;
    [TestFixture]
    public class ObjectWalkTest : RevWalkTestCase
    {
        protected ObjectWalk objw;


        protected override GitSharp.RevWalk.RevWalk createRevWalk()
        {
		return objw = new ObjectWalk(db);
	}

        [Test]
	public void testNoCommits() {
		Assert.IsNull(objw.next());
		Assert.IsNull(objw.nextObject());
	}

        [Test]
    public void testTwoCommitsEmptyTree()
        {
		RevCommit a = commit();
		RevCommit b = commit(a);
		markStart(b);

		assertCommit(b, objw.next());
		assertCommit(a, objw.next());
		Assert.IsNull(objw.next());

		Assert.AreSame(emptyTree, objw.nextObject());
		Assert.IsNull(objw.nextObject());
	}

        [Test]
    public void testOneCommitOneTreeTwoBlob()
        {
		RevBlob f0 = blob("0");
		RevBlob f1 = blob("1");
		RevTree t = tree(file("0", f0), file("1", f1), file("2", f1));
		RevCommit a = commit(t);
		markStart(a);

		assertCommit(a, objw.next());
		Assert.IsNull(objw.next());

		Assert.AreSame(t, objw.nextObject());
		Assert.AreSame(f0, objw.nextObject());
		Assert.AreSame(f1, objw.nextObject());
		Assert.IsNull(objw.nextObject());
	}

	[Test]
    public void testTwoCommitTwoTreeTwoBlob() {
		RevBlob f0 = blob("0");
		RevBlob f1 = blob("1");
		RevBlob f2 = blob("0v2");
		RevTree ta = tree(file("0", f0), file("1", f1), file("2", f1));
		RevTree tb = tree(file("0", f2), file("1", f1), file("2", f1));
		RevCommit a = commit(ta);
		RevCommit b = commit(tb, a);
		markStart(b);

		assertCommit(b, objw.next());
		assertCommit(a, objw.next());
		Assert.IsNull(objw.next());

		Assert.AreSame(tb, objw.nextObject());
		Assert.AreSame(f2, objw.nextObject());
		Assert.AreSame(f1, objw.nextObject());

		Assert.AreSame(ta, objw.nextObject());
		Assert.AreSame(f0, objw.nextObject());

		Assert.IsNull(objw.nextObject());
	}

    [Test]
	public void testTwoCommitDeepTree1() {
		RevBlob f0 = blob("0");
		RevBlob f1 = blob("0v2");
		RevTree ta = tree(file("a/b/0", f0));
		RevTree tb = tree(file("a/b/1", f1));
		RevCommit a = commit(ta);
		RevCommit b = commit(tb, a);
		markStart(b);

		assertCommit(b, objw.next());
		assertCommit(a, objw.next());
		Assert.IsNull(objw.next());

		Assert.AreSame(tb, objw.nextObject());
		Assert.AreSame(get(tb, "a"), objw.nextObject());
		Assert.AreSame(get(tb, "a/b"), objw.nextObject());
		Assert.AreSame(f1, objw.nextObject());

		Assert.AreSame(ta, objw.nextObject());
		Assert.AreSame(get(ta, "a"), objw.nextObject());
		Assert.AreSame(get(ta, "a/b"), objw.nextObject());
		Assert.AreSame(f0, objw.nextObject());

		Assert.IsNull(objw.nextObject());
	}

    [Test]
	public void testTwoCommitDeepTree2() {
		RevBlob f1 = blob("1");
		RevTree ta = tree(file("a/b/0", f1), file("a/c/q", f1));
		RevTree tb = tree(file("a/b/1", f1), file("a/c/q", f1));
		RevCommit a = commit(ta);
		RevCommit b = commit(tb, a);
		markStart(b);

		assertCommit(b, objw.next());
		assertCommit(a, objw.next());
		Assert.IsNull(objw.next());

		Assert.AreSame(tb, objw.nextObject());
		Assert.AreSame(get(tb, "a"), objw.nextObject());
		Assert.AreSame(get(tb, "a/b"), objw.nextObject());
		Assert.AreSame(f1, objw.nextObject());
		Assert.AreSame(get(tb, "a/c"), objw.nextObject());

		Assert.AreSame(ta, objw.nextObject());
		Assert.AreSame(get(ta, "a"), objw.nextObject());
		Assert.AreSame(get(ta, "a/b"), objw.nextObject());

		Assert.IsNull(objw.nextObject());
	}

    [Test]
	public void testCull() {
		RevBlob f1 = blob("1");
		RevBlob f2 = blob("2");
		RevBlob f3 = blob("3");
		RevBlob f4 = blob("4");

		RevTree ta = tree(file("a/1", f1), file("c/3", f3));
		RevCommit a = commit(ta);

		RevTree tb = tree(file("a/1", f2), file("c/3", f3));
		RevCommit b1 = commit(tb, a);
		RevCommit b2 = commit(tb, b1);

		RevTree tc = tree(file("a/1", f4));
		RevCommit c1 = commit(tc, a);
		RevCommit c2 = commit(tc, c1);

		markStart(b2);
		markUninteresting(c2);

		assertCommit(b2, objw.next());
		assertCommit(b1, objw.next());
		Assert.IsNull(objw.next());

		Assert.IsTrue(a.has(RevFlag.UNINTERESTING));
		Assert.IsTrue(ta.has(RevFlag.UNINTERESTING));
		Assert.IsTrue(f1.has(RevFlag.UNINTERESTING));
		Assert.IsTrue(f3.has(RevFlag.UNINTERESTING));

		Assert.AreSame(tb, objw.nextObject());
		Assert.AreSame(get(tb, "a"), objw.nextObject());
		Assert.AreSame(f2, objw.nextObject());
		Assert.IsNull(objw.nextObject());
	}
    }
}
