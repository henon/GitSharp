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

using System.IO;
using System.Text;
using GitSharp.Transport;
using GitSharp.Util;
using NUnit.Framework;

namespace GitSharp.Tests.Transport
{
    
    [TestFixture]
    public class SideBandOutputStreamTest
    {
        private MemoryStream rawOut;
        private PacketLineOut pckOut;

        [SetUp]
        protected void setUp()
        {
            rawOut = new MemoryStream();
            pckOut = new PacketLineOut(rawOut);
        }

        private void assertBuffer(string exp)
        {
            byte[] res = rawOut.ToArray();
            string ress = Encoding.GetEncoding(Constants.CHARACTER_ENCODING).GetString(res);
            Assert.AreEqual(exp, ress);
        }

        [Test]
        public void testWrite_CH_DATA()
        {
            SideBandOutputStream o;
            o = new SideBandOutputStream(SideBandOutputStream.CH_DATA, pckOut);
            byte[] b = new byte[] {(byte) 'a', (byte) 'b', (byte) 'c'};
            o.Write(b, 0, b.Length);
            assertBuffer("0008\x01" + "abc");
        }

        [Test]
        public void testWrite_CH_PROGRESS()
        {
            SideBandOutputStream o;
            o = new SideBandOutputStream(SideBandOutputStream.CH_PROGRESS, pckOut);
            byte[] b = new byte[] { (byte)'a', (byte)'b', (byte)'c' };
            o.Write(b, 0, b.Length);
            assertBuffer("0008\x02" + "abc");
        }

        [Test]
        public void testWrite_CH_ERROR()
        {
            SideBandOutputStream o;
            o = new SideBandOutputStream(SideBandOutputStream.CH_ERROR, pckOut);
            byte[] b = new byte[] { (byte)'a', (byte)'b', (byte)'c' };
            o.Write(b, 0, b.Length);
            assertBuffer("0008\x03" + "abc");
        }

        [Test]
        public void testWrite_Small()
        {
            SideBandOutputStream o;
            o = new SideBandOutputStream(SideBandOutputStream.CH_DATA, pckOut);
            o.WriteByte((byte)'a');
            o.WriteByte((byte)'b');
            o.WriteByte((byte)'c');
            assertBuffer("0006\x01" + "a0006\x01" + "b0006\x01" + "c");
        }

        [Test]
        public void testWrite_Large()
        {
            const int buflen = SideBandOutputStream.MAX_BUF - SideBandOutputStream.HDR_SIZE;
            byte[] buf = new byte[buflen];
            for (int i = 0; i < buf.Length; i++)
                buf[i] = (byte) i;

            SideBandOutputStream o;
            o = new SideBandOutputStream(SideBandOutputStream.CH_DATA, pckOut);
            o.Write(buf, 0, buf.Length);

            byte[] act = rawOut.ToArray();
            string explen = NB.DecimalToBase(buf.Length + 5, 16);
            Assert.AreEqual(5 + buf.Length, act.Length);
            Assert.AreEqual(Encoding.UTF8.GetString(act, 0, 4).ToUpper(), explen);
            Assert.AreEqual(1, act[4]);
            for (int i = 0, j = 5; i < buf.Length; i++, j++)
                Assert.AreEqual(buf[i], act[j]);
        }
    }

}