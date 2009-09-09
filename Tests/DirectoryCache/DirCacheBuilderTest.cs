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
using GitSharp.DirectoryCache;
using System.IO;

namespace GitSharp.Tests.DirectoryCache
{
    public class DirCacheBuilderTest : RepositoryTestCase
    {

        [Fact]
        public void testBuildEmpty()
        {
            {
                DirCache dc = DirCache.Lock(db);
                DirCacheBuilder b = dc.builder();
                Assert.NotNull(b);
                b.finish();
                dc.write();
                Assert.True(dc.commit());
            }
            {
                DirCache dc = DirCache.read(db);
                Assert.Equal(0, dc.getEntryCount());
            }
        }

        [Fact]
        public void testBuildOneFile_FinishWriteCommit()
        {
            string path = "a-file-path";
            var mode = GitSharp.FileMode.RegularFile;
            long lastModified = 1218123387057L;
            int Length = 1342;
            DirCacheEntry entOrig;
            {
                DirCache dc = DirCache.Lock(db);
                DirCacheBuilder b = dc.builder();
                Assert.NotNull(b);

                entOrig = new DirCacheEntry(path);
                entOrig.setFileMode(mode);
                entOrig.setLastModified(lastModified);
                entOrig.setLength(Length);

                Assert.NotSame(path, entOrig.getPathString());
                Assert.Equal(path, entOrig.getPathString());
                Assert.Equal(ObjectId.ZeroId, entOrig.getObjectId());
                Assert.Equal(mode.Bits, entOrig.getRawMode());
                Assert.Equal(0, entOrig.getStage());
                Assert.Equal(lastModified, entOrig.getLastModified());
                Assert.Equal(Length, entOrig.getLength());
                Assert.False(entOrig.isAssumeValid());
                b.add(entOrig);

                b.finish();
                Assert.Equal(1, dc.getEntryCount());
                Assert.Same(entOrig, dc.getEntry(0));

                dc.write();
                Assert.True(dc.commit());
            }
            {
                DirCache dc = DirCache.read(db);
                Assert.Equal(1, dc.getEntryCount());

                DirCacheEntry entRead = dc.getEntry(0);
                Assert.NotSame(entOrig, entRead);
                Assert.Equal(path, entRead.getPathString());
                Assert.Equal(ObjectId.ZeroId, entOrig.getObjectId());
                Assert.Equal(mode.Bits, entOrig.getRawMode());
                Assert.Equal(0, entOrig.getStage());
                Assert.Equal(lastModified, entOrig.getLastModified());
                Assert.Equal(Length, entOrig.getLength());
                Assert.False(entOrig.isAssumeValid());
            }
        }

        [Fact]
        public void testBuildOneFile_Commit()
        {
            string path = "a-file-path";
            var mode = GitSharp.FileMode.RegularFile;
            long lastModified = 1218123387057L;
            int Length = 1342;
            DirCacheEntry entOrig;
            {
                DirCache dc = DirCache.Lock(db);
                DirCacheBuilder b = dc.builder();
                Assert.NotNull(b);

                entOrig = new DirCacheEntry(path);
                entOrig.setFileMode(mode);
                entOrig.setLastModified(lastModified);
                entOrig.setLength(Length);

                Assert.NotSame(path, entOrig.getPathString());
                Assert.Equal(path, entOrig.getPathString());
                Assert.Equal(ObjectId.ZeroId, entOrig.getObjectId());
                Assert.Equal(mode.Bits, entOrig.getRawMode());
                Assert.Equal(0, entOrig.getStage());
                Assert.Equal(lastModified, entOrig.getLastModified());
                Assert.Equal(Length, entOrig.getLength());
                Assert.False(entOrig.isAssumeValid());
                b.add(entOrig);

                Assert.True(b.commit());
                Assert.Equal(1, dc.getEntryCount());
                Assert.Same(entOrig, dc.getEntry(0));
                Assert.False(new FileInfo(db.Directory+ "/index.lock").Exists);
            }
            {
                DirCache dc = DirCache.read(db);
                Assert.Equal(1, dc.getEntryCount());

                DirCacheEntry entRead = dc.getEntry(0);
                Assert.NotSame(entOrig, entRead);
                Assert.Equal(path, entRead.getPathString());
                Assert.Equal(ObjectId.ZeroId, entOrig.getObjectId());
                Assert.Equal(mode.Bits, entOrig.getRawMode());
                Assert.Equal(0, entOrig.getStage());
                Assert.Equal(lastModified, entOrig.getLastModified());
                Assert.Equal(Length, entOrig.getLength());
                Assert.False(entOrig.isAssumeValid());
            }
        }

        [Fact]
        public void testFindSingleFile()
        {
            string path = "a-file-path";
            DirCache dc = DirCache.read(db);
            DirCacheBuilder b = dc.builder();
            Assert.NotNull(b);

            DirCacheEntry entOrig = new DirCacheEntry(path);
            Assert.NotSame(path, entOrig.getPathString());
            Assert.Equal(path, entOrig.getPathString());
            b.add(entOrig);
            b.finish();

            Assert.Equal(1, dc.getEntryCount());
            Assert.Same(entOrig, dc.getEntry(0));
            Assert.Equal(0, dc.findEntry(path));

            Assert.Equal(-1, dc.findEntry("@@-before"));
            Assert.Equal(0, real(dc.findEntry("@@-before")));

            Assert.Equal(-2, dc.findEntry("a-zoo"));
            Assert.Equal(1, real(dc.findEntry("a-zoo")));

            Assert.Same(entOrig, dc.getEntry(path));
        }

        [Fact]
        public void testAdd_InGitSortOrder()
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
            for (int i = 0; i < paths.Length; i++)
            {
                Assert.Same(ents[i], dc.getEntry(i));
                Assert.Equal(paths[i], dc.getEntry(i).getPathString());
                Assert.Equal(i, dc.findEntry(paths[i]));
                Assert.Same(ents[i], dc.getEntry(paths[i]));
            }
        }

        [Fact]
        public void testAdd_ReverseGitSortOrder()
        {
            DirCache dc = DirCache.read(db);

            string[] paths = { "a.", "a.b", "a/b", "a0b" };
            DirCacheEntry[] ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
                ents[i] = new DirCacheEntry(paths[i]);

            DirCacheBuilder b = dc.builder();
            for (int i = ents.Length - 1; i >= 0; i--)
                b.add(ents[i]);
            b.finish();

            Assert.Equal(paths.Length, dc.getEntryCount());
            for (int i = 0; i < paths.Length; i++)
            {
                Assert.Same(ents[i], dc.getEntry(i));
                Assert.Equal(paths[i], dc.getEntry(i).getPathString());
                Assert.Equal(i, dc.findEntry(paths[i]));
                Assert.Same(ents[i], dc.getEntry(paths[i]));
            }
        }

        [Fact]
        public void testBuilderClear()
        {
            DirCache dc = DirCache.read(db);

            string[] paths = { "a.", "a.b", "a/b", "a0b" };
            DirCacheEntry[] ents = new DirCacheEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
                ents[i] = new DirCacheEntry(paths[i]);
            {
                DirCacheBuilder b = dc.builder();
                for (int i = 0; i < ents.Length; i++)
                    b.add(ents[i]);
                b.finish();
            }
            Assert.Equal(paths.Length, dc.getEntryCount());
            {
                DirCacheBuilder b = dc.builder();
                b.finish();
            }
            Assert.Equal(0, dc.getEntryCount());
        }

        private static int real(int eIdx)
        {
            if (eIdx < 0)
                eIdx = -(eIdx + 1);
            return eIdx;
        }

    }
}
