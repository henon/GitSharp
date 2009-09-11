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
    public class PacketLineOutTest
    {
        private MemoryStream rawOut;
        private PacketLineOut o;

        [SetUp]
        protected void setUp()
        {
            rawOut = new MemoryStream();
            o = new PacketLineOut(rawOut);
        }

        [Test]
        public void testWriteString1()
        {
            o.WriteString("a");
            o.WriteString("bc");
            assertBuffer("0005a0006bc");
        }

        [Test]
        public void testWriteString2()
        {
            o.WriteString("a\n");
            o.WriteString("bc\n");
            assertBuffer("0006a\n0007bc\n");
        }

        [Test]
        public void testWriteString3()
        {
            o.WriteString(string.Empty);
            assertBuffer("0004");
        }

        [Test]
        public void testWritePacket1()
        {
            o.WritePacket(Encoding.ASCII.GetBytes("a"));
            assertBuffer("0005a");
        }

        [Test]
        public void testWritePacket2()
        {
            o.WritePacket(Encoding.ASCII.GetBytes("abcd"));
            assertBuffer("0008abcd");
        }

        [Test]
        public void testWritePacket3()
        {
            const int buflen = SideBandOutputStream.MAX_BUF - SideBandOutputStream.HDR_SIZE;
            byte[] buf = new byte[buflen];
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (byte) i;
            }
            o.WritePacket(buf);
            o.Flush();

            byte[] act = rawOut.ToArray();
            string explen = NB.DecimalToBase(buf.Length + 4, 16);
            Assert.AreEqual(4 + buf.Length, act.Length);
            Assert.AreEqual(Constants.CHARSET.GetString(act, 0, 4).ToUpper(), explen.ToUpper());
            for (int i = 0, j = 4; i < buf.Length; i++, j++)
                Assert.AreEqual(buf[i], act[j]);
        }

        [Test]
        public void testWriteChannelPacket1()
        {
            o.WriteChannelPacket(1, new[] { (byte)'a' }, 0, 1);
            assertBuffer("0006\x01" + "a");
        }

        [Test]
        public void testWriteChannelPacket2()
        {
            o.WriteChannelPacket(2, new[] { (byte)'b' }, 0, 1);
            assertBuffer("0006\x02" + "b");
        }

        [Test]
        public void testWriteChannelPacket3()
        {
            o.WriteChannelPacket(3, new[] { (byte)'c' }, 0, 1);
            assertBuffer("0006\x03" + "c");
        }

        private void assertBuffer(string exp)
        {
            byte[] resb = rawOut.ToArray();
            string res = Constants.CHARSET.GetString(resb);
            Assert.AreEqual(exp, res);
        }
    }

}