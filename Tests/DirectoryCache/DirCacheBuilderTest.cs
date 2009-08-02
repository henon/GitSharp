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
    public class DirCacheBuilderTest : RepositoryTestCase
    {
#if false
	public void testBuildEmpty() throws Exception {
		{
			final DirCache dc = DirCache.lock(db);
			final DirCacheBuilder b = dc.builder();
			assertNotNull(b);
			b.finish();
			dc.write();
			assertTrue(dc.commit());
		}
		{
			final DirCache dc = DirCache.read(db);
			assertEquals(0, dc.getEntryCount());
		}
	}

	public void testBuildOneFile_FinishWriteCommit() throws Exception {
		final String path = "a-file-path";
		final FileMode mode = FileMode.REGULAR_FILE;
		final long lastModified = 1218123387057L;
		final int length = 1342;
		final DirCacheEntry entOrig;
		{
			final DirCache dc = DirCache.lock(db);
			final DirCacheBuilder b = dc.builder();
			assertNotNull(b);

			entOrig = new DirCacheEntry(path);
			entOrig.setFileMode(mode);
			entOrig.setLastModified(lastModified);
			entOrig.setLength(length);

			assertNotSame(path, entOrig.getPathString());
			assertEquals(path, entOrig.getPathString());
			assertEquals(ObjectId.zeroId(), entOrig.getObjectId());
			assertEquals(mode.getBits(), entOrig.getRawMode());
			assertEquals(0, entOrig.getStage());
			assertEquals(lastModified, entOrig.getLastModified());
			assertEquals(length, entOrig.getLength());
			assertFalse(entOrig.isAssumeValid());
			b.add(entOrig);

			b.finish();
			assertEquals(1, dc.getEntryCount());
			assertSame(entOrig, dc.getEntry(0));

			dc.write();
			assertTrue(dc.commit());
		}
		{
			final DirCache dc = DirCache.read(db);
			assertEquals(1, dc.getEntryCount());

			final DirCacheEntry entRead = dc.getEntry(0);
			assertNotSame(entOrig, entRead);
			assertEquals(path, entRead.getPathString());
			assertEquals(ObjectId.zeroId(), entOrig.getObjectId());
			assertEquals(mode.getBits(), entOrig.getRawMode());
			assertEquals(0, entOrig.getStage());
			assertEquals(lastModified, entOrig.getLastModified());
			assertEquals(length, entOrig.getLength());
			assertFalse(entOrig.isAssumeValid());
		}
	}

	public void testBuildOneFile_Commit() throws Exception {
		final String path = "a-file-path";
		final FileMode mode = FileMode.REGULAR_FILE;
		final long lastModified = 1218123387057L;
		final int length = 1342;
		final DirCacheEntry entOrig;
		{
			final DirCache dc = DirCache.lock(db);
			final DirCacheBuilder b = dc.builder();
			assertNotNull(b);

			entOrig = new DirCacheEntry(path);
			entOrig.setFileMode(mode);
			entOrig.setLastModified(lastModified);
			entOrig.setLength(length);

			assertNotSame(path, entOrig.getPathString());
			assertEquals(path, entOrig.getPathString());
			assertEquals(ObjectId.zeroId(), entOrig.getObjectId());
			assertEquals(mode.getBits(), entOrig.getRawMode());
			assertEquals(0, entOrig.getStage());
			assertEquals(lastModified, entOrig.getLastModified());
			assertEquals(length, entOrig.getLength());
			assertFalse(entOrig.isAssumeValid());
			b.add(entOrig);

			assertTrue(b.commit());
			assertEquals(1, dc.getEntryCount());
			assertSame(entOrig, dc.getEntry(0));
			assertFalse(new File(db.getDirectory(), "index.lock").exists());
		}
		{
			final DirCache dc = DirCache.read(db);
			assertEquals(1, dc.getEntryCount());

			final DirCacheEntry entRead = dc.getEntry(0);
			assertNotSame(entOrig, entRead);
			assertEquals(path, entRead.getPathString());
			assertEquals(ObjectId.zeroId(), entOrig.getObjectId());
			assertEquals(mode.getBits(), entOrig.getRawMode());
			assertEquals(0, entOrig.getStage());
			assertEquals(lastModified, entOrig.getLastModified());
			assertEquals(length, entOrig.getLength());
			assertFalse(entOrig.isAssumeValid());
		}
	}

	public void testFindSingleFile() throws Exception {
		final String path = "a-file-path";
		final DirCache dc = DirCache.read(db);
		final DirCacheBuilder b = dc.builder();
		assertNotNull(b);

		final DirCacheEntry entOrig = new DirCacheEntry(path);
		assertNotSame(path, entOrig.getPathString());
		assertEquals(path, entOrig.getPathString());
		b.add(entOrig);
		b.finish();

		assertEquals(1, dc.getEntryCount());
		assertSame(entOrig, dc.getEntry(0));
		assertEquals(0, dc.findEntry(path));

		assertEquals(-1, dc.findEntry("@@-before"));
		assertEquals(0, real(dc.findEntry("@@-before")));

		assertEquals(-2, dc.findEntry("a-zoo"));
		assertEquals(1, real(dc.findEntry("a-zoo")));

		assertSame(entOrig, dc.getEntry(path));
	}

	public void testAdd_InGitSortOrder() throws Exception {
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
		for (int i = 0; i < paths.length; i++) {
			assertSame(ents[i], dc.getEntry(i));
			assertEquals(paths[i], dc.getEntry(i).getPathString());
			assertEquals(i, dc.findEntry(paths[i]));
			assertSame(ents[i], dc.getEntry(paths[i]));
		}
	}

	public void testAdd_ReverseGitSortOrder() throws Exception {
		final DirCache dc = DirCache.read(db);

		final String[] paths = { "a.", "a.b", "a/b", "a0b" };
		final DirCacheEntry[] ents = new DirCacheEntry[paths.length];
		for (int i = 0; i < paths.length; i++)
			ents[i] = new DirCacheEntry(paths[i]);

		final DirCacheBuilder b = dc.builder();
		for (int i = ents.length - 1; i >= 0; i--)
			b.add(ents[i]);
		b.finish();

		assertEquals(paths.length, dc.getEntryCount());
		for (int i = 0; i < paths.length; i++) {
			assertSame(ents[i], dc.getEntry(i));
			assertEquals(paths[i], dc.getEntry(i).getPathString());
			assertEquals(i, dc.findEntry(paths[i]));
			assertSame(ents[i], dc.getEntry(paths[i]));
		}
	}

	public void testBuilderClear() throws Exception {
		final DirCache dc = DirCache.read(db);

		final String[] paths = { "a.", "a.b", "a/b", "a0b" };
		final DirCacheEntry[] ents = new DirCacheEntry[paths.length];
		for (int i = 0; i < paths.length; i++)
			ents[i] = new DirCacheEntry(paths[i]);
		{
			final DirCacheBuilder b = dc.builder();
			for (int i = 0; i < ents.length; i++)
				b.add(ents[i]);
			b.finish();
		}
		assertEquals(paths.length, dc.getEntryCount());
		{
			final DirCacheBuilder b = dc.builder();
			b.finish();
		}
		assertEquals(0, dc.getEntryCount());
	}

	private static int real(int eIdx) {
		if (eIdx < 0)
			eIdx = -(eIdx + 1);
		return eIdx;
	}
#endif
    }
}
