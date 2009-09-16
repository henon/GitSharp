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
using GitSharp.RevWalk.Filter;
using Xunit;

namespace GitSharp.Tests.RevWalk
{
    public class RevWalkFilterTest : RevWalkTestCase
    {
        private static readonly MyAll MY_ALL = new MyAll();

        [Fact]
        public void testFilter_ALL()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            Rw.setRevFilter(RevFilter.ALL);
            MarkStart(c);
            AssertCommit(c, Rw.next());
            AssertCommit(b, Rw.next());
            AssertCommit(a, Rw.next());
            Assert.Null(Rw.next());
        }

        [Fact]
        public void testFilter_Negate_ALL()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            Rw.setRevFilter(RevFilter.ALL.negate());
            MarkStart(c);
            Assert.Null(Rw.next());
        }

        [Fact]
        public void testFilter_NOT_ALL()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            Rw.setRevFilter(NotRevFilter.create(RevFilter.ALL));
            MarkStart(c);
            Assert.Null(Rw.next());
        }

        [Fact]
        public void testFilter_NONE()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            Rw.setRevFilter(RevFilter.NONE);
            MarkStart(c);
            Assert.Null(Rw.next());
        }

        [Fact]
        public void testFilter_NOT_NONE()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            Rw.setRevFilter(NotRevFilter.create(RevFilter.NONE));
            MarkStart(c);
            AssertCommit(c, Rw.next());
            AssertCommit(b, Rw.next());
            AssertCommit(a, Rw.next());
            Assert.Null(Rw.next());
        }

        [Fact]
        public void testFilter_ALL_And_NONE()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            Rw.setRevFilter(AndRevFilter.create(RevFilter.ALL, RevFilter.NONE));
            MarkStart(c);
            Assert.Null(Rw.next());
        }

        [Fact]
        public void testFilter_NONE_And_ALL()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            Rw.setRevFilter(AndRevFilter.create(RevFilter.NONE, RevFilter.ALL));
            MarkStart(c);
            Assert.Null(Rw.next());
        }

        [Fact]
        public void testFilter_ALL_Or_NONE()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            Rw.setRevFilter(OrRevFilter.create(RevFilter.ALL, RevFilter.NONE));
            MarkStart(c);
            AssertCommit(c, Rw.next());
            AssertCommit(b, Rw.next());
            AssertCommit(a, Rw.next());
            Assert.Null(Rw.next());
        }

        [Fact]
        public void testFilter_NONE_Or_ALL()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            Rw.setRevFilter(OrRevFilter.create(RevFilter.NONE, RevFilter.ALL));
            MarkStart(c);
            AssertCommit(c, Rw.next());
            AssertCommit(b, Rw.next());
            AssertCommit(a, Rw.next());
            Assert.Null(Rw.next());
        }

        [Fact]
        public void testFilter_MY_ALL_And_NONE()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            Rw.setRevFilter(AndRevFilter.create(MY_ALL, RevFilter.NONE));
            MarkStart(c);
            Assert.Null(Rw.next());
        }

        [Fact]
        public void testFilter_NONE_And_MY_ALL()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            Rw.setRevFilter(AndRevFilter.create(RevFilter.NONE, MY_ALL));
            MarkStart(c);
            Assert.Null(Rw.next());
        }

        [Fact]
        public void testFilter_MY_ALL_Or_NONE()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            Rw.setRevFilter(OrRevFilter.create(MY_ALL, RevFilter.NONE));
            MarkStart(c);
            AssertCommit(c, Rw.next());
            AssertCommit(b, Rw.next());
            AssertCommit(a, Rw.next());
            Assert.Null(Rw.next());
        }

        [Fact]
        public void testFilter_NONE_Or_MY_ALL()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            Rw.setRevFilter(OrRevFilter.create(RevFilter.NONE, MY_ALL));
            MarkStart(c);
            AssertCommit(c, Rw.next());
            AssertCommit(b, Rw.next());
            AssertCommit(a, Rw.next());
            Assert.Null(Rw.next());
        }

        [Fact]
        public void testFilter_NO_MERGES()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c1 = Commit(b);
            RevCommit c2 = Commit(b);
            RevCommit d = Commit(c1, c2);
            RevCommit e = Commit(d);

            Rw.setRevFilter(RevFilter.NO_MERGES);
            MarkStart(e);
            AssertCommit(e, Rw.next());
            AssertCommit(c2, Rw.next());
            AssertCommit(c1, Rw.next());
            AssertCommit(b, Rw.next());
            AssertCommit(a, Rw.next());
            Assert.Null(Rw.next());
        }

        private class MyAll : RevFilter
        {
            public override RevFilter Clone()
            {
                return this;
            }

            public override bool include(GitSharp.RevWalk.RevWalk walker, RevCommit cmit)
            {
                return true;
            }
        }
    }
}
