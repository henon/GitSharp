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
using GitSharp.Core.DirectoryCache;
using NUnit.Framework;

namespace GitSharp.Core.Tests.DirectoryCache
{
    [TestFixture]
    public class DirCacheLargePathTest : RepositoryTestCase
    {
        [Test]
        public void testPath_4090()
        {
            testLongPath(4090);
        }

        [Test]
        public void testPath_4094()
        {
            testLongPath(4094);
        }

        [Test]
        public void testPath_4095()
        {
            testLongPath(4095);
        }

        [Test]
        public void testPath_4096()
        {
            testLongPath(4096);
        }

        [Test]
        public void testPath_16384()
        {
            testLongPath(16384);
        }

        private void testLongPath(int len)
        {
            string longPath = makeLongPath(len);
            string shortPath = "~~~ shorter-path";

            DirCacheEntry longEnt = new DirCacheEntry(longPath);
            DirCacheEntry shortEnt = new DirCacheEntry(shortPath);
            Assert.AreEqual(longPath, longEnt.getPathString());
            Assert.AreEqual(shortPath, shortEnt.getPathString());

            DirCache dc1 = DirCache.Lock(db);
            DirCacheBuilder b = dc1.builder();
            b.add(longEnt);
            b.add(shortEnt);
            Assert.IsTrue(b.commit());
            Assert.AreEqual(2, dc1.getEntryCount());
            Assert.AreSame(longEnt, dc1.getEntry(0));
            Assert.AreSame(shortEnt, dc1.getEntry(1));

            DirCache dc2 = DirCache.read(db);
            Assert.AreEqual(2, dc2.getEntryCount());
            Assert.AreNotSame(longEnt, dc2.getEntry(0));
            Assert.AreEqual(longPath, dc2.getEntry(0).getPathString());
            Assert.AreNotSame(shortEnt, dc2.getEntry(1));
            Assert.AreEqual(shortPath, dc2.getEntry(1).getPathString());
        }

        private static string makeLongPath(int len)
        {
            StringBuilder r = new StringBuilder(len);
            for (int i = 0; i < len; i++)
            {
                r.Append('a' + (i % 26));
            }
            return r.ToString();
        }
    }
}
