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
    public class RevWalkFilterTest : RevWalkTestCase
    {
#if false
	private static final MyAll MY_ALL = new MyAll();

	public void testFilter_ALL() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c = commit(b);

		rw.setRevFilter(RevFilter.ALL);
		markStart(c);
		assertCommit(c, rw.next());
		assertCommit(b, rw.next());
		assertCommit(a, rw.next());
		assertNull(rw.next());
	}

	public void testFilter_Negate_ALL() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c = commit(b);

		rw.setRevFilter(RevFilter.ALL.negate());
		markStart(c);
		assertNull(rw.next());
	}

	public void testFilter_NOT_ALL() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c = commit(b);

		rw.setRevFilter(NotRevFilter.create(RevFilter.ALL));
		markStart(c);
		assertNull(rw.next());
	}

	public void testFilter_NONE() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c = commit(b);

		rw.setRevFilter(RevFilter.NONE);
		markStart(c);
		assertNull(rw.next());
	}

	public void testFilter_NOT_NONE() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c = commit(b);

		rw.setRevFilter(NotRevFilter.create(RevFilter.NONE));
		markStart(c);
		assertCommit(c, rw.next());
		assertCommit(b, rw.next());
		assertCommit(a, rw.next());
		assertNull(rw.next());
	}

	public void testFilter_ALL_And_NONE() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c = commit(b);

		rw.setRevFilter(AndRevFilter.create(RevFilter.ALL, RevFilter.NONE));
		markStart(c);
		assertNull(rw.next());
	}

	public void testFilter_NONE_And_ALL() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c = commit(b);

		rw.setRevFilter(AndRevFilter.create(RevFilter.NONE, RevFilter.ALL));
		markStart(c);
		assertNull(rw.next());
	}

	public void testFilter_ALL_Or_NONE() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c = commit(b);

		rw.setRevFilter(OrRevFilter.create(RevFilter.ALL, RevFilter.NONE));
		markStart(c);
		assertCommit(c, rw.next());
		assertCommit(b, rw.next());
		assertCommit(a, rw.next());
		assertNull(rw.next());
	}

	public void testFilter_NONE_Or_ALL() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c = commit(b);

		rw.setRevFilter(OrRevFilter.create(RevFilter.NONE, RevFilter.ALL));
		markStart(c);
		assertCommit(c, rw.next());
		assertCommit(b, rw.next());
		assertCommit(a, rw.next());
		assertNull(rw.next());
	}

	public void testFilter_MY_ALL_And_NONE() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c = commit(b);

		rw.setRevFilter(AndRevFilter.create(MY_ALL, RevFilter.NONE));
		markStart(c);
		assertNull(rw.next());
	}

	public void testFilter_NONE_And_MY_ALL() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c = commit(b);

		rw.setRevFilter(AndRevFilter.create(RevFilter.NONE, MY_ALL));
		markStart(c);
		assertNull(rw.next());
	}

	public void testFilter_MY_ALL_Or_NONE() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c = commit(b);

		rw.setRevFilter(OrRevFilter.create(MY_ALL, RevFilter.NONE));
		markStart(c);
		assertCommit(c, rw.next());
		assertCommit(b, rw.next());
		assertCommit(a, rw.next());
		assertNull(rw.next());
	}

	public void testFilter_NONE_Or_MY_ALL() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c = commit(b);

		rw.setRevFilter(OrRevFilter.create(RevFilter.NONE, MY_ALL));
		markStart(c);
		assertCommit(c, rw.next());
		assertCommit(b, rw.next());
		assertCommit(a, rw.next());
		assertNull(rw.next());
	}

	public void testFilter_NO_MERGES() throws Exception {
		final RevCommit a = commit();
		final RevCommit b = commit(a);
		final RevCommit c1 = commit(b);
		final RevCommit c2 = commit(b);
		final RevCommit d = commit(c1, c2);
		final RevCommit e = commit(d);

		rw.setRevFilter(RevFilter.NO_MERGES);
		markStart(e);
		assertCommit(e, rw.next());
		assertCommit(c2, rw.next());
		assertCommit(c1, rw.next());
		assertCommit(b, rw.next());
		assertCommit(a, rw.next());
		assertNull(rw.next());
	}

	private static class MyAll extends RevFilter {
		@Override
		public RevFilter clone() {
			return this;
		}

		@Override
		public boolean include(RevWalk walker, RevCommit cmit)
				throws StopWalkException, MissingObjectException,
				IncorrectObjectTypeException, IOException {
			return true;
		}
	}
#endif
    }
}
