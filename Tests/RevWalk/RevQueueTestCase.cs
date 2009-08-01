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
    using NUnit.Framework;
    [TestFixture]
    public abstract class RevQueueTestCase<T> : RevWalkTestCase
        where T : AbstractRevQueue
    {
#if false
	protected T q;

	public void setUp() throws Exception {
		super.setUp();
		q = create();
	}

	protected abstract T create();

	public void testEmpty() throws Exception {
		assertNull(q.next());
		assertTrue(q.everbodyHasFlag(RevWalk.UNINTERESTING));
		assertFalse(q.anybodyHasFlag(RevWalk.UNINTERESTING));
	}

	public void testClear() throws Exception {
		final RevCommit a = parse(commit());
		final RevCommit b = parse(commit(a));

		q.add(a);
		q.add(b);
		q.clear();
		assertNull(q.next());
	}

	public void testHasFlags() throws Exception {
		final RevCommit a = parse(commit());
		final RevCommit b = parse(commit(a));

		q.add(a);
		q.add(b);

		assertFalse(q.everbodyHasFlag(RevWalk.UNINTERESTING));
		assertFalse(q.anybodyHasFlag(RevWalk.UNINTERESTING));

		a.flags |= RevWalk.UNINTERESTING;
		assertFalse(q.everbodyHasFlag(RevWalk.UNINTERESTING));
		assertTrue(q.anybodyHasFlag(RevWalk.UNINTERESTING));

		b.flags |= RevWalk.UNINTERESTING;
		assertTrue(q.everbodyHasFlag(RevWalk.UNINTERESTING));
		assertTrue(q.anybodyHasFlag(RevWalk.UNINTERESTING));
	}
#endif
    }
}
