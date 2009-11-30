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


using System.IO;
using GitSharp.Core;
using GitSharp.Core.DirectoryCache;
using GitSharp.Core.TreeWalk;
using NUnit.Framework;
using FileMode=GitSharp.Core.FileMode;

namespace GitSharp.Core.Tests.TreeWalk
{	
	[TestFixture]
	public class NameConflictTreeWalkTest : RepositoryTestCase
	{
		private static readonly FileMode TREE = FileMode.Tree;

		private static readonly FileMode SYMLINK = FileMode.Symlink;

		private static readonly FileMode MISSING = FileMode.Missing;

		private static readonly FileMode REGULAR_FILE = FileMode.RegularFile;

		private static readonly FileMode EXECUTABLE_FILE = FileMode.ExecutableFile;

		[Test]
		public virtual void testNoDF_NoGap()
		{
			DirCache tree0 = DirCache.read(db);
			DirCache tree1 = DirCache.read(db);
			{
				DirCacheBuilder b0 = tree0.builder();
				DirCacheBuilder b1 = tree1.builder();

				b0.add(makeEntry("a", REGULAR_FILE));
				b0.add(makeEntry("a.b", EXECUTABLE_FILE));
				b1.add(makeEntry("a/b", REGULAR_FILE));
				b0.add(makeEntry("a0b", SYMLINK));

				b0.finish();
				b1.finish();
				Assert.AreEqual(3, tree0.getEntryCount());
				Assert.AreEqual(1, tree1.getEntryCount());
			}

			GitSharp.Core.TreeWalk.TreeWalk tw = new GitSharp.Core.TreeWalk.TreeWalk(db);
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

		[Test]
		public virtual void testDF_NoGap()
		{
			DirCache tree0 = DirCache.read(db);
			DirCache tree1 = DirCache.read(db);
			{
				DirCacheBuilder b0 = tree0.builder();
				DirCacheBuilder b1 = tree1.builder();

				b0.add(makeEntry("a", REGULAR_FILE));
				b0.add(makeEntry("a.b", EXECUTABLE_FILE));
				b1.add(makeEntry("a/b", REGULAR_FILE));
				b0.add(makeEntry("a0b", SYMLINK));

				b0.finish();
				b1.finish();
				Assert.AreEqual(3, tree0.getEntryCount());
				Assert.AreEqual(1, tree1.getEntryCount());
			}

			NameConflictTreeWalk tw = new NameConflictTreeWalk(db);
			tw.reset();
			tw.addTree(new DirCacheIterator(tree0));
			tw.addTree(new DirCacheIterator(tree1));

			assertModes("a", REGULAR_FILE, TREE, tw);
			Assert.IsTrue(tw.isSubtree());
			tw.enterSubtree();
			assertModes("a/b", MISSING, REGULAR_FILE, tw);
			assertModes("a.b", EXECUTABLE_FILE, MISSING, tw);
			assertModes("a0b", SYMLINK, MISSING, tw);
		}

		[Test]
		public virtual void testDF_GapByOne()
		{
			DirCache tree0 = DirCache.read(db);
			DirCache tree1 = DirCache.read(db);
			{
				DirCacheBuilder b0 = tree0.builder();
				DirCacheBuilder b1 = tree1.builder();

				b0.add(makeEntry("a", REGULAR_FILE));
				b0.add(makeEntry("a.b", EXECUTABLE_FILE));
				b1.add(makeEntry("a.b", EXECUTABLE_FILE));
				b1.add(makeEntry("a/b", REGULAR_FILE));
				b0.add(makeEntry("a0b", SYMLINK));

				b0.finish();
				b1.finish();
				Assert.AreEqual(3, tree0.getEntryCount());
				Assert.AreEqual(2, tree1.getEntryCount());
			}

			NameConflictTreeWalk tw = new NameConflictTreeWalk(db);
			tw.reset();
			tw.addTree(new DirCacheIterator(tree0));
			tw.addTree(new DirCacheIterator(tree1));

			assertModes("a", REGULAR_FILE, TREE, tw);
			Assert.IsTrue(tw.isSubtree());
			tw.enterSubtree();
			assertModes("a/b", MISSING, REGULAR_FILE, tw);
			assertModes("a.b", EXECUTABLE_FILE, EXECUTABLE_FILE, tw);
			assertModes("a0b", SYMLINK, MISSING, tw);
		}

		[Test]
		public virtual void testDF_SkipsSeenSubtree()
		{
			DirCache tree0 = DirCache.read(db);
			DirCache tree1 = DirCache.read(db);
			{
				DirCacheBuilder b0 = tree0.builder();
				DirCacheBuilder b1 = tree1.builder();

				b0.add(makeEntry("a", REGULAR_FILE));
				b1.add(makeEntry("a.b", EXECUTABLE_FILE));
				b1.add(makeEntry("a/b", REGULAR_FILE));
				b0.add(makeEntry("a0b", SYMLINK));
				b1.add(makeEntry("a0b", SYMLINK));

				b0.finish();
				b1.finish();
				Assert.AreEqual(2, tree0.getEntryCount());
				Assert.AreEqual(3, tree1.getEntryCount());
			}

			NameConflictTreeWalk tw = new NameConflictTreeWalk(db);
			tw.reset();
			tw.addTree(new DirCacheIterator(tree0));
			tw.addTree(new DirCacheIterator(tree1));

			assertModes("a", REGULAR_FILE, TREE, tw);
			Assert.IsTrue(tw.isSubtree());
			tw.enterSubtree();
			assertModes("a/b", MISSING, REGULAR_FILE, tw);
			assertModes("a.b", MISSING, EXECUTABLE_FILE, tw);
			assertModes("a0b", SYMLINK, SYMLINK, tw);
		}

		private DirCacheEntry makeEntry(string path, FileMode mode)
		{
			byte[] pathBytes = Constants.encode(path);
			DirCacheEntry ent = new DirCacheEntry(path);
			ent.setFileMode(mode);
			ent.setObjectId(new ObjectWriter(db).ComputeBlobSha1(pathBytes.Length, new MemoryStream(pathBytes)));
			return ent;
		}

		private static void assertModes(string path, FileMode mode0, FileMode mode1, GitSharp.Core.TreeWalk.TreeWalk tw)
		{
			Assert.IsTrue(tw.next(), "has " + path);
			Assert.AreEqual(path, tw.getPathString());
			Assert.AreEqual(mode0, tw.getFileMode(0));
			Assert.AreEqual(mode1, tw.getFileMode(1));
		}
	}
}
