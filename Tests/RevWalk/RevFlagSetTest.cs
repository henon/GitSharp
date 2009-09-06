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

using GitSharp.Tests.Util;
using GitSharp.RevWalk;
using NUnit.Framework;

namespace GitSharp.Tests.RevWalk
{
	[TestFixture]
	public class RevFlagSetTest : RevWalkTestCase
	{
		[Test]
		public void testEmpty()
		{
			var flagSet = new RevFlagSet();
			Assert.AreEqual(0, flagSet.Mask);
			Assert.AreEqual(0, flagSet.Count);
			Assert.IsNotNull(flagSet.GetEnumerator());
			Assert.IsFalse(flagSet.GetEnumerator().MoveNext());
		}

		[Test]
		public void testAddOne()
		{
			const string flagName = "flag";
			RevFlag flag = rw.newFlag(flagName);
			Assert.IsTrue(0 != flag.Mask);
			Assert.AreSame(flagName, flag.Name);

			var flagSet = new RevFlagSet();
			Assert.IsTrue(flagSet.Add(flag));
			Assert.IsFalse(flagSet.Add(flag));
			Assert.AreEqual(flag.Mask, flagSet.Mask);
			Assert.AreEqual(1, flagSet.Count);
			var i = flagSet.GetEnumerator();
			Assert.IsTrue(i.MoveNext());
			Assert.AreSame(flag, i.Current);
			Assert.IsFalse(i.MoveNext());
		}

		[Test]
		public void testAddTwo()
		{
			RevFlag flag1 = rw.newFlag("flag_1");
			RevFlag flag2 = rw.newFlag("flag_2");
			Assert.IsTrue((flag1.Mask & flag2.Mask) == 0);

			var flagSet = new RevFlagSet();
			Assert.IsTrue(flagSet.Add(flag1));
			Assert.IsTrue(flagSet.Add(flag2));
			Assert.AreEqual(flag1.Mask | flag2.Mask, flagSet.Mask);
			Assert.AreEqual(2, flagSet.Count);
		}

		[Test]
		public void testContainsAll()
		{
			RevFlag flag1 = rw.newFlag("flag_1");
			RevFlag flag2 = rw.newFlag("flag_2");
			var set1 = new RevFlagSet();
			Assert.IsTrue(set1.Add(flag1));
			Assert.IsTrue(set1.Add(flag2));

			Assert.IsTrue(set1.ContainsAll(set1));
			Assert.IsTrue(set1.ContainsAll(new[] { flag1, flag2 }));

			var set2 = new RevFlagSet { rw.newFlag("flag_3") };
			Assert.IsFalse(set1.ContainsAll(set2));
		}

		[Test]
		public void testEquals()
		{
			RevFlag flag1 = rw.newFlag("flag_1");
			RevFlag flag2 = rw.newFlag("flag_2");
			var flagSet = new RevFlagSet();
			Assert.IsTrue(flagSet.Add(flag1));
			Assert.IsTrue(flagSet.Add(flag2));

			Assert.IsTrue(new RevFlagSet(flagSet).Equals(flagSet));
			Assert.IsTrue(new RevFlagSet(new[] { flag1, flag2 }).Equals(flagSet));
		}

		[Test]
		public virtual void testRemove()
		{
			RevFlag flag1 = rw.newFlag("flag_1");
			RevFlag flag2 = rw.newFlag("flag_2");
			var flagSet = new RevFlagSet();
			Assert.IsTrue(flagSet.Add(flag1));
			Assert.IsTrue(flagSet.Add(flag2));

			Assert.IsTrue(flagSet.Remove(flag1));
			Assert.IsFalse(flagSet.Remove(flag1));
			Assert.AreEqual(flag2.Mask, flagSet.Mask);
			Assert.IsFalse(flagSet.Contains(flag1));
		}

		[Test]
		public virtual void testContains()
		{
			RevFlag flag1 = rw.newFlag("flag_1");
			RevFlag flag2 = rw.newFlag("flag_2");
			var flagSet = new RevFlagSet {flag1};
			Assert.IsTrue(flagSet.Contains(flag1));
			Assert.IsFalse(flagSet.Contains(flag2));
			//Assert.IsFalse(flagSet.Contains("bob"));
		}
	}
}