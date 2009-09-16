/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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
using GitSharp.Util;
using Xunit;

namespace GitSharp.Tests.Util
{
	public class QuotedStringGitPathStyleTest
	{
		private static void AssertQuote(string exp, string inStr)
		{
			string r = QuotedString.GitPath.quote(inStr);
			Assert.NotSame(inStr, r);
			Assert.False(inStr.Equals(r));
			Assert.Equal('"' + exp + '"', r);
		}

		private static void AssertDequote(string exp, string inStr)
		{
			byte[] b = Constants.encodeASCII('"' + inStr + '"');
			string r = QuotedString.GitPath.dequote(b, 0, b.Length);
			Assert.Equal(exp, r);
		}

		[Fact]
		public void testQuote_Empty()
		{
			Assert.Equal("\"\"", QuotedString.GitPath.quote(string.Empty));
		}

		[Fact]
		public void testDequote_Empty1()
		{
			Assert.Equal(string.Empty, QuotedString.GitPath.dequote(new byte[0], 0, 0));
		}

		[Fact]
		public void testDequote_Empty2()
		{
			Assert.Equal(string.Empty, QuotedString.GitPath.dequote(new[] { (byte)'"', (byte)'"' }, 0, 2));
		}

		[Fact]
		public void testDequote_SoleDq()
		{
			Assert.Equal("\"", QuotedString.GitPath.dequote(new[] { (byte)'"' }, 0, 1));
		}

		[Fact]
		public void testQuote_BareA()
		{
			const string inStr = "a";
			Assert.Same(inStr, QuotedString.GitPath.quote(inStr));
		}

		[Fact]
		public void testDequote_BareA()
		{
			const string inStr = "a";
			byte[] b = Constants.encode(inStr);
			Assert.Equal(inStr, QuotedString.GitPath.dequote(b, 0, b.Length));
		}

		[Fact]
		public void testDequote_BareABCZ_OnlyBC()
		{
			const string inStr = "abcz";
			byte[] b = Constants.encode(inStr);
			int p = inStr.IndexOf('b');
			Assert.Equal("bc", QuotedString.GitPath.dequote(b, p, p + 2));
		}

		[Fact]
		public void testDequote_LoneBackslash()
		{
			AssertDequote("\\", "\\");
		}

		[Fact]
		public void testQuote_NamedEscapes()
		{
			AssertQuote("\\a", "\u0007");
			AssertQuote("\\b", "\b");
			AssertQuote("\\f", "\f");
			AssertQuote("\\n", "\n");
			AssertQuote("\\r", "\r");
			AssertQuote("\\t", "\t");
			AssertQuote("\\v", "\u000B");
			AssertQuote("\\\\", "\\");
			AssertQuote("\\\"", "\"");
		}

		[Fact]
		public void testDequote_NamedEscapes()
		{
			AssertDequote("\u0007", "\\a");
			AssertDequote("\b", "\\b");
			AssertDequote("\f", "\\f");
			AssertDequote("\n", "\\n");
			AssertDequote("\r", "\\r");
			AssertDequote("\t", "\\t");
			AssertDequote("\u000B", "\\v");
			AssertDequote("\\", "\\\\");
			AssertDequote("\"", "\\\"");
		}

		[Fact]
		public void testDequote_OctalAll()
		{
			for (int i = 0; i < 127; i++)
			{
				AssertDequote(string.Empty + (char) i, OctalEscape(i));
			}

			for (int i = 128; i < 256; i++)
			{
				int f = 0xC0 | (i >> 6);
				int s = 0x80 | (i & 0x3f);
				AssertDequote(string.Empty + (char) i, OctalEscape(f)+OctalEscape(s));
			}
		}

		private static string OctalEscape(int i)
		{
			String s = Convert.ToString(i, 8);
			while (s.Length < 3) 
			{
				s = "0" + s;
			}
			return "\\"+s;
		}

		[Fact]
		public void testQuote_OctalAll()
		{
			AssertQuote("\\001", new string((char)1,1));
			AssertQuote("\\176", "~");
			AssertQuote("\\303\\277", "\u00ff"); // \u00ff in UTF-8
		}

		[Fact]
		public void testDequote_UnknownEscapeQ()
		{
			AssertDequote("\\q", "\\q");
		}

		[Fact]
		public void testDequote_FooTabBar()
		{
			AssertDequote("foo\tbar", "foo\\tbar");
		}

		[Fact]
		public void testDequote_Latin1()
		{
			AssertDequote("\u00c5ngstr\u00f6m", "\\305ngstr\\366m"); // Latin1
		}

		[Fact]
		public void testDequote_UTF8()
		{
			AssertDequote("\u00c5ngstr\u00f6m", "\\303\\205ngstr\\303\\266m");
		}

		[Fact]
		public void testDequote_RawUTF8()
		{
			AssertDequote("\u00c5ngstr\u00f6m", "\\303\\205ngstr\\303\\266m");
		}

		[Fact]
		public void testDequote_RawLatin1()
		{
			AssertDequote("\u00c5ngstr\u00f6m", "\\305ngstr\\366m");
		}

		[Fact]
		public void testQuote_Ang()
		{
			AssertQuote("\\303\\205ngstr\\303\\266m", "\u00c5ngstr\u00f6m");
		}
	}
}