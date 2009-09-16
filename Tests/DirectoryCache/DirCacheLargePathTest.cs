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

using System.Text;
using GitSharp.DirectoryCache;
using GitSharp.Tests.Util;
using Xunit;

namespace GitSharp.Tests.DirectoryCache
{
    public class DirCacheLargePathTest : RepositoryTestCase
    {
        [Fact]
        public void testPath_4090()
        {
            testLongPath(4090);
        }

        [Fact]
        public void testPath_4094()
        {
            testLongPath(4094);
        }

        [Fact]
        public void testPath_4095()
        {
            testLongPath(4095);
        }

        [Fact]
        public void testPath_4096()
        {
            testLongPath(4096);
        }

        [Fact]
        public void testPath_16384()
        {
            testLongPath(16384);
        }

        private void testLongPath(int len)
        {
            string longPath = MakeLongPath(len);
            const string shortPath = "~~~ shorter-path";

            var longEnt = new DirCacheEntry(longPath);
            var shortEnt = new DirCacheEntry(shortPath);
            Assert.Equal(longPath, longEnt.getPathString());
            Assert.Equal(shortPath, shortEnt.getPathString());

			DirCache dc1 = DirCache.Lock(db);
			DirCacheBuilder b = dc1.builder();
			b.add(longEnt);
			b.add(shortEnt);
			Assert.True(b.commit());

			Assert.Equal(2, dc1.getEntryCount());
			Assert.Same(longEnt, dc1.getEntry(0));
			Assert.Same(shortEnt, dc1.getEntry(1));

			DirCache dc2 = DirCache.read(db);
			Assert.Equal(2, dc2.getEntryCount());

			Assert.NotSame(longEnt, dc2.getEntry(0));
			Assert.Equal(longPath, dc2.getEntry(0).getPathString());

			Assert.NotSame(shortEnt, dc2.getEntry(1));
			Assert.Equal(shortPath, dc2.getEntry(1).getPathString());
        }

        private static string MakeLongPath(int len)
        {
            var r = new StringBuilder(len);
            for (int i = 0; i < len; i++)
            {
            	r.Append('a' + (i % 26));
            }
            return r.ToString();
        }
    }
}
