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
using NUnit.Framework;

namespace GitSharp.Tests.Transport
{
	[TestFixture]
	public class LongMapTest
	{
		private LongMap<long> _map;

		[SetUp]
		public void setUp()
		{
			_map = new LongMap<long>();
		}

		[Test]
		public void testEmptyMap()
		{
			Assert.IsFalse(_map.ContainsKey(0));
			Assert.IsFalse(_map.ContainsKey(1));

			AssertHelper.Throws<KeyNotFoundException>(() => { var number = _map[0]; });
			AssertHelper.Throws<KeyNotFoundException>(() => { var number = _map[1]; });

			Assert.IsFalse(_map.Remove(0));
			Assert.IsFalse(_map.Remove(1));
		}

		[Test]
		public void testInsertMinValue()
		{
			long min = long.MinValue;
			Assert.AreEqual(min, _map[long.MinValue] = min);
			Assert.IsTrue(_map.ContainsKey(long.MinValue));
			Assert.AreEqual(min, _map[long.MinValue]);
			Assert.IsFalse(_map.ContainsKey(int.MinValue));
		}

		[Test]
		public void testReplaceMaxValue()
		{
			long min = Convert.ToInt64(long.MaxValue);
			long one = Convert.ToInt64(1);
			Assert.AreEqual(min, _map[long.MaxValue] = min);
			Assert.AreEqual(min, _map[long.MaxValue]);
			Assert.AreEqual(one, _map[long.MaxValue] = one);
		}

		[Test]
		public void testRemoveOne()
		{
			const long start = 1;
			Assert.AreEqual(1, _map[start] = Convert.ToInt64(start));
			Assert.IsTrue(_map.Remove(start));
			Assert.IsFalse(_map.ContainsKey(start));
		}

		[Test]
		public void testRemoveCollision1()
		{
			// This test relies upon the fact that we always >>> 1 the value
			// to derive an unsigned hash code. Thus, 0 and 1 fall into the
			// same hash bucket. Further it relies on the fact that we add
			// the 2nd put at the top of the chain, so removing the 1st will
			// cause a different code path.
			//
			Assert.AreEqual(0, _map[0] = Convert.ToInt64(0));
			Assert.AreEqual(1, _map[1] = Convert.ToInt64(1));
			Assert.AreEqual(Convert.ToInt64(0), _map[0]);
			Assert.IsTrue(_map.Remove(0));

			Assert.IsFalse(_map.ContainsKey(0));
			Assert.IsTrue(_map.ContainsKey(1));
		}

		[Test]
		public void testRemoveCollision2()
		{
			// This test relies upon the fact that we always >>> 1 the value
			// to derive an unsigned hash code. Thus, 0 and 1 fall into the
			// same hash bucket. Further it relies on the fact that we add
			// the 2nd put at the top of the chain, so removing the 2nd will
			// cause a different code path.
			//
			Assert.AreEqual(0, _map[0] = Convert.ToInt64(0));
			Assert.AreEqual(1, _map[1] = Convert.ToInt64(1));
			Assert.AreEqual(Convert.ToInt64(1), _map[1]);
			Assert.IsTrue(_map.Remove(1));

			Assert.IsTrue(_map.ContainsKey(0));
			Assert.IsFalse(_map.ContainsKey(1));
		}

		[Test]
		public void testSmallMap()
		{
			const long start = 12;
			const long n = 8;
			for (long i = start; i < start + n; i++)
				Assert.AreEqual(i, _map[i] = Convert.ToInt64(i));
			for (long i = start; i < start + n; i++)
				Assert.AreEqual(Convert.ToInt64(i), _map[i]);
		}

		[Test]
		public void testLargeMap()
		{
			const long start = int.MaxValue;
			const long n = 100000;
			for (long i = start; i < start + n; i++)
			{
				Assert.AreEqual(i, _map[i] = Convert.ToInt64(i));
			}
			for (long i = start; i < start + n; i++)
			{
				Assert.AreEqual(Convert.ToInt64(i), _map[i]);
			}
		}
	}

	public class LongMap<T> : Dictionary<long, T>
	{
	}
}