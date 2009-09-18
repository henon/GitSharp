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
using Xunit;

namespace GitSharp.Tests.RevWalk
{
    public class ObjectWalkTest : RevWalkTestCase
    {
    	private ObjectWalk _objw;

        protected override GitSharp.RevWalk.RevWalk CreateRevWalk()
        {
            return _objw = new ObjectWalk(db);
        }

        [StrictFactAttribute]
        public void testNoCommits()
        {
            Assert.Null(_objw.next());
            Assert.Null(_objw.nextObject());
        }

        [StrictFactAttribute]
        public void testTwoCommitsEmptyTree()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            MarkStart(b);

            AssertCommit(b, _objw.next());
            AssertCommit(a, _objw.next());
            Assert.Null(_objw.next());

            Assert.Same(EmptyTree, _objw.nextObject());
            Assert.Null(_objw.nextObject());
        }

        [StrictFactAttribute]
        public void testOneCommitOneTreeTwoBlob()
        {
            RevBlob f0 = Blob("0");
            RevBlob f1 = Blob("1");
            RevTree t = Tree(File("0", f0), File("1", f1), File("2", f1));
            RevCommit a = Commit(t);
            MarkStart(a);

            AssertCommit(a, _objw.next());
            Assert.Null(_objw.next());

            Assert.Same(t, _objw.nextObject());
            Assert.Same(f0, _objw.nextObject());
            Assert.Same(f1, _objw.nextObject());
            Assert.Null(_objw.nextObject());
        }

        [StrictFactAttribute]
        public void testTwoCommitTwoTreeTwoBlob()
        {
            RevBlob f0 = Blob("0");
            RevBlob f1 = Blob("1");
            RevBlob f2 = Blob("0v2");
            RevTree ta = Tree(File("0", f0), File("1", f1), File("2", f1));
            RevTree tb = Tree(File("0", f2), File("1", f1), File("2", f1));
            RevCommit a = Commit(ta);
            RevCommit b = Commit(tb, a);
            MarkStart(b);

            AssertCommit(b, _objw.next());
            AssertCommit(a, _objw.next());
            Assert.Null(_objw.next());

            Assert.Same(tb, _objw.nextObject());
            Assert.Same(f2, _objw.nextObject());
            Assert.Same(f1, _objw.nextObject());

            Assert.Same(ta, _objw.nextObject());
            Assert.Same(f0, _objw.nextObject());

            Assert.Null(_objw.nextObject());
        }

        [StrictFactAttribute]
        public void testTwoCommitDeepTree1()
        {
            RevBlob f0 = Blob("0");
            RevBlob f1 = Blob("0v2");
            RevTree ta = Tree(File("a/b/0", f0));
            RevTree tb = Tree(File("a/b/1", f1));
            RevCommit a = Commit(ta);
            RevCommit b = Commit(tb, a);
            MarkStart(b);

            AssertCommit(b, _objw.next());
            AssertCommit(a, _objw.next());
            Assert.Null(_objw.next());

            Assert.Same(tb, _objw.nextObject());
            Assert.Same(Get(tb, "a"), _objw.nextObject());
            Assert.Same(Get(tb, "a/b"), _objw.nextObject());
            Assert.Same(f1, _objw.nextObject());

            Assert.Same(ta, _objw.nextObject());
            Assert.Same(Get(ta, "a"), _objw.nextObject());
            Assert.Same(Get(ta, "a/b"), _objw.nextObject());
            Assert.Same(f0, _objw.nextObject());

            Assert.Null(_objw.nextObject());
        }

        [StrictFactAttribute]
        public void testTwoCommitDeepTree2()
        {
            RevBlob f1 = Blob("1");
            RevTree ta = Tree(File("a/b/0", f1), File("a/c/q", f1));
            RevTree tb = Tree(File("a/b/1", f1), File("a/c/q", f1));
            RevCommit a = Commit(ta);
            RevCommit b = Commit(tb, a);
            MarkStart(b);

            AssertCommit(b, _objw.next());
            AssertCommit(a, _objw.next());
            Assert.Null(_objw.next());

            Assert.Same(tb, _objw.nextObject());
            Assert.Same(Get(tb, "a"), _objw.nextObject());
            Assert.Same(Get(tb, "a/b"), _objw.nextObject());
            Assert.Same(f1, _objw.nextObject());
            Assert.Same(Get(tb, "a/c"), _objw.nextObject());

            Assert.Same(ta, _objw.nextObject());
            Assert.Same(Get(ta, "a"), _objw.nextObject());
            Assert.Same(Get(ta, "a/b"), _objw.nextObject());

            Assert.Null(_objw.nextObject());
        }

        [StrictFactAttribute]
        public void testCull()
        {
            RevBlob f1 = Blob("1");
            RevBlob f2 = Blob("2");
            RevBlob f3 = Blob("3");
            RevBlob f4 = Blob("4");

            RevTree ta = Tree(File("a/1", f1), File("c/3", f3));
            RevCommit a = Commit(ta);

            RevTree tb = Tree(File("a/1", f2), File("c/3", f3));
            RevCommit b1 = Commit(tb, a);
            RevCommit b2 = Commit(tb, b1);

            RevTree tc = Tree(File("a/1", f4));
            RevCommit c1 = Commit(tc, a);
            RevCommit c2 = Commit(tc, c1);

            MarkStart(b2);
            MarkUninteresting(c2);

            AssertCommit(b2, _objw.next());
            AssertCommit(b1, _objw.next());
            Assert.Null(_objw.next());

            Assert.True(a.has(RevFlag.UNINTERESTING));
            Assert.True(ta.has(RevFlag.UNINTERESTING));
            Assert.True(f1.has(RevFlag.UNINTERESTING));
            Assert.True(f3.has(RevFlag.UNINTERESTING));

            Assert.Same(tb, _objw.nextObject());
            Assert.Same(Get(tb, "a"), _objw.nextObject());
            Assert.Same(f2, _objw.nextObject());
            Assert.Null(_objw.nextObject());
        }
    }
}
