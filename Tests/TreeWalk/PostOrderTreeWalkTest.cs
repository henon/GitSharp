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
    public class PostOrderTreeWalkTest : RepositoryTestCase
    {
#if false
	public void testInitialize_NoPostOrder() throws Exception {
		final TreeWalk tw = new TreeWalk(db);
		assertFalse(tw.isPostOrderTraversal());
	}

	public void testInitialize_TogglePostOrder() throws Exception {
		final TreeWalk tw = new TreeWalk(db);
		assertFalse(tw.isPostOrderTraversal());
		tw.setPostOrderTraversal(true);
		assertTrue(tw.isPostOrderTraversal());
		tw.setPostOrderTraversal(false);
		assertFalse(tw.isPostOrderTraversal());
	}

	public void testResetDoesNotAffectPostOrder() throws Exception {
		final TreeWalk tw = new TreeWalk(db);
		tw.setPostOrderTraversal(true);
		assertTrue(tw.isPostOrderTraversal());
		tw.reset();
		assertTrue(tw.isPostOrderTraversal());

		tw.setPostOrderTraversal(false);
		assertFalse(tw.isPostOrderTraversal());
		tw.reset();
		assertFalse(tw.isPostOrderTraversal());
	}

	public void testNoPostOrder() throws Exception {
		final DirCache tree = DirCache.read(db);
		{
			final DirCacheBuilder b = tree.builder();

			b.add(makeFile("a"));
			b.add(makeFile("b/c"));
			b.add(makeFile("b/d"));
			b.add(makeFile("q"));

			b.finish();
			assertEquals(4, tree.getEntryCount());
		}

		final TreeWalk tw = new TreeWalk(db);
		tw.reset();
		tw.setPostOrderTraversal(false);
		tw.addTree(new DirCacheIterator(tree));

		assertModes("a", REGULAR_FILE, tw);
		assertModes("b", TREE, tw);
		assertTrue(tw.isSubtree());
		assertFalse(tw.isPostChildren());
		tw.enterSubtree();
		assertModes("b/c", REGULAR_FILE, tw);
		assertModes("b/d", REGULAR_FILE, tw);
		assertModes("q", REGULAR_FILE, tw);
	}

	public void testWithPostOrder_EnterSubtree() throws Exception {
		final DirCache tree = DirCache.read(db);
		{
			final DirCacheBuilder b = tree.builder();

			b.add(makeFile("a"));
			b.add(makeFile("b/c"));
			b.add(makeFile("b/d"));
			b.add(makeFile("q"));

			b.finish();
			assertEquals(4, tree.getEntryCount());
		}

		final TreeWalk tw = new TreeWalk(db);
		tw.reset();
		tw.setPostOrderTraversal(true);
		tw.addTree(new DirCacheIterator(tree));

		assertModes("a", REGULAR_FILE, tw);

		assertModes("b", TREE, tw);
		assertTrue(tw.isSubtree());
		assertFalse(tw.isPostChildren());
		tw.enterSubtree();
		assertModes("b/c", REGULAR_FILE, tw);
		assertModes("b/d", REGULAR_FILE, tw);

		assertModes("b", TREE, tw);
		assertTrue(tw.isSubtree());
		assertTrue(tw.isPostChildren());

		assertModes("q", REGULAR_FILE, tw);
	}

	public void testWithPostOrder_NoEnterSubtree() throws Exception {
		final DirCache tree = DirCache.read(db);
		{
			final DirCacheBuilder b = tree.builder();

			b.add(makeFile("a"));
			b.add(makeFile("b/c"));
			b.add(makeFile("b/d"));
			b.add(makeFile("q"));

			b.finish();
			assertEquals(4, tree.getEntryCount());
		}

		final TreeWalk tw = new TreeWalk(db);
		tw.reset();
		tw.setPostOrderTraversal(true);
		tw.addTree(new DirCacheIterator(tree));

		assertModes("a", REGULAR_FILE, tw);

		assertModes("b", TREE, tw);
		assertTrue(tw.isSubtree());
		assertFalse(tw.isPostChildren());

		assertModes("q", REGULAR_FILE, tw);
	}

	private DirCacheEntry makeFile(final String path) throws Exception {
		final byte[] pathBytes = Constants.encode(path);
		final DirCacheEntry ent = new DirCacheEntry(path);
		ent.setFileMode(REGULAR_FILE);
		ent.setObjectId(new ObjectWriter(db).computeBlobSha1(pathBytes.length,
				new ByteArrayInputStream(pathBytes)));
		return ent;
	}

	private static void assertModes(final String path, final FileMode mode0,
			final TreeWalk tw) throws Exception {
		assertTrue("has " + path, tw.next());
		assertEquals(path, tw.getPathString());
		assertEquals(mode0, tw.getFileMode(0));
	}
#endif
    }
}
