/*
 * Copyright (C) 2009, Google Inc.
 * Copyright (C) 2008, Robin Rosenberg
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

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Tests.Merge
{
    [TestFixture]
    public class CherryPickTest : RepositoryTestCase
    {
#if false
	public void testPick() throws Exception {
		// B---O
		// \----P---T
		//
		// Cherry-pick "T" onto "O". This shouldn't introduce "p-fail", which
		// was created by "P", nor should it modify "a", which was done by "P".
		//
		final DirCache treeB = DirCache.read(db);
		final DirCache treeO = DirCache.read(db);
		final DirCache treeP = DirCache.read(db);
		final DirCache treeT = DirCache.read(db);
		{
			final DirCacheBuilder b = treeB.builder();
			final DirCacheBuilder o = treeO.builder();
			final DirCacheBuilder p = treeP.builder();
			final DirCacheBuilder t = treeT.builder();

			b.add(makeEntry("a", FileMode.REGULAR_FILE));

			o.add(makeEntry("a", FileMode.REGULAR_FILE));
			o.add(makeEntry("o", FileMode.REGULAR_FILE));

			p.add(makeEntry("a", FileMode.REGULAR_FILE, "q"));
			p.add(makeEntry("p-fail", FileMode.REGULAR_FILE));

			t.add(makeEntry("a", FileMode.REGULAR_FILE));
			t.add(makeEntry("t", FileMode.REGULAR_FILE));

			b.finish();
			o.finish();
			p.finish();
			t.finish();
		}

		final ObjectWriter ow = new ObjectWriter(db);
		final ObjectId B = commit(ow, treeB, new ObjectId[] {});
		final ObjectId O = commit(ow, treeO, new ObjectId[] { B });
		final ObjectId P = commit(ow, treeP, new ObjectId[] { B });
		final ObjectId T = commit(ow, treeT, new ObjectId[] { P });

		ThreeWayMerger twm = MergeStrategy.SIMPLE_TWO_WAY_IN_CORE.newMerger(db);
		twm.setBase(P);
		boolean merge = twm.merge(new ObjectId[] { O, T });
		assertTrue(merge);

		final TreeWalk tw = new TreeWalk(db);
		tw.setRecursive(true);
		tw.reset(twm.getResultTreeId());

		assertTrue(tw.next());
		assertEquals("a", tw.getPathString());
		assertCorrectId(treeO, tw);

		assertTrue(tw.next());
		assertEquals("o", tw.getPathString());
		assertCorrectId(treeO, tw);

		assertTrue(tw.next());
		assertEquals("t", tw.getPathString());
		assertCorrectId(treeT, tw);

		assertFalse(tw.next());
	}

	private void assertCorrectId(final DirCache treeT, final TreeWalk tw) {
		assertEquals(treeT.getEntry(tw.getPathString()).getObjectId(), tw
				.getObjectId(0));
	}

	private ObjectId commit(final ObjectWriter ow, final DirCache treeB,
			final ObjectId[] parentIds) throws Exception {
		final Commit c = new Commit(db);
		c.setTreeId(treeB.writeTree(ow));
		c.setAuthor(new PersonIdent("A U Thor", "a.u.thor", 1L, 0));
		c.setCommitter(c.getAuthor());
		c.setParentIds(parentIds);
		c.setMessage("Tree " + c.getTreeId().name());
		return ow.writeCommit(c);
	}

	private DirCacheEntry makeEntry(final String path, final FileMode mode)
			throws Exception {
		return makeEntry(path, mode, path);
	}

	private DirCacheEntry makeEntry(final String path, final FileMode mode,
			final String content) throws Exception {
		final DirCacheEntry ent = new DirCacheEntry(path);
		ent.setFileMode(mode);
		final byte[] contentBytes = Constants.encode(content);
		ent.setObjectId(new ObjectWriter(db).computeBlobSha1(
				contentBytes.length, new ByteArrayInputStream(contentBytes)));
		return ent;
	}
#endif
    }
}
