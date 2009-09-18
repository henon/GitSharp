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
using Xunit;

namespace GitSharp.Tests.RevWalk
{
    public class FIFORevQueueTest : RevQueueTestCase<FIFORevQueue>
    {
        protected override FIFORevQueue Create()
        {
            return new FIFORevQueue();
        }

        [StrictFactAttribute]
        public override void testEmpty()
        {
            base.testEmpty();
			Assert.Equal(Generator.GeneratorOutputType.None, q.OutputType);
        }

        [StrictFactAttribute]
        public void testCloneEmpty()
        {
            q = new FIFORevQueue(AbstractRevQueue.EmptyQueue);
            Assert.Null(q.next());
        }

        [StrictFactAttribute]
        public void testAddLargeBlocks()
        {
            var lst = new List<RevCommit>();
            for (int i = 0; i < 3*BlockRevQueue.Block.BLOCK_SIZE; i++)
            {
                RevCommit c = Commit();
                lst.Add(c);
                q.add(c);
            }
            for (int i = 0; i < lst.Count; i++)
                Assert.Same(lst[i], q.next());
        }

        [StrictFactAttribute]
        public void testUnpopAtFront()
        {
            RevCommit a = Commit();
            RevCommit b = Commit();
            RevCommit c = Commit();

            q.add(a);
            q.unpop(b);
            q.unpop(c);

            Assert.Same(c, q.next());
            Assert.Same(b, q.next());
            Assert.Same(a, q.next());
        }
    }
}
