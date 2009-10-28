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

using GitSharp.Core.RevWalk;
using GitSharp.Core.TreeWalk.Filter;
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
            RevCommit a = Commit();
            filter("a");
            MarkStart(a);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testEmpty_NoMatch()
        {
            RevCommit a = Commit(tree(File("0", blob("0"))));
            filter("a");
            MarkStart(a);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testSimple1()
        {
            RevCommit a = Commit(tree(File("0", blob("0"))));
            filter("0");
            MarkStart(a);
            AssertCommit(a, rw.next());
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testEdits_MatchNone()
        {
            RevCommit a = Commit(tree(File("0", blob("a"))));
            RevCommit b = Commit(tree(File("0", blob("b"))), a);
            RevCommit c = Commit(tree(File("0", blob("c"))), b);
            RevCommit d = Commit(tree(File("0", blob("d"))), c);
            filter("a");
            MarkStart(d);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testEdits_MatchAll()
        {
            RevCommit a = Commit(tree(File("0", blob("a"))));
            RevCommit b = Commit(tree(File("0", blob("b"))), a);
            RevCommit c = Commit(tree(File("0", blob("c"))), b);
            RevCommit d = Commit(tree(File("0", blob("d"))), c);
            filter("0");
            MarkStart(d);
            AssertCommit(d, rw.next());
            AssertCommit(c, rw.next());
            AssertCommit(b, rw.next());
            AssertCommit(a, rw.next());
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testStringOfPearls_FilePath1()
        {
            RevCommit a = Commit(tree(File("d/f", blob("a"))));
            RevCommit b = Commit(tree(File("d/f", blob("a"))), a);
            RevCommit c = Commit(tree(File("d/f", blob("b"))), b);
            filter("d/f");
            MarkStart(c);

            AssertCommit(c, rw.next());
            Assert.AreEqual(1, c.ParentCount);
            AssertCommit(a, c.GetParent(0)); // b was skipped

            AssertCommit(a, rw.next());
            Assert.AreEqual(0, a.ParentCount);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testStringOfPearls_FilePath2()
        {
            RevCommit a = Commit(tree(File("d/f", blob("a"))));
            RevCommit b = Commit(tree(File("d/f", blob("a"))), a);
            RevCommit c = Commit(tree(File("d/f", blob("b"))), b);
            RevCommit d = Commit(tree(File("d/f", blob("b"))), c);
            filter("d/f");
            MarkStart(d);

            // d was skipped
            AssertCommit(c, rw.next());
            Assert.AreEqual(1, c.ParentCount);
            AssertCommit(a, c.GetParent(0)); // b was skipped

            AssertCommit(a, rw.next());
            Assert.AreEqual(0, a.ParentCount);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testStringOfPearls_DirPath2()
        {
            RevCommit a = Commit(tree(File("d/f", blob("a"))));
            RevCommit b = Commit(tree(File("d/f", blob("a"))), a);
            RevCommit c = Commit(tree(File("d/f", blob("b"))), b);
            RevCommit d = Commit(tree(File("d/f", blob("b"))), c);
            filter("d");
            MarkStart(d);

            // d was skipped
            AssertCommit(c, rw.next());
            Assert.AreEqual(1, c.ParentCount);
            AssertCommit(a, c.GetParent(0)); // b was skipped

            AssertCommit(a, rw.next());
            Assert.AreEqual(0, a.ParentCount);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testStringOfPearls_FilePath3()
        {
            RevCommit a = Commit(tree(File("d/f", blob("a"))));
            RevCommit b = Commit(tree(File("d/f", blob("a"))), a);
            RevCommit c = Commit(tree(File("d/f", blob("b"))), b);
            RevCommit d = Commit(tree(File("d/f", blob("b"))), c);
            RevCommit e = Commit(tree(File("d/f", blob("b"))), d);
            RevCommit f = Commit(tree(File("d/f", blob("b"))), e);
            RevCommit g = Commit(tree(File("d/f", blob("b"))), f);
            RevCommit h = Commit(tree(File("d/f", blob("b"))), g);
            RevCommit i = Commit(tree(File("d/f", blob("c"))), h);
            filter("d/f");
            MarkStart(i);

            AssertCommit(i, rw.next());
            Assert.AreEqual(1, i.ParentCount);
            AssertCommit(c, i.GetParent(0)); // h..d was skipped

            AssertCommit(c, rw.next());
            Assert.AreEqual(1, c.ParentCount);
            AssertCommit(a, c.GetParent(0)); // b was skipped

            AssertCommit(a, rw.next());
            Assert.AreEqual(0, a.ParentCount);
            Assert.IsNull(rw.next());
        }
    }
}
