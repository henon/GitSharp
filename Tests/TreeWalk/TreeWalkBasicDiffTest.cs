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
    public class TreeWalkBasicDiffTest : RepositoryTestCase
    {
#if false
	public void testMissingSubtree_DetectFileAdded_FileModified()
			throws Exception {
		final ObjectWriter ow = new ObjectWriter(db);
		final ObjectId aFileId = ow.writeBlob("a".getBytes());
		final ObjectId bFileId = ow.writeBlob("b".getBytes());
		final ObjectId cFileId1 = ow.writeBlob("c-1".getBytes());
		final ObjectId cFileId2 = ow.writeBlob("c-2".getBytes());

		// Create sub-a/empty, sub-c/empty = hello.
		final ObjectId oldTree;
		{
			final Tree root = new Tree(db);
			{
				final Tree subA = root.addTree("sub-a");
				subA.addFile("empty").setId(aFileId);
				subA.setId(ow.writeTree(subA));
			}
			{
				final Tree subC = root.addTree("sub-c");
				subC.addFile("empty").setId(cFileId1);
				subC.setId(ow.writeTree(subC));
			}
			oldTree = ow.writeTree(root);
		}

		// Create sub-a/empty, sub-b/empty, sub-c/empty.
		final ObjectId newTree;
		{
			final Tree root = new Tree(db);
			{
				final Tree subA = root.addTree("sub-a");
				subA.addFile("empty").setId(aFileId);
				subA.setId(ow.writeTree(subA));
			}
			{
				final Tree subB = root.addTree("sub-b");
				subB.addFile("empty").setId(bFileId);
				subB.setId(ow.writeTree(subB));
			}
			{
				final Tree subC = root.addTree("sub-c");
				subC.addFile("empty").setId(cFileId2);
				subC.setId(ow.writeTree(subC));
			}
			newTree = ow.writeTree(root);
		}

		final TreeWalk tw = new TreeWalk(db);
		tw.reset(new ObjectId[] { oldTree, newTree });
		tw.setRecursive(true);
		tw.setFilter(TreeFilter.ANY_DIFF);

		assertTrue(tw.next());
		assertEquals("sub-b/empty", tw.getPathString());
		assertEquals(FileMode.MISSING, tw.getFileMode(0));
		assertEquals(FileMode.REGULAR_FILE, tw.getFileMode(1));
		assertEquals(ObjectId.zeroId(), tw.getObjectId(0));
		assertEquals(bFileId, tw.getObjectId(1));

		assertTrue(tw.next());
		assertEquals("sub-c/empty", tw.getPathString());
		assertEquals(FileMode.REGULAR_FILE, tw.getFileMode(0));
		assertEquals(FileMode.REGULAR_FILE, tw.getFileMode(1));
		assertEquals(cFileId1, tw.getObjectId(0));
		assertEquals(cFileId2, tw.getObjectId(1));

		assertFalse(tw.next());
	}
#endif
    }
}
