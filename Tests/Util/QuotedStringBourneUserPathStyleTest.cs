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
using NUnit.Framework;

namespace GitSharp.Tests
{
    [TestFixture]
    public class QuotedStringBourneUserPathStyleTest
    {
	    private static void assertQuote(String in_str, String exp)
        {
		    String r = QuotedString.BOURNE_USER_PATH.quote(in_str);
		    Assert.AreNotSame(in_str, r);
		    Assert.IsFalse(in_str.Equals(r));
		    Assert.AreEqual('\'' + exp + '\'', r);
	    }

	    private static void assertDequote(String exp, String in_str)
        {
		    byte[] b = Constants.encode('\'' + in_str + '\'');
		    string r = QuotedString.BOURNE_USER_PATH.dequote(b, 0, b.Length);
		    Assert.AreEqual(exp, r);
	    }

        [Test]
	    public void testQuote_Empty()
        {
            Assert.AreEqual("''", QuotedString.BOURNE_USER_PATH.quote(""));
	    }

        [Test]
	    public void testDequote_Empty1()
        {
            Assert.AreEqual("", QuotedString.BOURNE.dequote(new byte[0], 0, 0));
	    }

        [Test]
	    public void testDequote_Empty2()
        {
            Assert.AreEqual("", QuotedString.BOURNE_USER_PATH.dequote(new byte[] { (byte)'\'', (byte)'\'' }, 0,
				    2));
	    }

        [Test]
	    public void testDequote_SoleSq()
        {
            Assert.AreEqual("", QuotedString.BOURNE_USER_PATH.dequote(new byte[] { (byte)'\'' }, 0, 1));
	    }

        [Test]
	    public void testQuote_BareA()
        {
		    assertQuote("a", "a");
	    }

        [Test]
	    public void testDequote_BareA()
        {
		    String in_str = "a";
		    byte[] b = Constants.encode(in_str);
            Assert.AreEqual(in_str, QuotedString.BOURNE_USER_PATH.dequote(b, 0, b.Length));
	    }

        [Test]
	    public void testDequote_BareABCZ_OnlyBC()
        {
		    String in_str = "abcz";
		    byte[] b = Constants.encode(in_str);
		    int p = in_str.IndexOf('b');
            Assert.AreEqual("bc", QuotedString.BOURNE_USER_PATH.dequote(b, p, p + 2));
	    }

        [Test]
	    public void testDequote_LoneBackslash()
        {
		    assertDequote("\\", "\\");
	    }

        [Test]
	    public void testQuote_NamedEscapes()
        {
		    assertQuote("'", "'\\''");
		    assertQuote("!", "'\\!'");

		    assertQuote("a'b", "a'\\''b");
		    assertQuote("a!b", "a'\\!'b");
	    }

        [Test]
	    public void testDequote_NamedEscapes()
        {
		    assertDequote("'", "'\\''");
		    assertDequote("!", "'\\!'");

		    assertDequote("a'b", "a'\\''b");
		    assertDequote("a!b", "a'\\!'b");
	    }

        [Test]
	    public void testQuote_User()
        {
            Assert.AreEqual("~foo/", QuotedString.BOURNE_USER_PATH.quote("~foo"));
            Assert.AreEqual("~foo/", QuotedString.BOURNE_USER_PATH.quote("~foo/"));
            Assert.AreEqual("~/", QuotedString.BOURNE_USER_PATH.quote("~/"));

            Assert.AreEqual("~foo/'a'", QuotedString.BOURNE_USER_PATH.quote("~foo/a"));
            Assert.AreEqual("~/'a'", QuotedString.BOURNE_USER_PATH.quote("~/a"));
	    }

        [Test]
	    public void testDequote_User()
        {
            Assert.AreEqual("~foo", QuotedString.BOURNE_USER_PATH.dequote("~foo"));
            Assert.AreEqual("~foo/", QuotedString.BOURNE_USER_PATH.dequote("~foo/"));
            Assert.AreEqual("~/", QuotedString.BOURNE_USER_PATH.dequote("~/"));

            Assert.AreEqual("~foo/a", QuotedString.BOURNE_USER_PATH.dequote("~foo/'a'"));
            Assert.AreEqual("~/a", QuotedString.BOURNE_USER_PATH.dequote("~/'a'"));
	    }
    }
}