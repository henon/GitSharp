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
using GitSharp.TreeWalk.Filter;
using Xunit;

namespace GitSharp.Tests.DirectoryCache
{
    public class DirCacheIteratorTest : RepositoryTestCase
    {
        [StrictFactAttribute]
        public void testEmptyTree_NoTreeWalk()
        {
            DirCache dc = DirCache.read(db);
            Assert.Equal(0, dc.getEntryCount());

            var i = new DirCacheIterator(dc);
            Assert.True(i.eof());
        }

        [StrictFactAttribute]
        public void testEmptyTree_WithTreeWalk()
        {
            DirCache dc = DirCache.read(db);
            Assert.Equal(0, dc.getEntryCount());

            var tw = new GitSharp.TreeWalk.TreeWalk(db);
            tw.reset();
            tw.addTree(new DirCacheIterator(dc));
            Assert.False(tw.next());
        }

        [StrictFactAttribute]
        public void testNoSubtree_NoTreeWalk()
        {
            DirCache dc = DirCache.read(db);

            string[] paths = { "a.", "a0b" };
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

            b.finish();

            var iter = new DirCacheIterator(dc);
            int pathIdx = 0;
            for (; !iter.eof(); iter.next(1))
            {
                Assert.Equal(pathIdx, iter.Pointer);
                Assert.Same(ents[pathIdx], iter.getDirCacheEntry());
                pathIdx++;
            }

            Assert.Equal(paths.Length, pathIdx);
        }

        [StrictFactAttribute]
        public void testNoSubtree_WithTreeWalk()
        {
            DirCache dc = DirCache.read(db);

            string[] paths = { "a.", "a0b" };
            FileMode[] modes = { FileMode.ExecutableFile, FileMode.GitLink };
            var ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                ents[i] = new DirCacheEntry(paths[i]);
                ents[i].setFileMode(modes[i]);
            }

            DirCacheBuilder b = dc.builder();
            for (int i = 0; i < ents.Length; i++)
            {
            	b.add(ents[i]);
            }
            b.finish();

            var iter = new DirCacheIterator(dc);
            var tw = new GitSharp.TreeWalk.TreeWalk(db);
            tw.reset();
            tw.addTree(iter);
            int pathIdx = 0;
            while (tw.next())
            {
                Assert.Same(iter, tw.getTree<DirCacheIterator>(0, typeof(DirCacheIterator)));
                Assert.Equal(pathIdx, iter.Pointer);
                Assert.Same(ents[pathIdx], iter.getDirCacheEntry());
                Assert.Equal(paths[pathIdx], tw.getPathString());
                Assert.Equal(modes[pathIdx].Bits, tw.getRawMode(0));
                Assert.Same(modes[pathIdx], tw.getFileMode(0));
                pathIdx++;
            }
            Assert.Equal(paths.Length, pathIdx);
        }

        [StrictFactAttribute]
        public void testSingleSubtree_NoRecursion()
        {
            DirCache dc = DirCache.read(db);

            string[] paths = { "a.", "a/b", "a/c", "a/d", "a0b" };
            var ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                ents[i] = new DirCacheEntry(paths[i]);
                ents[i].setFileMode(FileMode.RegularFile);
            }

            DirCacheBuilder b = dc.builder();
            for (int i = 0; i < ents.Length; i++)
            {
            	b.add(ents[i]);
            }
            b.finish();

            string[] expPaths = { "a.", "a", "a0b" };
            FileMode[] expModes = { FileMode.RegularFile, FileMode.Tree, FileMode.RegularFile };
            var expPos = new[] { 0, -1, 4 };

            var iter = new DirCacheIterator(dc);
            var tw = new GitSharp.TreeWalk.TreeWalk(db);
            tw.reset();
            tw.addTree(iter);
            tw.Recursive = false;
            int pathIdx = 0;
            while (tw.next())
            {
                Assert.Same(iter, tw.getTree<DirCacheIterator>(0, typeof(DirCacheIterator)));
                Assert.Equal(expModes[pathIdx].Bits, tw.getRawMode(0));
                Assert.Same(expModes[pathIdx], tw.getFileMode(0));
                Assert.Equal(expPaths[pathIdx], tw.getPathString());

                if (expPos[pathIdx] >= 0)
                {
                    Assert.Equal(expPos[pathIdx], iter.Pointer);
                    Assert.Same(ents[expPos[pathIdx]], iter.getDirCacheEntry());
                }
                else
                {
                    Assert.Same(FileMode.Tree, tw.getFileMode(0));
                }

                pathIdx++;
            }
            Assert.Equal(expPaths.Length, pathIdx);
        }

        [StrictFactAttribute]
        public void testSingleSubtree_Recursive()
        {
            DirCache dc = DirCache.read(db);

            FileMode mode = FileMode.RegularFile;
            string[] paths = { "a.", "a/b", "a/c", "a/d", "a0b" };
            var ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                ents[i] = new DirCacheEntry(paths[i]);
                ents[i].setFileMode(mode);
            }

            DirCacheBuilder b = dc.builder();
            for (int i = 0; i < ents.Length; i++)
            {
            	b.add(ents[i]);
            }
            b.finish();

            var iter = new DirCacheIterator(dc);
            var tw = new GitSharp.TreeWalk.TreeWalk(db);
            tw.reset();
            tw.addTree(iter);
            tw.Recursive = true;
            int pathIdx = 0;
            while (tw.next())
            {
                var c = tw.getTree<DirCacheIterator>(0, typeof(DirCacheIterator));
                Assert.NotNull(c);
                Assert.Equal(pathIdx, c.Pointer);
                Assert.Same(ents[pathIdx], c.getDirCacheEntry());
                Assert.Equal(paths[pathIdx], tw.getPathString());
                Assert.Equal(mode.Bits, tw.getRawMode(0));
                Assert.Same(mode, tw.getFileMode(0));
                pathIdx++;
            }

            Assert.Equal(paths.Length, pathIdx);
        }

        [StrictFactAttribute]
        public void testTwoLevelSubtree_Recursive()
        {
            DirCache dc = DirCache.read(db);

            FileMode mode = FileMode.RegularFile;
            string[] paths = { "a.", "a/b", "a/c/e", "a/c/f", "a/d", "a0b" };
            var ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                ents[i] = new DirCacheEntry(paths[i]);
                ents[i].setFileMode(mode);
            }

            DirCacheBuilder b = dc.builder();
            for (int i = 0; i < ents.Length; i++)
            {
            	b.add(ents[i]);
            }
            b.finish();

            var tw = new GitSharp.TreeWalk.TreeWalk(db);
            tw.reset();
            tw.addTree(new DirCacheIterator(dc));
            tw.Recursive = true;
            int pathIdx = 0;
            while (tw.next())
            {
                var c = tw.getTree<DirCacheIterator>(0, typeof(DirCacheIterator));
                Assert.NotNull(c);
                Assert.Equal(pathIdx, c.Pointer);
                Assert.Same(ents[pathIdx], c.getDirCacheEntry());
                Assert.Equal(paths[pathIdx], tw.getPathString());
                Assert.Equal(mode.Bits, tw.getRawMode(0));
                Assert.Same(mode, tw.getFileMode(0));
                pathIdx++;
            }

            Assert.Equal(paths.Length, pathIdx);
        }

        [StrictFactAttribute]
        public void testTwoLevelSubtree_FilterPath()
        {
            DirCache dc = DirCache.read(db);

            FileMode mode = FileMode.RegularFile;
            string[] paths = { "a.", "a/b", "a/c/e", "a/c/f", "a/d", "a0b" };
            var ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                ents[i] = new DirCacheEntry(paths[i]);
                ents[i].setFileMode(mode);
            }

            DirCacheBuilder b = dc.builder();
            for (int i = 0; i < ents.Length; i++)
            {
            	b.add(ents[i]);
            }
            b.finish();

            var tw = new GitSharp.TreeWalk.TreeWalk(db);
            for (int victimIdx = 0; victimIdx < paths.Length; victimIdx++)
            {
                tw.reset();
                tw.addTree(new DirCacheIterator(dc));
                tw.setFilter(PathFilterGroup.createFromStrings(new[] { paths[victimIdx] }));
                tw.Recursive = tw.getFilter().shouldBeRecursive();
                Assert.True(tw.next());
                var c = tw.getTree<DirCacheIterator>(0, typeof(DirCacheIterator));
                Assert.NotNull(c);
                Assert.Equal(victimIdx, c.Pointer);
                Assert.Same(ents[victimIdx], c.getDirCacheEntry());
                Assert.Equal(paths[victimIdx], tw.getPathString());
                Assert.Equal(mode.Bits, tw.getRawMode(0));
                Assert.Same(mode, tw.getFileMode(0));
                Assert.False(tw.next());
            }
        }
    }
}