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


    // Note: Much of this test case is broken as it depends upon
    // the graph applying topological sorting *before* doing merge
    // simplification.  It also depends upon a difference between
    // full history and non-full history for a path, something we
    // don't quite yet have a distiction for in JGit.
    //
    public class RevWalkPathFilter6012Test : RevWalkTestCase
    {
#if false
	private static final String pA = "pA", pF = "pF", pE = "pE";

	private RevCommit a, b, c, d, e, f, g, h, i;

	private HashMap<RevCommit, String> byName;

	public void setUp() throws Exception {
		super.setUp();

		// Test graph was stolen from git-core t6012-rev-list-simplify
		// (by Junio C Hamano in 65347030590bcc251a9ff2ed96487a0f1b9e9fa8)
		//
		final RevBlob zF = blob("zF");
		final RevBlob zH = blob("zH");
		final RevBlob zI = blob("zI");
		final RevBlob zS = blob("zS");
		final RevBlob zY = blob("zY");

		a = commit(tree(file(pF, zH)));
		b = commit(tree(file(pF, zI)), a);
		c = commit(tree(file(pF, zI)), a);
		d = commit(tree(file(pA, zS), file(pF, zI)), c);
		parse(d);
		e = commit(d.getTree(), d, b);
		f = commit(tree(file(pA, zS), file(pE, zY), file(pF, zI)), e);
		parse(f);
		g = commit(tree(file(pE, zY), file(pF, zI)), b);
		h = commit(f.getTree(), g, f);
		i = commit(tree(file(pA, zS), file(pE, zY), file(pF, zF)), h);

		byName = new HashMap<RevCommit, String>();
		for (Field z : RevWalkPathFilter6012Test.class.getDeclaredFields()) {
			if (z.getType() == RevCommit.class)
				byName.put((RevCommit) z.get(this), z.getName());
		}
	}

	protected void check(final RevCommit... order) throws Exception {
		markStart(i);
		final StringBuilder act = new StringBuilder();
		for (final RevCommit z : rw) {
			final String name = byName.get(z);
			assertNotNull(name);
			act.append(name);
			act.append(' ');
		}
		final StringBuilder exp = new StringBuilder();
		for (final RevCommit z : order) {
			final String name = byName.get(z);
			assertNotNull(name);
			exp.append(name);
			exp.append(' ');
		}
		assertEquals(exp.toString(), act.toString());
	}

	protected void filter(final String path) {
		rw.setTreeFilter(AndTreeFilter.create(PathFilterGroup
				.createFromStrings(Collections.singleton(path)),
				TreeFilter.ANY_DIFF));
	}

	public void test1() throws Exception {
		// TODO --full-history
		check(i, h, g, f, e, d, c, b, a);
	}

	public void test2() throws Exception {
		// TODO --full-history
		filter(pF);
		// TODO fix broken test
		// check(i, h, e, c, b, a);
	}

	public void test3() throws Exception {
		// TODO --full-history
		rw.sort(RevSort.TOPO);
		filter(pF);
		// TODO fix broken test
		// check(i, h, e, c, b, a);
	}

	public void test4() throws Exception {
		// TODO --full-history
		rw.sort(RevSort.COMMIT_TIME_DESC);
		filter(pF);
		// TODO fix broken test
		// check(i, h, e, c, b, a);
	}

	public void test5() throws Exception {
		// TODO --simplify-merges
		filter(pF);
		// TODO fix broken test
		// check(i, e, c, b, a);
	}

	public void test6() throws Exception {
		filter(pF);
		check(i, b, a);
	}

	public void test7() throws Exception {
		rw.sort(RevSort.TOPO);
		filter(pF);
		check(i, b, a);
	}
#endif
    }
}
