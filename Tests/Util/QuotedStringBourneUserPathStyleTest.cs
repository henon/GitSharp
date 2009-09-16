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

namespace GitSharp.Tests
{
    public class QuotedStringBourneUserPathStyleTest
    {
	    private static void AssertQuote(string inStr, string exp)
        {
		    string r = QuotedString.BOURNE_USER_PATH.quote(inStr);
		    Assert.NotSame(inStr, r);
		    Assert.False(inStr.Equals(r));
		    Assert.Equal('\'' + exp + '\'', r);
	    }

	    private static void AssertDequote(string exp, string inStr)
        {
		    byte[] b = Constants.encode('\'' + inStr + '\'');
		    string r = QuotedString.BOURNE_USER_PATH.dequote(b, 0, b.Length);
		    Assert.Equal(exp, r);
	    }

        [Fact]
	    public void testQuote_Empty()
        {
            Assert.Equal("''", QuotedString.BOURNE_USER_PATH.quote(string.Empty));
	    }

        [Fact]
	    public void testDequote_Empty1()
        {
            Assert.Equal(string.Empty, QuotedString.BOURNE.dequote(new byte[0], 0, 0));
	    }

        [Fact]
	    public void testDequote_Empty2()
        {
            Assert.Equal(string.Empty, QuotedString.BOURNE_USER_PATH.dequote(new byte[] { (byte)'\'', (byte)'\'' }, 0,
				    2));
	    }

        [Fact]
	    public void testDequote_SoleSq()
        {
            Assert.Equal(string.Empty, QuotedString.BOURNE_USER_PATH.dequote(new byte[] { (byte)'\'' }, 0, 1));
	    }

        [Fact]
	    public void testQuote_BareA()
        {
		    AssertQuote("a", "a");
	    }

        [Fact]
	    public void testDequote_BareA()
        {
		    const string inStr = "a";
		    byte[] b = Constants.encode(inStr);
            Assert.Equal(inStr, QuotedString.BOURNE_USER_PATH.dequote(b, 0, b.Length));
	    }

        [Fact]
	    public void testDequote_BareABCZ_OnlyBC()
        {
		    const string inStr = "abcz";
		    byte[] b = Constants.encode(inStr);
		    int p = inStr.IndexOf('b');
            Assert.Equal("bc", QuotedString.BOURNE_USER_PATH.dequote(b, p, p + 2));
	    }

        [Fact]
	    public void testDequote_LoneBackslash()
        {
		    AssertDequote("\\", "\\");
	    }

        [Fact]
	    public void testQuote_NamedEscapes()
        {
		    AssertQuote("'", "'\\''");
		    AssertQuote("!", "'\\!'");

		    AssertQuote("a'b", "a'\\''b");
		    AssertQuote("a!b", "a'\\!'b");
	    }

        [Fact]
	    public void testDequote_NamedEscapes()
        {
		    AssertDequote("'", "'\\''");
		    AssertDequote("!", "'\\!'");

		    AssertDequote("a'b", "a'\\''b");
		    AssertDequote("a!b", "a'\\!'b");
	    }

        [Fact]
	    public void testQuote_User()
        {
            Assert.Equal("~foo/", QuotedString.BOURNE_USER_PATH.quote("~foo"));
            Assert.Equal("~foo/", QuotedString.BOURNE_USER_PATH.quote("~foo/"));
            Assert.Equal("~/", QuotedString.BOURNE_USER_PATH.quote("~/"));

            Assert.Equal("~foo/'a'", QuotedString.BOURNE_USER_PATH.quote("~foo/a"));
            Assert.Equal("~/'a'", QuotedString.BOURNE_USER_PATH.quote("~/a"));
	    }

        [Fact]
	    public void testDequote_User()
        {
            Assert.Equal("~foo", QuotedString.BOURNE_USER_PATH.dequote("~foo"));
            Assert.Equal("~foo/", QuotedString.BOURNE_USER_PATH.dequote("~foo/"));
            Assert.Equal("~/", QuotedString.BOURNE_USER_PATH.dequote("~/"));

            Assert.Equal("~foo/a", QuotedString.BOURNE_USER_PATH.dequote("~foo/'a'"));
            Assert.Equal("~/a", QuotedString.BOURNE_USER_PATH.dequote("~/'a'"));
	    }
    }
}