/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in_str source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in_str binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in_str the documentation and/or other materials provided
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
using GitSharp.Util;
using Xunit;

namespace GitSharp.Tests.Util
{
	public class QuotedStringBourneStyleTest
	{
		private static void AssertQuote(string inStr, string exp)
		{
			string r = QuotedString.BOURNE.quote(inStr);
			Assert.NotSame(inStr, r);
			Assert.False(inStr.Equals(r));
			Assert.Equal('\'' + exp + '\'', r);
		}

		private static void AssertDequote(string exp, string inStr)
		{
			byte[] b = Constants.encode('\'' + inStr + '\'');
			String r = QuotedString.BOURNE.dequote(b, 0, b.Length);
			Assert.Equal(exp, r);
		}

		[StrictFactAttribute]
		public void testDequote_BareA()
		{
			const string in_str = "a";
			byte[] b = Constants.encode(in_str);
			Assert.Equal(in_str, QuotedString.BOURNE.dequote(b, 0, b.Length));
		}

		[StrictFactAttribute]
		public void testDequote_BareABCZ_OnlyBC()
		{
			const string in_str = "abcz";
			byte[] b = Constants.encode(in_str);
			int p = in_str.IndexOf('b');
			Assert.Equal("bc", QuotedString.BOURNE.dequote(b, p, p + 2));
		}

		[StrictFactAttribute]
		public void testDequote_Empty1()
		{
			Assert.Equal(string.Empty, QuotedString.BOURNE.dequote(new byte[0], 0, 0));
		}

		[StrictFactAttribute]
		public void testDequote_Empty2()
		{
			Assert.Equal(string.Empty, QuotedString.BOURNE.dequote(new[] {(byte) '\'', (byte) '\''}, 0, 2));
		}

		[StrictFactAttribute]
		public void testDequote_LoneBackslash()
		{
			AssertDequote("\\", "\\");
		}

		[StrictFactAttribute]
		public void testDequote_NamedEscapes()
		{
			AssertDequote("'", "'\\''");
			AssertDequote("!", "'\\!'");

			AssertDequote("a'b", "a'\\''b");
			AssertDequote("a!b", "a'\\!'b");
		}

		[StrictFactAttribute]
		public void testDequote_SoleSq()
		{
			Assert.Equal(string.Empty, QuotedString.BOURNE.dequote(new[] {(byte) '\''}, 0, 1));
		}

		[StrictFactAttribute]
		public void testQuote_BareA()
		{
			AssertQuote("a", "a");
		}

		[StrictFactAttribute]
		public void testQuote_Empty()
		{
			Assert.Equal("''", QuotedString.BOURNE.quote(string.Empty));
		}

		[StrictFactAttribute]
		public void testQuote_NamedEscapes()
		{
			AssertQuote("'", "'\\''");
			AssertQuote("!", "'\\!'");

			AssertQuote("a'b", "a'\\''b");
			AssertQuote("a!b", "a'\\!'b");
		}
	}
}