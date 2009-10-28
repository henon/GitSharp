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

using GitSharp.Core.DirectoryCache;
using NUnit.Framework;

namespace GitSharp.Tests.DirectoryCache
{
    [TestFixture]
    public class DirCacheTreeTest : RepositoryTestCase
    {
        [Test]
        public void testEmptyCache_NoCacheTree()
        {
            DirCache dc = DirCache.read(db);
            Assert.IsNull(dc.getCacheTree(false));
        }

        [Test]
        public void testEmptyCache_CreateEmptyCacheTree()
        {
            DirCache dc = DirCache.read(db);
            DirCacheTree tree = dc.getCacheTree(true);
            Assert.IsNotNull(tree);
            Assert.AreSame(tree, dc.getCacheTree(false));
            Assert.AreSame(tree, dc.getCacheTree(true));
            Assert.AreEqual(string.Empty, tree.getNameString());
            Assert.AreEqual(string.Empty, tree.getPathString());
            Assert.AreEqual(0, tree.getChildCount());
            Assert.AreEqual(0, tree.getEntrySpan());
            Assert.IsFalse(tree.isValid());
        }

        [Test]
        public void testEmptyCache_Clear_NoCacheTree()
        {
            DirCache dc = DirCache.read(db);
            DirCacheTree tree = dc.getCacheTree(true);
            Assert.IsNotNull(tree);
            dc.clear();
            Assert.IsNull(dc.getCacheTree(false));
            Assert.AreNotSame(tree, dc.getCacheTree(true));
        }

        [Test]
        public void testSingleSubtree()
        {
            DirCache dc = DirCache.read(db);

            string[] paths = { "a.", "a/b", "a/c", "a/d", "a0b" };
            DirCacheEntry[] ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
                ents[i] = new DirCacheEntry(paths[i]);
            int aFirst = 1;
            int aLast = 3;

            DirCacheBuilder b = dc.builder();
            for (int i = 0; i < ents.Length; i++)
                b.add(ents[i]);
            b.finish();

            Assert.IsNull(dc.getCacheTree(false));
            DirCacheTree root = dc.getCacheTree(true);
            Assert.IsNotNull(root);
            Assert.AreSame(root, dc.getCacheTree(true));
            Assert.AreEqual(string.Empty, root.getNameString());
            Assert.AreEqual(string.Empty, root.getPathString());
            Assert.AreEqual(1, root.getChildCount());
            Assert.AreEqual(dc.getEntryCount(), root.getEntrySpan());
            Assert.IsFalse(root.isValid());

            DirCacheTree aTree = root.getChild(0);
            Assert.IsNotNull(aTree);
            Assert.AreSame(aTree, root.getChild(0));
            Assert.AreEqual("a", aTree.getNameString());
            Assert.AreEqual("a/", aTree.getPathString());
            Assert.AreEqual(0, aTree.getChildCount());
            Assert.AreEqual(aLast - aFirst + 1, aTree.getEntrySpan());
            Assert.IsFalse(aTree.isValid());
        }

        [Test]
        public void testTwoLevelSubtree()
        {
            DirCache dc = DirCache.read(db);

            string[] paths = { "a.", "a/b", "a/c/e", "a/c/f", "a/d", "a0b" };
            DirCacheEntry[] ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
                ents[i] = new DirCacheEntry(paths[i]);
            int aFirst = 1;
            int aLast = 4;
            int acFirst = 2;
            int acLast = 3;

            DirCacheBuilder b = dc.builder();
            for (int i = 0; i < ents.Length; i++)
                b.add(ents[i]);
            b.finish();

            Assert.IsNull(dc.getCacheTree(false));
            DirCacheTree root = dc.getCacheTree(true);
            Assert.IsNotNull(root);
            Assert.AreSame(root, dc.getCacheTree(true));
            Assert.AreEqual(string.Empty, root.getNameString());
            Assert.AreEqual(string.Empty, root.getPathString());
            Assert.AreEqual(1, root.getChildCount());
            Assert.AreEqual(dc.getEntryCount(), root.getEntrySpan());
            Assert.IsFalse(root.isValid());

            DirCacheTree aTree = root.getChild(0);
            Assert.IsNotNull(aTree);
            Assert.AreSame(aTree, root.getChild(0));
            Assert.AreEqual("a", aTree.getNameString());
            Assert.AreEqual("a/", aTree.getPathString());
            Assert.AreEqual(1, aTree.getChildCount());
            Assert.AreEqual(aLast - aFirst + 1, aTree.getEntrySpan());
            Assert.IsFalse(aTree.isValid());

            DirCacheTree acTree = aTree.getChild(0);
            Assert.IsNotNull(acTree);
            Assert.AreSame(acTree, aTree.getChild(0));
            Assert.AreEqual("c", acTree.getNameString());
            Assert.AreEqual("a/c/", acTree.getPathString());
            Assert.AreEqual(0, acTree.getChildCount());
            Assert.AreEqual(acLast - acFirst + 1, acTree.getEntrySpan());
            Assert.IsFalse(acTree.isValid());
        }

        /// <summary>
        /// We had bugs related to buffer size in the DirCache. This test creates an
		/// index larger than the default BufferedInputStream buffer size. This made
		/// the DirCache unable to Read the extensions when index size exceeded the
		/// buffer size (in some cases at least).
        /// </summary>
        [Test]
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

            Assert.AreEqual(paths.Length, read.getEntryCount());
            Assert.AreEqual(1, read.getCacheTree(true).getChildCount());
        }
    }
}