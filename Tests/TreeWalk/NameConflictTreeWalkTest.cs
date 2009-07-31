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
    public class NameConflictTreeWalkTest : RepositoryTestCase
    {
#if false
	private static final FileMode TREE = FileMode.TREE;

	private static final FileMode SYMLINK = FileMode.SYMLINK;

	private static final FileMode MISSING = FileMode.MISSING;

	private static final FileMode REGULAR_FILE = FileMode.REGULAR_FILE;

	private static final FileMode EXECUTABLE_FILE = FileMode.EXECUTABLE_FILE;

	public void testNoDF_NoGap() throws Exception {
		final DirCache tree0 = DirCache.read(db);
		final DirCache tree1 = DirCache.read(db);
		{
			final DirCacheBuilder b0 = tree0.builder();
			final DirCacheBuilder b1 = tree1.builder();

			b0.add(makeEntry("a", REGULAR_FILE));
			b0.add(makeEntry("a.b", EXECUTABLE_FILE));
			b1.add(makeEntry("a/b", REGULAR_FILE));
			b0.add(makeEntry("a0b", SYMLINK));

			b0.finish();
			b1.finish();
			assertEquals(3, tree0.getEntryCount());
			assertEquals(1, tree1.getEntryCount());
		}

		final TreeWalk tw = new TreeWalk(db);
		tw.reset();
		tw.addTree(new DirCacheIterator(tree0));
		tw.addTree(new DirCacheIterator(tree1));

		assertModes("a", REGULAR_FILE, MISSING, tw);
		assertModes("a.b", EXECUTABLE_FILE, MISSING, tw);
		assertModes("a", MISSING, TREE, tw);
		tw.enterSubtree();
		assertModes("a/b", MISSING, REGULAR_FILE, tw);
		assertModes("a0b", SYMLINK, MISSING, tw);
	}

	public void testDF_NoGap() throws Exception {
		final DirCache tree0 = DirCache.read(db);
		final DirCache tree1 = DirCache.read(db);
		{
			final DirCacheBuilder b0 = tree0.builder();
			final DirCacheBuilder b1 = tree1.builder();

			b0.add(makeEntry("a", REGULAR_FILE));
			b0.add(makeEntry("a.b", EXECUTABLE_FILE));
			b1.add(makeEntry("a/b", REGULAR_FILE));
			b0.add(makeEntry("a0b", SYMLINK));

			b0.finish();
			b1.finish();
			assertEquals(3, tree0.getEntryCount());
			assertEquals(1, tree1.getEntryCount());
		}

		final NameConflictTreeWalk tw = new NameConflictTreeWalk(db);
		tw.reset();
		tw.addTree(new DirCacheIterator(tree0));
		tw.addTree(new DirCacheIterator(tree1));

		assertModes("a", REGULAR_FILE, TREE, tw);
		assertTrue(tw.isSubtree());
		tw.enterSubtree();
		assertModes("a/b", MISSING, REGULAR_FILE, tw);
		assertModes("a.b", EXECUTABLE_FILE, MISSING, tw);
		assertModes("a0b", SYMLINK, MISSING, tw);
	}

	public void testDF_GapByOne() throws Exception {
		final DirCache tree0 = DirCache.read(db);
		final DirCache tree1 = DirCache.read(db);
		{
			final DirCacheBuilder b0 = tree0.builder();
			final DirCacheBuilder b1 = tree1.builder();

			b0.add(makeEntry("a", REGULAR_FILE));
			b0.add(makeEntry("a.b", EXECUTABLE_FILE));
			b1.add(makeEntry("a.b", EXECUTABLE_FILE));
			b1.add(makeEntry("a/b", REGULAR_FILE));
			b0.add(makeEntry("a0b", SYMLINK));

			b0.finish();
			b1.finish();
			assertEquals(3, tree0.getEntryCount());
			assertEquals(2, tree1.getEntryCount());
		}

		final NameConflictTreeWalk tw = new NameConflictTreeWalk(db);
		tw.reset();
		tw.addTree(new DirCacheIterator(tree0));
		tw.addTree(new DirCacheIterator(tree1));

		assertModes("a", REGULAR_FILE, TREE, tw);
		assertTrue(tw.isSubtree());
		tw.enterSubtree();
		assertModes("a/b", MISSING, REGULAR_FILE, tw);
		assertModes("a.b", EXECUTABLE_FILE, EXECUTABLE_FILE, tw);
		assertModes("a0b", SYMLINK, MISSING, tw);
	}

	public void testDF_SkipsSeenSubtree() throws Exception {
		final DirCache tree0 = DirCache.read(db);
		final DirCache tree1 = DirCache.read(db);
		{
			final DirCacheBuilder b0 = tree0.builder();
			final DirCacheBuilder b1 = tree1.builder();

			b0.add(makeEntry("a", REGULAR_FILE));
			b1.add(makeEntry("a.b", EXECUTABLE_FILE));
			b1.add(makeEntry("a/b", REGULAR_FILE));
			b0.add(makeEntry("a0b", SYMLINK));
			b1.add(makeEntry("a0b", SYMLINK));

			b0.finish();
			b1.finish();
			assertEquals(2, tree0.getEntryCount());
			assertEquals(3, tree1.getEntryCount());
		}

		final NameConflictTreeWalk tw = new NameConflictTreeWalk(db);
		tw.reset();
		tw.addTree(new DirCacheIterator(tree0));
		tw.addTree(new DirCacheIterator(tree1));

		assertModes("a", REGULAR_FILE, TREE, tw);
		assertTrue(tw.isSubtree());
		tw.enterSubtree();
		assertModes("a/b", MISSING, REGULAR_FILE, tw);
		assertModes("a.b", MISSING, EXECUTABLE_FILE, tw);
		assertModes("a0b", SYMLINK, SYMLINK, tw);
	}

	private DirCacheEntry makeEntry(final String path, final FileMode mode)
			throws Exception {
		final byte[] pathBytes = Constants.encode(path);
		final DirCacheEntry ent = new DirCacheEntry(path);
		ent.setFileMode(mode);
		ent.setObjectId(new ObjectWriter(db).computeBlobSha1(pathBytes.length,
				new ByteArrayInputStream(pathBytes)));
		return ent;
	}

	private static void assertModes(final String path, final FileMode mode0,
			final FileMode mode1, final TreeWalk tw) throws Exception {
		assertTrue("has " + path, tw.next());
		assertEquals(path, tw.getPathString());
		assertEquals(mode0, tw.getFileMode(0));
		assertEquals(mode1, tw.getFileMode(1));
	}
#endif
    }
}
