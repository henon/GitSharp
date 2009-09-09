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

using System.Collections.Generic;
using GitSharp.RevWalk;
using Xunit;

namespace GitSharp.Tests.RevWalk
{
	public class RevFlagSetTest : RevWalkTestCase
	{
		[Fact]
		public void testEmpty()
		{
			var flagSet = new RevFlagSet();
			Assert.Equal(0, flagSet.Mask);
			Assert.Equal(0, flagSet.Count);
			Assert.NotNull(flagSet.GetEnumerator());
			Assert.False(flagSet.GetEnumerator().MoveNext());
		}

		[Fact]
		public void testAddOne()
		{
			const string flagName = "flag";
			RevFlag flag = rw.newFlag(flagName);
			Assert.True(0 != flag.Mask);
			Assert.Same(flagName, flag.Name);

			var flagSet = new RevFlagSet();
			Assert.True(flagSet.Add(flag));
			Assert.False(flagSet.Add(flag));
			Assert.Equal(flag.Mask, flagSet.Mask);
			Assert.Equal(1, flagSet.Count);
			var i = flagSet.GetEnumerator();
			Assert.True(i.MoveNext());
			Assert.Same(flag, i.Current);
			Assert.False(i.MoveNext());
		}

		[Fact]
		public void testAddTwo()
		{
			RevFlag flag1 = rw.newFlag("flag_1");
			RevFlag flag2 = rw.newFlag("flag_2");
			Assert.True((flag1.Mask & flag2.Mask) == 0);

			var flagSet = new RevFlagSet();
			Assert.True(flagSet.Add(flag1));
			Assert.True(flagSet.Add(flag2));
			Assert.Equal(flag1.Mask | flag2.Mask, flagSet.Mask);
			Assert.Equal(2, flagSet.Count);
		}

		[Fact]
		public void testContainsAll()
		{
			RevFlag flag1 = rw.newFlag("flag_1");
			RevFlag flag2 = rw.newFlag("flag_2");
			var set1 = new RevFlagSet();
			Assert.True(set1.Add(flag1));
			Assert.True(set1.Add(flag2));

			Assert.True(set1.ContainsAll(set1));
			Assert.True(set1.ContainsAll(new[] { flag1, flag2 }));

			var set2 = new RevFlagSet { rw.newFlag("flag_3") };
			Assert.False(set1.ContainsAll(set2));
		}

		[Fact]
		public void testEquals()
		{
			RevFlag flag1 = rw.newFlag("flag_1");
			RevFlag flag2 = rw.newFlag("flag_2");
			var flagSet = new RevFlagSet();
			Assert.True(flagSet.Add(flag1));
			Assert.True(flagSet.Add(flag2));

			Assert.True(new RevFlagSet(flagSet).Equals(flagSet));
			Assert.True(new RevFlagSet(new[] { flag1, flag2 }).Equals(flagSet));
		}

		[Fact]
		public virtual void testRemove()
		{
			RevFlag flag1 = rw.newFlag("flag_1");
			RevFlag flag2 = rw.newFlag("flag_2");
			var flagSet = new RevFlagSet();
			Assert.True(flagSet.Add(flag1));
			Assert.True(flagSet.Add(flag2));

			Assert.True(flagSet.Remove(flag1));
			Assert.False(flagSet.Remove(flag1));
			Assert.Equal(flag2.Mask, flagSet.Mask);
			Assert.False(flagSet.Contains(flag1));
		}

		[Fact]
		public virtual void testContains()
		{
			RevFlag flag1 = rw.newFlag("flag_1");
			RevFlag flag2 = rw.newFlag("flag_2");
			var flagSet = new RevFlagSet {flag1};
			Assert.True(flagSet.Contains(flag1));
			Assert.False(flagSet.Contains(flag2));
			//Assert.False(flagSet.Contains("bob"));
		}
	}
}
