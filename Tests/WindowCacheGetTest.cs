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

using System;
using System.Collections.Generic;
using System.IO;
using GitSharp.Util;
using NUnit.Framework;

namespace GitSharp.Tests
{
	[TestFixture]
	public class WindowCacheGetTest : RepositoryTestCase
	{
		private IList<TestObject> toLoad;

		public override void setUp()
		{
			base.setUp();

			toLoad = new List<TestObject>();
			BufferedReader br = new BufferedReader(new StreamReader("Resources/all_packed_objects.txt", Constants.CHARSET));
			try
			{
				string line;
				while ((line = br.ReadLine()) != null)
				{
					string[] parts = line.Split(new char[] { ' '}, StringSplitOptions.RemoveEmptyEntries);
					TestObject o = new TestObject();
					o.id = ObjectId.FromString(parts[0]);
					o.setType(parts[1]);
					o.rawSize = Convert.ToInt32(parts[2]);
					// parts[3] is the size-in-pack
					o.offset = Convert.ToInt64(parts[4]);
					toLoad.Add(o);
				}
			}
			finally
			{
				br.Close();
			}

			Assert.AreEqual(96, toLoad.Count);
		}

		[Test]
		public void testCache_Defaults()
		{
			WindowCacheConfig cfg = new WindowCacheConfig();
			WindowCache.reconfigure(cfg);
			doCacheTests();
			checkLimits(cfg);

			WindowCache cache = WindowCache.getInstance();
			Assert.AreEqual(6, cache.getOpenFiles());
			Assert.AreEqual(17346, cache.getOpenBytes());
		}

		[Test]
		public void testCache_TooFewFiles()
		{
			WindowCacheConfig cfg = new WindowCacheConfig();
			cfg.setPackedGitOpenFiles(2);
			WindowCache.reconfigure(cfg);
			doCacheTests();
			checkLimits(cfg);
		}

		[Test]
		public void testCache_TooSmallLimit()
		{
			WindowCacheConfig cfg = new WindowCacheConfig();
			cfg.setPackedGitWindowSize(4096);
			cfg.setPackedGitLimit(4096);
			WindowCache.reconfigure(cfg);
			doCacheTests();
			checkLimits(cfg);
		}

		private void checkLimits(WindowCacheConfig cfg)
		{
			WindowCache cache = WindowCache.getInstance();
			Assert.IsTrue(cache.getOpenFiles() <= cfg.getPackedGitOpenFiles());
			Assert.IsTrue(cache.getOpenBytes() <= cfg.getPackedGitLimit());
			Assert.IsTrue(0 < cache.getOpenFiles());
			Assert.IsTrue(0 < cache.getOpenBytes());
		}

		private void doCacheTests()
		{
			foreach (TestObject o in toLoad)
			{
				ObjectLoader or = db.openObject(new WindowCursor(), o.id);
				Assert.IsNotNull(or);
				Assert.IsTrue(or is PackedObjectLoader);
				Assert.AreEqual(o.type, or.getType());
				Assert.AreEqual(o.rawSize, or.getRawSize());
				Assert.AreEqual(o.offset, ((PackedObjectLoader)or).getObjectOffset());
			}
		}

		private class TestObject
		{
			internal ObjectId id;
			internal int type;
			internal int rawSize;
			internal long offset;

			internal virtual void setType(string typeStr)
			{
				byte[] typeRaw = Constants.encode(typeStr + " ");
				MutableInteger ptr = new MutableInteger();
				type = Constants.decodeTypeString(id, typeRaw, (byte) ' ', ptr);
			}
		}
	}
}