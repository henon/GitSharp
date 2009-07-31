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
    public class DirCacheTreeTest : RepositoryTestCase
    {
#if false
	public void testEmptyCache_NoCacheTree() throws Exception {
		final DirCache dc = DirCache.read(db);
		assertNull(dc.getCacheTree(false));
	}

	public void testEmptyCache_CreateEmptyCacheTree() throws Exception {
		final DirCache dc = DirCache.read(db);
		final DirCacheTree tree = dc.getCacheTree(true);
		assertNotNull(tree);
		assertSame(tree, dc.getCacheTree(false));
		assertSame(tree, dc.getCacheTree(true));
		assertEquals("", tree.getNameString());
		assertEquals("", tree.getPathString());
		assertEquals(0, tree.getChildCount());
		assertEquals(0, tree.getEntrySpan());
		assertFalse(tree.isValid());
	}

	public void testEmptyCache_Clear_NoCacheTree() throws Exception {
		final DirCache dc = DirCache.read(db);
		final DirCacheTree tree = dc.getCacheTree(true);
		assertNotNull(tree);
		dc.clear();
		assertNull(dc.getCacheTree(false));
		assertNotSame(tree, dc.getCacheTree(true));
	}

	public void testSingleSubtree() throws Exception {
		final DirCache dc = DirCache.read(db);

		final String[] paths = { "a.", "a/b", "a/c", "a/d", "a0b" };
		final DirCacheEntry[] ents = new DirCacheEntry[paths.length];
		for (int i = 0; i < paths.length; i++)
			ents[i] = new DirCacheEntry(paths[i]);
		final int aFirst = 1;
		final int aLast = 3;

		final DirCacheBuilder b = dc.builder();
		for (int i = 0; i < ents.length; i++)
			b.add(ents[i]);
		b.finish();

		assertNull(dc.getCacheTree(false));
		final DirCacheTree root = dc.getCacheTree(true);
		assertNotNull(root);
		assertSame(root, dc.getCacheTree(true));
		assertEquals("", root.getNameString());
		assertEquals("", root.getPathString());
		assertEquals(1, root.getChildCount());
		assertEquals(dc.getEntryCount(), root.getEntrySpan());
		assertFalse(root.isValid());

		final DirCacheTree aTree = root.getChild(0);
		assertNotNull(aTree);
		assertSame(aTree, root.getChild(0));
		assertEquals("a", aTree.getNameString());
		assertEquals("a/", aTree.getPathString());
		assertEquals(0, aTree.getChildCount());
		assertEquals(aLast - aFirst + 1, aTree.getEntrySpan());
		assertFalse(aTree.isValid());
	}

	public void testTwoLevelSubtree() throws Exception {
		final DirCache dc = DirCache.read(db);

		final String[] paths = { "a.", "a/b", "a/c/e", "a/c/f", "a/d", "a0b" };
		final DirCacheEntry[] ents = new DirCacheEntry[paths.length];
		for (int i = 0; i < paths.length; i++)
			ents[i] = new DirCacheEntry(paths[i]);
		final int aFirst = 1;
		final int aLast = 4;
		final int acFirst = 2;
		final int acLast = 3;

		final DirCacheBuilder b = dc.builder();
		for (int i = 0; i < ents.length; i++)
			b.add(ents[i]);
		b.finish();

		assertNull(dc.getCacheTree(false));
		final DirCacheTree root = dc.getCacheTree(true);
		assertNotNull(root);
		assertSame(root, dc.getCacheTree(true));
		assertEquals("", root.getNameString());
		assertEquals("", root.getPathString());
		assertEquals(1, root.getChildCount());
		assertEquals(dc.getEntryCount(), root.getEntrySpan());
		assertFalse(root.isValid());

		final DirCacheTree aTree = root.getChild(0);
		assertNotNull(aTree);
		assertSame(aTree, root.getChild(0));
		assertEquals("a", aTree.getNameString());
		assertEquals("a/", aTree.getPathString());
		assertEquals(1, aTree.getChildCount());
		assertEquals(aLast - aFirst + 1, aTree.getEntrySpan());
		assertFalse(aTree.isValid());

		final DirCacheTree acTree = aTree.getChild(0);
		assertNotNull(acTree);
		assertSame(acTree, aTree.getChild(0));
		assertEquals("c", acTree.getNameString());
		assertEquals("a/c/", acTree.getPathString());
		assertEquals(0, acTree.getChildCount());
		assertEquals(acLast - acFirst + 1, acTree.getEntrySpan());
		assertFalse(acTree.isValid());
	}

	/**
	 * We had bugs related to buffer size in the DirCache. This test creates an
	 * index larger than the default BufferedInputStream buffer size. This made
	 * the DirCache unable to read the extensions when index size exceeded the
	 * buffer size (in some cases at least).
	 * 
	 * @throws CorruptObjectException
	 * @throws IOException
	 */
	public void testWriteReadTree() throws CorruptObjectException, IOException {
		final DirCache dc = DirCache.lock(db);

		final String A = String.format("a%2000s", "a");
		final String B = String.format("b%2000s", "b");
		final String[] paths = { A + ".", A + "." + B, A + "/" + B, A + "0" + B };
		final DirCacheEntry[] ents = new DirCacheEntry[paths.length];
		for (int i = 0; i < paths.length; i++)
			ents[i] = new DirCacheEntry(paths[i]);

		final DirCacheBuilder b = dc.builder();
		for (int i = 0; i < ents.length; i++)
			b.add(ents[i]);

		b.commit();
		DirCache read = DirCache.read(db);

		assertEquals(paths.length, read.getEntryCount());
		assertEquals(1, read.getCacheTree(true).getChildCount());
	}
#endif
    }
}
