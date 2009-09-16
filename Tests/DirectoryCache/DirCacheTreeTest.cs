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

using GitSharp.DirectoryCache;
using GitSharp.Tests.Util;
using Xunit;

namespace GitSharp.Tests.DirectoryCache
{
    public class DirCacheTreeTest : RepositoryTestCase
    {
        [Fact]
        public void testEmptyCache_NoCacheTree()
        {
            DirCache dc = DirCache.read(db);
            Assert.Null(dc.getCacheTree(false));
        }

        [Fact]
        public void testEmptyCache_CreateEmptyCacheTree()
        {
            DirCache dc = DirCache.read(db);
            DirCacheTree tree = dc.getCacheTree(true);
            Assert.NotNull(tree);
            Assert.Same(tree, dc.getCacheTree(false));
            Assert.Same(tree, dc.getCacheTree(true));
            Assert.Equal(string.Empty, tree.getNameString());
            Assert.Equal(string.Empty, tree.getPathString());
            Assert.Equal(0, tree.getChildCount());
            Assert.Equal(0, tree.getEntrySpan());
            Assert.False(tree.isValid());
        }

        [Fact]
        public void testEmptyCache_Clear_NoCacheTree()
        {
            DirCache dc = DirCache.read(db);
            DirCacheTree tree = dc.getCacheTree(true);
            Assert.NotNull(tree);
            dc.clear();
            Assert.Null(dc.getCacheTree(false));
            Assert.NotSame(tree, dc.getCacheTree(true));
        }

        [Fact]
        public void testSingleSubtree()
        {
            DirCache dc = DirCache.read(db);

            string[] paths = { "a.", "a/b", "a/c", "a/d", "a0b" };
            var ents = new DirCacheEntry[paths.Length];

            for (int i = 0; i < paths.Length; i++)
            {
            	ents[i] = new DirCacheEntry(paths[i]);
            }

            const int aFirst = 1;
            const int aLast = 3;

            DirCacheBuilder b = dc.builder();
            for (int i = 0; i < ents.Length; i++)
            {
            	b.add(ents[i]);
            }
            b.finish();

            Assert.Null(dc.getCacheTree(false));
            DirCacheTree root = dc.getCacheTree(true);
            Assert.NotNull(root);
            Assert.Same(root, dc.getCacheTree(true));
            Assert.Equal(string.Empty, root.getNameString());
            Assert.Equal(string.Empty, root.getPathString());
            Assert.Equal(1, root.getChildCount());
            Assert.Equal(dc.getEntryCount(), root.getEntrySpan());
            Assert.False(root.isValid());

            DirCacheTree aTree = root.getChild(0);
            Assert.NotNull(aTree);
            Assert.Same(aTree, root.getChild(0));
            Assert.Equal("a", aTree.getNameString());
            Assert.Equal("a/", aTree.getPathString());
            Assert.Equal(0, aTree.getChildCount());
            Assert.Equal(aLast - aFirst + 1, aTree.getEntrySpan());
            Assert.False(aTree.isValid());
        }

        [Fact]
        public void testTwoLevelSubtree()
        {
            DirCache dc = DirCache.read(db);

            string[] paths = { "a.", "a/b", "a/c/e", "a/c/f", "a/d", "a0b" };
            var ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
            	ents[i] = new DirCacheEntry(paths[i]);
            }

            const int aFirst = 1;
            const int aLast = 4;
            const int acFirst = 2;
            const int acLast = 3;

            DirCacheBuilder b = dc.builder();
            for (int i = 0; i < ents.Length; i++)
            {
            	b.add(ents[i]);
            }
            b.finish();

            Assert.Null(dc.getCacheTree(false));
            DirCacheTree root = dc.getCacheTree(true);
            Assert.NotNull(root);
            Assert.Same(root, dc.getCacheTree(true));
            Assert.Equal(string.Empty, root.getNameString());
            Assert.Equal(string.Empty, root.getPathString());
            Assert.Equal(1, root.getChildCount());
            Assert.Equal(dc.getEntryCount(), root.getEntrySpan());
            Assert.False(root.isValid());

            DirCacheTree aTree = root.getChild(0);
            Assert.NotNull(aTree);
            Assert.Same(aTree, root.getChild(0));
            Assert.Equal("a", aTree.getNameString());
            Assert.Equal("a/", aTree.getPathString());
            Assert.Equal(1, aTree.getChildCount());
            Assert.Equal(aLast - aFirst + 1, aTree.getEntrySpan());
            Assert.False(aTree.isValid());

            DirCacheTree acTree = aTree.getChild(0);
            Assert.NotNull(acTree);
            Assert.Same(acTree, aTree.getChild(0));
            Assert.Equal("c", acTree.getNameString());
            Assert.Equal("a/c/", acTree.getPathString());
            Assert.Equal(0, acTree.getChildCount());
            Assert.Equal(acLast - acFirst + 1, acTree.getEntrySpan());
            Assert.False(acTree.isValid());
        }

        /// <summary>
        /// We had bugs related to buffer size in the DirCache. This test creates an
		/// index larger than the default BufferedInputStream buffer size. This made
		/// the DirCache unable to Read the extensions when index size exceeded the
		/// buffer size (in some cases at least).
        /// </summary>
        [Fact]
        public void testWriteReadTree()
        {
            DirCache dc = DirCache.Lock(db);

            string A = string.Format("a%2000s", "a");
            string B = string.Format("b%2000s", "b");
            string[] paths = { A + ".", A + "." + B, A + "/" + B, A + "0" + B };
            var ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
            	ents[i] = new DirCacheEntry(paths[i]);
            }

            DirCacheBuilder b = dc.builder();
            for (int i = 0; i < ents.Length; i++)
            {
            	b.add(ents[i]);
            }

            b.commit();
            DirCache read = DirCache.read(db);

            Assert.Equal(paths.Length, read.getEntryCount());
            Assert.Equal(1, read.getCacheTree(true).getChildCount());
        }
    }
}