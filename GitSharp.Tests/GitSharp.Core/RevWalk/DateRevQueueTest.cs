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

using GitSharp.Core.Tests.Util;
using GitSharp.Core.RevWalk;
using NUnit.Framework;

namespace GitSharp.Core.Tests.RevWalk
{
    [TestFixture]
    public class DateRevQueueTest : RevQueueTestCase<DateRevQueue>
    {
        protected override DateRevQueue create()
        {
            return new DateRevQueue();
        }

        [Test]
        public override void testEmpty()
        {
            base.testEmpty();
            Assert.IsNull(q.peek());
            Assert.AreEqual(Generator.GeneratorOutputType.SortCommitTimeDesc, q.OutputType);
        }

        [Test]
        public void testCloneEmpty()
        {
            q = new DateRevQueue(AbstractRevQueue.EmptyQueue);
            Assert.IsNull(q.next());
        }

        [Test]
        public void testInsertOutOfOrder()
        {
            RevCommit a = Parse(Commit());
            RevCommit b = Parse(Commit(10, a));
            RevCommit c1 = Parse(Commit(5, b));
            RevCommit c2 = Parse(Commit(-50, b));

            q.add(c2);
            q.add(a);
            q.add(b);
            q.add(c1);

            AssertCommit(c1, q.next());
            AssertCommit(b, q.next());
            AssertCommit(a, q.next());
            AssertCommit(c2, q.next());
            Assert.IsNull(q.next());
        }

        [Test]
        public void testInsertTie()
        {
            RevCommit a = Parse(Commit());
            RevCommit b = Parse(Commit(0, a));
            {
                q = create();
                q.add(a);
                q.add(b);

                AssertCommit(a, q.next());
                AssertCommit(b, q.next());
                Assert.IsNull(q.next());
            }
            {
                q = create();
                q.add(b);
                q.add(a);

                AssertCommit(b, q.next());
                AssertCommit(a, q.next());
                Assert.IsNull(q.next());
            }
        }

        [Test]
        public void testCloneFIFO()
        {
            RevCommit a = Parse(Commit());
            RevCommit b = Parse(Commit(200, a));
            RevCommit c = Parse(Commit(200, b));

            var src = new FIFORevQueue();
            src.add(a);
            src.add(b);
            src.add(c);

            q = new DateRevQueue(src);
            Assert.IsFalse(q.everbodyHasFlag(GitSharp.Core.RevWalk.RevWalk.UNINTERESTING));
            Assert.IsFalse(q.anybodyHasFlag(GitSharp.Core.RevWalk.RevWalk.UNINTERESTING));
            AssertCommit(c, q.peek());
            AssertCommit(c, q.peek());

            AssertCommit(c, q.next());
            AssertCommit(b, q.next());
            AssertCommit(a, q.next());
            Assert.IsNull(q.next());
        }
    }
}
