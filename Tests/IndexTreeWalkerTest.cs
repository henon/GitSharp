/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2006, Shawn O. Pearce <spearce@spearce.org>
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
using GitSharp.Tests.Util;
using Xunit;

namespace GitSharp.Tests
{
    public class IndexTreeWalkerTest : RepositoryTestCase
    {
        private static readonly List<string> BothVisited = new List<string>();
        private static readonly List<string> IndexOnlyEntriesVisited = new List<string>();
        private static readonly AbstractIndexTreeVisitor TestIndexTreeVisitor;
        private static readonly AbstractIndexTreeVisitor TestTreeOnlyOneLevelTreeVisitor;
        private static readonly List<string> TreeOnlyEntriesVisited = new List<string>();

        static IndexTreeWalkerTest()
        {
            TestIndexTreeVisitor = new AbstractIndexTreeVisitor
                          	{
                          		VisitEntry = delegate(TreeEntry treeEntry, GitIndex.Entry indexEntry, FileInfo file)
                          		             	{
                          		             		if (treeEntry == null)
                          		             		{
                          		             			IndexOnlyEntriesVisited.Add(indexEntry.Name);
                          		             		}
                          		             		else if (indexEntry == null)
                          		             		{
                          		             			TreeOnlyEntriesVisited.Add(treeEntry.FullName);
                          		             		}
                          		             		else
                          		             		{
                          		             			BothVisited.Add(indexEntry.Name);
                          		             		}
                          		             	}
                          	};

			TestTreeOnlyOneLevelTreeVisitor = new AbstractIndexTreeVisitor
                           	{
                           		VisitEntry = delegate(TreeEntry entry, GitIndex.Entry indexEntry, FileInfo f)
                           		             	{
                           		             		if ((entry == null) || (indexEntry == null))
                           		             		{
                           		             			Assert.False(true, "Condition not expected.");
                           		             		}
                           		             	},
                           		FinishVisitTreeByIndex = delegate(Tree tree, int i, string curDir)
                           		                         	{
                           		                         		if (tree.MemberCount == 0)
                           		                         		{
																	Assert.False(true, "Condition not expected.");
                           		                         		}
                           		                         		if (i == 0)
                           		                         		{
                           		                         			Assert.False(true, "Condition not expected.");
                           		                         		}
                           		                         	}
                           	};
        }

        protected override void TearDown()
        {
            TreeOnlyEntriesVisited.Clear();
            BothVisited.Clear();
            IndexOnlyEntriesVisited.Clear();
        }

        [StrictFactAttribute]
        public void testBoth()
        {
            var index = new GitIndex(db);
            var mainTree = new Tree(db);

            index.add(trash, writeTrashFile("a", "a"));
            mainTree.AddFile("b/b");
            index.add(trash, writeTrashFile("c", "c"));
            mainTree.AddFile("c");

            new IndexTreeWalker(index, mainTree, trash, TestIndexTreeVisitor).Walk();
            Assert.True(IndexOnlyEntriesVisited.Contains("a"));
            Assert.True(TreeOnlyEntriesVisited.Contains("b/b"));
            Assert.True(BothVisited.Contains("c"));
        }

        [StrictFactAttribute]
        public void testIndexOnlyOneLevel()
        {
            var index = new GitIndex(db);
            var mainTree = new Tree(db);

            index.add(trash, writeTrashFile("foo", "foo"));
            index.add(trash, writeTrashFile("bar", "bar"));
            new IndexTreeWalker(index, mainTree, trash, TestIndexTreeVisitor).Walk();

            Assert.Equal(2, IndexOnlyEntriesVisited.Count);
            Assert.True(IndexOnlyEntriesVisited[0].Equals("bar"));
            Assert.True(IndexOnlyEntriesVisited[1].Equals("foo"));
        }

        [StrictFactAttribute]
        public void testIndexOnlySubDirs()
        {
            var index = new GitIndex(db);
            var mainTree = new Tree(db);

            index.add(trash, writeTrashFile("foo/bar/baz", "foobar"));
            index.add(trash, writeTrashFile("asdf", "asdf"));
            new IndexTreeWalker(index, mainTree, trash, TestIndexTreeVisitor).Walk();

            Assert.Equal("asdf", IndexOnlyEntriesVisited[0]);
            Assert.Equal("foo/bar/baz", IndexOnlyEntriesVisited[1]);
        }

        [StrictFactAttribute]
        public void testLeavingTree()
        {
            var index = new GitIndex(db);
            index.add(trash, writeTrashFile("foo/bar", "foo/bar"));
            index.add(trash, writeTrashFile("foobar", "foobar"));

            new IndexTreeWalker(index, db.MapTree(index.writeTree()), trash, TestTreeOnlyOneLevelTreeVisitor).Walk();
        }

        [StrictFactAttribute]
        public void testTreeOnlyOneLevel()
        {
            var index = new GitIndex(db);
            var mainTree = new Tree(db);
            mainTree.AddFile("foo");
            mainTree.AddFile("bar");
            new IndexTreeWalker(index, mainTree, trash, TestIndexTreeVisitor).Walk();
            Assert.True(TreeOnlyEntriesVisited[0].Equals("bar"));
            Assert.True(TreeOnlyEntriesVisited[1].Equals("foo"));
        }
    }
}