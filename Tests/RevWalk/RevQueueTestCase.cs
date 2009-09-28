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
using NUnit.Framework;

namespace GitSharp.Tests.RevWalk
{
    public abstract class RevQueueTestCase<T> : RevWalkTestCase
        where T : AbstractRevQueue
    {
        protected T q;

        [SetUp]
        public override void setUp()
        {
            base.setUp();
            q = create();
        }

        protected abstract T create();

        [Test]
        public virtual void testEmpty()
        {
            Assert.IsNull(q.next());
            Assert.IsTrue(q.everbodyHasFlag(GitSharp.Core.RevWalk.RevWalk.UNINTERESTING));
            Assert.IsFalse(q.anybodyHasFlag(GitSharp.Core.RevWalk.RevWalk.UNINTERESTING));
        }

        [Test]
        public void testClear()
        {
            RevCommit a = Parse(Commit());
            RevCommit b = Parse(Commit(a));

            q.add(a);
            q.add(b);
            q.clear();
            Assert.IsNull(q.next());
        }

        [Test]
        public void testHasFlags()
        {
            RevCommit a = Parse(Commit());
            RevCommit b = Parse(Commit(a));

            q.add(a);
            q.add(b);

            Assert.IsFalse(q.everbodyHasFlag(GitSharp.Core.RevWalk.RevWalk.UNINTERESTING));
            Assert.IsFalse(q.anybodyHasFlag(GitSharp.Core.RevWalk.RevWalk.UNINTERESTING));

            a.Flags |= GitSharp.Core.RevWalk.RevWalk.UNINTERESTING;
            Assert.IsFalse(q.everbodyHasFlag(GitSharp.Core.RevWalk.RevWalk.UNINTERESTING));
            Assert.IsTrue(q.anybodyHasFlag(GitSharp.Core.RevWalk.RevWalk.UNINTERESTING));

            b.Flags |= GitSharp.Core.RevWalk.RevWalk.UNINTERESTING;
            Assert.IsTrue(q.everbodyHasFlag(GitSharp.Core.RevWalk.RevWalk.UNINTERESTING));
            Assert.IsTrue(q.anybodyHasFlag(GitSharp.Core.RevWalk.RevWalk.UNINTERESTING));
        }
    }
}
