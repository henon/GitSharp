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
using System.IO;
using System.Linq;
using GitSharp.Util;
using Xunit;

namespace GitSharp.Tests.Util
{
    public class TemporaryBufferTest
    {
        private string GetName()
        {
            return ToString();
        }

        [StrictFactAttribute]
        public void testEmpty()
        {
            var b = new TemporaryBuffer();
            try
            {
                b.close();
                Assert.Equal(0, b.Length);
                byte[] r = b.ToArray();
                Assert.NotNull(r);
                Assert.Equal(0, r.Length);
            }
            finally
            {
                b.destroy();
            }
        }

        [StrictFactAttribute]
        public void testOneByte()
        {
            var b = new TemporaryBuffer();
            var test = (byte)new TestRng(GetName()).NextInt();
            try
            {
                b.write(test);
                b.close();
                Assert.Equal(1, b.Length);
				byte[] r = b.ToArray();
				Assert.NotNull(r);
				Assert.Equal(1, r.Length);
				Assert.Equal(test, r[0]);

				var o = new MemoryStream();
				b.writeTo(o, null);
				o.Close();
				r = o.ToArray();
				Assert.Equal(1, r.Length);
				Assert.Equal(test, r[0]);
            }
            finally
            {
                b.destroy();
            }
        }

        [StrictFactAttribute]
        public void testOneBlock_BulkWrite()
        {
            var b = new TemporaryBuffer();
            byte[] test = new TestRng(GetName())
                   .nextBytes(TemporaryBuffer.Block.SZ);
            try
            {
                b.write(test, 0, 2);
                b.write(test, 2, 4);
                b.write(test, 6, test.Length - 6 - 2);
                b.write(test, test.Length - 2, 2);
                b.close();
                Assert.Equal(test.Length, b.Length);

				byte[] r = b.ToArray();
				Assert.NotNull(r);
				Assert.Equal(test.Length, r.Length);
				Assert.True(test.SequenceEqual(r));

				var o = new MemoryStream();
				b.writeTo(o, null);
				o.Close();
				r = o.ToArray();
				Assert.Equal(test.Length, r.Length);
				Assert.True(test.SequenceEqual(r));
            }
            finally
            {
                b.destroy();
            }
        }

        [StrictFactAttribute]
        public void testOneBlockAndHalf_BulkWrite()
        {
            var b = new TemporaryBuffer();
            byte[] test = new TestRng(GetName()).nextBytes(TemporaryBuffer.Block.SZ * 3 / 2);
            try
            {
                b.write(test, 0, 2);
                b.write(test, 2, 4);
                b.write(test, 6, test.Length - 6 - 2);
                b.write(test, test.Length - 2, 2);
                b.close();
                Assert.Equal(test.Length, b.Length);
				byte[] r = b.ToArray();
				Assert.NotNull(r);
				Assert.Equal(test.Length, r.Length);
				Assert.True(test.SequenceEqual(r));

				var o = new MemoryStream();
				b.writeTo(o, null);
				o.Close();
				r = o.ToArray();
				Assert.Equal(test.Length, r.Length);
				Assert.True(test.SequenceEqual(r));
            }
            finally
            {
                b.destroy();
            }
        }

        [StrictFactAttribute]
        public void testOneBlockAndHalf_SingleWrite()
        {
            var b = new TemporaryBuffer();
            byte[] test = new TestRng(GetName())
                   .nextBytes(TemporaryBuffer.Block.SZ * 3 / 2);
            try
            {
                for (int i = 0; i < test.Length; i++)
                {
                	b.write(test[i]);
                }

                b.close();

                Assert.Equal(test.Length, b.Length);
				byte[] r = b.ToArray();
				Assert.NotNull(r);
				Assert.Equal(test.Length, r.Length);
				Assert.True(test.SequenceEqual(r));

				var o = new MemoryStream();
				b.writeTo(o, null);
				o.Close();
				r = o.ToArray();
				Assert.Equal(test.Length, r.Length);
				Assert.True(test.SequenceEqual(r));
            }
            finally
            {
                b.destroy();
            }
        }

        [StrictFactAttribute]
        public void testOneBlockAndHalf_Copy()
        {
            var b = new TemporaryBuffer();
            byte[] test = new TestRng(GetName())
                   .nextBytes(TemporaryBuffer.Block.SZ * 3 / 2);
            try
            {
                var @in = new MemoryStream(test);
                // [caytchen] StreamReader buffers data After the very first Read, thus advancing the Position in the underlying stream - causing this test to fail
                //var inReader = new StreamReader(@in);
                b.write(@in.ReadByte());
                b.copy(@in);
                b.close();
                Assert.Equal(test.Length, b.Length);

				byte[] r = b.ToArray();
				Assert.NotNull(r);
				Assert.Equal(test.Length, r.Length);
				Assert.True(test.SequenceEqual(r));

				var o = new MemoryStream();
				b.writeTo(o, null);
				o.Close();
				r = o.ToArray();
				Assert.Equal(test.Length, r.Length);
				Assert.True(test.SequenceEqual(r));
            }
            finally
            {
                b.destroy();
            }
        }

        [StrictFactAttribute]
        public void testLarge_SingleWrite()
        {
            var b = new TemporaryBuffer();
            byte[] test = new TestRng(GetName()).nextBytes(TemporaryBuffer.DEFAULT_IN_CORE_LIMIT * 3);
            try
            {
                b.write(test);
                b.close();
                Assert.Equal(test.Length, b.Length);
				byte[] r = b.ToArray();
				Assert.NotNull(r);
				Assert.Equal(test.Length, r.Length);
				Assert.True(test.SequenceEqual(r));

				var o = new MemoryStream();
				b.writeTo(o, null);
				o.Close();
				r = o.ToArray();
				Assert.Equal(test.Length, r.Length);
				Assert.True(test.SequenceEqual(r));
            }
            finally
            {
                b.destroy();
            }
        }

        [StrictFactAttribute]
        public void testInCoreLimit_SwitchOnAppendByte()
        {
            var b = new TemporaryBuffer();
            byte[] test = new TestRng(GetName())
                   .nextBytes(TemporaryBuffer.DEFAULT_IN_CORE_LIMIT + 1);
            try
            {
                b.write(test, 0, test.Length - 1);
                b.write(test[test.Length - 1]);
                b.close();
                Assert.Equal(test.Length, b.Length);

				byte[] r = b.ToArray();
				Assert.NotNull(r);
				Assert.Equal(test.Length, r.Length);
				Assert.True(test.SequenceEqual(r));

				var o = new MemoryStream();
				b.writeTo(o, null);
				o.Close();
				r = o.ToArray();
				Assert.Equal(test.Length, r.Length);
				Assert.True(test.SequenceEqual(r));
            }
            finally
            {
                b.destroy();
            }
        }

        [StrictFactAttribute]
        public void testInCoreLimit_SwitchBeforeAppendByte()
        {
            var b = new TemporaryBuffer();
            byte[] test = new TestRng(GetName())
                   .nextBytes(TemporaryBuffer.DEFAULT_IN_CORE_LIMIT * 3);
            try
            {
                b.write(test, 0, test.Length - 1);
                b.write(test[test.Length - 1]);
                b.close();
                Assert.Equal(test.Length, b.Length);

				byte[] r = b.ToArray();
				Assert.NotNull(r);
				Assert.Equal(test.Length, r.Length);
				Assert.True(test.SequenceEqual(r));

				var o = new MemoryStream();
				b.writeTo(o, null);
				o.Close();
				r = o.ToArray();
				Assert.Equal(test.Length, r.Length);
				Assert.True(test.SequenceEqual(r));
            }
            finally
            {
                b.destroy();
            }
        }

        [StrictFactAttribute]
        public void testInCoreLimit_SwitchOnCopy()
        {
            var b = new TemporaryBuffer();
            byte[] test = new TestRng(GetName())
                   .nextBytes(TemporaryBuffer.DEFAULT_IN_CORE_LIMIT * 2);
            try
            {
                var @in = new MemoryStream(test,
                       TemporaryBuffer.DEFAULT_IN_CORE_LIMIT, test.Length
                               - TemporaryBuffer.DEFAULT_IN_CORE_LIMIT);
                b.write(test, 0, TemporaryBuffer.DEFAULT_IN_CORE_LIMIT);
                b.copy(@in);
                b.close();
                Assert.Equal(test.Length, b.Length);
				byte[] r = b.ToArray();
				Assert.NotNull(r);
				Assert.Equal(test.Length, r.Length);
				Assert.True(test.SequenceEqual(r));

				var o = new MemoryStream();
				b.writeTo(o, null);
				o.Close();
				r = o.ToArray();
				Assert.Equal(test.Length, r.Length);
				Assert.True(test.SequenceEqual(r));
            }
            finally
            {
                b.destroy();
            }
        }

        [StrictFactAttribute]
        public void testDestroyWhileOpen()
        {
            var b = new TemporaryBuffer();
            try
            {
                b.write(new TestRng(GetName())
                        .nextBytes(TemporaryBuffer.DEFAULT_IN_CORE_LIMIT * 2));
            }
            finally
            {
                b.destroy();
            }
        }

        [StrictFactAttribute]
        public void testRandomWrites()
        {
            var b = new TemporaryBuffer();
            var rng = new TestRng(GetName());
            int max = TemporaryBuffer.DEFAULT_IN_CORE_LIMIT * 2;
            var expect = new byte[max];
            try
            {
                int written = 0;
                bool onebyte = true;
                while (written < max)
                {
                    if (onebyte)
                    {
                        var v = (byte)rng.NextInt();
                        b.write(v);
                        expect[written++] = v;
                    }
                    else
                    {
                        int len = Math.Min(rng.NextInt() & 127, max - written);
                        byte[] tmp = rng.nextBytes(len);
                        b.write(tmp, 0, len);
                        Array.Copy(tmp, 0, expect, written, len);
                        written += len;
                    }
                    onebyte = !onebyte;
                }
                Assert.Equal(expect.Length, written);
                b.close();

                Assert.Equal(expect.Length, b.Length);
				byte[] r = b.ToArray();
				Assert.NotNull(r);
				Assert.Equal(expect.Length, r.Length);
				Assert.True(expect.SequenceEqual(r));

				var o = new MemoryStream();
				b.writeTo(o, null);
				o.Close();
				r = o.ToArray();
				Assert.Equal(expect.Length, r.Length);
				Assert.True(expect.SequenceEqual(r));
            }
            finally
            {
                b.destroy();
            }
        }
    }
}
