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
using GitSharp.Core;
using GitSharp.Core.Util;
using NUnit.Framework;
using System.Text;

namespace GitSharp.Tests
{
    [TestFixture]
    public class QuotedStringGitPathStyleTest
    {
        private static readonly QuotedString.GitPathStyle GitPath = QuotedString.GitPathStyle.GIT_PATH;

	    private static void AssertQuote(String exp, String in_str)
        {
		    String r = GitPath.quote(in_str);
		    Assert.AreNotSame(in_str, r);
		    Assert.IsFalse(in_str.Equals(r));
		    Assert.AreEqual('"' + exp + '"', r);
	    }

	    private static void AssertDequote(string exp, string inStr)
        {
	    	byte[] b = (new ASCIIEncoding()).GetBytes('"' + inStr + '"');
		    
		    String r = GitPath.dequote(b, 0, b.Length);
		    Assert.AreEqual(exp, r);
	    }

        [Test]
	    public void testQuote_Empty()
        {
		    Assert.AreEqual("\"\"", GitPath.quote(string.Empty));
	    }

        [Test]
	    public void testDequote_Empty1()
        {
		    Assert.AreEqual(string.Empty, GitPath.dequote(new byte[0], 0, 0));
	    }

        [Test]
	    public void testDequote_Empty2()
        {
		    Assert.AreEqual(string.Empty, GitPath.dequote(new byte[] { (byte)'"', (byte)'"' }, 0, 2));
	    }

        [Test]
	    public void testDequote_SoleDq()
        {
		    Assert.AreEqual("\"", GitPath.dequote(new byte[] { (byte)'"' }, 0, 1));
	    }

        [Test]
	    public void testQuote_BareA()
        {
		    String in_str = "a";
		    Assert.AreSame(in_str, GitPath.quote(in_str));
	    }

        [Test]
	    public void testDequote_BareA()
        {
		    String in_str = "a";
		    byte[] b = Constants.encode(in_str);
		    Assert.AreEqual(in_str, GitPath.dequote(b, 0, b.Length));
	    }

        [Test]
	    public void testDequote_BareABCZ_OnlyBC()
        {
		    String in_str = "abcz";
		    byte[] b = Constants.encode(in_str);
		    int p = in_str.IndexOf('b');
		    Assert.AreEqual("bc", GitPath.dequote(b, p, p + 2));
	    }

        [Test]
	    public void testDequote_LoneBackslash()
        {
		    AssertDequote("\\", "\\");
	    }

        [Test]
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

        [Test]
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

        [Test]
	    public void testDequote_OctalAll()
        {
		    for (int i = 0; i < 127; i++)
            {
			    AssertDequote(string.Empty + (char) i, octalEscape(i));
		    }

		    for (int i = 128; i < 256; i++)
            {
			    int f = 0xC0 | (i >> 6);
			    int s = 0x80 | (i & 0x3f);
			    AssertDequote(string.Empty + (char) i, octalEscape(f)+octalEscape(s));
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

        [Test]
	    public void testQuote_OctalAll()
        {
		    AssertQuote("\\001", new string((char)1,1));
		    AssertQuote("\\176", "~");
		    AssertQuote("\\303\\277", "\u00ff"); // \u00ff in UTF-8
	    }

        [Test]
	    public void testDequote_UnknownEscapeQ()
        {
		    AssertDequote("\\q", "\\q");
	    }

        [Test]
	    public void testDequote_FooTabBar()
        {
		    AssertDequote("foo\tbar", "foo\\tbar");
	    }

        [Test]
	    public void testDequote_Latin1()
        {
		    AssertDequote("\u00c5ngstr\u00f6m", "\\305ngstr\\366m"); // Latin1
	    }

        [Test]
	    public void testDequote_UTF8()
        {
		    AssertDequote("\u00c5ngstr\u00f6m", "\\303\\205ngstr\\303\\266m");
	    }

        [Test]
	    public void testDequote_RawUTF8()
        {
		    AssertDequote("\u00c5ngstr\u00f6m", "\\303\\205ngstr\\303\\266m");
	    }

        [Test]
	    public void testDequote_RawLatin1()
        {
		    AssertDequote("\u00c5ngstr\u00f6m", "\\305ngstr\\366m");
	    }

        [Test]
	    public void testQuote_Ang()
        {
		    AssertQuote("\\303\\205ngstr\\303\\266m", "\u00c5ngstr\u00f6m");
	    }
    }
}