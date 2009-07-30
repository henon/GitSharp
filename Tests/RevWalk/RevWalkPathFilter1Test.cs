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


    public class RevWalkPathFilter1Test : RevWalkTestCase
    {
#if false
	protected void filter(final String path) {
		rw.setTreeFilter(AndTreeFilter.create(PathFilterGroup
				.createFromStrings(Collections.singleton(path)),
				TreeFilter.ANY_DIFF));
	}

	public void testEmpty_EmptyTree() throws Exception {
		final RevCommit a = commit();
		filter("a");
		markStart(a);
		assertNull(rw.next());
	}

	public void testEmpty_NoMatch() throws Exception {
		final RevCommit a = commit(tree(file("0", blob("0"))));
		filter("a");
		markStart(a);
		assertNull(rw.next());
	}

	public void testSimple1() throws Exception {
		final RevCommit a = commit(tree(file("0", blob("0"))));
		filter("0");
		markStart(a);
		assertCommit(a, rw.next());
		assertNull(rw.next());
	}

	public void testEdits_MatchNone() throws Exception {
		final RevCommit a = commit(tree(file("0", blob("a"))));
		final RevCommit b = commit(tree(file("0", blob("b"))), a);
		final RevCommit c = commit(tree(file("0", blob("c"))), b);
		final RevCommit d = commit(tree(file("0", blob("d"))), c);
		filter("a");
		markStart(d);
		assertNull(rw.next());
	}

	public void testEdits_MatchAll() throws Exception {
		final RevCommit a = commit(tree(file("0", blob("a"))));
		final RevCommit b = commit(tree(file("0", blob("b"))), a);
		final RevCommit c = commit(tree(file("0", blob("c"))), b);
		final RevCommit d = commit(tree(file("0", blob("d"))), c);
		filter("0");
		markStart(d);
		assertCommit(d, rw.next());
		assertCommit(c, rw.next());
		assertCommit(b, rw.next());
		assertCommit(a, rw.next());
		assertNull(rw.next());
	}

	public void testStringOfPearls_FilePath1() throws Exception {
		final RevCommit a = commit(tree(file("d/f", blob("a"))));
		final RevCommit b = commit(tree(file("d/f", blob("a"))), a);
		final RevCommit c = commit(tree(file("d/f", blob("b"))), b);
		filter("d/f");
		markStart(c);

		assertCommit(c, rw.next());
		assertEquals(1, c.getParentCount());
		assertCommit(a, c.getParent(0)); // b was skipped

		assertCommit(a, rw.next());
		assertEquals(0, a.getParentCount());
		assertNull(rw.next());
	}

	public void testStringOfPearls_FilePath2() throws Exception {
		final RevCommit a = commit(tree(file("d/f", blob("a"))));
		final RevCommit b = commit(tree(file("d/f", blob("a"))), a);
		final RevCommit c = commit(tree(file("d/f", blob("b"))), b);
		final RevCommit d = commit(tree(file("d/f", blob("b"))), c);
		filter("d/f");
		markStart(d);

		// d was skipped
		assertCommit(c, rw.next());
		assertEquals(1, c.getParentCount());
		assertCommit(a, c.getParent(0)); // b was skipped

		assertCommit(a, rw.next());
		assertEquals(0, a.getParentCount());
		assertNull(rw.next());
	}

	public void testStringOfPearls_DirPath2() throws Exception {
		final RevCommit a = commit(tree(file("d/f", blob("a"))));
		final RevCommit b = commit(tree(file("d/f", blob("a"))), a);
		final RevCommit c = commit(tree(file("d/f", blob("b"))), b);
		final RevCommit d = commit(tree(file("d/f", blob("b"))), c);
		filter("d");
		markStart(d);

		// d was skipped
		assertCommit(c, rw.next());
		assertEquals(1, c.getParentCount());
		assertCommit(a, c.getParent(0)); // b was skipped

		assertCommit(a, rw.next());
		assertEquals(0, a.getParentCount());
		assertNull(rw.next());
	}

	public void testStringOfPearls_FilePath3() throws Exception {
		final RevCommit a = commit(tree(file("d/f", blob("a"))));
		final RevCommit b = commit(tree(file("d/f", blob("a"))), a);
		final RevCommit c = commit(tree(file("d/f", blob("b"))), b);
		final RevCommit d = commit(tree(file("d/f", blob("b"))), c);
		final RevCommit e = commit(tree(file("d/f", blob("b"))), d);
		final RevCommit f = commit(tree(file("d/f", blob("b"))), e);
		final RevCommit g = commit(tree(file("d/f", blob("b"))), f);
		final RevCommit h = commit(tree(file("d/f", blob("b"))), g);
		final RevCommit i = commit(tree(file("d/f", blob("c"))), h);
		filter("d/f");
		markStart(i);

		assertCommit(i, rw.next());
		assertEquals(1, i.getParentCount());
		assertCommit(c, i.getParent(0)); // h..d was skipped

		assertCommit(c, rw.next());
		assertEquals(1, c.getParentCount());
		assertCommit(a, c.getParent(0)); // b was skipped

		assertCommit(a, rw.next());
		assertEquals(0, a.getParentCount());
		assertNull(rw.next());
	}
#endif
    }
}
