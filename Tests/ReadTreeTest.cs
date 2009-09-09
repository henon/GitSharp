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

using System;
using System.Collections.Generic;
using System.IO;
using GitSharp.Exceptions;
using GitSharp.Util;
using Xunit;

namespace GitSharp.Tests
{
    public class ReadTreeTest : RepositoryTestCase
    {
        /*
             * Directory/File Conflict cases:
             * It's entirely possible that in practice a number of these may be equivalent
             * to the cases described in git-Read-tree.txt. As long as it does the right thing,
             * that's all I care about. These are basically reverse-engineered from
             * what git currently does. If there are tests for these in git, it's kind of
             * hard to track them all down...
             * 
             *     H        I       M     Clean     H==M     H==I    I==M         Result
             *     ------------------------------------------------------------------
             *1    D        D       F       Y         N       Y       N           Update
             *2    D        D       F       N         N       Y       N           Conflict
             *3    D        F       D                 Y       N       N           Update
             *4    D        F       D                 N       N       N           Update
             *5    D        F       F       Y         N       N       Y           Keep
             *6    D        F       F       N         N       N       Y           Keep
             *7    F        D       F       Y         Y       N       N           Update
             *8    F        D       F       N         Y       N       N           Conflict
             *9    F        D       F       Y         N       N       N           Update
             *10   F        D       D                 N       N       Y           Keep
             *11   F		D		D				  N		  N		  N			  Conflict
             *12   F		F		D		Y		  N		  Y		  N			  Update
             *13   F		F		D		N		  N		  Y		  N			  Conflict
             *14   F		F		D				  N		  N		  N			  Conflict
             *15   0		F		D				  N		  N		  N			  Conflict
             *16   0		D		F		Y		  N		  N		  N			  Update
             *17   0		D		F		 		  N		  N		  N			  Conflict
             *18   F        0       D    										  Update
             *19   D	    0       F											  Update
        */

        // Fields
        private Tree _theHead;
        private GitIndex _theIndex;
        private Tree _theMerge;
        private WorkDirCheckout _theReadTree;

        // Methods
        private void assertAllEmpty()
        {
            Assert.True(_theReadTree.Removed.isEmpty());
            Assert.True(_theReadTree.Updated.isEmpty());
            Assert.True(_theReadTree.Conflicts.isEmpty());
        }

        private void AssertConflict(string s)
        {
            Assert.True(_theReadTree.Conflicts.Contains(s));
        }

        private void AssertNoConflicts()
        {
            Assert.True(_theReadTree.Conflicts.isEmpty());
        }

        private void AssertRemoved(string s)
        {
            Assert.True(_theReadTree.Removed.Contains(s));
        }

        private void AssertUpdated(string s)
        {
            Assert.True(_theReadTree.Updated.ContainsKey(s));
        }

        private GitIndex BuildIndex(Dictionary<string, string> indexEntries)
        {
            var index = new GitIndex(db);
            if (indexEntries != null)
            {
                foreach (var pair in indexEntries)
                {
                    index.add(trash, writeTrashFile(pair.Key, pair.Value)).forceRecheck();
                }
            }
            return index;
        }

        private Tree BuildTree(Dictionary<string, string> headEntries)
        {
            var tree = new Tree(db);
            if (headEntries != null)
            {
                foreach (var pair in headEntries)
                {
                    tree.AddFile(pair.Key).Id = GenSha1(pair.Value);
                }
            }
            return tree;
        }

        private void Checkout()
        {
            _theReadTree = new WorkDirCheckout(db, trash, _theHead, _theIndex, _theMerge);
            _theReadTree.checkout();
        }

        private void cleanUpDF()
        {
            TearDown();
            SetUp();
            recursiveDelete(new DirectoryInfo(Path.Combine(trash.FullName, "DF")));
        }

        private void DoIt(Dictionary<string, string> h, Dictionary<string, string> m, Dictionary<string, string> i)
        {
            SetupCase(h, m, i);
            Go();
        }

        private ObjectId GenSha1(string data)
        {
            byte[] buffer = Constants.encode(data);
            var input = new MemoryStream(buffer);
            var writer = new ObjectWriter(db);
            try
            {
                return writer.WriteObject(ObjectType.Blob, buffer.Length, input, true);
            }
            catch (IOException exception)
            {
                Assert.False(true, exception.ToString());
            }
            return null;
        }

        private WorkDirCheckout Go()
        {
            _theReadTree = new WorkDirCheckout(db, trash, _theHead, _theIndex, _theMerge);
            _theReadTree.PrescanTwoTrees();
            return _theReadTree;
        }

        private static Dictionary<string, string> MakeMap(string a)
        {
            return MakeMap(new[] { a, a });
        }

        private static Dictionary<string, string> MakeMap(params string[] args)
        {
            if ((args.Length % 2) > 0)
            {
                throw new ArgumentException("needs to be pairs");
            }
            var dictionary = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i += 2)
            {
                dictionary.Add(args[i], args[i + 1]);
            }
            return dictionary;
        }

        private void SetupCase(Dictionary<string, string> headEntries, Dictionary<string, string> mergeEntries,
                               Dictionary<string, string> indexEntries)
        {
            _theHead = BuildTree(headEntries);
            _theMerge = BuildTree(mergeEntries);
            _theIndex = BuildIndex(indexEntries);
        }

        [Fact]
        public void testCheckoutOutChanges()
        {
            SetupCase(MakeMap("foo"), MakeMap("foo/bar"), MakeMap("foo"));
            Checkout();
            Assert.False(new FileInfo(Path.Combine(trash.FullName, "foo")).IsFile());
            Assert.True(new FileInfo(Path.Combine(trash.FullName, "foo/bar")).IsFile());
            recursiveDelete(new DirectoryInfo(Path.Combine(trash.FullName, "foo")));

            SetupCase(MakeMap("foo/bar"), MakeMap("foo"), MakeMap("foo/bar"));
            Checkout();
            Assert.False(new FileInfo(Path.Combine(trash.FullName, "foo/bar")).IsFile());
            Assert.True(new FileInfo(Path.Combine(trash.FullName, "foo")).IsFile());
            SetupCase(MakeMap("foo"), MakeMap(new[] { "foo", "qux" }), MakeMap(new[] { "foo", "bar" }));
            try
            {
                Checkout();
                Assert.False(true, "did not throw exception");
            }
            catch (CheckoutConflictException)
            {
            }
        }

        [Fact]
        public void testCloseNameConflicts1()
        {
            SetupCase(MakeMap(new[] { "a/a", "a/a-c" }), MakeMap(new[] { "a/a", "a/a", "a.a/a.a", "a.a/a.a" }),
                      MakeMap(new[] { "a/a", "a/a-c" }));
            Checkout();
            Go();
            AssertNoConflicts();
        }

        [Fact]
        public void testCloseNameConflictsX0()
        {
            SetupCase(MakeMap(new[] { "a/a", "a/a-c" }),
                      MakeMap(new[] { "a/a", "a/a", "b.b/b.b", "b.b/b.bs" }),
                      MakeMap(new[] { "a/a", "a/a-c" }));
            Checkout();
            Go();
            AssertNoConflicts();
        }

        [Fact]
        public void testDirectoryFileConflicts_1()
        {
            DoIt(MakeMap("DF/DF"), MakeMap("DF"), MakeMap("DF/DF"));
            AssertNoConflicts();
            AssertUpdated("DF");
            AssertRemoved("DF/DF");
        }

        [Fact]
        public void testDirectoryFileConflicts_10()
        {
            cleanUpDF();
            DoIt(MakeMap("DF"), MakeMap("DF/DF"), MakeMap("DF/DF"));
            AssertNoConflicts();
        }

        [Fact]
        public void testDirectoryFileConflicts_11()
        {
            DoIt(MakeMap("DF"), MakeMap("DF/DF"), MakeMap(new[] { "DF/DF", "asdf" }));
            AssertConflict("DF/DF");
        }

        [Fact]
        public void testDirectoryFileConflicts_12()
        {
            cleanUpDF();
            DoIt(MakeMap("DF"), MakeMap("DF/DF"), MakeMap("DF"));
            AssertRemoved("DF");
            AssertUpdated("DF/DF");
        }

        [Fact]
        public void testDirectoryFileConflicts_13()
        {
            cleanUpDF();
            SetupCase(MakeMap("DF"), MakeMap("DF/DF"), MakeMap("DF"));
            writeTrashFile("DF", "asdfsdf");
            Go();
            AssertConflict("DF");
            AssertUpdated("DF/DF");
        }

        [Fact]
        public void testDirectoryFileConflicts_14()
        {
            cleanUpDF();
            DoIt(MakeMap("DF"), MakeMap("DF/DF"), MakeMap(new[] { "DF", "Foo" }));
            AssertConflict("DF");
            AssertUpdated("DF/DF");
        }

        [Fact]
        public void testDirectoryFileConflicts_15()
        {
            DoIt(MakeMap(new string[0]), MakeMap("DF/DF"), MakeMap("DF"));
            AssertRemoved("DF");
            AssertUpdated("DF/DF");
        }

        [Fact]
        public void testDirectoryFileConflicts_15b()
        {
            DoIt(MakeMap(new string[0]), MakeMap("DF/DF/DF/DF"), MakeMap("DF"));
            AssertRemoved("DF");
            AssertUpdated("DF/DF/DF/DF");
        }

        [Fact]
        public void testDirectoryFileConflicts_16()
        {
            cleanUpDF();
            DoIt(MakeMap(new string[0]), MakeMap("DF"), MakeMap("DF/DF/DF"));
            AssertRemoved("DF/DF/DF");
            AssertUpdated("DF");
        }

        [Fact]
        public void testDirectoryFileConflicts_17()
        {
            cleanUpDF();
            SetupCase(MakeMap(new string[0]), MakeMap("DF"), MakeMap("DF/DF/DF"));
            writeTrashFile("DF/DF/DF", "asdf");
            Go();
            AssertConflict("DF/DF/DF");
            AssertUpdated("DF");
        }

        [Fact]
        public void testDirectoryFileConflicts_18()
        {
            cleanUpDF();
            DoIt(MakeMap("DF/DF"), MakeMap("DF/DF/DF/DF"), null);
            AssertRemoved("DF/DF");
            AssertUpdated("DF/DF/DF/DF");
        }

        [Fact]
        public void testDirectoryFileConflicts_19()
        {
            cleanUpDF();
            DoIt(MakeMap("DF/DF/DF/DF"), MakeMap("DF/DF/DF"), null);
            AssertRemoved("DF/DF/DF/DF");
            AssertUpdated("DF/DF/DF");
        }

        [Fact]
        public void testDirectoryFileConflicts_2()
        {
            SetupCase(MakeMap("DF/DF"), MakeMap("DF"), MakeMap("DF/DF"));
            writeTrashFile("DF/DF", "different");
            Go();
            AssertConflict("DF/DF");
        }

        [Fact]
        public void testDirectoryFileConflicts_3()
        {
            DoIt(MakeMap("DF/DF"), MakeMap("DF/DF"), MakeMap("DF"));
            AssertUpdated("DF/DF");
            AssertRemoved("DF");
        }

        [Fact]
        public void testDirectoryFileConflicts_4()
        {
            DoIt(MakeMap("DF/DF"), MakeMap(new[] { "DF/DF", "foo" }), MakeMap("DF"));
            AssertUpdated("DF/DF");
            AssertRemoved("DF");
        }

        [Fact]
        public void testDirectoryFileConflicts_5()
        {
            DoIt(MakeMap("DF/DF"), MakeMap("DF"), MakeMap("DF"));
            AssertRemoved("DF/DF");
        }

        [Fact]
        public void testDirectoryFileConflicts_6()
        {
            SetupCase(MakeMap("DF/DF"), MakeMap("DF"), MakeMap("DF"));
            writeTrashFile("DF", "different");
            Go();
            AssertRemoved("DF/DF");
        }

        [Fact]
        public void testDirectoryFileConflicts_7()
        {
            DoIt(MakeMap("DF"), MakeMap("DF"), MakeMap("DF/DF"));
            AssertUpdated("DF");
            AssertRemoved("DF/DF");
            cleanUpDF();

            SetupCase(MakeMap("DF/DF"), MakeMap("DF/DF"), MakeMap("DF/DF/DF/DF/DF"));
            Go();
            AssertRemoved("DF/DF/DF/DF/DF");
            AssertUpdated("DF/DF");
            cleanUpDF();

            SetupCase(MakeMap("DF/DF"), MakeMap("DF/DF"), MakeMap("DF/DF/DF/DF/DF"));
            writeTrashFile("DF/DF/DF/DF/DF", "diff");
            Go();
            AssertConflict("DF/DF/DF/DF/DF");
            AssertUpdated("DF/DF");
        }

        [Fact]
        public void testDirectoryFileConflicts_9()
        {
            DoIt(MakeMap("DF"), MakeMap(new[] { "DF", "QP" }), MakeMap("DF/DF"));
            AssertRemoved("DF/DF");
            AssertUpdated("DF");
        }

        [Fact]
        public void testDirectoryFileSimple()
        {
            _theIndex = new GitIndex(db);
            _theIndex.add(trash, writeTrashFile("DF", "DF"));
            Tree head = db.MapTree(_theIndex.writeTree());
            recursiveDelete(new DirectoryInfo(Path.Combine(trash.FullName, "DF")));

            _theIndex = new GitIndex(db);
            _theIndex.add(trash, writeTrashFile("DF/DF", "DF/DF"));
            Tree merge = db.MapTree(_theIndex.writeTree());
            _theIndex = new GitIndex(db);
            recursiveDelete(new DirectoryInfo(Path.Combine(trash.FullName, "DF")));

            _theIndex.add(trash, writeTrashFile("DF", "DF"));
            _theReadTree = new WorkDirCheckout(db, trash, head, _theIndex, merge);
            _theReadTree.PrescanTwoTrees();
            Assert.True(_theReadTree.Removed.Contains("DF"));
            Assert.True(_theReadTree.Updated.ContainsKey("DF/DF"));
            recursiveDelete(new DirectoryInfo(Path.Combine(trash.FullName, "DF")));

            _theIndex = new GitIndex(db);
            _theIndex.add(trash, writeTrashFile("DF/DF", "DF/DF"));
            _theReadTree = new WorkDirCheckout(db, trash, merge, _theIndex, head);
            _theReadTree.PrescanTwoTrees();
            Assert.True(_theReadTree.Removed.Contains("DF/DF"));
            Assert.True(_theReadTree.Updated.ContainsKey("DF"));
        }

        [Fact]
        public void testRules1thru3_NoIndexEntry()
        {
            var index = new GitIndex(db);
            var head = new Tree(db);
            FileTreeEntry entry = head.AddFile("foo");
            ObjectId expected = ObjectId.FromString("ba78e065e2c261d4f7b8f42107588051e87e18e9");
            entry.Id = expected;
            var merge = new Tree(db);
            var checkout = new WorkDirCheckout(db, trash, head, index, merge);
            checkout.PrescanTwoTrees();
            Assert.True(checkout.Removed.Contains("foo"));
            checkout = new WorkDirCheckout(db, trash, merge, index, head);
            checkout.PrescanTwoTrees();
            Assert.Equal(expected, checkout.Updated["foo"]);
            ObjectId id2 = ObjectId.FromString("ba78e065e2c261d4f7b8f42107588051e87e18ee");
            merge.AddFile("foo").Id = id2;
            checkout = new WorkDirCheckout(db, trash, head, index, merge);
            checkout.PrescanTwoTrees();
            Assert.Equal(id2, checkout.Updated["foo"]);
        }

        [Fact]
        public void testRules4thru13_IndexEntryNotInHead()
        {
            var indexEntries = new Dictionary<string, string>();
            indexEntries.Add("foo", "foo");
            SetupCase(null, null, indexEntries);
            _theReadTree = Go();
            assertAllEmpty();

            indexEntries = new Dictionary<string, string> { { "foo", "foo" } };
            SetupCase(null, indexEntries, indexEntries);
            _theReadTree = Go();
            assertAllEmpty();

            var mergeEntries = new Dictionary<string, string> { { "foo", "merge" } };
            SetupCase(null, mergeEntries, indexEntries);
            Go();
            Assert.True(_theReadTree.Updated.isEmpty());
            Assert.True(_theReadTree.Removed.isEmpty());
            Assert.True(_theReadTree.Conflicts.Contains("foo"));

            var dictionary4 = new Dictionary<string, string> { { "foo", "foo" } };
            Dictionary<string, string> headEntries = dictionary4;
            SetupCase(headEntries, null, indexEntries);
            Go();
            Assert.True(_theReadTree.Removed.Contains("foo"));
            Assert.True(_theReadTree.Updated.isEmpty());
            Assert.True(_theReadTree.Conflicts.isEmpty());

            SetupCase(headEntries, null, indexEntries);
            new FileInfo(Path.Combine(trash.FullName, "foo")).Delete();
            writeTrashFile("foo", "bar");
            _theIndex.Members[0].forceRecheck();
            Go();
            Assert.True(_theReadTree.Removed.isEmpty());
            Assert.True(_theReadTree.Updated.isEmpty());
            Assert.True(_theReadTree.Conflicts.Contains("foo"));

            headEntries.Add("foo", "head");
            SetupCase(headEntries, null, indexEntries);
            Go();
            Assert.True(_theReadTree.Removed.isEmpty());
            Assert.True(_theReadTree.Updated.isEmpty());
            Assert.True(_theReadTree.Conflicts.Contains("foo"));

            SetupCase(headEntries, headEntries, indexEntries);
            Go();
            assertAllEmpty();

            SetupCase(headEntries, mergeEntries, indexEntries);
            Go();
            Assert.True(_theReadTree.Conflicts.Contains("foo"));

            SetupCase(headEntries, indexEntries, indexEntries);
            Go();
            assertAllEmpty();

            SetupCase(indexEntries, mergeEntries, indexEntries);
            Go();
            Assert.True(_theReadTree.Updated.ContainsKey("foo"));

            SetupCase(indexEntries, mergeEntries, indexEntries);
            new FileInfo(Path.Combine(trash.FullName, "foo")).Delete();
            writeTrashFile("foo", "bar");
            _theIndex.Members[0].forceRecheck();
            Go();
            Assert.True(_theReadTree.Conflicts.Contains("foo"));
        }

        [Fact]
        public void testUntrackedConflicts()
        {
            SetupCase(null, MakeMap("foo"), null);
            writeTrashFile("foo", "foo");
            Go();
            AssertConflict("foo");
            recursiveDelete(new DirectoryInfo(Path.Combine(trash.FullName, "foo")));
            SetupCase(null, MakeMap("foo"), null);
            writeTrashFile("foo/bar/baz", "");
            writeTrashFile("foo/blahblah", "");
            Go();
            AssertConflict("foo/bar/baz");
            AssertConflict("foo/blahblah");
            recursiveDelete(new DirectoryInfo(Path.Combine(trash.FullName, "foo")));
            SetupCase(MakeMap(new[] { "foo/bar", "", "foo/baz", "" }), MakeMap("foo"),
                      MakeMap(new[] { "foo/bar", "", "foo/baz", "" }));
            Assert.True(new DirectoryInfo(Path.Combine(trash.FullName, "foo")).Exists);
            Go();
            AssertNoConflicts();
        }
    }
}