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


using GitSharp.Core.Transport;
using NUnit.Framework;

[TestFixture]
public class LongMapTest
{
	private LongMap<long?> map;

    [SetUp]
    protected void setUp()
    {
        map = new LongMap<long?>();
    }

    [Test]
    public void testEmptyMap()
    {
        Assert.IsFalse(map.containsKey(0));
        Assert.IsFalse(map.containsKey(1));

        Assert.IsNull(map.get(0));
        Assert.IsNull(map.get(1));

        Assert.IsNull(map.remove(0));
        Assert.IsNull(map.remove(1));
    }

    [Test]
    public void testInsertMinValue()
    {
        long min = long.MinValue;
        Assert.IsNull(map.put(long.MinValue, min));
        Assert.IsTrue(map.containsKey(long.MinValue));
        Assert.AreEqual(min, map.get(long.MinValue));  // Switch from AreSame to AreEqual as valuetype as passed by value
        Assert.IsFalse(map.containsKey(int.MinValue));
    }

    [Test]
    public void testReplaceMaxValue()
    {
        long min = long.MaxValue;
        long one = 1L;
        Assert.IsNull(map.put(long.MaxValue, min));
        Assert.AreEqual(min, map.get(long.MaxValue));
        Assert.AreEqual(min, map.put(long.MaxValue, one));
        Assert.AreEqual(one, map.get(long.MaxValue));
    }

    [Test]
    public void testRemoveOne()
    {
        long start = 1;
        Assert.IsNull(map.put(start, start));
        Assert.AreEqual(start, map.remove(start));
        Assert.IsFalse(map.containsKey(start));
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
        Assert.IsNull(map.put(0L, 0L));
        Assert.IsNull(map.put(1, 1L));
        Assert.AreEqual(0L, map.remove(0));

        Assert.IsFalse(map.containsKey(0));
        Assert.IsTrue(map.containsKey(1));
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
        Assert.IsNull(map.put(0, 0L));
        Assert.IsNull(map.put(1, 1L));
        Assert.AreEqual(1L, map.remove(1));

        Assert.IsTrue(map.containsKey(0));
        Assert.IsFalse(map.containsKey(1));
    }

    [Test]
    public void testSmallMap()
    {
        long start = 12;
        long n = 8;
        for (long i = start; i < start + n; i++)
            Assert.IsNull(map.put(i, i));
        for (long i = start; i < start + n; i++)
            Assert.AreEqual(i, map.get(i));
    }

    [Test]
    public void testLargeMap()
    {
        long start = int.MaxValue;
        long n = 100000;
        for (long i = start; i < start + n; i++)
            Assert.IsNull(map.put(i, i));
        for (long i = start; i < start + n; i++)
            Assert.AreEqual(i, map.get(i));
    }
}
