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
using System.Linq;
using System.Text;
using NUnit.Framework;
using Gitty.Core;
using Gitty.Core.Util;

namespace Gitty.Core.Tests
{
    [TestFixture]
    public class TreeTests : RepositoryTestCase
    {
        private static ObjectId SOME_FAKE_ID = ObjectId.FromString("0123456789abcdef0123456789abcdef01234567");

        private int compareNamesUsingSpecialCompare(String a, String b)
        {
            char lasta = '\0';
            byte[] abytes;
            if (a.Length > 0 && a[a.Length - 1] == '/')
            {
                lasta = '/';
                a = a.Slice(0, a.Length - 1);
            }
            abytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(a);
            char lastb = '\0';
            byte[] bbytes;
            if (b.Length > 0 && b[b.Length - 1] == '/')
            {
                lastb = '/';
                b = b.Slice(0, b.Length - 1);
            }
            bbytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(b);
            return Tree.CompareNames(abytes, bbytes, lasta, lastb);
        }

        [Test]
        public void test000_sort_01()
        {
            Assert.AreEqual(0, compareNamesUsingSpecialCompare("a", "a"));
        }

        [Test]
        public void test000_sort_02()
        {
            Assert.AreEqual(-1, compareNamesUsingSpecialCompare("a", "b"));
            Assert.AreEqual(1, compareNamesUsingSpecialCompare("b", "a"));
        }

        [Test]
        public void test000_sort_03()
        {
            Assert.AreEqual(1, compareNamesUsingSpecialCompare("a:", "a"));
            Assert.AreEqual(1, compareNamesUsingSpecialCompare("a/", "a"));
            Assert.AreEqual(-1, compareNamesUsingSpecialCompare("a", "a/"));
            Assert.AreEqual(-1, compareNamesUsingSpecialCompare("a", "a:"));
            Assert.AreEqual(1, compareNamesUsingSpecialCompare("a:", "a/"));
            Assert.AreEqual(-1, compareNamesUsingSpecialCompare("a/", "a:"));
        }

        [Test]
        public void test000_sort_04()
        {
            Assert.AreEqual(-1, compareNamesUsingSpecialCompare("a.a", "a/a"));
            Assert.AreEqual(1, compareNamesUsingSpecialCompare("a/a", "a.a"));
        }

        [Test]
        public void test000_sort_05()
        {
            Assert.AreEqual(-1, compareNamesUsingSpecialCompare("a.", "a/"));
            Assert.AreEqual(1, compareNamesUsingSpecialCompare("a/", "a."));

        }

        [Test]
        public void test001_createEmpty()
        {
            Tree t = new Tree(db);
            Assert.IsTrue(t.IsLoaded);
            Assert.IsTrue( t.IsModified);
            Assert.IsTrue( t.Parent == null);
            Assert.IsTrue(t.IsRoot);
            Assert.IsTrue( t.Name == null);
            Assert.IsTrue( t.NameUTF8 == null);
            Assert.IsTrue( t.Members != null);
            Assert.IsTrue( t.Members.Length == 0);
            Assert.AreEqual( "", t.FullName);
            Assert.IsTrue( t.Id == null);
            Assert.IsTrue( t.TreeEntry == t);
            Assert.IsTrue( t.Repository == db);
            Assert.IsTrue( t.findTreeMember("foo") == null);
            Assert.IsTrue( t.FindBlobMember("foo") == null);
        }
#if false
        [Test]
        public void test002_addFile()
        {
            Tree t = new Tree(db);
            t.setId(SOME_FAKE_ID);
            Assert.IsTrue("has id", t.getId() != null);
            Assert.IsFalse("not modified", t.isModified());

            String n = "bob";
            FileTreeEntry f = t.addFile(n);
            Assert.IsNotNull("have file", f);
            Assert.AreEqual("name matches", n, f.getName());
            Assert.AreEqual("name matches", f.getName(), new String(f.getNameUTF8(),
                    "UTF-8"));
            Assert.AreEqual("full name matches", n, f.getFullName());
            Assert.IsTrue("no id", f.getId() == null);
            Assert.IsTrue("is modified", t.isModified());
            Assert.IsTrue("has no id", t.getId() == null);
            Assert.IsTrue("found bob", t.findBlobMember(f.getName()) == f);

            TreeEntry[] i = t.members();
            Assert.IsNotNull("members array not null", i);
            Assert.IsTrue("iterator is not empty", i != null && i.length > 0);
            Assert.IsTrue("iterator returns file", i != null && i[0] == f);
            Assert.IsTrue("iterator is empty", i != null && i.length == 1);
        }

        [Test]
        public void test004_addTree()
        {
            Tree t = new Tree(db);
            t.setId(SOME_FAKE_ID);
            Assert.IsTrue("has id", t.getId() != null);
            Assert.IsFalse("not modified", t.isModified());

            String n = "bob";
            Tree f = t.addTree(n);
            Assert.IsNotNull("have tree", f);
            Assert.AreEqual("name matches", n, f.getName());
            Assert.AreEqual("name matches", f.getName(), new String(f.getNameUTF8(),
                    "UTF-8"));
            Assert.AreEqual("full name matches", n, f.getFullName());
            Assert.IsTrue("no id", f.getId() == null);
            Assert.IsTrue("parent matches", f.getParent() == t);
            Assert.IsTrue("repository matches", f.getRepository() == db);
            Assert.IsTrue("isLoaded", f.isLoaded());
            Assert.IsFalse("has items", f.members().length > 0);
            Assert.IsFalse("is root", f.isRoot());
            Assert.IsTrue("tree is self", f.getTree() == f);
            Assert.IsTrue("parent is modified", t.isModified());
            Assert.IsTrue("parent has no id", t.getId() == null);
            Assert.IsTrue("found bob child", t.findTreeMember(f.getName()) == f);

            TreeEntry[] i = t.members();
            Assert.IsTrue("iterator is not empty", i.length > 0);
            Assert.IsTrue("iterator returns file", i[0] == f);
            Assert.IsTrue("iterator is empty", i.length == 1);
        }

        [Test]
        public void test005_addRecursiveFile()
        {
            Tree t = new Tree(db);
            FileTreeEntry f = t.addFile("a/b/c");
            Assert.IsNotNull("created f", f);
            Assert.AreEqual("c", f.getName());
            Assert.AreEqual("b", f.getParent().getName());
            Assert.AreEqual("a", f.getParent().getParent().getName());
            Assert.IsTrue("t is great-grandparent", t == f.getParent().getParent()
                    .getParent());
        }

        [Test]
        public void test005_addRecursiveTree()
        {
            Tree t = new Tree(db);
            Tree f = t.addTree("a/b/c");
            Assert.IsNotNull("created f", f);
            Assert.AreEqual("c", f.getName());
            Assert.AreEqual("b", f.getParent().getName());
            Assert.AreEqual("a", f.getParent().getParent().getName());
            Assert.IsTrue("t is great-grandparent", t == f.getParent().getParent()
                    .getParent());
        }

        [Test]
        public void test006_addDeepTree()
        {
            Tree t = new Tree(db);

            Tree e = t.addTree("e");
            Assert.IsNotNull("have e", e);
            Assert.IsTrue("e.parent == t", e.getParent() == t);
            Tree f = t.addTree("f");
            Assert.IsNotNull("have f", f);
            Assert.IsTrue("f.parent == t", f.getParent() == t);
            Tree g = f.addTree("g");
            Assert.IsNotNull("have g", g);
            Assert.IsTrue("g.parent == f", g.getParent() == f);
            Tree h = g.addTree("h");
            Assert.IsNotNull("have h", h);
            Assert.IsTrue("h.parent = g", h.getParent() == g);

            h.setId(SOME_FAKE_ID);
            Assert.IsTrue("h not modified", !h.isModified());
            g.setId(SOME_FAKE_ID);
            Assert.IsTrue("g not modified", !g.isModified());
            f.setId(SOME_FAKE_ID);
            Assert.IsTrue("f not modified", !f.isModified());
            e.setId(SOME_FAKE_ID);
            Assert.IsTrue("e not modified", !e.isModified());
            t.setId(SOME_FAKE_ID);
            Assert.IsTrue("t not modified.", !t.isModified());

            Assert.AreEqual("full path of h ok", "f/g/h", h.getFullName());
            Assert.IsTrue("Can find h", t.findTreeMember(h.getFullName()) == h);
            Assert.IsTrue("Can't find f/z", t.findBlobMember("f/z") == null);
            Assert.IsTrue("Can't find y/z", t.findBlobMember("y/z") == null);

            FileTreeEntry i = h.addFile("i");
            Assert.IsNotNull(i);
            Assert.AreEqual("full path of i ok", "f/g/h/i", i.getFullName());
            Assert.IsTrue("Can find i", t.findBlobMember(i.getFullName()) == i);
            Assert.IsTrue("h modified", h.isModified());
            Assert.IsTrue("g modified", g.isModified());
            Assert.IsTrue("f modified", f.isModified());
            Assert.IsTrue("e not modified", !e.isModified());
            Assert.IsTrue("t modified", t.isModified());

            Assert.IsTrue("h no id", h.getId() == null);
            Assert.IsTrue("g no id", g.getId() == null);
            Assert.IsTrue("f no id", f.getId() == null);
            Assert.IsTrue("e has id", e.getId() != null);
            Assert.IsTrue("t no id", t.getId() == null);
        }

        [Test]
        public void test007_manyFileLookup()
        {
            Tree t = new Tree(db);
            List<FileTreeEntry> files = new ArrayList<FileTreeEntry>(26 * 26);
            for (char level1 = 'a'; level1 <= 'z'; level1++)
            {
                for (char level2 = 'a'; level2 <= 'z'; level2++)
                {
                    String n = "." + level1 + level2 + "9";
                    FileTreeEntry f = t.addFile(n);
                    Assert.IsNotNull("File " + n + " added.", f);
                    Assert.AreEqual(n, f.getName());
                    files.add(f);
                }
            }
            Assert.AreEqual(files.size(), t.memberCount());
            TreeEntry[] ents = t.members();
            Assert.IsNotNull(ents);
            Assert.AreEqual(files.size(), ents.length);
            for (int k = 0; k < ents.length; k++)
            {
                Assert.IsTrue("File " + files.get(k).getName()
                        + " is at " + k + ".", files.get(k) == ents[k]);
            }
        }

        [Test]
        public void test008_SubtreeInternalSorting()
        {
            Tree t = new Tree(db);
            FileTreeEntry e0 = t.addFile("a-b");
            FileTreeEntry e1 = t.addFile("a-");
            FileTreeEntry e2 = t.addFile("a=b");
            Tree e3 = t.addTree("a");
            FileTreeEntry e4 = t.addFile("a=");

            TreeEntry[] ents = t.members();
            Assert.AreSame(e1, ents[0]);
            Assert.AreSame(e0, ents[1]);
            Assert.AreSame(e3, ents[2]);
            Assert.AreSame(e4, ents[3]);
            Assert.AreSame(e2, ents[4]);
        }
#endif
    }
}