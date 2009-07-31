/*
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
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

namespace GitSharp.Tests
{
    [TestFixture]
    public class PackReverseIndexTest : RepositoryTestCase
    {
#if false
	private PackIndex idx;

	private PackReverseIndex reverseIdx;

	/**
	 * Set up tested class instance, test constructor by the way.
	 */
	public void setUp() throws Exception {
		super.setUp();
		// index with both small (< 2^31) and big offsets
		idx = PackIndex.open(JGitTestUtil.getTestResourceFile(
				"pack-huge.idx"));
		reverseIdx = new PackReverseIndex(idx);
	}

	/**
	 * Test findObject() for all index entries.
	 */
	public void testFindObject() {
		for (MutableEntry me : idx)
			assertEquals(me.toObjectId(), reverseIdx.findObject(me.getOffset()));
	}

	/**
	 * Test findObject() with illegal argument.
	 */
	public void testFindObjectWrongOffset() {
		assertNull(reverseIdx.findObject(0));
	}

	/**
	 * Test findNextOffset() for all index entries.
	 *
	 * @throws CorruptObjectException
	 */
	public void testFindNextOffset() throws CorruptObjectException {
		long offset = findFirstOffset();
		assertTrue(offset > 0);
		for (int i = 0; i < idx.getObjectCount(); i++) {
			long newOffset = reverseIdx.findNextOffset(offset, Long.MAX_VALUE);
			assertTrue(newOffset > offset);
			if (i == idx.getObjectCount() - 1)
				assertEquals(newOffset, Long.MAX_VALUE);
			else
				assertEquals(newOffset, idx.findOffset(reverseIdx
						.findObject(newOffset)));
			offset = newOffset;
		}
	}

	/**
	 * Test findNextOffset() with wrong illegal argument as offset.
	 */
	public void testFindNextOffsetWrongOffset() {
		try {
			reverseIdx.findNextOffset(0, Long.MAX_VALUE);
			fail("findNextOffset() should throw exception");
		} catch (CorruptObjectException x) {
			// expected
		}
	}

	private long findFirstOffset() {
		long min = Long.MAX_VALUE;
		for (MutableEntry me : idx)
			min = Math.min(min, me.getOffset());
		return min;
	}
#endif
    }

}
