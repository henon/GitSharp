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

using System.Collections.Generic;
using GitSharp.RevWalk;
using NUnit.Framework;

namespace GitSharp.Tests.RevWalk
{
    [TestFixture]
    public class FIFORevQueueTest : RevQueueTestCase<FIFORevQueue>
    {
        protected override FIFORevQueue create()
        {
            return new FIFORevQueue();
        }

        [Test]
        public override void testEmpty()
        {
            base.testEmpty();
			Assert.AreEqual(Generator.GeneratorOutputType.None, q.OutputType);
        }

        [Test]
        public void testCloneEmpty()
        {
            q = new FIFORevQueue(AbstractRevQueue.EmptyQueue);
            Assert.IsNull(q.next());
        }

        [Test]
        public void testAddLargeBlocks()
        {
            var lst = new List<RevCommit>();
            for (int i = 0; i < 3*BlockRevQueue.Block.BLOCK_SIZE; i++)
            {
                RevCommit c = commit();
                lst.Add(c);
                q.add(c);
            }
            for (int i = 0; i < lst.Count; i++)
                Assert.AreSame(lst[i], q.next());
        }

        [Test]
        public void testUnpopAtFront()
        {
            RevCommit a = commit();
            RevCommit b = commit();
            RevCommit c = commit();

            q.add(a);
            q.unpop(b);
            q.unpop(c);

            Assert.AreSame(c, q.next());
            Assert.AreSame(b, q.next());
            Assert.AreSame(a, q.next());
        }
    }
}
