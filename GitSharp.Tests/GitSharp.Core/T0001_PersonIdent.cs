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
using GitSharp.Core;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Core.Tests
{
    [TestFixture]
    public class T0001_PersonIdent
    {
        [Test]
        public void GitTimeToDateTimeOffset()
        {
            Assert.AreEqual("06/21/2009 15:04:53 +02:00", 1245589493000L.MillisToDateTimeOffset(2 * 60).ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual("04/29/2009 18:41:17 -07:00", 1241055677000L.MillisToDateTimeOffset(-7 * 60).ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(1245589493000L, new DateTimeOffset(2009, 06, 21, 15, 04, 53, new TimeSpan(2, 0, 0)).ToMillisecondsSinceEpoch());
            Assert.AreEqual(1241055677000L, new DateTimeOffset(2009, 04, 29, 18, 41, 17, new TimeSpan(-7, 0, 0)).ToMillisecondsSinceEpoch());
        }

        [Test]
        public void test001_NewIdent()
        {
            PersonIdent p;
            p = new PersonIdent("A U Thor", "author@example.com", 1142878501000L.MillisToUtcDateTime(),
                                    (int)new TimeSpan(-5, 0, 0).TotalMinutes);

            Assert.AreEqual("A U Thor", p.Name);
            Assert.AreEqual("author@example.com", p.EmailAddress);
            Assert.AreEqual(1142878501000L, p.When);
            Assert.AreEqual("A U Thor <author@example.com> 1142878501 -0500", p.ToExternalString());
        }

        [Test]
        public void test002_ParseIdent()
        {
            string i = "A U Thor <author@example.com> 1142878501 -0500";
            var p = new PersonIdent(i);
            Assert.AreEqual(i, p.ToExternalString());
            Assert.AreEqual("A U Thor", p.Name);
            Assert.AreEqual("author@example.com", p.EmailAddress);
            Assert.AreEqual(1142878501000L, p.When);
        }

        [Test]
        public void test003_ParseIdent()
        {
            string i = "A U Thor <author@example.com> 1142878501 +0230";
            var p = new PersonIdent(i);
            Assert.AreEqual(i, p.ToExternalString());
            Assert.AreEqual("A U Thor", p.Name);
            Assert.AreEqual("author@example.com", p.EmailAddress);
            Assert.AreEqual(1142878501000L, p.When);
        }

        [Test]
        public void test004_ParseIdent()
        {
            string i = "A U Thor<author@example.com> 1142878501 +0230";
            var p = new PersonIdent(i);
            Assert.AreEqual("A U Thor", p.Name);
            Assert.AreEqual("author@example.com", p.EmailAddress);
            Assert.AreEqual(1142878501000L, p.When);
        }

        [Test]
        public void test005_ParseIdent()
        {
            string i = "A U Thor<author@example.com>1142878501 +0230";
            var p = new PersonIdent(i);
            Assert.AreEqual("A U Thor", p.Name);
            Assert.AreEqual("author@example.com", p.EmailAddress);
            Assert.AreEqual(1142878501000L, p.When);
        }

        [Test]
        public void test006_ParseIdent()
        {
            string i = "A U Thor   <author@example.com>1142878501 +0230";
            var p = new PersonIdent(i);
            Assert.AreEqual("A U Thor", p.Name);
            Assert.AreEqual("author@example.com", p.EmailAddress);
            Assert.AreEqual(1142878501000L, p.When);
        }

        [Test]
        public void test007_ParseIdent()
        {
            string i = "A U Thor<author@example.com>1142878501 +0230 ";
            var p = new PersonIdent(i);
            Assert.AreEqual("A U Thor", p.Name);
            Assert.AreEqual("author@example.com", p.EmailAddress);
            Assert.AreEqual(1142878501000L, p.When);
        }
    }
}
