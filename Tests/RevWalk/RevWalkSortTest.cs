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
    public class RevWalkSortTest : RevWalkTestCase
    {
#if false
	public void testSort_Default() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit(1, a);
		final RevCommit c = commit(1, b);
		final RevCommit d = commit(1, c);

		markStart(d);
		assertCommit(d, rw.next());
		assertCommit(c, rw.next());
		assertCommit(b, rw.next());
		assertCommit(a, rw.next());
		assertNull(rw.next());
	}

	public void testSort_COMMIT_TIME_DESC() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c = commit(b);
		final RevCommit d = commit(c);

		rw.sort(RevSort.COMMIT_TIME_DESC);
		markStart(d);
		assertCommit(d, rw.next());
		assertCommit(c, rw.next());
		assertCommit(b, rw.next());
		assertCommit(a, rw.next());
		assertNull(rw.next());
	}

	public void testSort_REVERSE() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c = commit(b);
		final RevCommit d = commit(c);

		rw.sort(RevSort.REVERSE);
		markStart(d);
		assertCommit(a, rw.next());
		assertCommit(b, rw.next());
		assertCommit(c, rw.next());
		assertCommit(d, rw.next());
		assertNull(rw.next());
	}

	public void testSort_COMMIT_TIME_DESC_OutOfOrder1() throws Exception {
		// Despite being out of order time-wise, a strand-of-pearls must
		// still maintain topological order.
		//
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c = commit(-5, b);
		final RevCommit d = commit(10, c);
		assertTrue(parse(a).getCommitTime() < parse(d).getCommitTime());
		assertTrue(parse(c).getCommitTime() < parse(b).getCommitTime());

		rw.sort(RevSort.COMMIT_TIME_DESC);
		markStart(d);
		assertCommit(d, rw.next());
		assertCommit(c, rw.next());
		assertCommit(b, rw.next());
		assertCommit(a, rw.next());
		assertNull(rw.next());
	}

	public void testSort_COMMIT_TIME_DESC_OutOfOrder2() throws Exception {
		// c1 is back dated before its parent.
		//
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c1 = commit(-5, b);
		final RevCommit c2 = commit(10, b);
		final RevCommit d = commit(c1, c2);

		rw.sort(RevSort.COMMIT_TIME_DESC);
		markStart(d);
		assertCommit(d, rw.next());
		assertCommit(c2, rw.next());
		assertCommit(b, rw.next());
		assertCommit(a, rw.next());
		assertCommit(c1, rw.next());
		assertNull(rw.next());
	}

	public void testSort_TOPO() throws Exception {
		// c1 is back dated before its parent.
		//
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c1 = commit(-5, b);
		final RevCommit c2 = commit(10, b);
		final RevCommit d = commit(c1, c2);

		rw.sort(RevSort.TOPO);
		markStart(d);
		assertCommit(d, rw.next());
		assertCommit(c2, rw.next());
		assertCommit(c1, rw.next());
		assertCommit(b, rw.next());
		assertCommit(a, rw.next());
		assertNull(rw.next());
	}

	public void testSort_TOPO_REVERSE() throws Exception {
		// c1 is back dated before its parent.
		//
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c1 = commit(-5, b);
		final RevCommit c2 = commit(10, b);
		final RevCommit d = commit(c1, c2);

		rw.sort(RevSort.TOPO);
		rw.sort(RevSort.REVERSE, true);
		markStart(d);
		assertCommit(a, rw.next());
		assertCommit(b, rw.next());
		assertCommit(c1, rw.next());
		assertCommit(c2, rw.next());
		assertCommit(d, rw.next());
		assertNull(rw.next());
	}
#endif
    }
}
