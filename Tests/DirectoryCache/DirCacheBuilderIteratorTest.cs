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
    public class DirCacheBuilderIteratorTest : RepositoryTestCase
    {
        [Test]
        public void testPathFilterGroup_DoesNotSkipTail()
        {
            DirCache dc = DirCache.read(db);

            var mode = FileMode.RegularFile;
            string[] paths = { "a.", "a/b", "a/c", "a/d", "a0b" };
            DirCacheEntry[] ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                ents[i] = new DirCacheEntry(paths[i]);
                ents[i].setFileMode(mode);
            }
            {
                DirCacheBuilder builder = dc.builder();
                for (int i = 0; i < ents.Length; i++)
                    builder.add(ents[i]);
                builder.finish();
            }

            int expIdx = 2;
            DirCacheBuilder b = dc.builder();
            GitSharp.TreeWalk.TreeWalk tw = new GitSharp.TreeWalk.TreeWalk(db);
            tw.reset();
            tw.addTree(new DirCacheBuildIterator(b));
            tw.setRecursive(true);
            tw.setFilter(PathFilterGroup.createFromStrings(new string[]{ paths[expIdx] }));

            Assert.IsTrue( tw.next(),"found " + paths[expIdx]);
            DirCacheIterator c = tw.getTree<DirCacheIterator>(0, typeof(DirCacheIterator));
            Assert.IsNotNull(c);
            Assert.AreEqual(expIdx, c.ptr);
            Assert.AreSame(ents[expIdx], c.getDirCacheEntry());
            Assert.AreEqual(paths[expIdx], tw.getPathString());
            Assert.AreEqual(mode.Bits, tw.getRawMode(0));
            Assert.AreSame(mode, tw.getFileMode(0));
            b.add(c.getDirCacheEntry());

            Assert.IsFalse(tw.next(),"no more entries" ); 

            b.finish();
            Assert.AreEqual(ents.Length, dc.getEntryCount());
            for (int i = 0; i < ents.Length; i++)
                Assert.AreSame(ents[i], dc.getEntry(i));
        }

    }
}
