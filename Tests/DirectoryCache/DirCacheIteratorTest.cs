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

namespace GitSharp.Tests.DirectoryCache
{
    using NUnit.Framework;
    using GitSharp.DirectoryCache;
    using GitSharp.TreeWalk.Filter;
    [TestFixture]
    public class DirCacheIteratorTest : RepositoryTestCase
    {

        [Test]
        public void testEmptyTree_NoTreeWalk()
        {
            DirCache dc = DirCache.read(db);
            Assert.AreEqual(0, dc.getEntryCount());

            DirCacheIterator i = new DirCacheIterator(dc);
            Assert.IsTrue(i.eof());
        }

        [Test]
        public void testEmptyTree_WithTreeWalk()
        {
            DirCache dc = DirCache.read(db);
            Assert.AreEqual(0, dc.getEntryCount());

            var tw = new GitSharp.TreeWalk.TreeWalk(db);
            tw.reset();
            tw.addTree(new DirCacheIterator(dc));
            Assert.IsFalse(tw.next());
        }

        [Test]
        public void testNoSubtree_NoTreeWalk()
        {
            DirCache dc = DirCache.read(db);

            string[] paths = { "a.", "a0b" };
            DirCacheEntry[] ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
                ents[i] = new DirCacheEntry(paths[i]);

            DirCacheBuilder b = dc.builder();
            for (int i = 0; i < ents.Length; i++)
                b.add(ents[i]);
            b.finish();

            DirCacheIterator iter = new DirCacheIterator(dc);
            int pathIdx = 0;
            for (; !iter.eof(); iter.next(1))
            {
                Assert.AreEqual(pathIdx, iter.ptr);
                Assert.AreSame(ents[pathIdx], iter.getDirCacheEntry());
                pathIdx++;
            }
            Assert.AreEqual(paths.Length, pathIdx);
        }

        [Test]
        public void testNoSubtree_WithTreeWalk()
        {
            DirCache dc = DirCache.read(db);

            string[] paths = { "a.", "a0b" };
            FileMode[] modes = { FileMode.ExecutableFile, FileMode.GitLink };
            DirCacheEntry[] ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                ents[i] = new DirCacheEntry(paths[i]);
                ents[i].setFileMode(modes[i]);
            }

            DirCacheBuilder b = dc.builder();
            for (int i = 0; i < ents.Length; i++)
                b.add(ents[i]);
            b.finish();

            DirCacheIterator iter = new DirCacheIterator(dc);
            var tw = new GitSharp.TreeWalk.TreeWalk(db);
            tw.reset();
            tw.addTree(iter);
            int pathIdx = 0;
            while (tw.next())
            {
                Assert.AreSame(iter, tw.getTree<DirCacheIterator>(0, typeof(DirCacheIterator)));
                Assert.AreEqual(pathIdx, iter.ptr);
                Assert.AreSame(ents[pathIdx], iter.getDirCacheEntry());
                Assert.AreEqual(paths[pathIdx], tw.getPathString());
                Assert.AreEqual(modes[pathIdx].Bits, tw.getRawMode(0));
                Assert.AreSame(modes[pathIdx], tw.getFileMode(0));
                pathIdx++;
            }
            Assert.AreEqual(paths.Length, pathIdx);
        }

        [Test]
        public void testSingleSubtree_NoRecursion()
        {
            DirCache dc = DirCache.read(db);

            string[] paths = { "a.", "a/b", "a/c", "a/d", "a0b" };
            DirCacheEntry[] ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                ents[i] = new DirCacheEntry(paths[i]);
                ents[i].setFileMode(FileMode.RegularFile);
            }

            DirCacheBuilder b = dc.builder();
            for (int i = 0; i < ents.Length; i++)
                b.add(ents[i]);
            b.finish();

            string[] expPaths = { "a.", "a", "a0b" };
            FileMode[] expModes = { FileMode.RegularFile, FileMode.Tree, FileMode.RegularFile };
            int[] expPos = new int[] { 0, -1, 4 };

            DirCacheIterator iter = new DirCacheIterator(dc);
            var tw = new GitSharp.TreeWalk.TreeWalk(db);
            tw.reset();
            tw.addTree(iter);
            tw.setRecursive(false);
            int pathIdx = 0;
            while (tw.next())
            {
                Assert.AreSame(iter, tw.getTree<DirCacheIterator>(0, typeof(DirCacheIterator)));
                Assert.AreEqual(expModes[pathIdx].Bits, tw.getRawMode(0));
                Assert.AreSame(expModes[pathIdx], tw.getFileMode(0));
                Assert.AreEqual(expPaths[pathIdx], tw.getPathString());

                if (expPos[pathIdx] >= 0)
                {
                    Assert.AreEqual(expPos[pathIdx], iter.ptr);
                    Assert.AreSame(ents[expPos[pathIdx]], iter.getDirCacheEntry());
                }
                else
                {
                    Assert.AreSame(FileMode.Tree, tw.getFileMode(0));
                }

                pathIdx++;
            }
            Assert.AreEqual(expPaths.Length, pathIdx);
        }

        [Test]
        public void testSingleSubtree_Recursive()
        {
            DirCache dc = DirCache.read(db);

            FileMode mode = FileMode.RegularFile;
            string[] paths = { "a.", "a/b", "a/c", "a/d", "a0b" };
            DirCacheEntry[] ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                ents[i] = new DirCacheEntry(paths[i]);
                ents[i].setFileMode(mode);
            }

            DirCacheBuilder b = dc.builder();
            for (int i = 0; i < ents.Length; i++)
                b.add(ents[i]);
            b.finish();

            DirCacheIterator iter = new DirCacheIterator(dc);
            var tw = new GitSharp.TreeWalk.TreeWalk(db);
            tw.reset();
            tw.addTree(iter);
            tw.setRecursive(true);
            int pathIdx = 0;
            while (tw.next())
            {
                DirCacheIterator c = tw.getTree<DirCacheIterator>(0, typeof(DirCacheIterator));
                Assert.IsNotNull(c);
                Assert.AreEqual(pathIdx, c.ptr);
                Assert.AreSame(ents[pathIdx], c.getDirCacheEntry());
                Assert.AreEqual(paths[pathIdx], tw.getPathString());
                Assert.AreEqual(mode.Bits, tw.getRawMode(0));
                Assert.AreSame(mode, tw.getFileMode(0));
                pathIdx++;
            }
            Assert.AreEqual(paths.Length, pathIdx);
        }

        [Test]
        public void testTwoLevelSubtree_Recursive()
        {
            DirCache dc = DirCache.read(db);

            FileMode mode = FileMode.RegularFile;
            string[] paths = { "a.", "a/b", "a/c/e", "a/c/f", "a/d", "a0b" };
            DirCacheEntry[] ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                ents[i] = new DirCacheEntry(paths[i]);
                ents[i].setFileMode(mode);
            }

            DirCacheBuilder b = dc.builder();
            for (int i = 0; i < ents.Length; i++)
                b.add(ents[i]);
            b.finish();

            var tw = new GitSharp.TreeWalk.TreeWalk(db);
            tw.reset();
            tw.addTree(new DirCacheIterator(dc));
            tw.setRecursive(true);
            int pathIdx = 0;
            while (tw.next())
            {
                DirCacheIterator c = tw.getTree<DirCacheIterator>(0, typeof(DirCacheIterator));
                Assert.IsNotNull(c);
                Assert.AreEqual(pathIdx, c.ptr);
                Assert.AreSame(ents[pathIdx], c.getDirCacheEntry());
                Assert.AreEqual(paths[pathIdx], tw.getPathString());
                Assert.AreEqual(mode.Bits, tw.getRawMode(0));
                Assert.AreSame(mode, tw.getFileMode(0));
                pathIdx++;
            }
            Assert.AreEqual(paths.Length, pathIdx);
        }

        [Test]
        public void testTwoLevelSubtree_FilterPath()
        {
            DirCache dc = DirCache.read(db);

            FileMode mode = FileMode.RegularFile;
            string[] paths = { "a.", "a/b", "a/c/e", "a/c/f", "a/d", "a0b" };
            DirCacheEntry[] ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                ents[i] = new DirCacheEntry(paths[i]);
                ents[i].setFileMode(mode);
            }

            DirCacheBuilder b = dc.builder();
            for (int i = 0; i < ents.Length; i++)
                b.add(ents[i]);
            b.finish();

            var tw = new GitSharp.TreeWalk.TreeWalk(db);
            for (int victimIdx = 0; victimIdx < paths.Length; victimIdx++)
            {
                tw.reset();
                tw.addTree(new DirCacheIterator(dc));
                tw.setFilter(PathFilterGroup.createFromStrings(new string[] { paths[victimIdx] }));
                tw.setRecursive(tw.getFilter().shouldBeRecursive());
                Assert.IsTrue(tw.next());
                var c = tw.getTree<DirCacheIterator>(0, typeof(DirCacheIterator));
                Assert.IsNotNull(c);
                Assert.AreEqual(victimIdx, c.ptr);
                Assert.AreSame(ents[victimIdx], c.getDirCacheEntry());
                Assert.AreEqual(paths[victimIdx], tw.getPathString());
                Assert.AreEqual(mode.Bits, tw.getRawMode(0));
                Assert.AreSame(mode, tw.getFileMode(0));
                Assert.IsFalse(tw.next());
            }
        }

    }
}