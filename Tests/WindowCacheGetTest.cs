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
		private IList<TestObject> _toLoad;

		public override void setUp()
		{
			base.setUp();

			_toLoad = new List<TestObject>();
			var br = new StreamReader("Resources/all_packed_objects.txt", Constants.CHARSET);
			try
			{
				string line;
				while ((line = br.ReadLine()) != null)
				{
					string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					var testObject = new TestObject
								{
									Id = ObjectId.FromString(parts[0]),
									Type = parts[1],
									RawSize = Convert.ToInt32(parts[2]),
									Size = Convert.ToInt64(parts[3]),
									Offset = Convert.ToInt64(parts[4])
								};

					_toLoad.Add(testObject);
				}
			}
			finally
			{
				br.Close();
			}

			Assert.AreEqual(96, _toLoad.Count);
		}

		[Test]
		public void testCache_Defaults()
		{
			var cfg = new WindowCacheConfig();
			WindowCache.reconfigure(cfg);
			DoCacheTests();
			CheckLimits(cfg);

			WindowCache cache = WindowCache.Instance;
			Assert.AreEqual(6, cache.getOpenFiles());
			Assert.AreEqual(17346, cache.getOpenBytes());
		}

		[Test]
		public void testCache_TooFewFiles()
		{
			var cfg = new WindowCacheConfig { PackedGitOpenFiles = 2 };
			WindowCache.reconfigure(cfg);
			DoCacheTests();
			CheckLimits(cfg);
		}

		[Test]
		public void testCache_TooSmallLimit()
		{
			var cfg = new WindowCacheConfig { PackedGitWindowSize = 4096, PackedGitLimit = 4096 };
			WindowCache.reconfigure(cfg);
			DoCacheTests();
			CheckLimits(cfg);
		}

		private static void CheckLimits(WindowCacheConfig cfg)
		{
			WindowCache cache = WindowCache.Instance;
			Assert.IsTrue(cache.getOpenFiles() <= cfg.PackedGitOpenFiles);
			Assert.IsTrue(cache.getOpenBytes() <= cfg.PackedGitLimit);
			Assert.IsTrue(0 < cache.getOpenFiles());
			Assert.IsTrue(0 < cache.getOpenBytes());
		}

		private void DoCacheTests()
		{
			foreach (TestObject o in _toLoad)
			{
				ObjectLoader or = db.openObject(new WindowCursor(), o.Id);
				Assert.IsNotNull(or);
				Assert.IsTrue(or is PackedObjectLoader);
				Assert.AreEqual(o.Type, or.Type);
				Assert.AreEqual(o.RawSize, or.RawSize);
				Assert.AreEqual(o.Offset, ((PackedObjectLoader)or).ObjectOffset);
			}
		}

		#region Nested Types

		private class TestObject
		{
			private string _type;

			public ObjectId Id { get; set; }
			public int RawSize { get; set; }
			public long Offset { get; set; }
			public long Size { private get; set; }

			public string Type
			{
				get { return _type; }
				set
				{
					_type = value;
					byte[] typeRaw = Constants.encode(value + " ");
					var ptr = new MutableInteger();
					Constants.decodeTypeString(Id, typeRaw, (byte)' ', ptr);
				}
			}

			public override string ToString()
			{
				// 4b825dc642cb6eb9a060e54bf8d69288fbee4904 tree   0 9 7782
				return Id + " " + Type + " " + Size + " " + RawSize + " " + Offset;
			}
		}

		#endregion
	}
}