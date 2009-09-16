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

using GitSharp.Util;
using Xunit;

namespace GitSharp.Tests
{
    public class RawParseUtils_LineMapTest
    {
        [Fact]
	    public void testEmpty()
        {
		    IntList map = RawParseUtils.lineMap(new byte[] {}, 0, 0);
		    Assert.NotNull(map);
		    Assert.Equal(2, map.size());
		    Assert.Equal(int.MinValue, map.get(0));
		    Assert.Equal(0, map.get(1));
	    }

        [Fact]
	    public void testOneBlankLine()
        {
		    IntList map = RawParseUtils.lineMap(new byte[] { (byte)'\n' }, 0, 1);
		    Assert.Equal(3, map.size());
		    Assert.Equal(int.MinValue, map.get(0));
		    Assert.Equal(0, map.get(1));
		    Assert.Equal(1, map.get(2));
	    }

        [Fact]
	    public void testTwoLineFooBar()
        {
            byte[] buf = "foo\nbar\n".getBytes("ISO-8859-1");
		    IntList map = RawParseUtils.lineMap(buf, 0, buf.Length);
		    Assert.Equal(4, map.size());
		    Assert.Equal(int.MinValue, map.get(0));
		    Assert.Equal(0, map.get(1));
		    Assert.Equal(4, map.get(2));
		    Assert.Equal(buf.Length, map.get(3));
	    }

        [Fact]
	    public void testTwoLineNoLF()
        {
            byte[] buf = "foo\nbar".getBytes("ISO-8859-1");
		    IntList map = RawParseUtils.lineMap(buf, 0, buf.Length);
		    Assert.Equal(4, map.size());
		    Assert.Equal(int.MinValue, map.get(0));
		    Assert.Equal(0, map.get(1));
		    Assert.Equal(4, map.get(2));
		    Assert.Equal(buf.Length, map.get(3));
	    }

        [Fact]
	    public void testFourLineBlanks()
        {
            byte[] buf = "foo\n\n\nbar\n".getBytes("ISO-8859-1");
		    IntList map = RawParseUtils.lineMap(buf, 0, buf.Length);
            Assert.Equal(6, map.size());
            Assert.Equal(int.MinValue, map.get(0));
            Assert.Equal(0, map.get(1));
            Assert.Equal(4, map.get(2));
            Assert.Equal(5, map.get(3));
            Assert.Equal(6, map.get(4));
            Assert.Equal(buf.Length, map.get(5));
	    }
    }
}