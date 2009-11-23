/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using System.Linq;
using System.Text;
using GitSharp.Core;
using GitSharp.Tests.Util;
using NUnit.Framework;

namespace GitSharp.Tests
{
    [TestFixture]
    public class ConstantsEncodingTest
    {
        [Test]
        public void testEncodeASCII_SimpleASCII()
        {
            const string src = "abc";
            byte[] exp = { (byte)'a', (byte)'b', (byte)'c' };
            byte[] res = Constants.encodeASCII(src);
            Assert.IsTrue(exp.SequenceEqual(res));
            Assert.AreEqual(src, Constants.CHARSET.GetString(res, 0, res.Length));
        }

        [Test]
        public void testEncodeASCII_FailOnNonASCII()
        {
            const string src = "Ūnĭcōde̽";

            var err = AssertHelper.Throws<ArgumentException>(() => Constants.encodeASCII(src));

            Assert.AreEqual("Not ASCII string: " + src, err.Message);
        }

    	[Test]
        public void testEncodeASCII_Number13()
        {
            const long src = 13;
            byte[] exp = { (byte)'1', (byte)'3' };
            byte[] res = Constants.encodeASCII(src);
            Assert.IsTrue(exp.SequenceEqual(res));
        }

        [Test]
        public void testEncode_SimpleASCII()
        {
            const string src = "abc";
            byte[] exp = { (byte)'a', (byte)'b', (byte)'c' };
            byte[] res = Constants.encode(src);
            Assert.IsTrue(exp.SequenceEqual(res));
            Assert.AreEqual(src, Constants.CHARSET.GetString(res, 0, res.Length));
        }

        [Test]
        public void testEncode_Unicode()
        {
            const string src = "Ūnĭcōde̽"; 
            byte[] exp = { 0xC5, 0xAA, 0x6E, 0xC4,
                0xAD, 0x63, 0xC5, 0x8D, 0x64, 0x65,
                0xCC, 0xBD };

            byte[] res = Constants.encode(src);
            Assert.IsTrue(exp.SequenceEqual(res));
            Assert.AreEqual(src, Constants.CHARSET.GetString(res, 0, res.Length));
        }
    }
}