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
using NUnit.Framework;

namespace GitSharp.Tests.RevWalk
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
            Assert.AreEqual(Generator.SORT_COMMIT_TIME_DESC, q.outputType());
        }

        [Test]
        public void testCloneEmpty()
        {
            q = new DateRevQueue(AbstractRevQueue.EMPTY_QUEUE);
            Assert.IsNull(q.next());
        }

        [Test]
        public void testInsertOutOfOrder()
        {
            RevCommit a = parse(commit());
            RevCommit b = parse(commit(10, a));
            RevCommit c1 = parse(commit(5, b));
            RevCommit c2 = parse(commit(-50, b));

            q.add(c2);
            q.add(a);
            q.add(b);
            q.add(c1);

            assertCommit(c1, q.next());
            assertCommit(b, q.next());
            assertCommit(a, q.next());
            assertCommit(c2, q.next());
            Assert.IsNull(q.next());
        }

        [Test]
        public void testInsertTie()
        {
            RevCommit a = parse(commit());
            RevCommit b = parse(commit(0, a));
            {
                q = create();
                q.add(a);
                q.add(b);

                assertCommit(a, q.next());
                assertCommit(b, q.next());
                Assert.IsNull(q.next());
            }
            {
                q = create();
                q.add(b);
                q.add(a);

                assertCommit(b, q.next());
                assertCommit(a, q.next());
                Assert.IsNull(q.next());
            }
        }

        [Test]
        public void testCloneFIFO()
        {
            RevCommit a = parse(commit());
            RevCommit b = parse(commit(200, a));
            RevCommit c = parse(commit(200, b));

            var src = new FIFORevQueue();
            src.add(a);
            src.add(b);
            src.add(c);

            q = new DateRevQueue(src);
            Assert.IsFalse(q.everbodyHasFlag(GitSharp.RevWalk.RevWalk.UNINTERESTING));
            Assert.IsFalse(q.anybodyHasFlag(GitSharp.RevWalk.RevWalk.UNINTERESTING));
            assertCommit(c, q.peek());
            assertCommit(c, q.peek());

            assertCommit(c, q.next());
            assertCommit(b, q.next());
            assertCommit(a, q.next());
            Assert.IsNull(q.next());
        }
    }
}
