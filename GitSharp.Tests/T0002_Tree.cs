/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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

using System;
using System.Collections.Generic;
using GitSharp.Core;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests
{
	[TestFixture]
    public class T0002_Tree : SampleDataRepositoryTestCase
	{
		private static readonly ObjectId SomeFakeId = ObjectId.FromString("0123456789abcdef0123456789abcdef01234567");

		private static int CompareNamesUsingSpecialCompare(String a, String b)
		{
			char lasta = '\0';
			if (a.Length > 0 && a[a.Length - 1] == '/')
			{
				lasta = '/';
				a = a.Slice(0, a.Length - 1);
			}
			byte[] abytes = a.getBytes("ISO-8859-1");
			char lastb = '\0';
			if (b.Length > 0 && b[b.Length - 1] == '/')
			{
				lastb = '/';
				b = b.Slice(0, b.Length - 1);
			}
			byte[] bbytes = b.getBytes("ISO-8859-1");
            return Core.Tree.CompareNames(abytes, bbytes, lasta, lastb);
		}

		[Test]
		public void test000_sort_01()
		{
			Assert.AreEqual(0, CompareNamesUsingSpecialCompare("a", "a"));
		}

		[Test]
		public void test000_sort_02()
		{
			Assert.AreEqual(-1, CompareNamesUsingSpecialCompare("a", "b"));
			Assert.AreEqual(1, CompareNamesUsingSpecialCompare("b", "a"));
		}

		[Test]
		public void test000_sort_03()
		{
			Assert.AreEqual(1, CompareNamesUsingSpecialCompare("a:", "a"));
			Assert.AreEqual(1, CompareNamesUsingSpecialCompare("a/", "a"));
			Assert.AreEqual(-1, CompareNamesUsingSpecialCompare("a", "a/"));
			Assert.AreEqual(-1, CompareNamesUsingSpecialCompare("a", "a:"));
			Assert.AreEqual(1, CompareNamesUsingSpecialCompare("a:", "a/"));
			Assert.AreEqual(-1, CompareNamesUsingSpecialCompare("a/", "a:"));
		}

		[Test]
		public void test000_sort_04()
		{
			Assert.AreEqual(-1, CompareNamesUsingSpecialCompare("a.a", "a/a"));
			Assert.AreEqual(1, CompareNamesUsingSpecialCompare("a/a", "a.a"));
		}

		[Test]
		public void test000_sort_05()
		{
			Assert.AreEqual(-1, CompareNamesUsingSpecialCompare("a.", "a/"));
			Assert.AreEqual(1, CompareNamesUsingSpecialCompare("a/", "a."));
		}

		[Test]
		public void test001_createEmpty()
		{
            var t = new Core.Tree(db);
			Assert.IsTrue(t.IsLoaded);
			Assert.IsTrue(t.IsModified);
			Assert.IsTrue(t.Parent == null);
			Assert.IsTrue(t.IsRoot);
			Assert.IsTrue(t.Name == null);
			Assert.IsTrue(t.NameUTF8 == null);
			Assert.IsTrue(t.Members != null);
			Assert.IsTrue(t.Members.Length == 0);
			Assert.AreEqual(string.Empty, t.FullName);
			Assert.IsTrue(t.Id == null);
			Assert.IsTrue(t.TreeEntry == t);
			Assert.IsTrue(t.Repository == db);
			Assert.IsTrue(t.findTreeMember("foo") == null);
			Assert.IsTrue(t.FindBlobMember("foo") == null);
		}

		[Test]
		public void test002_addFile()
		{
            var t = new Core.Tree(db) { Id = SomeFakeId };
			Assert.IsTrue(t.Id != null);
			Assert.IsFalse(t.IsModified);

			const string n = "bob";
			FileTreeEntry f = t.AddFile(n);
			Assert.IsNotNull(f);
			Assert.AreEqual(n, f.Name);
            Assert.AreEqual(f.Name, Constants.CHARSET.GetString(f.NameUTF8));
			Assert.AreEqual(n, f.FullName);
			Assert.IsTrue(f.Id == null);
			Assert.IsTrue(t.IsModified);
			Assert.IsTrue(t.Id == null);
			Assert.IsTrue(t.FindBlobMember(f.Name) == f);

			TreeEntry[] i = t.Members;
			Assert.IsNotNull(i);
			Assert.IsTrue(i != null && i.Length > 0);
			Assert.IsTrue(i != null && i[0] == f);
			Assert.IsTrue(i != null && i.Length == 1);
		}

		[Test]
		public void test004_addTree()
		{
            var t = new Core.Tree(db) { Id = SomeFakeId };
			Assert.IsTrue(t.Id != null);
			Assert.IsFalse(t.IsModified);

			const string n = "bob";
            Core.Tree f = t.AddTree(n);
			Assert.IsNotNull(f);
			Assert.AreEqual(n, f.Name);
            Assert.AreEqual(f.Name, Constants.CHARSET.GetString(f.NameUTF8));
			Assert.AreEqual(n, f.FullName);
			Assert.IsTrue(f.Id == null);
			Assert.IsTrue(f.Parent == t);
			Assert.IsTrue(f.Repository == db);
			Assert.IsTrue(f.IsLoaded);
			Assert.IsFalse(f.Members.Length > 0);
			Assert.IsFalse(f.IsRoot);
			Assert.IsTrue(f.TreeEntry == f);
			Assert.IsTrue(t.IsModified);
			Assert.IsTrue(t.Id == null);
			Assert.IsTrue(t.findTreeMember(f.Name) == f);

			TreeEntry[] i = t.Members;
			Assert.IsTrue(i.Length > 0);
			Assert.IsTrue(i[0] == f);
			Assert.IsTrue(i.Length == 1);
		}

		[Test]
		public void test005_addRecursiveFile()
		{
            var t = new Core.Tree(db);
			FileTreeEntry f = t.AddFile("a/b/c");
			Assert.IsNotNull(f);
			Assert.AreEqual(f.Name, "c");
			Assert.AreEqual(f.Parent.Name, "b");
			Assert.AreEqual(f.Parent.Parent.Name, "a");
			Assert.IsTrue(t == f.Parent.Parent.Parent, "t is great-grandparent");
		}

		[Test]
		public void test005_addRecursiveTree()
		{
            var t = new Core.Tree(db);
            Core.Tree f = t.AddTree("a/b/c");
			Assert.IsNotNull(f);
			Assert.AreEqual(f.Name, "c");
			Assert.AreEqual(f.Parent.Name, "b");
			Assert.AreEqual(f.Parent.Parent.Name, "a");
			Assert.IsTrue(t == f.Parent.Parent.Parent, "t is great-grandparent");
		}

		[Test]
		public void test006_addDeepTree()
		{
            var t = new Core.Tree(db);

            Core.Tree e = t.AddTree("e");
			Assert.IsNotNull(e);
			Assert.IsTrue(e.Parent == t);
            Core.Tree f = t.AddTree("f");
			Assert.IsNotNull(f);
			Assert.IsTrue(f.Parent == t);
            Core.Tree g = f.AddTree("g");
			Assert.IsNotNull(g);
			Assert.IsTrue(g.Parent == f);
            Core.Tree h = g.AddTree("h");
			Assert.IsNotNull(h);
			Assert.IsTrue(h.Parent == g);

			h.Id = SomeFakeId;
			Assert.IsTrue(!h.IsModified);
			g.Id = SomeFakeId;
			Assert.IsTrue(!g.IsModified);
			f.Id = SomeFakeId;
			Assert.IsTrue(!f.IsModified);
			e.Id = SomeFakeId;
			Assert.IsTrue(!e.IsModified);
			t.Id = SomeFakeId;
			Assert.IsTrue(!t.IsModified);

			Assert.AreEqual("f/g/h", h.FullName);
			Assert.IsTrue(t.findTreeMember(h.FullName) == h);
			Assert.IsTrue(t.FindBlobMember("f/z") == null);
			Assert.IsTrue(t.FindBlobMember("y/z") == null);

			FileTreeEntry i = h.AddFile("i");
			Assert.IsNotNull(i);
			Assert.AreEqual("f/g/h/i", i.FullName);
			Assert.IsTrue(t.FindBlobMember(i.FullName) == i);
			Assert.IsTrue(h.IsModified);
			Assert.IsTrue(g.IsModified);
			Assert.IsTrue(f.IsModified);
			Assert.IsTrue(!e.IsModified);
			Assert.IsTrue(t.IsModified);

			Assert.IsTrue(h.Id == null);
			Assert.IsTrue(g.Id == null);
			Assert.IsTrue(f.Id == null);
			Assert.IsTrue(e.Id != null);
			Assert.IsTrue(t.Id == null);
		}

		[Test]
		public void test007_manyFileLookup()
		{
            var t = new Core.Tree(db);
			var files = new List<FileTreeEntry>(26 * 26);
			for (char level1 = 'a'; level1 <= 'z'; level1++)
			{
				for (char level2 = 'a'; level2 <= 'z'; level2++)
				{
					String n = "." + level1 + level2 + "9";
					FileTreeEntry f = t.AddFile(n);
					Assert.IsNotNull(f, "File " + n + " added.");
					Assert.AreEqual(n, f.Name);
					files.Add(f);
				}
			}
			Assert.AreEqual(files.Count, t.MemberCount);
			TreeEntry[] ents = t.Members;
			Assert.IsNotNull(ents);
			Assert.AreEqual(files.Count, ents.Length);
			for (int k = 0; k < ents.Length; k++)
			{
				Assert.IsTrue(files[k] == ents[k], "File " + files[k].Name + " is at " + k + ".");
			}
		}

		[Test]
		public void test008_SubtreeInternalSorting()
		{
            var t = new Core.Tree(db);
			FileTreeEntry e0 = t.AddFile("a-b");
			FileTreeEntry e1 = t.AddFile("a-");
			FileTreeEntry e2 = t.AddFile("a=b");
            Core.Tree e3 = t.AddTree("a");
			FileTreeEntry e4 = t.AddFile("a=");

			TreeEntry[] ents = t.Members;
			Assert.AreSame(e1, ents[0]);
			Assert.AreSame(e0, ents[1]);
			Assert.AreSame(e3, ents[2]);
			Assert.AreSame(e4, ents[3]);
			Assert.AreSame(e2, ents[4]);
		}

        [Test]
	    public void test009_SymlinkAndGitlink()
	    {
            Core.Tree symlinkTree = db.MapTree("symlink");
	        Assert.IsTrue(symlinkTree.ExistsBlob("symlink.txt"), "Symlink entry exists");
            Core.Tree gitlinkTree = db.MapTree("gitlink");
	        Assert.IsTrue(gitlinkTree.ExistsBlob("submodule"), "Gitlink entry exists");
	    }
	}
}