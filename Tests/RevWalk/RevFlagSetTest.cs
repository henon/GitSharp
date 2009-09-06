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
using GitSharp.Tests.Util;
using GitSharp.RevWalk;
using NUnit.Framework;

namespace GitSharp.Tests.RevWalk
{
    [TestFixture]
    public class RevFlagSetTest : RevWalkTestCase
    {

    [Test]
	public void testEmpty() {
		RevFlagSet set = new RevFlagSet();
		Assert.AreEqual(0, set.Mask);
		Assert.AreEqual(0, set.Count);

        Assert.Fail("Test not fully migrated");
        //Assert.IsNotNull(set.iterator());
        //Assert.IsFalse(set.iterator().hasNext());
	}

    [Test]
	public void testAddOne() {
		string flagName = "flag";
		RevFlag flag = rw.newFlag(flagName);
		Assert.IsTrue(0 != flag.Mask);
		Assert.AreSame(flagName, flag.Name);

		RevFlagSet set = new RevFlagSet();
		Assert.IsTrue(set.Add(flag));
		Assert.IsFalse(set.Add(flag));
		Assert.AreEqual(flag.Mask, set.Mask);
		Assert.AreEqual(1, set.Count);

        Assert.Fail("Test not fully migrated");
        //Iterator<RevFlag> i = set.iterator();
        //Assert.IsTrue(i.hasNext());
        //Assert.AreSame(flag, i.next());
        //Assert.IsFalse(i.hasNext());
	}

    [Test]
	public void testAddTwo() {
		RevFlag flag1 = rw.newFlag("flag_1");
		RevFlag flag2 = rw.newFlag("flag_2");
		Assert.IsTrue((flag1.Mask & flag2.Mask) == 0);

		RevFlagSet set = new RevFlagSet();
		Assert.IsTrue(set.Add(flag1));
		Assert.IsTrue(set.Add(flag2));
		Assert.AreEqual(flag1.Mask | flag2.Mask, set.Mask);
		Assert.AreEqual(2, set.Count);
	}

    [Test]
	public void testContainsAll() {
		RevFlag flag1 = rw.newFlag("flag_1");
		RevFlag flag2 = rw.newFlag("flag_2");
		RevFlagSet set1 = new RevFlagSet();
		Assert.IsTrue(set1.Add(flag1));
		Assert.IsTrue(set1.Add(flag2));

		Assert.IsTrue(set1.ContainsAll(set1));
		Assert.IsTrue(set1.ContainsAll(new List<RevFlag>(new [] { flag1, flag2 })));

		RevFlagSet set2 = new RevFlagSet();
		set2.Add(rw.newFlag("flag_3"));
		Assert.IsFalse(set1.ContainsAll(set2));
	}

    [Test]
	public void testEquals() {
		RevFlag flag1 = rw.newFlag("flag_1");
		RevFlag flag2 = rw.newFlag("flag_2");
		RevFlagSet set = new RevFlagSet();
		Assert.IsTrue(set.Add(flag1));
		Assert.IsTrue(set.Add(flag2));

		Assert.IsTrue(new RevFlagSet(set).Equals(set));
        Assert.IsTrue(new RevFlagSet(new List<RevFlag>(new [] { flag1, flag2 }))
				.Equals(set));
	}

    [Test]
	public void testRemove() {
		RevFlag flag1 = rw.newFlag("flag_1");
		RevFlag flag2 = rw.newFlag("flag_2");
		RevFlagSet set = new RevFlagSet();
		Assert.IsTrue(set.Add(flag1));
		Assert.IsTrue(set.Add(flag2));

		Assert.IsTrue(set.Remove(flag1));
		Assert.IsFalse(set.Remove(flag1));
		Assert.AreEqual(flag2.Mask, set.Mask);
		Assert.IsFalse(set.Contains(flag1));
	}

    [Test]
	public void testContains() {
		RevFlag flag1 = rw.newFlag("flag_1");
		RevFlag flag2 = rw.newFlag("flag_2");
		RevFlagSet set = new RevFlagSet();
		set.Add(flag1);
		Assert.IsTrue(set.Contains(flag1));
		Assert.IsFalse(set.Contains(flag2));

        Assert.Fail("Test not fully migrated");
		//Assert.IsFalse(set.Contains("bob"));
	}
    }
}
