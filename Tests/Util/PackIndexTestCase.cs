/*
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NUnit.Framework;

namespace GitSharp.Tests.Util
{
    public abstract class PackIndexTestCase : RepositoryTestCase
    {

        PackIndex smallIdx;

        PackIndex denseIdx;
#if false
        [SetUp]
        public void setUp()
        {
            super.setUp();
            smallIdx = PackIndex.open(getFileForPack34be9032());
            denseIdx = PackIndex.open(getFileForPackdf2982f28());
        }

	/**
	 * Return file with appropriate index version for prepared pack.
	 * 
	 * @return file with index
	 */
	public abstract FileInfo getFileForPack34be9032();

	/**
	 * Return file with appropriate index version for prepared pack.
	 * 
	 * @return file with index
	 */
	public abstract FileInfo getFileForPackdf2982f28();

	/**
	 * Verify CRC32 support.
	 *
	 * @throws MissingObjectException
	 * @throws UnsupportedOperationException
	 */
	public abstract void testCRC32() ;

	/**
	 * Test contracts of Iterator methods and this implementation remove()
	 * limitations.
	 */
	public void testIteratorMethodsContract() {
		Iterator<PackIndex.MutableEntry> iter = smallIdx.iterator();
		while (iter.hasNext()) {
			iter.next();
		}

		try {
			iter.next();
			fail("next() unexpectedly returned element");
		} catch (NoSuchElementException x) {
			// expected
		}

		try {
			iter.remove();
			fail("remove() shouldn't be implemented");
		} catch (UnsupportedOperationException x) {
			// expected
		}
	}

	/**
	 * Test results of iterator comparing to content of well-known (prepared)
	 * small index.
	 */
	public void testIteratorReturnedValues1() {
		Iterator<PackIndex.MutableEntry> iter = smallIdx.iterator();
		assertEquals("4b825dc642cb6eb9a060e54bf8d69288fbee4904", iter.next()
				.name());
		assertEquals("540a36d136cf413e4b064c2b0e0a4db60f77feab", iter.next()
				.name());
		assertEquals("5b6e7c66c276e7610d4a73c70ec1a1f7c1003259", iter.next()
				.name());
		assertEquals("6ff87c4664981e4397625791c8ea3bbb5f2279a3", iter.next()
				.name());
		assertEquals("82c6b885ff600be425b4ea96dee75dca255b69e7", iter.next()
				.name());
		assertEquals("902d5476fa249b7abc9d84c611577a81381f0327", iter.next()
				.name());
		assertEquals("aabf2ffaec9b497f0950352b3e582d73035c2035", iter.next()
				.name());
		assertEquals("c59759f143fb1fe21c197981df75a7ee00290799", iter.next()
				.name());
		assertFalse(iter.hasNext());
	}

	/**
	 * Compare offset from iterator entries with output of findOffset() method.
	 */
	public void testCompareEntriesOffsetsWithFindOffsets() {
		for (MutableEntry me : smallIdx) {
			assertEquals(smallIdx.findOffset(me.toObjectId()), me.getOffset());
		}
		for (MutableEntry me : denseIdx) {
			assertEquals(denseIdx.findOffset(me.toObjectId()), me.getOffset());
		}
	}

	/**
	 * Test partial results of iterator comparing to content of well-known
	 * (prepared) dense index, that may need multi-level indexing.
	 */
	public void testIteratorReturnedValues2() {
		Iterator<PackIndex.MutableEntry> iter = denseIdx.iterator();
		while (!iter.next().name().equals(
				"0a3d7772488b6b106fb62813c4d6d627918d9181")) {
			// just iterating
		}
		assertEquals("1004d0d7ac26fbf63050a234c9b88a46075719d3", iter.next()
				.name()); // same level-1
		assertEquals("10da5895682013006950e7da534b705252b03be6", iter.next()
				.name()); // same level-1
		assertEquals("1203b03dc816ccbb67773f28b3c19318654b0bc8", iter.next()
				.name());
	}
#endif
    }

}
