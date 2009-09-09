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
using NUnit.Framework;

namespace GitSharp.Tests.RevWalk
{
    [TestFixture]
    public class RevWalkPathFilter1Test : RevWalkTestCase
    {
        protected void filter(string path)
        {
            rw.setTreeFilter(AndTreeFilter.create(
                                 PathFilterGroup.createFromStrings(new[] {path}), TreeFilter.ANY_DIFF));
        }

        [Test]
        public void testEmpty_EmptyTree()
        {
            RevCommit a = commit();
            filter("a");
            markStart(a);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testEmpty_NoMatch()
        {
            RevCommit a = commit(tree(file("0", blob("0"))));
            filter("a");
            markStart(a);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testSimple1()
        {
            RevCommit a = commit(tree(file("0", blob("0"))));
            filter("0");
            markStart(a);
            assertCommit(a, rw.next());
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testEdits_MatchNone()
        {
            RevCommit a = commit(tree(file("0", blob("a"))));
            RevCommit b = commit(tree(file("0", blob("b"))), a);
            RevCommit c = commit(tree(file("0", blob("c"))), b);
            RevCommit d = commit(tree(file("0", blob("d"))), c);
            filter("a");
            markStart(d);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testEdits_MatchAll()
        {
            RevCommit a = commit(tree(file("0", blob("a"))));
            RevCommit b = commit(tree(file("0", blob("b"))), a);
            RevCommit c = commit(tree(file("0", blob("c"))), b);
            RevCommit d = commit(tree(file("0", blob("d"))), c);
            filter("0");
            markStart(d);
            assertCommit(d, rw.next());
            assertCommit(c, rw.next());
            assertCommit(b, rw.next());
            assertCommit(a, rw.next());
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testStringOfPearls_FilePath1()
        {
            RevCommit a = commit(tree(file("d/f", blob("a"))));
            RevCommit b = commit(tree(file("d/f", blob("a"))), a);
            RevCommit c = commit(tree(file("d/f", blob("b"))), b);
            filter("d/f");
            markStart(c);

            assertCommit(c, rw.next());
            Assert.AreEqual(1, c.ParentCount);
            assertCommit(a, c.GetParent(0)); // b was skipped

            assertCommit(a, rw.next());
            Assert.AreEqual(0, a.ParentCount);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testStringOfPearls_FilePath2()
        {
            RevCommit a = commit(tree(file("d/f", blob("a"))));
            RevCommit b = commit(tree(file("d/f", blob("a"))), a);
            RevCommit c = commit(tree(file("d/f", blob("b"))), b);
            RevCommit d = commit(tree(file("d/f", blob("b"))), c);
            filter("d/f");
            markStart(d);

            // d was skipped
            assertCommit(c, rw.next());
            Assert.AreEqual(1, c.ParentCount);
            assertCommit(a, c.GetParent(0)); // b was skipped

            assertCommit(a, rw.next());
            Assert.AreEqual(0, a.ParentCount);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testStringOfPearls_DirPath2()
        {
            RevCommit a = commit(tree(file("d/f", blob("a"))));
            RevCommit b = commit(tree(file("d/f", blob("a"))), a);
            RevCommit c = commit(tree(file("d/f", blob("b"))), b);
            RevCommit d = commit(tree(file("d/f", blob("b"))), c);
            filter("d");
            markStart(d);

            // d was skipped
            assertCommit(c, rw.next());
            Assert.AreEqual(1, c.ParentCount);
            assertCommit(a, c.GetParent(0)); // b was skipped

            assertCommit(a, rw.next());
            Assert.AreEqual(0, a.ParentCount);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testStringOfPearls_FilePath3()
        {
            RevCommit a = commit(tree(file("d/f", blob("a"))));
            RevCommit b = commit(tree(file("d/f", blob("a"))), a);
            RevCommit c = commit(tree(file("d/f", blob("b"))), b);
            RevCommit d = commit(tree(file("d/f", blob("b"))), c);
            RevCommit e = commit(tree(file("d/f", blob("b"))), d);
            RevCommit f = commit(tree(file("d/f", blob("b"))), e);
            RevCommit g = commit(tree(file("d/f", blob("b"))), f);
            RevCommit h = commit(tree(file("d/f", blob("b"))), g);
            RevCommit i = commit(tree(file("d/f", blob("c"))), h);
            filter("d/f");
            markStart(i);

            assertCommit(i, rw.next());
            Assert.AreEqual(1, i.ParentCount);
            assertCommit(c, i.GetParent(0)); // h..d was skipped

            assertCommit(c, rw.next());
            Assert.AreEqual(1, c.ParentCount);
            assertCommit(a, c.GetParent(0)); // b was skipped

            assertCommit(a, rw.next());
            Assert.AreEqual(0, a.ParentCount);
            Assert.IsNull(rw.next());
        }
    }
}
