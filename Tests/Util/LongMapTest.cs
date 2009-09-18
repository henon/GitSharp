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
using Xunit;

namespace GitSharp.Tests.Util
{
	public class LongMapTest : XunitBaseFact
	{
		private LongMap<long> _map;

		protected override void SetUp()
		{
			_map = new LongMap<long>();
		}

		[StrictFactAttribute]
		public void testEmptyMap()
		{
			Assert.False(_map.ContainsKey(0));
			Assert.False(_map.ContainsKey(1));

			Assert.Throws<KeyNotFoundException>(() => { var number = _map[0]; });
			Assert.Throws<KeyNotFoundException>(() => { var number = _map[1]; });

			Assert.False(_map.Remove(0));
			Assert.False(_map.Remove(1));
		}

		[StrictFactAttribute]
		public void testInsertMinValue()
		{
			const long min = long.MinValue;
			Assert.Equal(min, _map[long.MinValue] = min);
			Assert.True(_map.ContainsKey(long.MinValue));
			Assert.Equal(min, _map[long.MinValue]);
			Assert.False(_map.ContainsKey(int.MinValue));
		}

		[StrictFactAttribute]
		public void testReplaceMaxValue()
		{
			long min = Convert.ToInt64(long.MaxValue);
			long one = Convert.ToInt64(1);
			Assert.Equal(min, _map[long.MaxValue] = min);
			Assert.Equal(min, _map[long.MaxValue]);
			Assert.Equal(one, _map[long.MaxValue] = one);
		}

		[StrictFactAttribute]
		public void testRemoveOne()
		{
			const long start = 1;
			Assert.Equal(1, _map[start] = Convert.ToInt64(start));
			Assert.True(_map.Remove(start));
			Assert.False(_map.ContainsKey(start));
		}

		[StrictFactAttribute]
		public void testRemoveCollision1()
		{
			// This test relies upon the fact that we always >>> 1 the value
			// to derive an unsigned hash code. Thus, 0 and 1 fall into the
			// same hash bucket. Further it relies on the fact that we add
			// the 2nd put at the top of the chain, so removing the 1st will
			// cause a different code path.
			//
			Assert.Equal(0, _map[0] = Convert.ToInt64(0));
			Assert.Equal(1, _map[1] = Convert.ToInt64(1));
			Assert.Equal(Convert.ToInt64(0), _map[0]);
			Assert.True(_map.Remove(0));

			Assert.False(_map.ContainsKey(0));
			Assert.True(_map.ContainsKey(1));
		}

		[StrictFactAttribute]
		public void testRemoveCollision2()
		{
			// This test relies upon the fact that we always >>> 1 the value
			// to derive an unsigned hash code. Thus, 0 and 1 fall into the
			// same hash bucket. Further it relies on the fact that we add
			// the 2nd put at the top of the chain, so removing the 2nd will
			// cause a different code path.
			//
			Assert.Equal(0, _map[0] = Convert.ToInt64(0));
			Assert.Equal(1, _map[1] = Convert.ToInt64(1));
			Assert.Equal(Convert.ToInt64(1), _map[1]);
			Assert.True(_map.Remove(1));

			Assert.True(_map.ContainsKey(0));
			Assert.False(_map.ContainsKey(1));
		}

		[StrictFactAttribute]
		public void testSmallMap()
		{
			const long start = 12;
			const long n = 8;

			for (long i = start; i < start + n; i++)
			{
				Assert.Equal(i, _map[i] = Convert.ToInt64(i));
			}

			for (long i = start; i < start + n; i++)
			{
				Assert.Equal(Convert.ToInt64(i), _map[i]);
			}
		}

		[StrictFactAttribute]
		public void testLargeMap()
		{
			const long start = int.MaxValue;
			const long n = 100000;

			for (long i = start; i < start + n; i++)
			{
				Assert.Equal(i, _map[i] = Convert.ToInt64(i));
			}

			for (long i = start; i < start + n; i++)
			{
				Assert.Equal(Convert.ToInt64(i), _map[i]);
			}
		}
	}

	public class LongMap<T> : Dictionary<long, T>
	{
	}
}