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
    public class DirCacheFindTest : RepositoryTestCase
    {
        [Fact]
        public void testEntriesWithin()
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

            Assert.Equal(paths.Length, dc.getEntryCount());
            for (int i = 0; i < ents.Length; i++)
            {
            	Assert.Same(ents[i], dc.getEntry(i));
            }

			DirCacheEntry[] aContents = dc.getEntriesWithin("a");
			Assert.NotNull(aContents);
			Assert.Equal(aLast - aFirst + 1, aContents.Length);
			for (int i = aFirst, j = 0; i <= aLast; i++, j++)
			{
				Assert.Same(ents[i], aContents[j]);
			}

			aContents = dc.getEntriesWithin("a/");
			Assert.NotNull(aContents);
			Assert.Equal(aLast - aFirst + 1, aContents.Length);
			for (int i = aFirst, j = 0; i <= aLast; i++, j++)
			{
				Assert.Same(ents[i], aContents[j]);
			}

            Assert.NotNull(dc.getEntriesWithin("a."));
            Assert.Equal(0, dc.getEntriesWithin("a.").Length);

            Assert.NotNull(dc.getEntriesWithin("a0b"));
            Assert.Equal(0, dc.getEntriesWithin("a0b.").Length);

            Assert.NotNull(dc.getEntriesWithin("zoo"));
            Assert.Equal(0, dc.getEntriesWithin("zoo.").Length);
        }
    }
}
