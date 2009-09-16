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
using GitSharp.DirectoryCache;
using GitSharp.Tests.Util;
using GitSharp.TreeWalk;
using Xunit;

namespace GitSharp.Tests.TreeWalk
{	
	public class NameConflictTreeWalkTest : RepositoryTestCase
	{
		[Fact]
		public virtual void testNoDF_NoGap()
		{
			DirCache tree0 = DirCache.read(db);
			DirCache tree1 = DirCache.read(db);
			DirCacheBuilder b0 = tree0.builder();
			DirCacheBuilder b1 = tree1.builder();

			b0.add(MakeEntry("a", FileMode.RegularFile));
			b0.add(MakeEntry("a.b", FileMode.ExecutableFile));
			b1.add(MakeEntry("a/b", FileMode.RegularFile));
			b0.add(MakeEntry("a0b", FileMode.Symlink));

			b0.finish();
			b1.finish();
			Assert.Equal(3, tree0.getEntryCount());
			Assert.Equal(1, tree1.getEntryCount());

			var tw = new GitSharp.TreeWalk.TreeWalk(db);
			tw.reset();
			tw.addTree(new DirCacheIterator(tree0));
			tw.addTree(new DirCacheIterator(tree1));

			AssertModes("a", FileMode.RegularFile, FileMode.Missing, tw);
			AssertModes("a.b", FileMode.ExecutableFile, FileMode.Missing, tw);
			AssertModes("a", FileMode.Missing, FileMode.Tree, tw);
			tw.enterSubtree();
			AssertModes("a/b", FileMode.Missing, FileMode.RegularFile, tw);
			AssertModes("a0b", FileMode.Symlink, FileMode.Missing, tw);
		}

		[Fact]
		public virtual void testDF_NoGap()
		{
			DirCache tree0 = DirCache.read(db);
			DirCache tree1 = DirCache.read(db);
			DirCacheBuilder b0 = tree0.builder();
			DirCacheBuilder b1 = tree1.builder();

			b0.add(MakeEntry("a", FileMode.RegularFile));
			b0.add(MakeEntry("a.b", FileMode.ExecutableFile));
			b1.add(MakeEntry("a/b", FileMode.RegularFile));
			b0.add(MakeEntry("a0b", FileMode.Symlink));

			b0.finish();
			b1.finish();
			Assert.Equal(3, tree0.getEntryCount());
			Assert.Equal(1, tree1.getEntryCount());

			var tw = new NameConflictTreeWalk(db);
			tw.reset();
			tw.addTree(new DirCacheIterator(tree0));
			tw.addTree(new DirCacheIterator(tree1));

			AssertModes("a", FileMode.RegularFile, FileMode.Tree, tw);
			Assert.True(tw.isSubtree());
			tw.enterSubtree();
			AssertModes("a/b", FileMode.Missing, FileMode.RegularFile, tw);
			AssertModes("a.b", FileMode.ExecutableFile, FileMode.Missing, tw);
			AssertModes("a0b", FileMode.Symlink, FileMode.Missing, tw);
		}

		[Fact]
		public virtual void testDF_GapByOne()
		{
			DirCache tree0 = DirCache.read(db);
			DirCache tree1 = DirCache.read(db);
			DirCacheBuilder b0 = tree0.builder();
			DirCacheBuilder b1 = tree1.builder();

			b0.add(MakeEntry("a", FileMode.RegularFile));
			b0.add(MakeEntry("a.b", FileMode.ExecutableFile));
			b1.add(MakeEntry("a.b", FileMode.ExecutableFile));
			b1.add(MakeEntry("a/b", FileMode.RegularFile));
			b0.add(MakeEntry("a0b", FileMode.Symlink));

			b0.finish();
			b1.finish();
			Assert.Equal(3, tree0.getEntryCount());
			Assert.Equal(2, tree1.getEntryCount());

			var tw = new NameConflictTreeWalk(db);
			tw.reset();
			tw.addTree(new DirCacheIterator(tree0));
			tw.addTree(new DirCacheIterator(tree1));

			AssertModes("a", FileMode.RegularFile, FileMode.Tree, tw);
			Assert.True(tw.isSubtree());
			tw.enterSubtree();
			AssertModes("a/b", FileMode.Missing, FileMode.RegularFile, tw);
			AssertModes("a.b", FileMode.ExecutableFile, FileMode.ExecutableFile, tw);
			AssertModes("a0b", FileMode.Symlink, FileMode.Missing, tw);
		}

		[Fact]
		public virtual void testDF_SkipsSeenSubtree()
		{
			DirCache tree0 = DirCache.read(db);
			DirCache tree1 = DirCache.read(db);
			DirCacheBuilder b0 = tree0.builder();
			DirCacheBuilder b1 = tree1.builder();

			b0.add(MakeEntry("a", FileMode.RegularFile));
			b1.add(MakeEntry("a.b", FileMode.ExecutableFile));
			b1.add(MakeEntry("a/b", FileMode.RegularFile));
			b0.add(MakeEntry("a0b", FileMode.Symlink));
			b1.add(MakeEntry("a0b", FileMode.Symlink));

			b0.finish();
			b1.finish();
			Assert.Equal(2, tree0.getEntryCount());
			Assert.Equal(3, tree1.getEntryCount());

			var tw = new NameConflictTreeWalk(db);
			tw.reset();
			tw.addTree(new DirCacheIterator(tree0));
			tw.addTree(new DirCacheIterator(tree1));

			AssertModes("a", FileMode.RegularFile, FileMode.Tree, tw);
			Assert.True(tw.isSubtree());
			tw.enterSubtree();
			AssertModes("a/b", FileMode.Missing, FileMode.RegularFile, tw);
			AssertModes("a.b", FileMode.Missing, FileMode.ExecutableFile, tw);
			AssertModes("a0b", FileMode.Symlink, FileMode.Symlink, tw);
		}

		private DirCacheEntry MakeEntry(string path, FileMode mode)
		{
			byte[] pathBytes = Constants.encode(path);
			var ent = new DirCacheEntry(path);
			ent.setFileMode(mode);
			ent.setObjectId(new ObjectWriter(db).ComputeBlobSha1(pathBytes.Length, new MemoryStream(pathBytes)));
			return ent;
		}

		private static void AssertModes(string path, FileMode mode0, FileMode mode1, GitSharp.TreeWalk.TreeWalk tw)
		{
			Assert.True(tw.next(), "has " + path);
			Assert.Equal(path, tw.getPathString());
			Assert.Equal(mode0, tw.getFileMode(0));
			Assert.Equal(mode1, tw.getFileMode(1));
		}
	}
}
