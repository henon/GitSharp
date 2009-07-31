/*
 * Copyright (C) 2008, Google Inc.
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

using GitSharp.TreeWalk;
namespace GitSharp.Tests.TreeWalk
{

    using NUnit.Framework;
    [TestFixture]
    public class EmptyTreeIteratorTest : RepositoryTestCase
    {
#if false
	public void testAtEOF() throws Exception {
		final EmptyTreeIterator etp = new EmptyTreeIterator();
		assertTrue(etp.first());
		assertTrue(etp.eof());
	}

	public void testCreateSubtreeIterator() throws Exception {
		final EmptyTreeIterator etp = new EmptyTreeIterator();
		final AbstractTreeIterator sub = etp.createSubtreeIterator(db);
		assertNotNull(sub);
		assertTrue(sub.first());
		assertTrue(sub.eof());
		assertTrue(sub instanceof EmptyTreeIterator);
	}

	public void testEntryObjectId() throws Exception {
		final EmptyTreeIterator etp = new EmptyTreeIterator();
		assertSame(ObjectId.zeroId(), etp.getEntryObjectId());
		assertNotNull(etp.idBuffer());
		assertEquals(0, etp.idOffset());
		assertEquals(ObjectId.zeroId(), ObjectId.fromRaw(etp.idBuffer()));
	}

	public void testNextDoesNothing() throws Exception {
		final EmptyTreeIterator etp = new EmptyTreeIterator();
		etp.next(1);
		assertTrue(etp.first());
		assertTrue(etp.eof());
		assertEquals(ObjectId.zeroId(), ObjectId.fromRaw(etp.idBuffer()));

		etp.next(1);
		assertTrue(etp.first());
		assertTrue(etp.eof());
		assertEquals(ObjectId.zeroId(), ObjectId.fromRaw(etp.idBuffer()));
	}

	public void testBackDoesNothing() throws Exception {
		final EmptyTreeIterator etp = new EmptyTreeIterator();
		etp.back(1);
		assertTrue(etp.first());
		assertTrue(etp.eof());
		assertEquals(ObjectId.zeroId(), ObjectId.fromRaw(etp.idBuffer()));

		etp.back(1);
		assertTrue(etp.first());
		assertTrue(etp.eof());
		assertEquals(ObjectId.zeroId(), ObjectId.fromRaw(etp.idBuffer()));
	}

	public void testStopWalkCallsParent() throws Exception {
		final boolean called[] = new boolean[1];
		assertFalse(called[0]);

		final EmptyTreeIterator parent = new EmptyTreeIterator() {
			@Override
			public void stopWalk() {
				called[0] = true;
			}
		};
		parent.createSubtreeIterator(db).stopWalk();
		assertTrue(called[0]);
	}
#endif
    }
}
