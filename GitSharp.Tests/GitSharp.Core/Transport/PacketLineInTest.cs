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
using GitSharp.Core;
using GitSharp.Core.Transport;
using NUnit.Framework;

namespace GitSharp.Core.Tests.Transport
{

    [TestFixture]
    public class PacketLineInTest
    {
        private MemoryStream rawIn;
        private PacketLineIn pckIn;

        private void init(string msg)
        {
            rawIn = new MemoryStream(Constants.encodeASCII(msg));
            pckIn = new PacketLineIn(rawIn);
        }

        [Test]
        public void testReadString1()
        {
            init("0006a\n0007bc\n");
            Assert.AreEqual("a", pckIn.ReadString());
            Assert.AreEqual("bc", pckIn.ReadString());
            assertEOF();
        }

        [Test]
        public void testReadString2()
        {
            init("0032want fcfcfb1fd94829c1a1704f894fc111d14770d34e\n");
            string act = pckIn.ReadString();
            Assert.AreEqual("want fcfcfb1fd94829c1a1704f894fc111d14770d34e", act);
            assertEOF();
        }

        [Test]
        public void testReadString4()
        {
            init("0005a0006bc");
            Assert.AreEqual("a", pckIn.ReadString());
            Assert.AreEqual("bc", pckIn.ReadString());
            assertEOF();
        }

        [Test]
        public void testReadString5()
        {
            init("000Fhi i am a s");
            Assert.AreEqual("hi i am a s", pckIn.ReadString());
            assertEOF();

            init("000fhi i am a s");
            Assert.AreEqual("hi i am a s", pckIn.ReadString());
            assertEOF();
        }

        [Test]
        public void testReadString_LenHELO()
        {
            init("HELO");
            try
            {
                pckIn.ReadString();
                Assert.Fail("incorrectly accepted invalid packet header");
            }
            catch (IOException e)
            {
                Assert.AreEqual("Invalid packet line header: HELO", e.Message);
            }
        }

        [Test]
        public void testReadString_Len0001()
        {
            init("0001");
            try
            {
                pckIn.ReadString();
                Assert.Fail("incorrectly accepted invalid packet header");
            }
            catch (IOException e)
            {
                Assert.AreEqual("Invalid packet line header: 0001", e.Message);
            }
        }

        [Test]
        public void testReadString_Len0002()
        {
            init("0002");
            try
            {
                pckIn.ReadString();
                Assert.Fail("incorrectly accepted invalid packet header");
            }
            catch (IOException e)
            {
                Assert.AreEqual("Invalid packet line header: 0002", e.Message);
            }
        }

        [Test]
        public void testReadString_Len0003()
        {
            init("0003");
            try
            {
                pckIn.ReadString();
                Assert.Fail("incorrectly accepted invalid packet header");
            }
            catch (IOException e)
            {
                Assert.AreEqual("Invalid packet line header: 0003", e.Message);
            }
        }

        [Test]
        public void testReadString_Len0004()
        {
            init("0004");
            string act = pckIn.ReadString();
            Assert.AreEqual(string.Empty, act);
            assertEOF();
        }

        [Test]
        public void testReadString_End()
        {
            init("0000");
            Assert.AreEqual(string.Empty, pckIn.ReadString());
            assertEOF();
        }

        [Test]
        public void testReadStringRaw1()
        {
            init("0005a0006bc");
            Assert.AreEqual("a", pckIn.ReadStringRaw());
            Assert.AreEqual("bc", pckIn.ReadStringRaw());
            assertEOF();
        }

        [Test]
        public void testReadStringRaw2()
        {
            init("0031want fcfcfb1fd94829c1a1704f894fc111d14770d34e");
            string act = pckIn.ReadStringRaw();
            Assert.AreEqual("want fcfcfb1fd94829c1a1704f894fc111d14770d34e", act);
            assertEOF();
        }

        [Test]
        public void testReadStringRaw3()
        {
            init("0004");
            string act = pckIn.ReadStringRaw();
            Assert.AreEqual(string.Empty, act);
            assertEOF();
        }

        [Test]
        public void testReadStringRaw4()
        {
            init("HELO");
            try
            {
                pckIn.ReadStringRaw();
                Assert.Fail("incorrectly accepted invalid packet header");
            }
            catch (IOException e)
            {
                Assert.AreEqual("Invalid packet line header: HELO", e.Message);
            }
        }

        [Test]
        public void testReadStringRaw_End()
        {
            init("0000");
            Assert.AreEqual(string.Empty, pckIn.ReadStringRaw());
            assertEOF();
        }

        [Test]
        public void testReadACK_NAK()
        {
            ObjectId expid = ObjectId.FromString("fcfcfb1fd94829c1a1704f894fc111d14770d34e");
            MutableObjectId actid = new MutableObjectId();
            actid.FromString(expid.Name);

            init("0008NAK\n");
            Assert.AreEqual(PacketLineIn.AckNackResult.NAK, pckIn.readACK(actid));
            Assert.IsTrue(actid.Equals(expid));
            assertEOF();
        }

        [Test]
        public void testReadACK_ACK1()
        {
            ObjectId expid = ObjectId.FromString("fcfcfb1fd94829c1a1704f894fc111d14770d34e");
            MutableObjectId actid = new MutableObjectId();
            actid.FromString(expid.Name);

            init("0031ACK fcfcfb1fd94829c1a1704f894fc111d14770d34e\n");
            Assert.AreEqual(PacketLineIn.AckNackResult.ACK, pckIn.readACK(actid));
            Assert.IsTrue(actid.Equals(expid));
            assertEOF();
        }

        [Test]
        public void testReadACK_ACKcontinue1()
        {
            ObjectId expid = ObjectId.FromString("fcfcfb1fd94829c1a1704f894fc111d14770d34e");
            MutableObjectId actid = new MutableObjectId();
            actid.FromString(expid.Name);

            init("003aACK fcfcfb1fd94829c1a1704f894fc111d14770d34e continue\n");
            Assert.AreEqual(PacketLineIn.AckNackResult.ACK_CONTINUE, pckIn.readACK(actid));
            Assert.IsTrue(actid.Equals(expid));
            assertEOF();
        }

        [Test]
        public void testReadACK_ACKcommon1()
        {
            ObjectId expid = ObjectId
                   .FromString("fcfcfb1fd94829c1a1704f894fc111d14770d34e");
            MutableObjectId actid = new MutableObjectId();

            init("0038ACK fcfcfb1fd94829c1a1704f894fc111d14770d34e common\n");
            Assert.AreEqual(PacketLineIn.AckNackResult.ACK_COMMON, pckIn.readACK(actid));
            Assert.IsTrue(actid.Equals(expid));
            assertEOF();
        }
        [Test]
        public void testReadACK_ACKready1()
        {
            ObjectId expid = ObjectId
                   .FromString("fcfcfb1fd94829c1a1704f894fc111d14770d34e");
            MutableObjectId actid = new MutableObjectId();

            init("0037ACK fcfcfb1fd94829c1a1704f894fc111d14770d34e ready\n");
            Assert.AreEqual(PacketLineIn.AckNackResult.ACK_READY, pckIn.readACK(actid));
            Assert.IsTrue(actid.Equals(expid));
            assertEOF();
        }

        [Test]
        public void testReadACK_Invalid1()
        {
            init("HELO");
            try
            {
                pckIn.readACK(new MutableObjectId());
                Assert.Fail("incorrectly accepted invalid packet header");
            }
            catch (IOException e)
            {
                Assert.AreEqual("Invalid packet line header: HELO", e.Message);
            }
        }

        [Test]
        public void testReadACK_Invalid2()
        {
            init("0009HELO\n");
            try
            {
                pckIn.readACK(new MutableObjectId());
                Assert.Fail("incorrectly accepted invalid ACK/NAK");
            }
            catch (IOException e)
            {
                Assert.AreEqual("Expected ACK/NAK, got: HELO", e.Message);
            }
        }

        [Test]
        public void testReadACK_Invalid3()
        {
            string s = "ACK fcfcfb1fd94829c1a1704f894fc111d14770d34e neverhappen";
            init("003d" + s + "\n");
            try
            {
                pckIn.readACK(new MutableObjectId());
                Assert.Fail("incorrectly accepted unsupported ACK status");
            }
            catch (IOException e)
            {
                Assert.AreEqual("Expected ACK/NAK, got: " + s, e.Message);
            }
        }
        [Test]
        public void testReadACK_Invalid4()
        {
            init("0000");
            try
            {
                pckIn.readACK(new MutableObjectId());
                Assert.Fail("incorrectly accepted no ACK/NAK");
            }
            catch (IOException e)
            {
                Assert.AreEqual("Expected ACK/NAK, found EOF", e.Message);
            }
        }

        private void assertEOF()
        {
            Assert.AreEqual(-1, rawIn.ReadByte());
        }
    }

}