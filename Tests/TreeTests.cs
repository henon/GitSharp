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
using System.Text;
using GitSharp.Tests.Util;
using GitSharp.Util;
using Xunit;

namespace GitSharp.Tests
{
	public class TreeTests : RepositoryTestCase
	{
		private static readonly ObjectId SomeFakeId = ObjectId.FromString("0123456789abcdef0123456789abcdef01234567");

		private static int CompareNamesUsingSpecialCompare(string a, string b)
		{
			char lasta = '\0';
			if (a.Length > 0 && a[a.Length - 1] == '/')
			{
				lasta = '/';
				a = a.Slice(0, a.Length - 1);
			}
			byte[] abytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(a);
			char lastb = '\0';
			if (b.Length > 0 && b[b.Length - 1] == '/')
			{
				lastb = '/';
				b = b.Slice(0, b.Length - 1);
			}
			byte[] bbytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(b);
			return Tree.CompareNames(abytes, bbytes, lasta, lastb);
		}

		[Fact]
		public void test000_sort_01()
		{
			Assert.Equal(0, CompareNamesUsingSpecialCompare("a", "a"));
		}

		[Fact]
		public void test000_sort_02()
		{
			Assert.Equal(-1, CompareNamesUsingSpecialCompare("a", "b"));
			Assert.Equal(1, CompareNamesUsingSpecialCompare("b", "a"));
		}

		[Fact]
		public void test000_sort_03()
		{
			Assert.Equal(1, CompareNamesUsingSpecialCompare("a:", "a"));
			Assert.Equal(1, CompareNamesUsingSpecialCompare("a/", "a"));
			Assert.Equal(-1, CompareNamesUsingSpecialCompare("a", "a/"));
			Assert.Equal(-1, CompareNamesUsingSpecialCompare("a", "a:"));
			Assert.Equal(1, CompareNamesUsingSpecialCompare("a:", "a/"));
			Assert.Equal(-1, CompareNamesUsingSpecialCompare("a/", "a:"));
		}

		[Fact]
		public void test000_sort_04()
		{
			Assert.Equal(-1, CompareNamesUsingSpecialCompare("a.a", "a/a"));
			Assert.Equal(1, CompareNamesUsingSpecialCompare("a/a", "a.a"));
		}

		[Fact]
		public void test000_sort_05()
		{
			Assert.Equal(-1, CompareNamesUsingSpecialCompare("a.", "a/"));
			Assert.Equal(1, CompareNamesUsingSpecialCompare("a/", "a."));
		}

		[Fact]
		public void test001_createEmpty()
		{
			var t = new Tree(db);
			Assert.True(t.IsLoaded);
			Assert.True(t.IsModified);
			Assert.True(t.Parent == null);
			Assert.True(t.IsRoot);
			Assert.True(t.Name == null);
			Assert.True(t.NameUTF8 == null);
			Assert.True(t.Members != null);
			Assert.True(t.Members.Length == 0);
			Assert.Equal(string.Empty, t.FullName);
			Assert.True(t.Id == null);
			Assert.True(t.TreeEntry == t);
			Assert.True(t.Repository == db);
			Assert.True(t.FindTreeMember("foo") == null);
			Assert.True(t.FindBlobMember("foo") == null);
		}

		[Fact]
		public void test002_addFile()
		{
			var t = new Tree(db) { Id = SomeFakeId };
			Assert.True(t.Id != null);
			Assert.False(t.IsModified);

			const string n = "bob";
			FileTreeEntry f = t.AddFile(n);
			Assert.NotNull(f);
			Assert.Equal(n, f.Name);
            Assert.Equal(f.Name, Constants.CHARSET.GetString(f.NameUTF8));
			Assert.Equal(n, f.FullName);
			Assert.True(f.Id == null);
			Assert.True(t.IsModified);
			Assert.True(t.Id == null);
			Assert.True(t.FindBlobMember(f.Name) == f);

			TreeEntry[] i = t.Members;
			Assert.NotNull(i);
			Assert.True(i != null && i.Length > 0);
			Assert.True(i != null && i[0] == f);
			Assert.True(i != null && i.Length == 1);
		}

		[Fact]
		public void test004_addTree()
		{
			var t = new Tree(db) {Id = SomeFakeId};
			Assert.True(t.Id != null);
			Assert.False(t.IsModified);

			const string n = "bob";
			Tree f = t.AddTree(n);
			Assert.NotNull(f);
			Assert.Equal(n, f.Name);
            Assert.Equal(f.Name, Constants.CHARSET.GetString(f.NameUTF8));
			Assert.Equal(n, f.FullName);
			Assert.True(f.Id == null);
			Assert.True(f.Parent == t);
			Assert.True(f.Repository == db);
			Assert.True(f.IsLoaded);
			Assert.False(f.Members.Length > 0);
			Assert.False(f.IsRoot);
			Assert.True(f.TreeEntry == f);
			Assert.True(t.IsModified);
			Assert.True(t.Id == null);
			Assert.True(t.FindTreeMember(f.Name) == f);

			TreeEntry[] i = t.Members;
			Assert.True(i.Length > 0);
			Assert.True(i[0] == f);
			Assert.True(i.Length == 1);
		}

		[Fact]
		public void test005_addRecursiveFile()
		{
			var t = new Tree(db);
			FileTreeEntry f = t.AddFile("a/b/c");
			Assert.NotNull(f);
			Assert.Equal(f.Name, "c");
			Assert.Equal(f.Parent.Name, "b");
			Assert.Equal(f.Parent.Parent.Name, "a");
			Assert.True(t == f.Parent.Parent.Parent, "t is great-grandparent");
		}

		[Fact]
		public void test005_addRecursiveTree()
		{
			var t = new Tree(db);
			Tree f = t.AddTree("a/b/c");
			Assert.NotNull(f);
			Assert.Equal(f.Name, "c");
			Assert.Equal(f.Parent.Name, "b");
			Assert.Equal(f.Parent.Parent.Name, "a");
			Assert.True(t == f.Parent.Parent.Parent, "t is great-grandparent");
		}

		[Fact]
		public void test006_addDeepTree()
		{
			var t = new Tree(db);

			Tree e = t.AddTree("e");
			Assert.NotNull(e);
			Assert.True(e.Parent == t);
			Tree f = t.AddTree("f");
			Assert.NotNull(f);
			Assert.True(f.Parent == t);
			Tree g = f.AddTree("g");
			Assert.NotNull(g);
			Assert.True(g.Parent == f);
			Tree h = g.AddTree("h");
			Assert.NotNull(h);
			Assert.True(h.Parent == g);

			h.Id = SomeFakeId;
			Assert.True(!h.IsModified);
			g.Id = SomeFakeId;
			Assert.True(!g.IsModified);
			f.Id = SomeFakeId;
			Assert.True(!f.IsModified);
			e.Id = SomeFakeId;
			Assert.True(!e.IsModified);
			t.Id = SomeFakeId;
			Assert.True(!t.IsModified);

			Assert.Equal("f/g/h", h.FullName);
			Assert.True(t.FindTreeMember(h.FullName) == h);
			Assert.True(t.FindBlobMember("f/z") == null);
			Assert.True(t.FindBlobMember("y/z") == null);

			FileTreeEntry i = h.AddFile("i");
			Assert.NotNull(i);
			Assert.Equal("f/g/h/i", i.FullName);
			Assert.True(t.FindBlobMember(i.FullName) == i);
			Assert.True(h.IsModified);
			Assert.True(g.IsModified);
			Assert.True(f.IsModified);
			Assert.True(!e.IsModified);
			Assert.True(t.IsModified);

			Assert.True(h.Id == null);
			Assert.True(g.Id == null);
			Assert.True(f.Id == null);
			Assert.True(e.Id != null);
			Assert.True(t.Id == null);
		}

		[Fact]
		public void test007_manyFileLookup()
		{
			var t = new Tree(db);
			var files = new List<FileTreeEntry>(26 * 26);
			for (char level1 = 'a'; level1 <= 'z'; level1++)
			{
				for (char level2 = 'a'; level2 <= 'z'; level2++)
				{
					String n = "." + level1 + level2 + "9";
					FileTreeEntry f = t.AddFile(n);
					Assert.NotNull(f);
					Assert.Equal(n, f.Name);
					files.Add(f);
				}
			}
			Assert.Equal(files.Count, t.MemberCount);
			TreeEntry[] ents = t.Members;
			Assert.NotNull(ents);
			Assert.Equal(files.Count, ents.Length);
			for (int k = 0; k < ents.Length; k++)
			{
				Assert.True(files[k] == ents[k], "File " + files[k].Name + " is at " + k + ".");
			}
		}

		[Fact]
		public void test008_SubtreeInternalSorting()
		{
			var t = new Tree(db);
			FileTreeEntry e0 = t.AddFile("a-b");
			FileTreeEntry e1 = t.AddFile("a-");
			FileTreeEntry e2 = t.AddFile("a=b");
			Tree e3 = t.AddTree("a");
			FileTreeEntry e4 = t.AddFile("a=");

			TreeEntry[] ents = t.Members;
			Assert.Same(e1, ents[0]);
			Assert.Same(e0, ents[1]);
			Assert.Same(e3, ents[2]);
			Assert.Same(e4, ents[3]);
			Assert.Same(e2, ents[4]);
		}
	}
}