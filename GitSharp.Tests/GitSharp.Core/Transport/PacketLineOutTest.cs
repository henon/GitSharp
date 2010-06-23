/*
 * Copyright (C) 2009-2010, Google Inc.
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
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
using GitSharp.Core;
using GitSharp.Core.Transport;
using GitSharp.Core.Util;
using GitSharp.Core.Util.JavaHelper;
using NUnit.Framework;

namespace GitSharp.Core.Tests.Transport
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
		public void testWriteEnd()
		{
			var flushCnt = new int[1];
			var mockout = new FlushCounterStream(rawOut, flushCnt);

			new PacketLineOut(mockout).End();
			assertBuffer("0000");
			Assert.AreEqual(1, flushCnt[0]);
		}

		internal class FlushCounterStream : MemoryStream
		{
			private readonly Stream _rawout;
			private readonly int[] _flushCnt;

			public FlushCounterStream(Stream rawout, int[] flushCnt)
			{
				_rawout = rawout;
				_flushCnt = flushCnt;
			}

			public override void WriteByte(byte value)
			{
				_rawout.WriteByte(value);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				_rawout.Write(buffer, offset, count);
			}

			public override void Flush()
			{
				_flushCnt[0]++;
			}
		}

		[Test]
		public void testWritePacket1()
		{
			o.WritePacket(new[] { (byte)'a' });
			assertBuffer("0005a");
		}

		[Test]
		public void testWritePacket2()
		{
			o.WritePacket(new[] { (byte)'a', (byte)'b', (byte)'c', (byte)'d' });
			assertBuffer("0008abcd");
		}

		[Test]
		public void testWritePacket3()
		{
			const int buflen = SideBandOutputStream.MAX_BUF - 5;
			byte[] buf = new byte[buflen];
			for (int i = 0; i < buf.Length; i++)
			{
				buf[i] = (byte)i;
			}
			o.WritePacket(buf);
			o.Flush();

			byte[] act = rawOut.ToArray();
			string explen = NB.DecimalToBase(buf.Length + 4, 16);
			Assert.AreEqual(4 + buf.Length, act.Length);
			Assert.AreEqual(Charset.forName("UTF-8").GetString(act, 0, 4), explen);
			for (int i = 0, j = 4; i < buf.Length; i++, j++)
				Assert.AreEqual(buf[i], act[j]);
		}

		[Test]
		public void testFlush()
		{
			var flushCnt = new int[1];
			var mockout = new FlushCounterFailWriterStream(flushCnt);

			new PacketLineOut(mockout).Flush();
			Assert.AreEqual(1, flushCnt[0]);
		}

		private void assertBuffer(string exp)
		{
			byte[] resb = rawOut.ToArray();
			string res = Constants.CHARSET.GetString(resb);
			Assert.AreEqual(exp, res);
		}
	}

}