/*
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp
{
	[TestFixture]
	public class TextTests : ApiTestCase
	{
		const string TEXT = @"Player Queen:
Both here and hence pursue me lasting strife,
If once I be a widow, ever I be a wife!

Player King:
'Tis deeply sworn. Sweet, leave me here a while,
My spirits grow dull, and fain I would beguile
The tedious day with sleep.

Player Queen:
Sleep rock thy brain,
And never come mischance between us twain!

Hamlet:
Madam, how like you this play?

Queen:
The lady doth protest too much, methinks.";

		[Test]
		public void GetLineTest()
		{
			var text = new Text(TEXT);
			Assert.AreEqual("Player Queen:\r\n", text.GetLine(1));
			Assert.AreEqual("\r\n", text.GetLine(4));
			Assert.AreEqual(new[] { (byte)'P', (byte)'l', (byte)'a', (byte)'y', (byte)'e', (byte)'r', (byte)' ', (byte)'Q', (byte)'u', (byte)'e', (byte)'e', (byte)'n', (byte)':', (byte)'\r', (byte)'\n' }, text.GetRawLine(1));
			Assert.AreEqual(new[] { (byte)'\r', (byte)'\n' }, text.GetRawLine(4));
			Assert.AreEqual("The lady doth protest too much, methinks.", text.GetLine(18));
			Assert.Throws(typeof(ArgumentOutOfRangeException), () => text.GetLine(-1));
			Assert.Throws(typeof(ArgumentOutOfRangeException), () => text.GetLine(0));
			Assert.Throws(typeof(ArgumentOutOfRangeException), () => text.GetLine(19));
			Assert.AreEqual(18, text.NumberOfLines);
		}

		[Test]
		public void EncodingTest()
		{
			const string s = "üöäß\nÖÄÜ\n";
			var t = new Text(s, Encoding.GetEncoding("latin1"));
			Assert.AreEqual("üöäß\n", t.GetLine(1));
			Assert.AreEqual("ÖÄÜ\n", t.GetLine(2));
			Assert.Throws(typeof(ArgumentOutOfRangeException), () => t.GetLine(3));
			Assert.AreEqual(2, t.NumberOfLines);
			Assert.AreEqual(Encoding.UTF8, new Text("hmm").Encoding);
			Assert.AreEqual(Encoding.GetEncoding("latin1"), new Text("hmm", Encoding.GetEncoding("latin1")).Encoding);
			Assert.AreEqual(s, t.ToString());
			Assert.AreEqual(9, t.Length);
			Assert.AreEqual(9, new Text(s).Length);
			Assert.AreEqual(2, new Text("你好").Length);
			Assert.AreEqual(6, new Text("你好").RawLength);
			Assert.AreEqual(2, new Text("你好", Encoding.UTF32).Length);
			Assert.AreEqual(8, new Text("你好", Encoding.UTF32).RawLength);
		}

		[Test]
		public void GetBlockTest()
		{
			const string block = "Hamlet:\r\nMadam, how like you this play?\r\n";
			var text = new Text(TEXT);
			Assert.AreEqual("", text.GetBlock(14, 14));
			Assert.AreEqual("Hamlet:\r\n", text.GetBlock(14, 15));
			Assert.AreEqual(block, text.GetBlock(14, 16));
			Assert.AreEqual(Encoding.UTF8.GetBytes(TEXT), text.GetRawBlock(1, 19));
			Assert.AreEqual(TEXT, text.GetBlock(1, 19));
			Assert.Throws(typeof(ArgumentException), () => text.GetBlock(14, 13));
			Assert.Throws(typeof(ArgumentOutOfRangeException), () => text.GetBlock(0, 14));
			Assert.Throws(typeof(ArgumentOutOfRangeException), () => text.GetBlock(14, 20));
		}
	}
}
