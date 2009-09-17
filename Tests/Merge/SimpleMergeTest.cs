/*
 * Copyright (C) 2008, Robin Rosenberg
 * Copyright (C) 2009, Dan Rigby <dan@danrigby.com>
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

using System;
using System.IO;
using GitSharp.DirectoryCache;
using GitSharp.Merge;
using GitSharp.Tests.Util;
using Xunit;

namespace GitSharp.Tests.Merge
{
	public class SimpleMergeTest : RepositoryTestCase
	{
		[Fact(Timeout = 30000)]
		public void TestOurs()
		{
			Merger ourMerger = MergeStrategy.Ours.NewMerger(db);
			bool merge = ourMerger.Merge(new[] { db.Resolve("a"), db.Resolve("c") });
			Assert.True(merge);
			
			var mappedTree = db.MapTree("a");
			var ourMergerResult = ourMerger.GetResultTreeId();
			Assert.Equal(mappedTree.Id, ourMergerResult);

			Assert.Equal(db.MapTree("a").Id, ourMerger.GetResultTreeId());
		}

		[Fact(Timeout = 30000)]
		public void TestTheirs()
		{
			Merger ourMerger = MergeStrategy.Theirs.NewMerger(db);
			bool merge = ourMerger.Merge(new[] { db.Resolve("a"), db.Resolve("c") });
			Assert.True(merge);
			Assert.Equal(db.MapTree("c").Id, ourMerger.GetResultTreeId());
		}

		[Fact(Timeout = 30000)]
		public void TestTrivialTwoWay()
		{
			Merger ourMerger = MergeStrategy.SimpleTwoWayInCore.NewMerger(db);
			bool merge = ourMerger.Merge(new[] { db.Resolve("a"), db.Resolve("c") });
			Assert.True(merge);
			Assert.Equal("02ba32d3649e510002c21651936b7077aa75ffa9", ourMerger.GetResultTreeId().Name);
		}

		[Fact(Timeout = 30000)]
		public void TestTrivialTwoWayDisjointHistories()
		{
			Merger ourMerger = MergeStrategy.SimpleTwoWayInCore.NewMerger(db);
			bool merge = ourMerger.Merge(new[] { db.Resolve("a"), db.Resolve("c~4") });
			Assert.True(merge);
			Assert.Equal("86265c33b19b2be71bdd7b8cb95823f2743d03a8", ourMerger.GetResultTreeId().Name);
		}

		[Fact(Timeout = 30000)]
		public void TestTrivialTwoWayOk()
		{
			Merger ourMerger = MergeStrategy.SimpleTwoWayInCore.NewMerger(db);
			bool merge = ourMerger.Merge(new[] { db.Resolve("a^0^0^0"), db.Resolve("a^0^0^1") });
			Assert.True(merge);
			Assert.Equal(db.MapTree("a^0^0").Id, ourMerger.GetResultTreeId());
		}

		[Fact(Timeout = 30000)]
		public void TestTrivialTwoWayConflict()
		{
			Merger ourMerger = MergeStrategy.SimpleTwoWayInCore.NewMerger(db);
			bool merge = ourMerger.Merge(new[] { db.Resolve("f"), db.Resolve("g") });
			Assert.False(merge);
		}

		[Fact(Timeout = 30000)]
		public void TestTrivialTwoWayValidSubtreeSort()
		{
			DirCache treeB = DirCache.read(db);
			DirCache treeO = DirCache.read(db);
			DirCache treeT = DirCache.read(db);
			{
				DirCacheBuilder b = treeB.builder();
				DirCacheBuilder o = treeO.builder();
				DirCacheBuilder t = treeT.builder();

				b.add(MakeEntry("libelf-po/a", FileMode.RegularFile));
				b.add(MakeEntry("libelf/c", FileMode.RegularFile));

				o.add(MakeEntry("Makefile", FileMode.RegularFile));
				o.add(MakeEntry("libelf-po/a", FileMode.RegularFile));
				o.add(MakeEntry("libelf/c", FileMode.RegularFile));

				t.add(MakeEntry("libelf-po/a", FileMode.RegularFile));
				t.add(MakeEntry("libelf/c", FileMode.RegularFile, "blah"));

				b.finish();
				o.finish();
				t.finish();
			}

			var ow = new ObjectWriter(db);
			ObjectId B = Commit(ow, treeB, new ObjectId[] { });
			ObjectId O = Commit(ow, treeO, new[] { B });
			ObjectId T = Commit(ow, treeT, new[] { B });

			Merger ourMerger = MergeStrategy.SimpleTwoWayInCore.NewMerger(db);
			bool merge = ourMerger.Merge(new[] { O, T });
			Assert.True(merge);

			var tw = new GitSharp.TreeWalk.TreeWalk(db) { Recursive = true };
			tw.reset(ourMerger.GetResultTreeId());

			Assert.True(tw.next());
			Assert.Equal("Makefile", tw.getPathString());
			AssertCorrectId(treeO, tw);

			Assert.True(tw.next());
			Assert.Equal("libelf-po/a", tw.getPathString());
			AssertCorrectId(treeO, tw);

			Assert.True(tw.next());
			Assert.Equal("libelf/c", tw.getPathString());
			AssertCorrectId(treeT, tw);

			Assert.False(tw.next());
		}

		[Fact(Timeout = 30000)]
		public void TestTrivialTwoWayConcurrentSubtreeChange()
		{
			DirCache treeB = DirCache.read(db);
			DirCache treeO = DirCache.read(db);
			DirCache treeT = DirCache.read(db);
			{
				DirCacheBuilder b = treeB.builder();
				DirCacheBuilder o = treeO.builder();
				DirCacheBuilder t = treeT.builder();

				b.add(MakeEntry("d/o", FileMode.RegularFile));
				b.add(MakeEntry("d/t", FileMode.RegularFile));

				o.add(MakeEntry("d/o", FileMode.RegularFile, "o !"));
				o.add(MakeEntry("d/t", FileMode.RegularFile));

				t.add(MakeEntry("d/o", FileMode.RegularFile));
				t.add(MakeEntry("d/t", FileMode.RegularFile, "t !"));

				b.finish();
				o.finish();
				t.finish();
			}

			var ow = new ObjectWriter(db);
			ObjectId B = Commit(ow, treeB, new ObjectId[] { });
			ObjectId O = Commit(ow, treeO, new[] { B });
			ObjectId T = Commit(ow, treeT, new[] { B });

			Merger ourMerger = MergeStrategy.SimpleTwoWayInCore.NewMerger(db);
			bool merge = ourMerger.Merge(new[] { O, T });
			Assert.True(merge);

			var tw = new GitSharp.TreeWalk.TreeWalk(db) { Recursive = true };
			tw.reset(ourMerger.GetResultTreeId());

			Assert.True(tw.next());
			Assert.Equal("d/o", tw.getPathString());
			AssertCorrectId(treeO, tw);

			Assert.True(tw.next());
			Assert.Equal("d/t", tw.getPathString());
			AssertCorrectId(treeT, tw);

			Assert.False(tw.next());
		}

		[Fact(Timeout = 30000)]
		public void TestTrivialTwoWayConflictSubtreeChange()
		{
			DirCache treeB = DirCache.read(db);
			DirCache treeO = DirCache.read(db);
			DirCache treeT = DirCache.read(db);
			{
				DirCacheBuilder b = treeB.builder();
				DirCacheBuilder o = treeO.builder();
				DirCacheBuilder t = treeT.builder();

				b.add(MakeEntry("d/o", FileMode.RegularFile));
				b.add(MakeEntry("d/t", FileMode.RegularFile));

				o.add(MakeEntry("d/o", FileMode.RegularFile));
				o.add(MakeEntry("d/t", FileMode.RegularFile, "o !"));

				t.add(MakeEntry("d/o", FileMode.RegularFile, "t !"));
				t.add(MakeEntry("d/t", FileMode.RegularFile, "t !"));

				b.finish();
				o.finish();
				t.finish();
			}

			var ow = new ObjectWriter(db);
			ObjectId B = Commit(ow, treeB, new ObjectId[] { });
			ObjectId O = Commit(ow, treeO, new[] { B });
			ObjectId T = Commit(ow, treeT, new[] { B });

			Merger ourMerger = MergeStrategy.SimpleTwoWayInCore.NewMerger(db);
			bool merge = ourMerger.Merge(new[] { O, T });
			Assert.False(merge);
		}

		[Fact(Timeout = 30000)]
		public void TestTrivialTwoWayLeftDFconflict1()
		{
			DirCache treeB = DirCache.read(db);
			DirCache treeO = DirCache.read(db);
			DirCache treeT = DirCache.read(db);
			{
				DirCacheBuilder b = treeB.builder();
				DirCacheBuilder o = treeO.builder();
				DirCacheBuilder t = treeT.builder();

				b.add(MakeEntry("d/o", FileMode.RegularFile));
				b.add(MakeEntry("d/t", FileMode.RegularFile));

				o.add(MakeEntry("d", FileMode.RegularFile));

				t.add(MakeEntry("d/o", FileMode.RegularFile));
				t.add(MakeEntry("d/t", FileMode.RegularFile, "t !"));

				b.finish();
				o.finish();
				t.finish();
			}

			var ow = new ObjectWriter(db);
			ObjectId B = Commit(ow, treeB, new ObjectId[] { });
			ObjectId O = Commit(ow, treeO, new[] { B });
			ObjectId T = Commit(ow, treeT, new[] { B });

			Merger ourMerger = MergeStrategy.SimpleTwoWayInCore.NewMerger(db);
			bool merge = ourMerger.Merge(new[] { O, T });
			Assert.False(merge);
		}

		[Fact(Timeout = 30000)]
		public void TestTrivialTwoWayRightDFconflict1()
		{
			DirCache treeB = DirCache.read(db);
			DirCache treeO = DirCache.read(db);
			DirCache treeT = DirCache.read(db);
			{
				DirCacheBuilder b = treeB.builder();
				DirCacheBuilder o = treeO.builder();
				DirCacheBuilder t = treeT.builder();

				b.add(MakeEntry("d/o", FileMode.RegularFile));
				b.add(MakeEntry("d/t", FileMode.RegularFile));

				o.add(MakeEntry("d/o", FileMode.RegularFile));
				o.add(MakeEntry("d/t", FileMode.RegularFile, "o !"));

				t.add(MakeEntry("d", FileMode.RegularFile));

				b.finish();
				o.finish();
				t.finish();
			}

			var ow = new ObjectWriter(db);
			ObjectId B = Commit(ow, treeB, new ObjectId[] { });
			ObjectId O = Commit(ow, treeO, new[] { B });
			ObjectId T = Commit(ow, treeT, new[] { B });

			Merger ourMerger = MergeStrategy.SimpleTwoWayInCore.NewMerger(db);
			bool merge = ourMerger.Merge(new[] { O, T });
			Assert.False(merge);
		}

		[Fact(Timeout = 30000)]
		public void TestTrivialTwoWayLeftDFconflict2()
		{
			DirCache treeB = DirCache.read(db);
			DirCache treeO = DirCache.read(db);
			DirCache treeT = DirCache.read(db);
			{
				DirCacheBuilder b = treeB.builder();
				DirCacheBuilder o = treeO.builder();
				DirCacheBuilder t = treeT.builder();

				b.add(MakeEntry("d", FileMode.RegularFile));

				o.add(MakeEntry("d", FileMode.RegularFile, "o !"));

				t.add(MakeEntry("d/o", FileMode.RegularFile));

				b.finish();
				o.finish();
				t.finish();
			}

			var ow = new ObjectWriter(db);
			ObjectId B = Commit(ow, treeB, new ObjectId[] { });
			ObjectId O = Commit(ow, treeO, new[] { B });
			ObjectId T = Commit(ow, treeT, new[] { B });

			Merger ourMerger = MergeStrategy.SimpleTwoWayInCore.NewMerger(db);
			bool merge = ourMerger.Merge(new[] { O, T });
			Assert.False(merge);
		}

		[Fact(Timeout = 30000)]
		public void TestTrivialTwoWayRightDFconflict2()
		{
			DirCache treeB = DirCache.read(db);
			DirCache treeO = DirCache.read(db);
			DirCache treeT = DirCache.read(db);
			{
				DirCacheBuilder b = treeB.builder();
				DirCacheBuilder o = treeO.builder();
				DirCacheBuilder t = treeT.builder();

				b.add(MakeEntry("d", FileMode.RegularFile));

				o.add(MakeEntry("d/o", FileMode.RegularFile));

				t.add(MakeEntry("d", FileMode.RegularFile, "t !"));

				b.finish();
				o.finish();
				t.finish();
			}

			var ow = new ObjectWriter(db);
			ObjectId B = Commit(ow, treeB, new ObjectId[] { });
			ObjectId O = Commit(ow, treeO, new[] { B });
			ObjectId T = Commit(ow, treeT, new[] { B });

			Merger ourMerger = MergeStrategy.SimpleTwoWayInCore.NewMerger(db);
			bool merge = ourMerger.Merge(new[] { O, T });
			Assert.False(merge);
		}

		private static void AssertCorrectId(DirCache treeT, GitSharp.TreeWalk.TreeWalk tw)
		{
			Assert.Equal(treeT.getEntry(tw.getPathString()).getObjectId(), tw.getObjectId(0));
		}

		private ObjectId Commit(ObjectWriter ow, DirCache treeB, ObjectId[] parentIds)
		{
			var c = new Commit(db) { TreeId = treeB.writeTree(ow), Author = new PersonIdent("A U Thor", "a.u.thor", 1L, 0) };
			c.Committer = c.Author;
			c.ParentIds = parentIds;
			c.Message = "Tree " + c.TreeId.Name;
			return ow.WriteCommit(c);
		}

		private DirCacheEntry MakeEntry(string path, FileMode mode)
		{
			return MakeEntry(path, mode, path);
		}

		private DirCacheEntry MakeEntry(string path, FileMode mode, String content)
		{
			var ent = new DirCacheEntry(path);
			ent.setFileMode(mode);
			byte[] contentBytes = Constants.encode(content);
			ent.setObjectId(new ObjectWriter(db).ComputeBlobSha1(contentBytes.Length, new MemoryStream(contentBytes)));
			return ent;
		}
	}
}