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
using GitSharp;
using GitSharp.Util;
using Xunit;
using System.Text;

namespace GitSharp.Tests
{
    public class QuotedStringGitPathStyleTest
    {
        private static QuotedString.GitPathStyle GIT_PATH = QuotedString.GitPathStyle.GIT_PATH;

	    private static void assertQuote(String exp, String in_str)
        {
		    String r = GIT_PATH.quote(in_str);
		    Assert.NotSame(in_str, r);
		    Assert.False(in_str.Equals(r));
		    Assert.Equal('"' + exp + '"', r);
	    }

	    private static void assertDequote(String exp, String in_str)
        {
		    byte[] b;
		    
            b = (new ASCIIEncoding()).GetBytes('"' + in_str + '"');
		    
		    String r = GIT_PATH.dequote(b, 0, b.Length);
		    Assert.Equal(exp, r);
	    }

        [Fact]
	    public void testQuote_Empty()
        {
		    Assert.Equal("\"\"", GIT_PATH.quote(""));
	    }

        [Fact]
	    public void testDequote_Empty1()
        {
		    Assert.Equal("", GIT_PATH.dequote(new byte[0], 0, 0));
	    }

        [Fact]
	    public void testDequote_Empty2()
        {
		    Assert.Equal("", GIT_PATH.dequote(new byte[] { (byte)'"', (byte)'"' }, 0, 2));
	    }

        [Fact]
	    public void testDequote_SoleDq()
        {
		    Assert.Equal("\"", GIT_PATH.dequote(new byte[] { (byte)'"' }, 0, 1));
	    }

        [Fact]
	    public void testQuote_BareA()
        {
		    String in_str = "a";
		    Assert.Same(in_str, GIT_PATH.quote(in_str));
	    }

        [Fact]
	    public void testDequote_BareA()
        {
		    String in_str = "a";
		    byte[] b = Constants.encode(in_str);
		    Assert.Equal(in_str, GIT_PATH.dequote(b, 0, b.Length));
	    }

        [Fact]
	    public void testDequote_BareABCZ_OnlyBC()
        {
		    String in_str = "abcz";
		    byte[] b = Constants.encode(in_str);
		    int p = in_str.IndexOf('b');
		    Assert.Equal("bc", GIT_PATH.dequote(b, p, p + 2));
	    }

        [Fact]
	    public void testDequote_LoneBackslash()
        {
		    assertDequote("\\", "\\");
	    }

        [Fact]
	    public void testQuote_NamedEscapes()
        {
		    assertQuote("\\a", "\u0007");
		    assertQuote("\\b", "\b");
		    assertQuote("\\f", "\f");
		    assertQuote("\\n", "\n");
		    assertQuote("\\r", "\r");
		    assertQuote("\\t", "\t");
		    assertQuote("\\v", "\u000B");
		    assertQuote("\\\\", "\\");
		    assertQuote("\\\"", "\"");
	    }

        [Fact]
	    public void testDequote_NamedEscapes()
        {
		    assertDequote("\u0007", "\\a");
		    assertDequote("\b", "\\b");
		    assertDequote("\f", "\\f");
		    assertDequote("\n", "\\n");
		    assertDequote("\r", "\\r");
		    assertDequote("\t", "\\t");
		    assertDequote("\u000B", "\\v");
		    assertDequote("\\", "\\\\");
		    assertDequote("\"", "\\\"");
	    }

        [Fact]
	    public void testDequote_OctalAll()
        {
		    for (int i = 0; i < 127; i++)
            {
			    assertDequote("" + (char) i, octalEscape(i));
		    }

		    for (int i = 128; i < 256; i++)
            {
			    int f = 0xC0 | (i >> 6);
			    int s = 0x80 | (i & 0x3f);
			    assertDequote("" + (char) i, octalEscape(f)+octalEscape(s));
		    }
	    }

	    private String octalEscape(int i)
        {
            String s = Convert.ToString(i, 8);
		    while (s.Length < 3) {
			    s = "0" + s;
		    }
		    return "\\"+s;
	    }

        [Fact]
	    public void testQuote_OctalAll()
        {
		    assertQuote("\\001", new string((char)1,1));
		    assertQuote("\\176", "~");
		    assertQuote("\\303\\277", "\u00ff"); // \u00ff in UTF-8
	    }

        [Fact]
	    public void testDequote_UnknownEscapeQ()
        {
		    assertDequote("\\q", "\\q");
	    }

        [Fact]
	    public void testDequote_FooTabBar()
        {
		    assertDequote("foo\tbar", "foo\\tbar");
	    }

        [Fact]
	    public void testDequote_Latin1()
        {
		    assertDequote("\u00c5ngstr\u00f6m", "\\305ngstr\\366m"); // Latin1
	    }

        [Fact]
	    public void testDequote_UTF8()
        {
		    assertDequote("\u00c5ngstr\u00f6m", "\\303\\205ngstr\\303\\266m");
	    }

        [Fact]
	    public void testDequote_RawUTF8()
        {
		    assertDequote("\u00c5ngstr\u00f6m", "\\303\\205ngstr\\303\\266m");
	    }

        [Fact]
	    public void testDequote_RawLatin1()
        {
		    assertDequote("\u00c5ngstr\u00f6m", "\\305ngstr\\366m");
	    }

        [Fact]
	    public void testQuote_Ang()
        {
		    assertQuote("\\303\\205ngstr\\303\\266m", "\u00c5ngstr\u00f6m");
	    }
    }
}