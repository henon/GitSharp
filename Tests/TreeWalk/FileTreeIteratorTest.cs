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


    public class FileTreeIteratorTest : RepositoryTestCase
    {
#if false
	private final String[] paths = { "a,", "a,b", "a/b", "a0b" };

	private long[] mtime;

	public void setUp() throws Exception {
		super.setUp();

		// We build the entries backwards so that on POSIX systems we
		// are likely to get the entries in the trash directory in the
		// opposite order of what they should be in for the iteration.
		// This should stress the sorting code better than doing it in
		// the correct order.
		//
		mtime = new long[paths.length];
		for (int i = paths.length - 1; i >= 0; i--) {
			final String s = paths[i];
			writeTrashFile(s, s);
			mtime[i] = new File(trash, s).lastModified();
		}
	}

	public void testEmptyIfRootIsFile() throws Exception {
		final File r = new File(trash, paths[0]);
		assertTrue(r.isFile());
		final FileTreeIterator fti = new FileTreeIterator(r);
		assertTrue(fti.first());
		assertTrue(fti.eof());
	}

	public void testEmptyIfRootDoesNotExist() throws Exception {
		final File r = new File(trash, "not-existing-file");
		assertFalse(r.exists());
		final FileTreeIterator fti = new FileTreeIterator(r);
		assertTrue(fti.first());
		assertTrue(fti.eof());
	}

	public void testEmptyIfRootIsEmpty() throws Exception {
		final File r = new File(trash, "not-existing-file");
		assertFalse(r.exists());
		r.mkdir();
		assertTrue(r.isDirectory());

		final FileTreeIterator fti = new FileTreeIterator(r);
		assertTrue(fti.first());
		assertTrue(fti.eof());
	}

	public void testSimpleIterate() throws Exception {
		final FileTreeIterator top = new FileTreeIterator(trash);

		assertTrue(top.first());
		assertFalse(top.eof());
		assertEquals(FileMode.REGULAR_FILE.getBits(), top.mode);
		assertEquals(paths[0], nameOf(top));
		assertEquals(paths[0].length(), top.getEntryLength());
		assertEquals(mtime[0], top.getEntryLastModified());

		top.next(1);
		assertFalse(top.first());
		assertFalse(top.eof());
		assertEquals(FileMode.REGULAR_FILE.getBits(), top.mode);
		assertEquals(paths[1], nameOf(top));
		assertEquals(paths[1].length(), top.getEntryLength());
		assertEquals(mtime[1], top.getEntryLastModified());

		top.next(1);
		assertFalse(top.first());
		assertFalse(top.eof());
		assertEquals(FileMode.TREE.getBits(), top.mode);

		final AbstractTreeIterator sub = top.createSubtreeIterator(db);
		assertTrue(sub instanceof FileTreeIterator);
		final FileTreeIterator subfti = (FileTreeIterator) sub;
		assertTrue(sub.first());
		assertFalse(sub.eof());
		assertEquals(paths[2], nameOf(sub));
		assertEquals(paths[2].length(), subfti.getEntryLength());
		assertEquals(mtime[2], subfti.getEntryLastModified());

		sub.next(1);
		assertTrue(sub.eof());

		top.next(1);
		assertFalse(top.first());
		assertFalse(top.eof());
		assertEquals(FileMode.REGULAR_FILE.getBits(), top.mode);
		assertEquals(paths[3], nameOf(top));
		assertEquals(paths[3].length(), top.getEntryLength());
		assertEquals(mtime[3], top.getEntryLastModified());

		top.next(1);
		assertTrue(top.eof());
	}

	public void testComputeFileObjectId() throws Exception {
		final FileTreeIterator top = new FileTreeIterator(trash);

		final MessageDigest md = Constants.newMessageDigest();
		md.update(Constants.encodeASCII(Constants.TYPE_BLOB));
		md.update((byte) ' ');
		md.update(Constants.encodeASCII(paths[0].length()));
		md.update((byte) 0);
		md.update(Constants.encode(paths[0]));
		final ObjectId expect = ObjectId.fromRaw(md.digest());

		assertEquals(expect, top.getEntryObjectId());

		// Verify it was cached by removing the file and getting it again.
		//
		new File(trash, paths[0]).delete();
		assertEquals(expect, top.getEntryObjectId());
	}

	private static String nameOf(final AbstractTreeIterator i) {
		return RawParseUtils.decode(Constants.CHARSET, i.path, 0, i.pathLen);
	}
#endif
    }
}
