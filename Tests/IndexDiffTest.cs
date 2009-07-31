/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
    public class IndexDiffTest : RepositoryTestCase
    {
#if false
	public void testAdded() throws IOException {
		GitIndex index = new GitIndex(db);
		writeTrashFile("file1", "file1");
		writeTrashFile("dir/subfile", "dir/subfile");
		Tree tree = new Tree(db);

		index.add(trash, new File(trash, "file1"));
		index.add(trash, new File(trash, "dir/subfile"));
		IndexDiff diff = new IndexDiff(tree, index);
		diff.diff();
		assertEquals(2, diff.getAdded().size());
		assertTrue(diff.getAdded().contains("file1"));
		assertTrue(diff.getAdded().contains("dir/subfile"));
		assertEquals(0, diff.getChanged().size());
		assertEquals(0, diff.getModified().size());
		assertEquals(0, diff.getRemoved().size());
	}

	public void testRemoved() throws IOException {
		GitIndex index = new GitIndex(db);
		writeTrashFile("file2", "file2");
		writeTrashFile("dir/file3", "dir/file3");

		Tree tree = new Tree(db);
		tree.addFile("file2");
		tree.addFile("dir/file3");
		assertEquals(2, tree.memberCount());
		tree.findBlobMember("file2").setId(ObjectId.fromString("30d67d4672d5c05833b7192cc77a79eaafb5c7ad"));
		Tree tree2 = (Tree) tree.findTreeMember("dir");
		tree2.findBlobMember("file3").setId(ObjectId.fromString("873fb8d667d05436d728c52b1d7a09528e6eb59b"));
		tree2.setId(new ObjectWriter(db).writeTree(tree2));
		tree.setId(new ObjectWriter(db).writeTree(tree));

		IndexDiff diff = new IndexDiff(tree, index);
		diff.diff();
		assertEquals(2, diff.getRemoved().size());
		assertTrue(diff.getRemoved().contains("file2"));
		assertTrue(diff.getRemoved().contains("dir/file3"));
		assertEquals(0, diff.getChanged().size());
		assertEquals(0, diff.getModified().size());
		assertEquals(0, diff.getAdded().size());
	}

	public void testModified() throws IOException {
		GitIndex index = new GitIndex(db);
		
		
		index.add(trash, writeTrashFile("file2", "file2"));
		index.add(trash, writeTrashFile("dir/file3", "dir/file3"));
		
		writeTrashFile("dir/file3", "changed");

		Tree tree = new Tree(db);
		tree.addFile("file2").setId(ObjectId.fromString("0123456789012345678901234567890123456789"));
		tree.addFile("dir/file3").setId(ObjectId.fromString("0123456789012345678901234567890123456789"));
		assertEquals(2, tree.memberCount());

		Tree tree2 = (Tree) tree.findTreeMember("dir");
		tree2.setId(new ObjectWriter(db).writeTree(tree2));
		tree.setId(new ObjectWriter(db).writeTree(tree));
		IndexDiff diff = new IndexDiff(tree, index);
		diff.diff();
		assertEquals(2, diff.getChanged().size());
		assertTrue(diff.getChanged().contains("file2"));
		assertTrue(diff.getChanged().contains("dir/file3"));
		assertEquals(1, diff.getModified().size());
		assertTrue(diff.getModified().contains("dir/file3"));
		assertEquals(0, diff.getAdded().size());
		assertEquals(0, diff.getRemoved().size());
		assertEquals(0, diff.getMissing().size());
	}

	public void testUnchangedSimple() throws IOException {
		GitIndex index = new GitIndex(db);

		index.add(trash, writeTrashFile("a.b", "a.b"));
		index.add(trash, writeTrashFile("a.c", "a.c"));
		index.add(trash, writeTrashFile("a=c", "a=c"));
		index.add(trash, writeTrashFile("a=d", "a=d"));

		Tree tree = new Tree(db);
		// got the hash id'd from the data using echo -n a.b|git hash-object -t blob --stdin
		tree.addFile("a.b").setId(ObjectId.fromString("f6f28df96c2b40c951164286e08be7c38ec74851"));
		tree.addFile("a.c").setId(ObjectId.fromString("6bc0e647512d2a0bef4f26111e484dc87df7f5ca"));
		tree.addFile("a=c").setId(ObjectId.fromString("06022365ddbd7fb126761319633bf73517770714"));
		tree.addFile("a=d").setId(ObjectId.fromString("fa6414df3da87840700e9eeb7fc261dd77ccd5c2"));

		tree.setId(new ObjectWriter(db).writeTree(tree));

		IndexDiff diff = new IndexDiff(tree, index);
		diff.diff();
		assertEquals(0, diff.getChanged().size());
		assertEquals(0, diff.getAdded().size());
		assertEquals(0, diff.getRemoved().size());
		assertEquals(0, diff.getMissing().size());
		assertEquals(0, diff.getModified().size());
	}

	/**
	 * This test has both files and directories that involve
	 * the tricky ordering used by Git.
	 *
	 * @throws IOException
	 */
	public void testUnchangedComplex() throws IOException {
		GitIndex index = new GitIndex(db);

		index.add(trash, writeTrashFile("a.b", "a.b"));
		index.add(trash, writeTrashFile("a.c", "a.c"));
		index.add(trash, writeTrashFile("a/b.b/b", "a/b.b/b"));
		index.add(trash, writeTrashFile("a/b", "a/b"));
		index.add(trash, writeTrashFile("a/c", "a/c"));
		index.add(trash, writeTrashFile("a=c", "a=c"));
		index.add(trash, writeTrashFile("a=d", "a=d"));

		Tree tree = new Tree(db);
		// got the hash id'd from the data using echo -n a.b|git hash-object -t blob --stdin
		tree.addFile("a.b").setId(ObjectId.fromString("f6f28df96c2b40c951164286e08be7c38ec74851"));
		tree.addFile("a.c").setId(ObjectId.fromString("6bc0e647512d2a0bef4f26111e484dc87df7f5ca"));
		tree.addFile("a/b.b/b").setId(ObjectId.fromString("8d840bd4e2f3a48ff417c8e927d94996849933fd"));
		tree.addFile("a/b").setId(ObjectId.fromString("db89c972fc57862eae378f45b74aca228037d415"));
		tree.addFile("a/c").setId(ObjectId.fromString("52ad142a008aeb39694bafff8e8f1be75ed7f007"));
		tree.addFile("a=c").setId(ObjectId.fromString("06022365ddbd7fb126761319633bf73517770714"));
		tree.addFile("a=d").setId(ObjectId.fromString("fa6414df3da87840700e9eeb7fc261dd77ccd5c2"));

		Tree tree3 = (Tree) tree.findTreeMember("a/b.b");
		tree3.setId(new ObjectWriter(db).writeTree(tree3));
		Tree tree2 = (Tree) tree.findTreeMember("a");
		tree2.setId(new ObjectWriter(db).writeTree(tree2));
		tree.setId(new ObjectWriter(db).writeTree(tree));

		IndexDiff diff = new IndexDiff(tree, index);
		diff.diff();
		assertEquals(0, diff.getChanged().size());
		assertEquals(0, diff.getAdded().size());
		assertEquals(0, diff.getRemoved().size());
		assertEquals(0, diff.getMissing().size());
		assertEquals(0, diff.getModified().size());
	}
#endif
    }
}
