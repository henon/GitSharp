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
namespace GitSharp.Tests.RevWalk
{

    public class FIFORevQueueTest : RevQueueTestCase<FIFORevQueue>
    {
#if false
	protected FIFORevQueue create() {
		return new FIFORevQueue();
	}

	public void testEmpty() throws Exception {
		super.testEmpty();
		assertEquals(0, q.outputType());
	}

	public void testCloneEmpty() throws Exception {
		q = new FIFORevQueue(AbstractRevQueue.EMPTY_QUEUE);
		assertNull(q.next());
	}

	public void testAddLargeBlocks() throws Exception {
		final ArrayList<RevCommit> lst = new ArrayList<RevCommit>();
		for (int i = 0; i < 3 * BlockRevQueue.Block.BLOCK_SIZE; i++) {
			final RevCommit c = commit();
			lst.add(c);
			q.add(c);
		}
		for (int i = 0; i < lst.size(); i++)
			assertSame(lst.get(i), q.next());
	}

	public void testUnpopAtFront() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit();
		final RevCommit c = commit();

		q.add(a);
		q.unpop(b);
		q.unpop(c);

		assertSame(c, q.next());
		assertSame(b, q.next());
		assertSame(a, q.next());
	}
#endif
    }
}
