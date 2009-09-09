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

using Xunit;
using System.IO;
using GitSharp.DirectoryCache;

namespace GitSharp.Tests.DirectoryCache
{
    public class DirCacheBasicTest : RepositoryTestCase
    {

        [Fact]
        public void testReadMissing_RealIndex()
        {
            FileInfo idx = new FileInfo(db.Directory + "/index");
            Assert.False(File.Exists(idx.FullName));

            DirCache dc = DirCache.read(db);
            Assert.NotNull(dc);
            Assert.Equal(0, dc.getEntryCount());
        }

        [Fact]
        public void testReadMissing_TempIndex()
        {
            FileInfo idx = new FileInfo(db.Directory + "/tmp_index");
            Assert.False(File.Exists(idx.FullName));

            DirCache dc = DirCache.read(idx);
            Assert.NotNull(dc);
            Assert.Equal(0, dc.getEntryCount());
        }

        [Fact]
        public void testLockMissing_RealIndex()
        {
            FileInfo idx = new FileInfo(db.Directory + "/index");
            FileInfo lck = new FileInfo(db.Directory + "/index.lock");
            Assert.False(File.Exists(idx.FullName));
            Assert.False(File.Exists(lck.FullName));

            DirCache dc = DirCache.Lock(db);
            Assert.NotNull(dc);
            Assert.False(File.Exists(idx.FullName));
            Assert.True(File.Exists(lck.FullName));
            Assert.Equal(0, dc.getEntryCount());

            dc.unlock();
            Assert.False(File.Exists(idx.FullName));
            Assert.False(File.Exists(lck.FullName));
        }

        [Fact]
        public void testLockMissing_TempIndex()
        {
            FileInfo idx = new FileInfo(db.Directory + "/tmp_index");
            FileInfo lck = new FileInfo(db.Directory + "/tmp_index.lock");
            Assert.False(File.Exists(idx.FullName));
            Assert.False(File.Exists(lck.FullName));

            DirCache dc = DirCache.Lock(idx);
            Assert.NotNull(dc);
            Assert.False(File.Exists(idx.FullName));
            Assert.True(File.Exists(lck.FullName));
            Assert.Equal(0, dc.getEntryCount());

            dc.unlock();
            Assert.False(File.Exists(idx.FullName));
            Assert.False(File.Exists(lck.FullName));
        }

        [Fact]
        public void testWriteEmptyUnlock_RealIndex()
        {
            FileInfo idx = new FileInfo(db.Directory + "/index");
            FileInfo lck = new FileInfo(db.Directory + "/index.lock");
            Assert.False(File.Exists(idx.FullName));
            Assert.False(File.Exists(lck.FullName));

            DirCache dc = DirCache.Lock(db);
            Assert.Equal(0, lck.Length);
            dc.write();
            Assert.Equal(12 + 20, new FileInfo(lck.FullName).Length);

            dc.unlock();
            Assert.False(File.Exists(idx.FullName));
            Assert.False(File.Exists(lck.FullName));
        }

        [Fact]
        public void testWriteEmptyCommit_RealIndex()
        {
            FileInfo idx = new FileInfo(db.Directory + "/index");
            FileInfo lck = new FileInfo(db.Directory + "/index.lock");
            Assert.False(File.Exists(idx.FullName));
            Assert.False(File.Exists(lck.FullName));

            DirCache dc = DirCache.Lock(db);
            Assert.Equal(0, lck.Length);
            dc.write();
            Assert.Equal(12 + 20, new FileInfo(lck.FullName).Length);

            Assert.True(dc.commit());
            Assert.True(File.Exists(idx.FullName));
            Assert.False(File.Exists(lck.FullName));
            Assert.Equal(12 + 20, new FileInfo(idx.FullName).Length);
        }

        [Fact]
        public void testWriteEmptyReadEmpty_RealIndex()
        {
            FileInfo idx = new FileInfo(db.Directory + "/index");
            FileInfo lck = new FileInfo(db.Directory + "/index.lock");
            Assert.False(File.Exists(idx.FullName));
            Assert.False(File.Exists(lck.FullName));
            {
                DirCache dc = DirCache.Lock(db);
                dc.write();
                Assert.True(dc.commit());
                Assert.True(File.Exists(idx.FullName));
            }
            {
                DirCache dc = DirCache.read(db);
                Assert.Equal(0, dc.getEntryCount());
            }
        }

        [Fact]
        public void testWriteEmptyLockEmpty_RealIndex()
        {
            FileInfo idx = new FileInfo(db.Directory + "/index");
            FileInfo lck = new FileInfo(db.Directory + "/index.lock");
            Assert.False(File.Exists(idx.FullName));
            Assert.False(File.Exists(lck.FullName));
            {
                DirCache dc = DirCache.Lock(db);
                dc.write();
                Assert.True(dc.commit());
                Assert.True(File.Exists(idx.FullName));
            }
            {
                DirCache dc = DirCache.Lock(db);
                Assert.Equal(0, dc.getEntryCount());
                Assert.True(File.Exists(idx.FullName));
                Assert.True(File.Exists(lck.FullName));
                dc.unlock();
            }
        }

        [Fact]
        public void testBuildThenClear()
        {
            DirCache dc = DirCache.read(db);

            string[] paths = { "a.", "a.b", "a/b", "a0b" };
            DirCacheEntry[] ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
                ents[i] = new DirCacheEntry(paths[i]);

            DirCacheBuilder b = dc.builder();
            for (int i = 0; i < ents.Length; i++)
                b.add(ents[i]);
            b.finish();

            Assert.Equal(paths.Length, dc.getEntryCount());
            dc.clear();
            Assert.Equal(0, dc.getEntryCount());
        }

    }
}
