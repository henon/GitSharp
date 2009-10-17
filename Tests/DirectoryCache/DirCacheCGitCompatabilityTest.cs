/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

using System.Collections.Generic;
using System.IO;
using GitSharp.Core;
using GitSharp.Core.DirectoryCache;
using GitSharp.Core.Util;
using NUnit.Framework;
using FileMode=GitSharp.Core.FileMode;


namespace GitSharp.Tests.DirectoryCache
{
	[TestFixture]
	public class DirCacheCGitCompatabilityTest : RepositoryTestCase
	{
		private readonly FileInfo _index = new FileInfo("Resources/gitgit.index");

		[Test]
		public void testReadIndex_LsFiles()
		{
			List<CGitIndexRecord> ls = ReadLsFiles();
			var dc = new DirCache(_index);
			Assert.AreEqual(0, dc.getEntryCount());
			dc.read();
			Assert.AreEqual(ls.Count, dc.getEntryCount());
			
			var cache = new List<DirCacheEntry>();
			
			for (int i = 0; i < dc.getEntryCount(); i++)
			{
				cache.Add(dc.getEntry(i));
			}
			
			ls.Sort((x, y) => x.Path.CompareTo(y.Path));
			cache.Sort((x, y) => x.getPathString().CompareTo(y.getPathString()));
			
			int j = 0;
			foreach (var val in ls)
			{
				AssertAreEqual(val, cache[j]);
				j++;
			}
		}

		[Test]
		public void testTreeWalk_LsFiles()
		{
			List<CGitIndexRecord> ls = ReadLsFiles();
			var dc = new DirCache(_index);
			Assert.AreEqual(0, dc.getEntryCount());
			dc.read();
			Assert.AreEqual(ls.Count, dc.getEntryCount());

			var rItr = ls.GetEnumerator();
			var tw = new GitSharp.Core.TreeWalk.TreeWalk(db);
			tw.reset();
			tw.Recursive = true;
			tw.addTree(new DirCacheIterator(dc));
			while (rItr.MoveNext())
			{
				Assert.IsTrue(tw.next());
				var dcItr = tw.getTree<DirCacheIterator>(0, typeof(DirCacheIterator));
				Assert.IsNotNull(dcItr);
				AssertAreEqual(rItr.Current, dcItr.getDirCacheEntry());
			}
		}

		[Test]
		public void testReadIndex_DirCacheTree()
		{
			List<CGitIndexRecord> cList = ReadLsFiles();
			List<CGitLsTreeRecord> cTree = ReadLsTree();
			var dc = new DirCache(_index);
			Assert.AreEqual(0, dc.getEntryCount());
			dc.read();
			Assert.AreEqual(cList.Count, dc.getEntryCount());

			DirCacheTree jTree = dc.getCacheTree(false);
			Assert.IsNotNull(jTree);
			Assert.AreEqual(string.Empty, jTree.getNameString());
			Assert.AreEqual(string.Empty, jTree.getPathString());
			Assert.IsTrue(jTree.isValid());
			Assert.AreEqual(ObjectId
					.FromString("698dd0b8d0c299f080559a1cffc7fe029479a408"), jTree
					.getObjectId());
			Assert.AreEqual(cList.Count, jTree.getEntrySpan());

			var subtrees = new List<CGitLsTreeRecord>();
			foreach (CGitLsTreeRecord r in cTree)
			{
				if (FileMode.Tree.Equals(r.Mode))
					subtrees.Add(r);
			}
			Assert.AreEqual(subtrees.Count, jTree.getChildCount());

			for (int i = 0; i < jTree.getChildCount(); i++)
			{
				DirCacheTree sj = jTree.getChild(i);
				CGitLsTreeRecord sc = subtrees[i];
				Assert.AreEqual(sc.Path, sj.getNameString());
				Assert.AreEqual(sc.Path + "/", sj.getPathString());
				Assert.IsTrue(sj.isValid());
				Assert.AreEqual(sc.Id, sj.getObjectId());
			}
		}

		private static void AssertAreEqual(CGitIndexRecord c, DirCacheEntry j)
		{
			Assert.IsNotNull(c);
			Assert.IsNotNull(j);

			Assert.AreEqual(c.Path, j.getPathString());
			Assert.AreEqual(c.Id, j.getObjectId());
			Assert.AreEqual(c.Mode, j.getRawMode());
			Assert.AreEqual(c.Stage, j.getStage());
		}

		private static List<CGitIndexRecord> ReadLsFiles()
		{
			var r = new List<CGitIndexRecord>();
			using (var br = new StreamReader(new FileStream("Resources/gitgit.lsfiles", System.IO.FileMode.Open, FileAccess.Read), Constants.CHARSET))
			{
				string line;
				while (!string.IsNullOrEmpty(line = br.ReadLine()))
				{
					var cr = new CGitIndexRecord(line);
					r.Add(cr);
				}
			}
			return r;
		}

		private static List<CGitLsTreeRecord> ReadLsTree()
		{
			var r = new List<CGitLsTreeRecord>();
			using (var br = new StreamReader(new FileStream("Resources/gitgit.lstree", System.IO.FileMode.Open, System.IO.FileAccess.Read), Constants.CHARSET))
			{
				string line;
				while ((line = br.ReadLine()) != null)
				{
					r.Add(new CGitLsTreeRecord(line));
				}
			}
			
			return r;
		}

		#region Nested Types

		private class CGitIndexRecord
		{
			public int Mode { get; private set; }
			public ObjectId Id { get; private set; }
			public int Stage { get; private set; }
			public string Path { get; private set; }

			public CGitIndexRecord(string line)
			{
				int tab = line.IndexOf('\t');
				int sp1 = line.IndexOf(' ');
				int sp2 = line.IndexOf(' ', sp1 + 1);
				Mode = NB.BaseToDecimal(line.Slice(0, sp1), 8);
				Id = ObjectId.FromString(line.Slice(sp1 + 1, sp2));
				Stage = int.Parse(line.Slice(sp2 + 1, tab));
				Path = line.Substring(tab + 1);
			}
		}

		private class CGitLsTreeRecord
		{
			public int Mode { get; private set; }
			public ObjectId Id { get; private set; }
			public string Path { get; private set; }

			public CGitLsTreeRecord(string line)
			{
				int tab = line.IndexOf('\t');
				int sp1 = line.IndexOf(' ');
				int sp2 = line.IndexOf(' ', sp1 + 1);
				Mode = NB.BaseToDecimal(line.Slice(0, sp1), 8);
				Id = ObjectId.FromString(line.Slice(sp2 + 1, tab));
				Path = line.Substring(tab + 1);
			}
		}

		#endregion
	}
}