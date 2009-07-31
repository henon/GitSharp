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
    public class RevObjectTest : RevWalkTestCase
    {
#if false
	public void testId() throws Exception {
		final RevCommit a = commit();
		assertSame(a, a.getId());
	}

	public void testEqualsIsIdentity() throws Exception {
		final RevCommit a1 = commit();
		final RevCommit b1 = commit();

		assertTrue(a1.equals(a1));
		assertTrue(a1.equals((Object) a1));
		assertFalse(a1.equals(b1));

		assertFalse(a1.equals(a1.copy()));
		assertFalse(a1.equals((Object) a1.copy()));
		assertFalse(a1.equals(""));

		final RevWalk rw2 = new RevWalk(db);
		final RevCommit a2 = rw2.parseCommit(a1);
		final RevCommit b2 = rw2.parseCommit(b1);
		assertNotSame(a1, a2);
		assertNotSame(b1, b2);

		assertFalse(a1.equals(a2));
		assertFalse(b1.equals(b2));

		assertEquals(a1.hashCode(), a2.hashCode());
		assertEquals(b1.hashCode(), b2.hashCode());

		assertTrue(AnyObjectId.equals(a1, a2));
		assertTrue(AnyObjectId.equals(b1, b2));
	}

	public void testRevObjectTypes() throws Exception {
		assertEquals(Constants.OBJ_TREE, emptyTree.getType());
		assertEquals(Constants.OBJ_COMMIT, commit().getType());
		assertEquals(Constants.OBJ_BLOB, blob("").getType());
		assertEquals(Constants.OBJ_TAG, tag("emptyTree", emptyTree).getType());
	}

	public void testHasRevFlag() throws Exception {
		final RevCommit a = commit();
		assertFalse(a.has(RevFlag.UNINTERESTING));
		a.flags |= RevWalk.UNINTERESTING;
		assertTrue(a.has(RevFlag.UNINTERESTING));
	}

	public void testHasAnyFlag() throws Exception {
		final RevCommit a = commit();
		final RevFlag flag1 = rw.newFlag("flag1");
		final RevFlag flag2 = rw.newFlag("flag2");
		final RevFlagSet s = new RevFlagSet();
		s.add(flag1);
		s.add(flag2);

		assertFalse(a.hasAny(s));
		a.flags |= flag1.mask;
		assertTrue(a.hasAny(s));
	}

	public void testHasAllFlag() throws Exception {
		final RevCommit a = commit();
		final RevFlag flag1 = rw.newFlag("flag1");
		final RevFlag flag2 = rw.newFlag("flag2");
		final RevFlagSet s = new RevFlagSet();
		s.add(flag1);
		s.add(flag2);

		assertFalse(a.hasAll(s));
		a.flags |= flag1.mask;
		assertFalse(a.hasAll(s));
		a.flags |= flag2.mask;
		assertTrue(a.hasAll(s));
	}

	public void testAddRevFlag() throws Exception {
		final RevCommit a = commit();
		final RevFlag flag1 = rw.newFlag("flag1");
		final RevFlag flag2 = rw.newFlag("flag2");
		assertEquals(0, a.flags);

		a.add(flag1);
		assertEquals(flag1.mask, a.flags);

		a.add(flag2);
		assertEquals(flag1.mask | flag2.mask, a.flags);
	}

	public void testAddRevFlagSet() throws Exception {
		final RevCommit a = commit();
		final RevFlag flag1 = rw.newFlag("flag1");
		final RevFlag flag2 = rw.newFlag("flag2");
		final RevFlagSet s = new RevFlagSet();
		s.add(flag1);
		s.add(flag2);

		assertEquals(0, a.flags);

		a.add(s);
		assertEquals(flag1.mask | flag2.mask, a.flags);
	}

	public void testRemoveRevFlag() throws Exception {
		final RevCommit a = commit();
		final RevFlag flag1 = rw.newFlag("flag1");
		final RevFlag flag2 = rw.newFlag("flag2");
		a.add(flag1);
		a.add(flag2);
		assertEquals(flag1.mask | flag2.mask, a.flags);
		a.remove(flag2);
		assertEquals(flag1.mask, a.flags);
	}

	public void testRemoveRevFlagSet() throws Exception {
		final RevCommit a = commit();
		final RevFlag flag1 = rw.newFlag("flag1");
		final RevFlag flag2 = rw.newFlag("flag2");
		final RevFlag flag3 = rw.newFlag("flag3");
		final RevFlagSet s = new RevFlagSet();
		s.add(flag1);
		s.add(flag2);
		a.add(flag3);
		a.add(s);
		assertEquals(flag1.mask | flag2.mask | flag3.mask, a.flags);
		a.remove(s);
		assertEquals(flag3.mask, a.flags);
	}
#endif
    }
}

