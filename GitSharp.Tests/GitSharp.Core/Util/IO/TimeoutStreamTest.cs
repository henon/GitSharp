/*
 * Copyright (C) 2009, Google Inc.
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
 *
 * This program and the accompanying materials are made available
 * under the terms of the Eclipse Distribution License v1.0 which
 * accompanies this distribution, is reproduced below, and is
 * available at http://www.eclipse.org/org/documents/edl-v10.php
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp.Core.Util
{

	[TestFixture]
	public class TimeoutStreamTest
	{
		private static int timeout = 250;

		private PipeStream _stream;

		private StreamWriter _writer;

		private TimeoutStream _timeoutstream;

		private long start;

		[SetUp]
		public void setUp()
		{
			_stream = new PipeStream();
			_writer = new StreamWriter(_stream);
			//timer = new InterruptTimer();
			_timeoutstream = new TimeoutStream(_stream);
			_timeoutstream.setTimeout(timeout);
		}

		//protected void tearDown()  {
		//   timer.terminate();
		//   for (Thread t : active())
		//      assertFalse(t instanceof InterruptTimer.AlarmThread);
		//   super.tearDown();
		//}

		[Test]
		public void testTimeout_readByte_Success1()
		{
			_writer.Write('a');
			_writer.Flush();
			Assert.AreEqual((byte)'a', _timeoutstream.ReadByte());
		}

		[Test]
		public void testTimeout_readByte_Success2()
		{
			byte[] exp = new byte[] { (byte)'a', (byte)'b', (byte)'c' };
			_stream.Write(exp, 0, exp.Length);
			Assert.AreEqual(exp[0], _timeoutstream.ReadByte());
			Assert.AreEqual(exp[1], _timeoutstream.ReadByte());
			Assert.AreEqual(exp[2], _timeoutstream.ReadByte());
			_stream.Close(); // note [henon]: we can't distinguish a read from closed stream (returns -1 in java) from read timeout, so this testcase is different than in jgit
			try
			{
				_timeoutstream.ReadByte();
				Assert.Fail("incorrectly read a byte");
			}
			catch (TimeoutException)
			{
				// expected
			}
		}

		[Test]
		public void testTimeout_readByte_Timeout()
		{
			beginRead();
			try
			{
				_timeoutstream.ReadByte();
				Assert.Fail("incorrectly read a byte");
			}
			catch (TimeoutException)
			{
				// expected
			}
			assertTimeout();
		}

		[Test]
		public void testTimeout_readBuffer_Success1()
		{
			byte[] exp = new byte[] { (byte)'a', (byte)'b', (byte)'c' };
			byte[] act = new byte[exp.Length];
			_stream.Write(exp, 0, exp.Length);
			IO.ReadFully(_timeoutstream, act, 0, act.Length);
			Assert.AreEqual(exp, act);
		}

		[Test]
		public void testTimeout_readBuffer_Success2()
		{
			var s = new MemoryStream();
			var t = new TimeoutStream(s);
			t.setTimeout(timeout);
			byte[] exp = new byte[] { (byte)'a', (byte)'b', (byte)'c' };
			byte[] act = new byte[exp.Length];
			s.Write(exp, 0, exp.Length);
			s.Seek(0, SeekOrigin.Begin);
			IO.ReadFully(t, act, 0, 1);
			IO.ReadFully(t, act, 1, 1);
			IO.ReadFully(t, act, 2, 1);
			Assert.AreEqual(exp, act);
		}

		[Test]
		public void testTimeout_readBuffer_Timeout()
		{
			beginRead();
			try
			{
				IO.ReadFully(_timeoutstream, new byte[512], 0, 512);
				Assert.Fail("incorrectly read bytes");
			}
			catch (TimeoutException)
			{
				// expected
			}
			assertTimeout();
		}

		[Test]
		public void testTimeout_skip_Success()
		{
			var s = new MemoryStream();
			var t = new TimeoutStream(s);
			byte[] exp = new byte[] { (byte)'a', (byte)'b', (byte)'c' };
			s.Write(exp, 0, exp.Length);
			s.Seek(0, SeekOrigin.Begin);
			Assert.AreEqual(2, t.skip(2));
			Assert.AreEqual((byte)'c', t.ReadByte());
		}

		[Test]
		public void testTimeout_skip_Timeout()
		{
			beginRead();
			try
			{
				_timeoutstream.skip(1024);
				Assert.Fail("incorrectly skipped bytes");
			}
			catch (TimeoutException)
			{
				// expected
			}
			assertTimeout();
		}

		private void beginRead()
		{
			start = now();
		}

		private void assertTimeout()
		{
			// Our timeout was supposed to be ~250 ms. Since this is a timing
			// test we can't assume we spent *exactly* the timeout period, as
			// there may be other activity going on in the system. Instead we
			// look for the delta between the start and end times to be within
			// 50 ms of the expected timeout.
			//
			long wait = now() - start;
			Assert.IsTrue(Math.Abs(wait - timeout) < 50);
		}

		//private static List<Thread> active() {
		//   Thread[] all = new Thread[16];
		//   int n = Thread.currentThread().getThreadGroup().enumerate(all);
		//   while (n == all.length) {
		//      all = new Thread[all.length * 2];
		//      n = Thread.currentThread().getThreadGroup().enumerate(all);
		//   }
		//   return Arrays.asList(all).subList(0, n);
		//}

		private static long now()
		{
			return DateTimeOffset.Now.ToMillisecondsSinceEpoch();
		}
	}
}
