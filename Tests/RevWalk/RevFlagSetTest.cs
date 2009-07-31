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
    public class RevFlagSetTest : RevWalkTestCase
    {
#if false
	public void testEmpty() {
		final RevFlagSet set = new RevFlagSet();
		assertEquals(0, set.mask);
		assertEquals(0, set.size());
		assertNotNull(set.iterator());
		assertFalse(set.iterator().hasNext());
	}

	public void testAddOne() {
		final String flagName = "flag";
		final RevFlag flag = rw.newFlag(flagName);
		assertTrue(0 != flag.mask);
		assertSame(flagName, flag.name);

		final RevFlagSet set = new RevFlagSet();
		assertTrue(set.add(flag));
		assertFalse(set.add(flag));
		assertEquals(flag.mask, set.mask);
		assertEquals(1, set.size());
		final Iterator<RevFlag> i = set.iterator();
		assertTrue(i.hasNext());
		assertSame(flag, i.next());
		assertFalse(i.hasNext());
	}

	public void testAddTwo() {
		final RevFlag flag1 = rw.newFlag("flag_1");
		final RevFlag flag2 = rw.newFlag("flag_2");
		assertTrue((flag1.mask & flag2.mask) == 0);

		final RevFlagSet set = new RevFlagSet();
		assertTrue(set.add(flag1));
		assertTrue(set.add(flag2));
		assertEquals(flag1.mask | flag2.mask, set.mask);
		assertEquals(2, set.size());
	}

	public void testContainsAll() {
		final RevFlag flag1 = rw.newFlag("flag_1");
		final RevFlag flag2 = rw.newFlag("flag_2");
		final RevFlagSet set1 = new RevFlagSet();
		assertTrue(set1.add(flag1));
		assertTrue(set1.add(flag2));

		assertTrue(set1.containsAll(set1));
		assertTrue(set1.containsAll(Arrays
				.asList(new RevFlag[] { flag1, flag2 })));

		final RevFlagSet set2 = new RevFlagSet();
		set2.add(rw.newFlag("flag_3"));
		assertFalse(set1.containsAll(set2));
	}

	public void testEquals() {
		final RevFlag flag1 = rw.newFlag("flag_1");
		final RevFlag flag2 = rw.newFlag("flag_2");
		final RevFlagSet set = new RevFlagSet();
		assertTrue(set.add(flag1));
		assertTrue(set.add(flag2));

		assertTrue(new RevFlagSet(set).equals(set));
		assertTrue(new RevFlagSet(Arrays.asList(new RevFlag[] { flag1, flag2 }))
				.equals(set));
	}

	public void testRemove() {
		final RevFlag flag1 = rw.newFlag("flag_1");
		final RevFlag flag2 = rw.newFlag("flag_2");
		final RevFlagSet set = new RevFlagSet();
		assertTrue(set.add(flag1));
		assertTrue(set.add(flag2));

		assertTrue(set.remove(flag1));
		assertFalse(set.remove(flag1));
		assertEquals(flag2.mask, set.mask);
		assertFalse(set.contains(flag1));
	}

	public void testContains() {
		final RevFlag flag1 = rw.newFlag("flag_1");
		final RevFlag flag2 = rw.newFlag("flag_2");
		final RevFlagSet set = new RevFlagSet();
		set.add(flag1);
		assertTrue(set.contains(flag1));
		assertFalse(set.contains(flag2));
		assertFalse(set.contains("bob"));
	}
#endif
    }
}
