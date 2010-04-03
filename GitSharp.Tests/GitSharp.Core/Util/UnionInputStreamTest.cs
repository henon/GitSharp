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
 * - Neither the name of the Eclipse Foundation, Inc. nor the
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
using System.IO;
using GitSharp.Core.Util;
using NUnit.Framework;
using System.Linq;

namespace GitSharp.Tests.GitSharp.Core.Util
{
    [TestFixture]
    public class UnionInputStreamTest
    {
        [Test]
        public void testEmptyStream()
        {
            var u = new UnionInputStream();
            Assert.IsTrue(u.isEmpty());
            Assert.AreEqual(-1, u.read());
            Assert.AreEqual(-1, u.Read(new byte[1], 0, 1));
            Assert.AreEqual(0, u.available());
            Assert.AreEqual(0, u.skip(1));
            u.Close();
        }

        [Test]
        public void testReadSingleBytes()
        {
            var u = new UnionInputStream();

            Assert.IsTrue(u.isEmpty());
            u.add(new MemoryStream(new byte[] { 1, 0, 2 }));
            u.add(new MemoryStream(new byte[] { 3 }));
            u.add(new MemoryStream(new byte[] { 4, 5 }));

            Assert.IsFalse(u.isEmpty());
            Assert.AreEqual(3, u.available());
            Assert.AreEqual(1, u.read());
            Assert.AreEqual(0, u.read());
            Assert.AreEqual(2, u.read());
            Assert.AreEqual(0, u.available());

            Assert.AreEqual(3, u.read());
            Assert.AreEqual(0, u.available());

            Assert.AreEqual(4, u.read());
            Assert.AreEqual(1, u.available());
            Assert.AreEqual(5, u.read());
            Assert.AreEqual(0, u.available());
            Assert.AreEqual(-1, u.read());

            Assert.IsTrue(u.isEmpty());
            u.add(new MemoryStream(new byte[] { (byte)255 }));
            Assert.AreEqual(255, u.read());
            Assert.AreEqual(-1, u.read());
            Assert.IsTrue(u.isEmpty());
        }

        [Test]
        public void testReadByteBlocks()
        {
            var u = new UnionInputStream();
            u.add(new MemoryStream(new byte[] { 1, 0, 2 }));
            u.add(new MemoryStream(new byte[] { 3 }));
            u.add(new MemoryStream(new byte[] { 4, 5 }));

            var r = new byte[5];
            Assert.AreEqual(5, u.Read(r, 0, 5));
            Assert.IsTrue(r.SequenceEqual(new byte[] { 1, 0, 2, 3, 4 }));
            Assert.AreEqual(1, u.Read(r, 0, 5));
            Assert.AreEqual(5, r[0]);
            Assert.AreEqual(-1, u.Read(r, 0, 5));
        }

        [Test]
        public void testArrayConstructor()
        {
            var u = new UnionInputStream(
                new MemoryStream(new byte[] { 1, 0, 2 }),
                new MemoryStream(new byte[] { 3 }),
                new MemoryStream(new byte[] { 4, 5 }));

            var r = new byte[5];
            Assert.AreEqual(5, u.Read(r, 0, 5));
            Assert.IsTrue(r.SequenceEqual(new byte[] { 1, 0, 2, 3, 4 }));
            Assert.AreEqual(1, u.Read(r, 0, 5));
            Assert.AreEqual(5, r[0]);
            Assert.AreEqual(-1, u.Read(r, 0, 5));
        }

        [Test]
        public void testMarkSupported()
        {
            var u = new UnionInputStream();
            Assert.IsFalse(u.markSupported());
            u.add(new MemoryStream(new byte[] { 1, 0, 2 }));
            Assert.IsFalse(u.markSupported());
        }

        [Test]
        public void testSkip()
        {
            var u = new UnionInputStream();
            u.add(new MemoryStream(new byte[] { 1, 0, 2 }));
            u.add(new MemoryStream(new byte[] { 3 }));
            u.add(new MemoryStream(new byte[] { 4, 5 }));
            Assert.AreEqual(0, u.skip(0));
            Assert.AreEqual(4, u.skip(4));
            Assert.AreEqual(4, u.read());
            Assert.AreEqual(1, u.skip(5));
            Assert.AreEqual(0, u.skip(5));
            Assert.AreEqual(-1, u.read());

            u.add(new MockMemoryStream(new byte[] { 20, 30 }, null)); // can't mock skip behavior :-(
            Assert.AreEqual(2, u.skip(8));
            Assert.AreEqual(-1, u.read());
        }

        private class MockMemoryStream : MemoryStream
        {
            private readonly Action _closeBehavior;

            public MockMemoryStream(byte[] buffer, Action closeBehavior)
                : base(buffer)
            {
                _closeBehavior = closeBehavior;
            }

            public override void Close()
            {
                base.Close();
                if (_closeBehavior == null)
                {
                    return;
                }

                _closeBehavior();
            }
        }

        [Test]
        public void testAutoCloseDuringRead()
        {
            var u = new UnionInputStream();
            var closed = new bool[2];
            u.add(new MockMemoryStream(new byte[] { 1 }, () => { closed[0] = true; }));
            u.add(new MockMemoryStream(new byte[] { 2 }, () => { closed[1] = true; }));

            Assert.IsFalse(closed[0]);
            Assert.IsFalse(closed[1]);

            Assert.AreEqual(1, u.read());
            Assert.IsFalse(closed[0]);
            Assert.IsFalse(closed[1]);

            Assert.AreEqual(2, u.read());
            Assert.IsTrue(closed[0]);
            Assert.IsFalse(closed[1]);

            Assert.AreEqual(-1, u.read());
            Assert.IsTrue(closed[0]);
            Assert.IsTrue(closed[1]);
        }

        [Test]
        public void testCloseDuringClose()
        {
            var u = new UnionInputStream();
            var closed = new bool[2];
            u.add(new MockMemoryStream(new byte[] { 1 }, () => { closed[0] = true; }));
            u.add(new MockMemoryStream(new byte[] { 2 }, () => { closed[1] = true; }));

            Assert.IsFalse(closed[0]);
            Assert.IsFalse(closed[1]);

            u.Close();

            Assert.IsTrue(closed[0]);
            Assert.IsTrue(closed[1]);
        }

        [Test]
        public void testExceptionDuringClose()
        {
            var u = new UnionInputStream();
            u.add(new MockMemoryStream(new byte[] { 1 }, () => { throw new IOException("I AM A TEST"); }));

            try
            {
                u.Close();
                Assert.Fail("close ignored inner stream exception");
            }
            catch (IOException e)
            {
                Assert.AreEqual("I AM A TEST", e.Message);
            }
        }
    }
}
