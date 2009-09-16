/*
 * Copyright (C) 2007, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using System.Globalization;
using GitSharp.Util;
using Xunit;

namespace GitSharp.Tests
{
    public class PersonIdentTests
    {
        [Fact]
        public void GitTimeToDateTimeOffset()
        {
            Assert.Equal("06/21/2009 15:04:53 +02:00", 1245589493L.GitTimeToDateTimeOffset(2 * 60).ToString(CultureInfo.InvariantCulture));
            Assert.Equal("04/29/2009 18:41:17 -07:00", 1241055677L.GitTimeToDateTimeOffset(-7 * 60).ToString(CultureInfo.InvariantCulture));
            Assert.Equal(new DateTimeOffset(2009, 06, 21, 15, 04, 53, new TimeSpan(2, 0, 0)).ToGitInternalTime(), 1245589493L);
            Assert.Equal(new DateTimeOffset(2009, 04, 29, 18, 41, 17, new TimeSpan(-7, 0, 0)).ToGitInternalTime(), 1241055677L);
        }

        [Fact]
        public void test001_NewIdent()
        {
            var p = new PersonIdent("A U Thor", "author@example.com", 1142878501L, 0);
            Assert.Equal("A U Thor", p.Name);
            Assert.Equal("author@example.com", p.EmailAddress);
            Assert.Equal(1142878501L, p.When.ToGitInternalTime());
            Assert.Equal("A U Thor <author@example.com> 1142878501 +0000", p.ToExternalString());
        }

        [Fact]
        public void test002_ParseIdent()
        {
            const string i = "A U Thor <author@example.com> 1142878501 -0500";
            var p = new PersonIdent(i);
            Assert.Equal(i, p.ToExternalString());
            Assert.Equal("A U Thor", p.Name);
            Assert.Equal("author@example.com", p.EmailAddress);
            Assert.Equal(1142878501L, p.When.ToGitInternalTime());
        }

        [Fact]
        public void test003_ParseIdent()
        {
            const string i = "A U Thor <author@example.com> 1142878501 +0230";
            var p = new PersonIdent(i);
            Assert.Equal(i, p.ToExternalString());
            Assert.Equal("A U Thor", p.Name);
            Assert.Equal("author@example.com", p.EmailAddress);
            Assert.Equal(1142878501L, p.When.ToGitInternalTime());
        }

        [Fact]
        public void test004_ParseIdent()
        {
            const string i = "A U Thor<author@example.com> 1142878501 +0230";
            var p = new PersonIdent(i);
            Assert.Equal("A U Thor", p.Name);
            Assert.Equal("author@example.com", p.EmailAddress);
            Assert.Equal(1142878501L, p.When.ToGitInternalTime());
        }

        [Fact]
        public void test005_ParseIdent()
        {
            const string i = "A U Thor<author@example.com>1142878501 +0230";
            var p = new PersonIdent(i);
            Assert.Equal("A U Thor", p.Name);
            Assert.Equal("author@example.com", p.EmailAddress);
            Assert.Equal(1142878501L, p.When.ToGitInternalTime());
        }

        [Fact]
        public void test006_ParseIdent()
        {
            const string i = "A U Thor   <author@example.com>1142878501 +0230";
            var p = new PersonIdent(i);
            Assert.Equal("A U Thor", p.Name);
            Assert.Equal("author@example.com", p.EmailAddress);
            Assert.Equal(1142878501L, p.When.ToGitInternalTime());
        }

        [Fact]
        public void test007_ParseIdent()
        {
            const string i = "A U Thor<author@example.com>1142878501 +0230 ";
            var p = new PersonIdent(i);
            Assert.Equal("A U Thor", p.Name);
            Assert.Equal("author@example.com", p.EmailAddress);
            Assert.Equal(1142878501L, p.When.ToGitInternalTime());
        }
    }
}
