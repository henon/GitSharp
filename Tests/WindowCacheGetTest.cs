/*
 * Copyright (C) 2009, Google Inc.
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

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Tests
{
    [TestFixture]
    public class WindowCacheGetTest : RepositoryTestCase
    {
#if false
	private List<TestObject> toLoad;

	@Override
	public void setUp() throws Exception {
		super.setUp();

		toLoad = new ArrayList<TestObject>();
		final BufferedReader br = new BufferedReader(new InputStreamReader(
				new FileInputStream(JGitTestUtil
						.getTestResourceFile("all_packed_objects.txt")),
				Constants.CHARSET));
		try {
			String line;
			while ((line = br.readLine()) != null) {
				final String[] parts = line.split(" {1,}");
				final TestObject o = new TestObject();
				o.id = ObjectId.fromString(parts[0]);
				o.setType(parts[1]);
				o.rawSize = Integer.parseInt(parts[2]);
				// parts[3] is the size-in-pack
				o.offset = Long.parseLong(parts[4]);
				toLoad.add(o);
			}
		} finally {
			br.close();
		}
		assertEquals(96, toLoad.size());
	}

	public void testCache_Defaults() throws IOException {
		final WindowCacheConfig cfg = new WindowCacheConfig();
		WindowCache.reconfigure(cfg);
		doCacheTests();
		checkLimits(cfg);

		final WindowCache cache = WindowCache.getInstance();
		assertEquals(6, cache.getOpenFiles());
		assertEquals(17346, cache.getOpenBytes());
	}

	public void testCache_TooFewFiles() throws IOException {
		final WindowCacheConfig cfg = new WindowCacheConfig();
		cfg.setPackedGitOpenFiles(2);
		WindowCache.reconfigure(cfg);
		doCacheTests();
		checkLimits(cfg);
	}

	public void testCache_TooSmallLimit() throws IOException {
		final WindowCacheConfig cfg = new WindowCacheConfig();
		cfg.setPackedGitWindowSize(4096);
		cfg.setPackedGitLimit(4096);
		WindowCache.reconfigure(cfg);
		doCacheTests();
		checkLimits(cfg);
	}

	private void checkLimits(final WindowCacheConfig cfg) {
		final WindowCache cache = WindowCache.getInstance();
		assertTrue(cache.getOpenFiles() <= cfg.getPackedGitOpenFiles());
		assertTrue(cache.getOpenBytes() <= cfg.getPackedGitLimit());
		assertTrue(0 < cache.getOpenFiles());
		assertTrue(0 < cache.getOpenBytes());
	}

	private void doCacheTests() throws IOException {
		for (final TestObject o : toLoad) {
			final ObjectLoader or = db.openObject(o.id);
			assertNotNull(or);
			assertTrue(or instanceof PackedObjectLoader);
			assertEquals(o.type, or.getType());
			assertEquals(o.rawSize, or.getRawSize());
			assertEquals(o.offset, ((PackedObjectLoader) or).getObjectOffset());
		}
	}

	private class TestObject {
		ObjectId id;

		int type;

		int rawSize;

		long offset;

		void setType(final String typeStr) throws CorruptObjectException {
			final byte[] typeRaw = Constants.encode(typeStr + " ");
			final MutableInteger ptr = new MutableInteger();
			type = Constants.decodeTypeString(id, typeRaw, (byte) ' ', ptr);
		}
	}
#endif
    }
}
