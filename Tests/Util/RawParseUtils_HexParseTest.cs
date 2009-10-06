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
using GitSharp.Core;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests.Util
{
    
    [TestFixture]
    public class RawParseUtils_HexParseTest
    {
        
        [Test]
        public void testInt4_1()
        {
            Assert.AreEqual(0, RawParseUtils.parseHexInt4((byte)'0'));
            Assert.AreEqual(1, RawParseUtils.parseHexInt4((byte)'1'));
            Assert.AreEqual(2, RawParseUtils.parseHexInt4((byte)'2'));
            Assert.AreEqual(3, RawParseUtils.parseHexInt4((byte)'3'));
            Assert.AreEqual(4, RawParseUtils.parseHexInt4((byte)'4'));
            Assert.AreEqual(5, RawParseUtils.parseHexInt4((byte)'5'));
            Assert.AreEqual(6, RawParseUtils.parseHexInt4((byte)'6'));
            Assert.AreEqual(7, RawParseUtils.parseHexInt4((byte)'7'));
            Assert.AreEqual(8, RawParseUtils.parseHexInt4((byte)'8'));
            Assert.AreEqual(9, RawParseUtils.parseHexInt4((byte)'9'));
            Assert.AreEqual(10, RawParseUtils.parseHexInt4((byte)'a'));
            Assert.AreEqual(11, RawParseUtils.parseHexInt4((byte)'b'));
            Assert.AreEqual(12, RawParseUtils.parseHexInt4((byte)'c'));
            Assert.AreEqual(13, RawParseUtils.parseHexInt4((byte)'d'));
            Assert.AreEqual(14, RawParseUtils.parseHexInt4((byte)'e'));
            Assert.AreEqual(15, RawParseUtils.parseHexInt4((byte)'f'));

            Assert.AreEqual(10, RawParseUtils.parseHexInt4((byte)'A'));
            Assert.AreEqual(11, RawParseUtils.parseHexInt4((byte)'B'));
            Assert.AreEqual(12, RawParseUtils.parseHexInt4((byte)'C'));
            Assert.AreEqual(13, RawParseUtils.parseHexInt4((byte)'D'));
            Assert.AreEqual(14, RawParseUtils.parseHexInt4((byte)'E'));
            Assert.AreEqual(15, RawParseUtils.parseHexInt4((byte)'F'));

            assertNotHex('q');
            assertNotHex(' ');
            assertNotHex('.');
        }

        [Test]
        public void testInt16()
        {
            Assert.AreEqual(0x0000, parse16("0000"));
            Assert.AreEqual(0x0001, parse16("0001"));
            Assert.AreEqual(0x1234, parse16("1234"));
            Assert.AreEqual(0xdead, parse16("dead")); 
            Assert.AreEqual(0xbeef, parse16("BEEF"));
            Assert.AreEqual(0x4321, parse16("4321"));
            Assert.AreEqual(0xffff, parse16("ffff"));

            AssertHelper.Throws<IndexOutOfRangeException>(() => parse16("noth"));
            AssertHelper.Throws<IndexOutOfRangeException>(() => parse16("01"));
            AssertHelper.Throws<IndexOutOfRangeException>(() => parse16("000."));
        }

        [Test]
        public void testInt32()
        {
            Assert.AreEqual(0x00000000, parse32("00000000"));
            Assert.AreEqual(0x00000001, parse32("00000001"));
            Assert.AreEqual(0xc0ffEE42, (uint)parse32("c0ffEE42"));
            Assert.AreEqual(0xffffffff, (uint)parse32("ffffffff"));
            Assert.AreEqual(-1, parse32("ffffffff"));

            AssertHelper.Throws<IndexOutOfRangeException>(() => parse32("noth"));
            AssertHelper.Throws<IndexOutOfRangeException>(() => parse32("notahexs"));
            AssertHelper.Throws<IndexOutOfRangeException>(() => parse32("01"));
            AssertHelper.Throws<IndexOutOfRangeException>(() => parse32("0000000."));
        }

        private static int parse16(string str)
        {
            return RawParseUtils.parseHexInt16(Constants.encodeASCII(str), 0);
        }

        private static int parse32(string str)
        {
            return RawParseUtils.parseHexInt32(Constants.encodeASCII(str), 0);
        }

        private static void assertNotHex(char c)
        {
            AssertHelper.Throws<IndexOutOfRangeException>(() => RawParseUtils.parseHexInt4((byte) c));
        }

    }

}