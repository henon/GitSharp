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


namespace GitSharp.Tests.DirectoryCache
{
    using NUnit.Framework;
    [TestFixture]
    public class DirCacheBasicTest : RepositoryTestCase
    {

#if false
	public void testReadMissing_RealIndex() throws Exception {
		final File idx = new File(db.getDirectory(), "index");
		assertFalse(idx.exists());

		final DirCache dc = DirCache.read(db);
		assertNotNull(dc);
		assertEquals(0, dc.getEntryCount());
	}

	public void testReadMissing_TempIndex() throws Exception {
		final File idx = new File(db.getDirectory(), "tmp_index");
		assertFalse(idx.exists());

		final DirCache dc = DirCache.read(idx);
		assertNotNull(dc);
		assertEquals(0, dc.getEntryCount());
	}

	public void testLockMissing_RealIndex() throws Exception {
		final File idx = new File(db.getDirectory(), "index");
		final File lck = new File(db.getDirectory(), "index.lock");
		assertFalse(idx.exists());
		assertFalse(lck.exists());

		final DirCache dc = DirCache.lock(db);
		assertNotNull(dc);
		assertFalse(idx.exists());
		assertTrue(lck.exists());
		assertEquals(0, dc.getEntryCount());

		dc.unlock();
		assertFalse(idx.exists());
		assertFalse(lck.exists());
	}

	public void testLockMissing_TempIndex() throws Exception {
		final File idx = new File(db.getDirectory(), "tmp_index");
		final File lck = new File(db.getDirectory(), "tmp_index.lock");
		assertFalse(idx.exists());
		assertFalse(lck.exists());

		final DirCache dc = DirCache.lock(idx);
		assertNotNull(dc);
		assertFalse(idx.exists());
		assertTrue(lck.exists());
		assertEquals(0, dc.getEntryCount());

		dc.unlock();
		assertFalse(idx.exists());
		assertFalse(lck.exists());
	}

	public void testWriteEmptyUnlock_RealIndex() throws Exception {
		final File idx = new File(db.getDirectory(), "index");
		final File lck = new File(db.getDirectory(), "index.lock");
		assertFalse(idx.exists());
		assertFalse(lck.exists());

		final DirCache dc = DirCache.lock(db);
		assertEquals(0, lck.length());
		dc.write();
		assertEquals(12 + 20, lck.length());

		dc.unlock();
		assertFalse(idx.exists());
		assertFalse(lck.exists());
	}

	public void testWriteEmptyCommit_RealIndex() throws Exception {
		final File idx = new File(db.getDirectory(), "index");
		final File lck = new File(db.getDirectory(), "index.lock");
		assertFalse(idx.exists());
		assertFalse(lck.exists());

		final DirCache dc = DirCache.lock(db);
		assertEquals(0, lck.length());
		dc.write();
		assertEquals(12 + 20, lck.length());

		assertTrue(dc.commit());
		assertTrue(idx.exists());
		assertFalse(lck.exists());
		assertEquals(12 + 20, idx.length());
	}

	public void testWriteEmptyReadEmpty_RealIndex() throws Exception {
		final File idx = new File(db.getDirectory(), "index");
		final File lck = new File(db.getDirectory(), "index.lock");
		assertFalse(idx.exists());
		assertFalse(lck.exists());
		{
			final DirCache dc = DirCache.lock(db);
			dc.write();
			assertTrue(dc.commit());
			assertTrue(idx.exists());
		}
		{
			final DirCache dc = DirCache.read(db);
			assertEquals(0, dc.getEntryCount());
		}
	}

	public void testWriteEmptyLockEmpty_RealIndex() throws Exception {
		final File idx = new File(db.getDirectory(), "index");
		final File lck = new File(db.getDirectory(), "index.lock");
		assertFalse(idx.exists());
		assertFalse(lck.exists());
		{
			final DirCache dc = DirCache.lock(db);
			dc.write();
			assertTrue(dc.commit());
			assertTrue(idx.exists());
		}
		{
			final DirCache dc = DirCache.lock(db);
			assertEquals(0, dc.getEntryCount());
			assertTrue(idx.exists());
			assertTrue(lck.exists());
			dc.unlock();
		}
	}

	public void testBuildThenClear() throws Exception {
		final DirCache dc = DirCache.read(db);

		final String[] paths = { "a.", "a.b", "a/b", "a0b" };
		final DirCacheEntry[] ents = new DirCacheEntry[paths.length];
		for (int i = 0; i < paths.length; i++)
			ents[i] = new DirCacheEntry(paths[i]);

		final DirCacheBuilder b = dc.builder();
		for (int i = 0; i < ents.length; i++)
			b.add(ents[i]);
		b.finish();

		assertEquals(paths.length, dc.getEntryCount());
		dc.clear();
		assertEquals(0, dc.getEntryCount());
	}
#endif
    }
}
