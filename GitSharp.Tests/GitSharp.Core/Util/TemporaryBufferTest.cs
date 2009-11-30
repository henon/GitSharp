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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.Core.Util;
using NUnit.Framework;
using System.IO;

namespace GitSharp.Core.Tests.Util
{
    [TestFixture]
    public class TemporaryBufferTest
    {
        private string getName()
        {
            return this.ToString();
        }

        [Test]
        public void testEmpty()
        {
            TemporaryBuffer b = new TemporaryBuffer();
            try
            {
                b.close();
                Assert.AreEqual(0, b.Length);
                byte[] r = b.ToArray();
                Assert.IsNotNull(r);
                Assert.AreEqual(0, r.Length);
            }
            finally
            {
                b.destroy();
            }
        }

        [Test]
        public void testOneByte()
        {
            TemporaryBuffer b = new TemporaryBuffer();
            byte test = (byte)new TestRng(getName()).nextInt();
            try
            {
                b.write(test);
                b.close();
                Assert.AreEqual(1, b.Length);
                {
                    byte[] r = b.ToArray();
                    Assert.IsNotNull(r);
                    Assert.AreEqual(1, r.Length);
                    Assert.AreEqual(test, r[0]);
                }
                {
                    byte[] r;
                    using (MemoryStream o = new MemoryStream())
                    {
                        b.writeTo(o, null);
                        r = o.ToArray();
                    } 
                    Assert.AreEqual(1, r.Length);
                    Assert.AreEqual(test, r[0]);
                }
            }
            finally
            {
                b.destroy();
            }
        }

        [Test]
        public void testOneBlock_BulkWrite()
        {
            TemporaryBuffer b = new TemporaryBuffer();
            byte[] test = new TestRng(getName())
                   .nextBytes(TemporaryBuffer.Block.SZ);
            try
            {
                b.write(test, 0, 2);
                b.write(test, 2, 4);
                b.write(test, 6, test.Length - 6 - 2);
                b.write(test, test.Length - 2, 2);
                b.close();
                Assert.AreEqual(test.Length, b.Length);
                {
                    byte[] r = b.ToArray();
                    Assert.IsNotNull(r);
                    Assert.AreEqual(test.Length, r.Length);
                    Assert.IsTrue(test.SequenceEqual(r));
                }
                {
                    byte[] r;
                    using (MemoryStream o = new MemoryStream())
                    {
                        b.writeTo(o, null);
                        r = o.ToArray();
                    } 
                    Assert.AreEqual(test.Length, r.Length);
                    Assert.IsTrue(test.SequenceEqual(r));
                }
            }
            finally
            {
                b.destroy();
            }
        }

        [Test]
        public void testOneBlockAndHalf_BulkWrite()
        {
            TemporaryBuffer b = new TemporaryBuffer();
            byte[] test = new TestRng(getName()).nextBytes(TemporaryBuffer.Block.SZ * 3 / 2);
            try
            {
                b.write(test, 0, 2);
                b.write(test, 2, 4);
                b.write(test, 6, test.Length - 6 - 2);
                b.write(test, test.Length - 2, 2);
                b.close();
                Assert.AreEqual(test.Length, b.Length);
                {
                    byte[] r = b.ToArray();
                    Assert.IsNotNull(r);
                    Assert.AreEqual(test.Length, r.Length);
                    Assert.IsTrue(test.SequenceEqual(r));
                }
                {
                    byte[] r;
                    using (MemoryStream o = new MemoryStream())
                    {
                        b.writeTo(o, null);
                        r = o.ToArray();
                    }
                    Assert.AreEqual(test.Length, r.Length);
                    Assert.IsTrue(test.SequenceEqual(r));
                }
            }
            finally
            {
                b.destroy();
            }
        }

        [Test]
        public void testOneBlockAndHalf_SingleWrite()
        {
            TemporaryBuffer b = new TemporaryBuffer();
            byte[] test = new TestRng(getName())
                   .nextBytes(TemporaryBuffer.Block.SZ * 3 / 2);
            try
            {
                for (int i = 0; i < test.Length; i++)
                    b.write(test[i]);
                b.close();
                Assert.AreEqual(test.Length, b.Length);
                {
                    byte[] r = b.ToArray();
                    Assert.IsNotNull(r);
                    Assert.AreEqual(test.Length, r.Length);
                    Assert.IsTrue(test.SequenceEqual(r));
                }
                {
                    byte[] r;
                    using (MemoryStream o = new MemoryStream())
                    {
                        b.writeTo(o, null);
                        r = o.ToArray();
                    }
                    Assert.AreEqual(test.Length, r.Length);
                    Assert.IsTrue(test.SequenceEqual(r));
                }
            }
            finally
            {
                b.destroy();
            }
        }

        [Test]
        public void testOneBlockAndHalf_Copy()
        {
            TemporaryBuffer b = new TemporaryBuffer();
            byte[] test = new TestRng(getName())
                   .nextBytes(TemporaryBuffer.Block.SZ * 3 / 2);
            try
            {
                var @in = new MemoryStream(test);
                // [caytchen] StreamReader buffers data After the very first Read, thus advancing the Position in the underlying stream - causing this test to fail
                //var inReader = new StreamReader(@in);
                b.write(@in.ReadByte());
                b.copy(@in);
                b.close();
                Assert.AreEqual(test.Length, b.Length);
                {
                    byte[] r = b.ToArray();
                    Assert.IsNotNull(r);
                    Assert.AreEqual(test.Length, r.Length);
                    Assert.IsTrue(test.SequenceEqual(r));
                }
                {
                    byte[] r;
                    using (MemoryStream o = new MemoryStream())
                    {
                        b.writeTo(o, null);
                        r = o.ToArray();
                    }
                    Assert.AreEqual(test.Length, r.Length);
                    Assert.IsTrue(test.SequenceEqual(r));
                }
            }
            finally
            {
                b.destroy();
            }
        }

        [Test]
        public void testLarge_SingleWrite()
        {
            TemporaryBuffer b = new TemporaryBuffer();
            byte[] test = new TestRng(getName()).nextBytes(TemporaryBuffer.DEFAULT_IN_CORE_LIMIT * 3);
            try
            {
                b.write(test);
                b.close();
                Assert.AreEqual(test.Length, b.Length);
                {
                    byte[] r = b.ToArray();
                    Assert.IsNotNull(r);
                    Assert.AreEqual(test.Length, r.Length);
                    Assert.IsTrue(test.SequenceEqual(r));
                }
                {
                    byte[] r;
                    using (MemoryStream o = new MemoryStream())
                    {
                        b.writeTo(o, null);
                        r = o.ToArray();
                    }
                    Assert.AreEqual(test.Length, r.Length);
                    Assert.IsTrue(test.SequenceEqual(r));
                }
            }
            finally
            {
                b.destroy();
            }
        }

        [Test]
        public void testInCoreLimit_SwitchOnAppendByte()
        {
            TemporaryBuffer b = new TemporaryBuffer();
            byte[] test = new TestRng(getName())
                   .nextBytes(TemporaryBuffer.DEFAULT_IN_CORE_LIMIT + 1);
            try
            {
                b.write(test, 0, test.Length - 1);
                b.write(test[test.Length - 1]);
                b.close();
                Assert.AreEqual(test.Length, b.Length);
                {
                    byte[] r = b.ToArray();
                    Assert.IsNotNull(r);
                    Assert.AreEqual(test.Length, r.Length);
                    Assert.IsTrue(test.SequenceEqual(r));
                }
                {
                    byte[] r;
                    using (MemoryStream o = new MemoryStream())
                    {
                        b.writeTo(o, null);
                        r = o.ToArray();
                    }
                    Assert.AreEqual(test.Length, r.Length);
                    Assert.IsTrue(test.SequenceEqual(r));
                }
            }
            finally
            {
                b.destroy();
            }
        }

        [Test]
        public void testInCoreLimit_SwitchBeforeAppendByte()
        {
            TemporaryBuffer b = new TemporaryBuffer();
            byte[] test = new TestRng(getName())
                   .nextBytes(TemporaryBuffer.DEFAULT_IN_CORE_LIMIT * 3);
            try
            {
                b.write(test, 0, test.Length - 1);
                b.write(test[test.Length - 1]);
                b.close();
                Assert.AreEqual(test.Length, b.Length);
                {
                    byte[] r = b.ToArray();
                    Assert.IsNotNull(r);
                    Assert.AreEqual(test.Length, r.Length);
                    Assert.IsTrue(test.SequenceEqual(r));
                }
                {
                    byte[] r;
                    using (MemoryStream o = new MemoryStream())
                    {
                        b.writeTo(o, null);
                        r = o.ToArray();
                    }
                    Assert.AreEqual(test.Length, r.Length);
                    Assert.IsTrue(test.SequenceEqual(r));
                }
            }
            finally
            {
                b.destroy();
            }
        }

        [Test]
        public void testInCoreLimit_SwitchOnCopy()
        {
            TemporaryBuffer b = new TemporaryBuffer();
            byte[] test = new TestRng(getName())
                   .nextBytes(TemporaryBuffer.DEFAULT_IN_CORE_LIMIT * 2);
            try
            {
                MemoryStream @in = new MemoryStream(test,
                       TemporaryBuffer.DEFAULT_IN_CORE_LIMIT, test.Length
                               - TemporaryBuffer.DEFAULT_IN_CORE_LIMIT);
                b.write(test, 0, TemporaryBuffer.DEFAULT_IN_CORE_LIMIT);
                b.copy(@in);
                b.close();
                Assert.AreEqual(test.Length, b.Length);
                {
                    byte[] r = b.ToArray();
                    Assert.IsNotNull(r);
                    Assert.AreEqual(test.Length, r.Length);
                    Assert.IsTrue(test.SequenceEqual(r));
                }
                {
                    byte[] r;
                    using (MemoryStream o = new MemoryStream())
                    {
                        b.writeTo(o, null);
                        r = o.ToArray();
                    }
                    Assert.AreEqual(test.Length, r.Length);
                    Assert.IsTrue(test.SequenceEqual(r));
                }
            }
            finally
            {
                b.destroy();
            }
        }

        [Test]
        public void testDestroyWhileOpen()
        {
            TemporaryBuffer b = new TemporaryBuffer();
            try
            {
                b.write(new TestRng(getName())
                        .nextBytes(TemporaryBuffer.DEFAULT_IN_CORE_LIMIT * 2));
            }
            finally
            {
                b.destroy();
            }
        }

        [Test]
        public void testRandomWrites()
        {
            TemporaryBuffer b = new TemporaryBuffer();
            TestRng rng = new TestRng(getName());
            int max = TemporaryBuffer.DEFAULT_IN_CORE_LIMIT * 2;
            byte[] expect = new byte[max];
            try
            {
                int written = 0;
                bool onebyte = true;
                while (written < max)
                {
                    if (onebyte)
                    {
                        byte v = (byte)rng.nextInt();
                        b.write(v);
                        expect[written++] = v;
                    }
                    else
                    {
                        int len = Math.Min(rng.nextInt() & 127, max - written);
                        byte[] tmp = rng.nextBytes(len);
                        b.write(tmp, 0, len);
                        Array.Copy(tmp, 0, expect, written, len);
                        written += len;
                    }
                    onebyte = !onebyte;
                }
                Assert.AreEqual(expect.Length, written);
                b.close();

                Assert.AreEqual(expect.Length, b.Length);
                {
                    byte[] r = b.ToArray();
                    Assert.IsNotNull(r);
                    Assert.AreEqual(expect.Length, r.Length);
                    Assert.IsTrue(expect.SequenceEqual(r));
                }
                {
                    byte[] r;
                    using (MemoryStream o = new MemoryStream())
                    {
                        b.writeTo(o, null);
                        r = o.ToArray();
                    }
                    Assert.AreEqual(expect.Length, r.Length);
                    Assert.IsTrue(expect.SequenceEqual(r));
                }
            }
            finally
            {
                b.destroy();
            }
        }
    }
}
