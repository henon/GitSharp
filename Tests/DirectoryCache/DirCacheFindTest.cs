/*
 * Copyright (C) 2008, Google Inc.
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
    [TestFixture]
    public class DirCacheFindTest : RepositoryTestCase
    {
#if false
	public void testEntriesWithin() throws Exception {
		final DirCache dc = DirCache.read(db);

		final String[] paths = { "a.", "a/b", "a/c", "a/d", "a0b" };
		final DirCacheEntry[] ents = new DirCacheEntry[paths.length];
		for (int i = 0; i < paths.length; i++)
			ents[i] = new DirCacheEntry(paths[i]);
		final int aFirst = 1;
		final int aLast = 3;

		final DirCacheBuilder b = dc.builder();
		for (int i = 0; i < ents.length; i++)
			b.add(ents[i]);
		b.finish();

		assertEquals(paths.length, dc.getEntryCount());
		for (int i = 0; i < ents.length; i++)
			assertSame(ents[i], dc.getEntry(i));

		{
			final DirCacheEntry[] aContents = dc.getEntriesWithin("a");
			assertNotNull(aContents);
			assertEquals(aLast - aFirst + 1, aContents.length);
			for (int i = aFirst, j = 0; i <= aLast; i++, j++)
				assertSame(ents[i], aContents[j]);
		}
		{
			final DirCacheEntry[] aContents = dc.getEntriesWithin("a/");
			assertNotNull(aContents);
			assertEquals(aLast - aFirst + 1, aContents.length);
			for (int i = aFirst, j = 0; i <= aLast; i++, j++)
				assertSame(ents[i], aContents[j]);
		}

		assertNotNull(dc.getEntriesWithin("a."));
		assertEquals(0, dc.getEntriesWithin("a.").length);

		assertNotNull(dc.getEntriesWithin("a0b"));
		assertEquals(0, dc.getEntriesWithin("a0b.").length);

		assertNotNull(dc.getEntriesWithin("zoo"));
		assertEquals(0, dc.getEntriesWithin("zoo.").length);
	}
#endif
    }
}
