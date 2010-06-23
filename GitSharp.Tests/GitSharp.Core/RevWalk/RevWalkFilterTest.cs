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

using System;
using GitSharp.Core.RevWalk.Filter;
using GitSharp.Core.RevWalk;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Core.Tests.RevWalk
{
    [TestFixture]
    public class RevWalkFilterTest : RevWalkTestCase
    {
        private static readonly MyAll MY_ALL = new MyAll();

        [Test]
        public void testFilter_ALL()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            rw.setRevFilter(RevFilter.ALL);
            MarkStart(c);
            AssertCommit(c, rw.next());
            AssertCommit(b, rw.next());
            AssertCommit(a, rw.next());
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testFilter_Negate_ALL()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            rw.setRevFilter(RevFilter.ALL.negate());
            MarkStart(c);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testFilter_NOT_ALL()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            rw.setRevFilter(NotRevFilter.create(RevFilter.ALL));
            MarkStart(c);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testFilter_NONE()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            rw.setRevFilter(RevFilter.NONE);
            MarkStart(c);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testFilter_NOT_NONE()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            rw.setRevFilter(NotRevFilter.create(RevFilter.NONE));
            MarkStart(c);
            AssertCommit(c, rw.next());
            AssertCommit(b, rw.next());
            AssertCommit(a, rw.next());
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testFilter_ALL_And_NONE()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            rw.setRevFilter(AndRevFilter.create(RevFilter.ALL, RevFilter.NONE));
            MarkStart(c);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testFilter_NONE_And_ALL()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            rw.setRevFilter(AndRevFilter.create(RevFilter.NONE, RevFilter.ALL));
            MarkStart(c);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testFilter_ALL_Or_NONE()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            rw.setRevFilter(OrRevFilter.create(RevFilter.ALL, RevFilter.NONE));
            MarkStart(c);
            AssertCommit(c, rw.next());
            AssertCommit(b, rw.next());
            AssertCommit(a, rw.next());
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testFilter_NONE_Or_ALL()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            rw.setRevFilter(OrRevFilter.create(RevFilter.NONE, RevFilter.ALL));
            MarkStart(c);
            AssertCommit(c, rw.next());
            AssertCommit(b, rw.next());
            AssertCommit(a, rw.next());
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testFilter_MY_ALL_And_NONE()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            rw.setRevFilter(AndRevFilter.create(MY_ALL, RevFilter.NONE));
            MarkStart(c);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testFilter_NONE_And_MY_ALL()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            rw.setRevFilter(AndRevFilter.create(RevFilter.NONE, MY_ALL));
            MarkStart(c);
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testFilter_MY_ALL_Or_NONE()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            rw.setRevFilter(OrRevFilter.create(MY_ALL, RevFilter.NONE));
            MarkStart(c);
            AssertCommit(c, rw.next());
            AssertCommit(b, rw.next());
            AssertCommit(a, rw.next());
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testFilter_NONE_Or_MY_ALL()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c = Commit(b);

            rw.setRevFilter(OrRevFilter.create(RevFilter.NONE, MY_ALL));
            MarkStart(c);
            AssertCommit(c, rw.next());
            AssertCommit(b, rw.next());
            AssertCommit(a, rw.next());
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testFilter_NO_MERGES()
        {
            RevCommit a = Commit();
            RevCommit b = Commit(a);
            RevCommit c1 = Commit(b);
            RevCommit c2 = Commit(b);
            RevCommit d = Commit(c1, c2);
            RevCommit e = Commit(d);

            rw.setRevFilter(RevFilter.NO_MERGES);
            MarkStart(e);
            AssertCommit(e, rw.next());
            AssertCommit(c2, rw.next());
            AssertCommit(c1, rw.next());
            AssertCommit(b, rw.next());
            AssertCommit(a, rw.next());
            Assert.IsNull(rw.next());
        }

        [Test]
        public void testCommitTimeRevFilter()
        {
            RevCommit a = Commit();
            Tick(100);

            RevCommit b = Commit(a);
            Tick(100);

            DateTime since = getClock();
            RevCommit c1 = Commit(b);
            Tick(100);

            RevCommit c2 = Commit(b);
            Tick(100);

            DateTime until = getClock();
            RevCommit d = Commit(c1, c2);
            Tick(100);

            RevCommit e = Commit(d);

            {
                RevFilter after = CommitTimeRevFilter.After(since);
                Assert.IsNotNull(after);
                rw.setRevFilter(after);
                MarkStart(e);
                AssertCommit(e, rw.next());
                AssertCommit(d, rw.next());
                AssertCommit(c2, rw.next());
                AssertCommit(c1, rw.next());
                Assert.IsNull(rw.next());
            }

            {
                RevFilter before = CommitTimeRevFilter.Before(until);
                Assert.IsNotNull(before);
                rw.reset();
                rw.setRevFilter(before);
                MarkStart(e);
                AssertCommit(c2, rw.next());
                AssertCommit(c1, rw.next());
                AssertCommit(b, rw.next());
                AssertCommit(a, rw.next());
                Assert.IsNull(rw.next());
            }

            {
                RevFilter between = CommitTimeRevFilter.Between(since, until);
                Assert.IsNotNull(between);
                rw.reset();
                rw.setRevFilter(between);
                MarkStart(e);
                AssertCommit(c2, rw.next());
                AssertCommit(c1, rw.next());
                Assert.IsNull(rw.next());
            }
        }

        private class MyAll : RevFilter
        {
            public override RevFilter Clone()
            {
                return this;
            }

            public override bool include(GitSharp.Core.RevWalk.RevWalk walker, RevCommit cmit)
            {
                return true;
            }
        }
    }
}
