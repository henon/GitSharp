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
    using GitSharp.Core.DirectoryCache;
    [TestFixture]
    public class DirCacheFindTest : RepositoryTestCase
    {
        [Test]
        public void testEntriesWithin()
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
            {
            	b.add(ents[i]);
            }
            b.finish();

            Assert.AreEqual(paths.Length, dc.getEntryCount());
            for (int i = 0; i < ents.Length; i++)
            {
            	Assert.AreSame(ents[i], dc.getEntry(i));
            }

            DirCacheEntry[] aContents = dc.getEntriesWithin("a");
            Assert.IsNotNull(aContents);
            Assert.AreEqual(aLast - aFirst + 1, aContents.Length);
            for (int i = aFirst, j = 0; i <= aLast; i++, j++)
			{
                    Assert.AreSame(ents[i], aContents[j]);
            }

            aContents = dc.getEntriesWithin("a/");
            Assert.IsNotNull(aContents);
            Assert.AreEqual(aLast - aFirst + 1, aContents.Length);
            for (int i = aFirst, j = 0; i <= aLast; i++, j++)
			{
                Assert.AreSame(ents[i], aContents[j]);
            }

            Assert.IsNotNull(dc.getEntriesWithin("a."));
            Assert.AreEqual(0, dc.getEntriesWithin("a.").Length);

            Assert.IsNotNull(dc.getEntriesWithin("a0b"));
            Assert.AreEqual(0, dc.getEntriesWithin("a0b.").Length);

            Assert.IsNotNull(dc.getEntriesWithin("zoo"));
            Assert.AreEqual(0, dc.getEntriesWithin("zoo.").Length);
        }
    }
}
