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
using GitSharp.Transport;
using Xunit;

namespace GitSharp.Tests.Transport
{
    public class PacketLineInTest
    {
        private MemoryStream rawIn;
        private PacketLineIn pckIn;

        private void init(string msg)
        {
            rawIn = new MemoryStream(Constants.encodeASCII(msg));
            pckIn = new PacketLineIn(rawIn);
        }

        [Fact]
        public void testReadString1()
        {
            init("0006a\n0007bc\n");
            Assert.Equal("a", pckIn.ReadString());
            Assert.Equal("bc", pckIn.ReadString());
            assertEOF();
        }

        [Fact]
        public void testReadString2()
        {
            init("0032want fcfcfb1fd94829c1a1704f894fc111d14770d34e\n");
            string act = pckIn.ReadString();
            Assert.Equal("want fcfcfb1fd94829c1a1704f894fc111d14770d34e", act);
            assertEOF();
        }

        [Fact]
        public void testReadString4()
        {
            init("0005a0006bc");
            Assert.Equal("a", pckIn.ReadString());
            Assert.Equal("bc", pckIn.ReadString());
            assertEOF();
        }

        [Fact]
        public void testReadString5()
        {
            init("000Fhi i am a s");
            Assert.Equal("hi i am a s", pckIn.ReadString());
            assertEOF();

            init("000fhi i am a s");
            Assert.Equal("hi i am a s", pckIn.ReadString());
            assertEOF();
        }

        [Fact]
        public void testReadString_LenHELO()
        {
            init("HELO");
            try
            {
                pckIn.ReadString();
                Assert.False(true, "incorrectly accepted invalid packet header");
            }
            catch (IOException e)
            {
                Assert.Equal("Invalid packet line header: HELO", e.Message);
            }
        }

        [Fact]
        public void testReadString_Len0001()
        {
            init("0001");
            try
            {
                pckIn.ReadString();
                Assert.False(true, "incorrectly accepted invalid packet header");
            }
            catch (IOException e)
            {
                Assert.Equal("Invalid packet line header: 0001", e.Message);
            }
        }

        [Fact]
        public void testReadString_Len0002()
        {
            init("0002");
            try
            {
                pckIn.ReadString();
                Assert.False(true, "incorrectly accepted invalid packet header");
            }
            catch (IOException e)
            {
                Assert.Equal("Invalid packet line header: 0002", e.Message);
            }
        }

        [Fact]
        public void testReadString_Len0003()
        {
            init("0003");
            try
            {
                pckIn.ReadString();
                Assert.False(true, "incorrectly accepted invalid packet header");
            }
            catch (IOException e)
            {
                Assert.Equal("Invalid packet line header: 0003", e.Message);
            }
        }

        [Fact]
        public void testReadString_Len0004()
        {
            init("0004");
            string act = pckIn.ReadString();
            Assert.Equal("", act);
            assertEOF();
        }

        [Fact]
        public void testReadString_End()
        {
            init("0000");
            Assert.Equal("", pckIn.ReadString());
            assertEOF();
        }

        [Fact]
        public void testReadStringRaw1()
        {
            init("0005a0006bc");
            Assert.Equal("a", pckIn.ReadStringRaw());
            Assert.Equal("bc", pckIn.ReadStringRaw());
            assertEOF();
        }

        [Fact]
        public void testReadStringRaw2()
        {
            init("0031want fcfcfb1fd94829c1a1704f894fc111d14770d34e");
            string act = pckIn.ReadStringRaw();
            Assert.Equal("want fcfcfb1fd94829c1a1704f894fc111d14770d34e", act);
            assertEOF();
        }

        [Fact]
        public void testReadStringRaw3()
        {
            init("0004");
            string act = pckIn.ReadStringRaw();
            Assert.Equal("", act);
            assertEOF();
        }

        [Fact]
        public void testReadStringRaw4()
        {
            init("HELO");
            try
            {
                pckIn.ReadStringRaw();
                Assert.False(true, "incorrectly accepted invalid packet header");
            }
            catch (IOException e)
            {
                Assert.Equal("Invalid packet line header: HELO", e.Message);
            }
        }

        [Fact]
        public void testReadStringRaw_End()
        {
            init("0000");
            Assert.Equal("", pckIn.ReadStringRaw());
            assertEOF();
        }

        [Fact]
        public void testReadACK_NAK()
        {
            ObjectId expid = ObjectId.FromString("fcfcfb1fd94829c1a1704f894fc111d14770d34e");
            MutableObjectId actid = new MutableObjectId();
            actid.FromString(expid.Name);

            init("0008NAK\n");
            Assert.Equal(PacketLineIn.AckNackResult.NAK, pckIn.readACK(actid));
            Assert.True(actid.Equals(expid));
            assertEOF();
        }

        [Fact]
        public void testReadACK_ACK1()
        {
            ObjectId expid = ObjectId.FromString("fcfcfb1fd94829c1a1704f894fc111d14770d34e");
            MutableObjectId actid = new MutableObjectId();
            actid.FromString(expid.Name);

            init("0031ACK fcfcfb1fd94829c1a1704f894fc111d14770d34e\n");
            Assert.Equal(PacketLineIn.AckNackResult.ACK, pckIn.readACK(actid));
            Assert.True(actid.Equals(expid));
            assertEOF();
        }

        [Fact]
        public void testReadACK_ACKcontinue1()
        {
            ObjectId expid = ObjectId.FromString("fcfcfb1fd94829c1a1704f894fc111d14770d34e");
            MutableObjectId actid = new MutableObjectId();
            actid.FromString(expid.Name);

            init("003aACK fcfcfb1fd94829c1a1704f894fc111d14770d34e continue\n");
            Assert.Equal(PacketLineIn.AckNackResult.ACK_CONTINUE, pckIn.readACK(actid));
            Assert.True(actid.Equals(expid));
            assertEOF();
        }

        [Fact]
        public void testReadACK_Invalid1()
        {
            init("HELO");
            try
            {
                pckIn.readACK(new MutableObjectId());
                Assert.False(true, "incorrectly accepted invalid packet header");
            }
            catch (IOException e)
            {
                Assert.Equal("Invalid packet line header: HELO", e.Message);
            }
        }

        [Fact]
        public void testReadACK_Invalid2()
        {
            init("0009HELO\n");
            try
            {
                pckIn.readACK(new MutableObjectId());
                Assert.False(true, "incorrectly accepted invalid ACK/NAK");
            }
            catch (IOException e)
            {
                Assert.Equal("Expected ACK/NAK, got: HELO", e.Message);
            }
        }

        [Fact]
        public void testReadACK_Invalid3()
        {
            init("0000");
            try
            {
                pckIn.readACK(new MutableObjectId());
                Assert.False(true, "incorrectly accepted no ACK/NAK");
            }
            catch (IOException e)
            {
                Assert.Equal("Expected ACK/NAK, found EOF", e.Message);
            }
        }

        private void assertEOF()
        {
            Assert.Equal(-1, rawIn.ReadByte());
        }
    }

}