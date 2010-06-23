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

using System;
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
	public class SideBandOutputStreamTest
	{
		private MemoryStream rawOut;

		[SetUp]
		protected void setUp()
		{
			rawOut = new MemoryStream();
		}

		private void assertBuffer(string exp)
		{
			byte[] res = rawOut.ToArray();
			string ress = Constants.CHARSET.GetString(res);
			Assert.AreEqual(exp, ress);
		}

		[Test]
		public void testWrite_CH_DATA()
		{
			SideBandOutputStream o;
			o = new SideBandOutputStream(SideBandOutputStream.CH_DATA, SideBandOutputStream.SMALL_BUF, rawOut);
			byte[] b = new byte[] { (byte)'a', (byte)'b', (byte)'c' };
			o.Write(b, 0, b.Length);
			o.Flush();
			assertBuffer("0008\x01" + "abc");
		}

		[Test]
		public void testWrite_CH_PROGRESS()
		{
			SideBandOutputStream o;
			o = new SideBandOutputStream(SideBandOutputStream.CH_PROGRESS, SideBandOutputStream.SMALL_BUF, rawOut);
			byte[] b = new byte[] { (byte)'a', (byte)'b', (byte)'c' };
			o.Write(b, 0, b.Length);
			o.Flush();
			assertBuffer("0008\x02" + "abc");
		}

		[Test]
		public void testWrite_CH_ERROR()
		{
			SideBandOutputStream o;
			o = new SideBandOutputStream(SideBandOutputStream.CH_ERROR, SideBandOutputStream.SMALL_BUF, rawOut);
			byte[] b = new byte[] { (byte)'a', (byte)'b', (byte)'c' };
			o.Write(b, 0, b.Length);
			o.Flush();
			assertBuffer("0008\x03" + "abc");
		}

		[Test]
		public void testWrite_Small()
		{
			SideBandOutputStream o;
			o = new SideBandOutputStream(SideBandOutputStream.CH_DATA, SideBandOutputStream.SMALL_BUF, rawOut);
			o.WriteByte((byte)'a');
			o.WriteByte((byte)'b');
			o.WriteByte((byte)'c');
			o.Flush();
			assertBuffer("0008\x01" + "abc");
		}

		[Test]
		public void testWrite_SmallBlocks1()
		{
			SideBandOutputStream o;
			o = new SideBandOutputStream(SideBandOutputStream.CH_DATA, 6, rawOut);
			o.WriteByte((byte)'a');
			o.WriteByte((byte)'b');
			o.WriteByte((byte)'c');
			o.Flush();
			assertBuffer("0006\x01" + "a0006\x01" + "b0006\x01" + "c");
		}

		[Test]
		public void testWrite_SmallBlocks2()
		{
			SideBandOutputStream o;
			o = new SideBandOutputStream(SideBandOutputStream.CH_DATA, 6, rawOut);
			o.Write(new byte[] { (byte)'a', (byte)'b', (byte)'c' }, 0, 3);
			o.Flush();
			assertBuffer("0006\x01" + "a0006\x01" + "b0006\x01" + "c");
		}

		[Test]
		public void testWrite_SmallBlocks3()
		{
			SideBandOutputStream o;
			o = new SideBandOutputStream(SideBandOutputStream.CH_DATA, 7, rawOut);
			o.WriteByte((byte)'a');
			o.Write(new byte[] { (byte)'b', (byte)'c' }, 0, 2);
			o.Flush();
			assertBuffer("0007\x01" + "ab0006\x01" + "c");
		}

		[Test]
		public void testWrite_Large()
		{
			const int buflen = SideBandOutputStream.MAX_BUF - SideBandOutputStream.HDR_SIZE;
			byte[] buf = new byte[buflen];
			for (int i = 0; i < buf.Length; i++)
				buf[i] = (byte)i;

			SideBandOutputStream o;
			o = new SideBandOutputStream(SideBandOutputStream.CH_DATA, SideBandOutputStream.MAX_BUF, rawOut);
			o.Write(buf, 0, buf.Length);
			o.Flush();
			byte[] act = rawOut.ToArray();
			string explen = NB.DecimalToBase(buf.Length + SideBandOutputStream.HDR_SIZE, 16);
			Assert.AreEqual(SideBandOutputStream.HDR_SIZE + buf.Length, act.Length);
			Assert.AreEqual(Charset.forName("UTF-8").GetString(act, 0, 4), explen);
			Assert.AreEqual(1, act[4]);
			for (int i = 0, j = SideBandOutputStream.HDR_SIZE; i < buf.Length; i++, j++)
				Assert.AreEqual(buf[i], act[j]);
		}

		[Test]
		public void testFlush()
		{
			var flushCnt = new int[1];
			var mockout = new FlushCounterFailWriterStream(flushCnt);

			new SideBandOutputStream(SideBandOutputStream.CH_DATA, SideBandOutputStream.SMALL_BUF,
											 mockout).Flush();

			Assert.AreEqual(1, flushCnt[0]);
		}

		public void testConstructor_RejectsBadChannel()
		{
			try
			{
				new SideBandOutputStream(-1, SideBandOutputStream.MAX_BUF, rawOut);
				Assert.Fail("Accepted -1 channel number");
			}
			catch (ArgumentException e)
			{
				Assert.Equals("channel -1 must be in range [0, 255]", e.Message);
			}

			try
			{
				new SideBandOutputStream(0, SideBandOutputStream.MAX_BUF, rawOut);
				Assert.Fail("Accepted 0 channel number");
			}
			catch (ArgumentException e)
			{
				Assert.Equals("channel 0 must be in range [0, 255]", e.Message);
			}

			try
			{
				new SideBandOutputStream(256, SideBandOutputStream.MAX_BUF, rawOut);
				Assert.Fail("Accepted 256 channel number");
			}
			catch (ArgumentException e)
			{
				Assert.Equals("channel 256 must be in range [0, 255]", e
						.Message);
			}
		}

		public void testConstructor_RejectsBadBufferSize()
		{
			try
			{
				new SideBandOutputStream(SideBandOutputStream.CH_DATA, -1, rawOut);
				Assert.Fail("Accepted -1 for buffer size");
			}
			catch (ArgumentException e)
			{
				Assert.Equals("packet size -1 must be >= 5", e.Message);
			}

			try
			{
				new SideBandOutputStream(SideBandOutputStream.CH_DATA, 0, rawOut);
				Assert.Fail("Accepted 0 for buffer size");
			}
			catch (ArgumentException e)
			{
				Assert.Equals("packet size 0 must be >= 5", e.Message);
			}

			try
			{
				new SideBandOutputStream(SideBandOutputStream.CH_DATA, 1, rawOut);
				Assert.Fail("Accepted 1 for buffer size");
			}
			catch (ArgumentException e)
			{
				Assert.Equals("packet size 1 must be >= 5", e.Message);
			}

			try
			{
				new SideBandOutputStream(SideBandOutputStream.CH_DATA, int.MaxValue, rawOut);
				Assert.Fail("Accepted " + int.MaxValue + " for buffer size");
			}
			catch (ArgumentException e)
			{
				Assert.Equals("packet size " + int.MaxValue
						+ " must be <= 65520", e.Message);
			}
		}

	}

	internal class FlushCounterFailWriterStream : MemoryStream
	{
		private readonly int[] _flushCnt;

		public FlushCounterFailWriterStream(int[] flushCnt)
		{
			_flushCnt = flushCnt;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			Assert.Fail("should not write");
		}

		public override void WriteByte(byte value)
		{
			Assert.Fail("should not write");
		}

		public override void Flush()
		{
			_flushCnt[0]++;
		}
	}
}