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

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Tests.Transport
{
    [TestFixture]
    public class LongMapTest
    {
#if false
	private LongMap<Long> map;

	protected void setUp() throws Exception {
		super.setUp();
		map = new LongMap<Long>();
	}

	public void testEmptyMap() {
		assertFalse(map.containsKey(0));
		assertFalse(map.containsKey(1));

		assertNull(map.get(0));
		assertNull(map.get(1));

		assertNull(map.remove(0));
		assertNull(map.remove(1));
	}

	public void testInsertMinValue() {
		final Long min = Long.valueOf(Long.MIN_VALUE);
		assertNull(map.put(Long.MIN_VALUE, min));
		assertTrue(map.containsKey(Long.MIN_VALUE));
		assertSame(min, map.get(Long.MIN_VALUE));
		assertFalse(map.containsKey(Integer.MIN_VALUE));
	}

	public void testReplaceMaxValue() {
		final Long min = Long.valueOf(Long.MAX_VALUE);
		final Long one = Long.valueOf(1);
		assertNull(map.put(Long.MAX_VALUE, min));
		assertSame(min, map.get(Long.MAX_VALUE));
		assertSame(min, map.put(Long.MAX_VALUE, one));
		assertSame(one, map.get(Long.MAX_VALUE));
	}

	public void testRemoveOne() {
		final long start = 1;
		assertNull(map.put(start, Long.valueOf(start)));
		assertEquals(Long.valueOf(start), map.remove(start));
		assertFalse(map.containsKey(start));
	}

	public void testRemoveCollision1() {
		// This test relies upon the fact that we always >>> 1 the value
		// to derive an unsigned hash code. Thus, 0 and 1 fall into the
		// same hash bucket. Further it relies on the fact that we add
		// the 2nd put at the top of the chain, so removing the 1st will
		// cause a different code path.
		//
		assertNull(map.put(0, Long.valueOf(0)));
		assertNull(map.put(1, Long.valueOf(1)));
		assertEquals(Long.valueOf(0), map.remove(0));

		assertFalse(map.containsKey(0));
		assertTrue(map.containsKey(1));
	}

	public void testRemoveCollision2() {
		// This test relies upon the fact that we always >>> 1 the value
		// to derive an unsigned hash code. Thus, 0 and 1 fall into the
		// same hash bucket. Further it relies on the fact that we add
		// the 2nd put at the top of the chain, so removing the 2nd will
		// cause a different code path.
		//
		assertNull(map.put(0, Long.valueOf(0)));
		assertNull(map.put(1, Long.valueOf(1)));
		assertEquals(Long.valueOf(1), map.remove(1));

		assertTrue(map.containsKey(0));
		assertFalse(map.containsKey(1));
	}

	public void testSmallMap() {
		final long start = 12;
		final long n = 8;
		for (long i = start; i < start + n; i++)
			assertNull(map.put(i, Long.valueOf(i)));
		for (long i = start; i < start + n; i++)
			assertEquals(Long.valueOf(i), map.get(i));
	}

	public void testLargeMap() {
		final long start = Integer.MAX_VALUE;
		final long n = 100000;
		for (long i = start; i < start + n; i++)
			assertNull(map.put(i, Long.valueOf(i)));
		for (long i = start; i < start + n; i++)
			assertEquals(Long.valueOf(i), map.get(i));
	}
#endif
    }
}
