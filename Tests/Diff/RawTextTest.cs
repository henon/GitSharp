/*
 * Copyright (C) 2009, Google Inc.
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

using System.IO;
using GitSharp.Diff;
using NUnit.Framework;

namespace GitSharp.Tests
{
    [TestFixture]
    public class RawTextTest
    {
        [Test]
	    public void testEmpty()
        {
		    RawText r = new RawText(new byte[0]);
		    Assert.AreEqual(0, r.size());
	    }

        [Test]
	    public void testEquals()
        {
		    RawText a = new RawText(Constants.encodeASCII("foo-a\nfoo-b\n"));
		    RawText b = new RawText(Constants.encodeASCII("foo-b\nfoo-c\n"));

		    Assert.AreEqual(2, a.size());
		    Assert.AreEqual(2, b.size());

		    // foo-a != foo-b
		    Assert.IsFalse(a.equals(0, b, 0));
		    Assert.IsFalse(b.equals(0, a, 0));

		    // foo-b == foo-b
		    Assert.IsTrue(a.equals(1, b, 0));
		    Assert.IsTrue(b.equals(0, a, 1));
	    }

        [Test]
	    public void testWriteLine1()
        {
		    RawText a = new RawText(Constants.encodeASCII("foo-a\nfoo-b\n"));
		    MemoryStream o = new MemoryStream();
		    a.writeLine(o, 0);
		    byte[] r = o.ToArray();
		    Assert.AreEqual("foo-a", RawParseUtils.decode(r));
	    }

        [Test]
	    public void testWriteLine2()
        {
		    RawText a = new RawText(Constants.encodeASCII("foo-a\nfoo-b"));
		    MemoryStream o = new MemoryStream();
		    a.writeLine(o, 1);
		    byte[] r = o.ToArray();
		    Assert.AreEqual("foo-b", RawParseUtils.decode(r));
	    }

        [Test]
	    public void testWriteLine3()
        {
		    RawText a = new RawText(Constants.encodeASCII("a\n\nb\n"));
		    MemoryStream o = new MemoryStream();
		    a.writeLine(o, 1);
		    byte[] r = o.ToArray();
		    Assert.AreEqual("", RawParseUtils.decode(r));
	    }
    }
}