/*
 * Copyright (C) 2009, Robin Rosenberg
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.Util;
using NUnit.Framework;

namespace GitSharp.Tests
{
    [TestFixture]
    public class ReflogReaderTest : RepositoryTestCase
    {

        static byte[] oneLine = "da85355dfc525c9f6f3927b876f379f46ccf826e 3e7549db262d1e836d9bf0af7e22355468f1717c A O Thor Too <authortoo@wri.tr> 1243028200 +0200\tcommit: Add a toString for debugging to RemoteRefUpdate\n"
                .getBytes();

        static byte[] twoLine = ("0000000000000000000000000000000000000000 c6734895958052a9dbc396cff4459dc1a25029ab A U Thor <thor@committer.au> 1243028201 -0100\tbranch: Created from rr/renamebranchv4\n"
                + "c6734895958052a9dbc396cff4459dc1a25029ab 54794942a18a237c57a80719afed44bb78172b10 Same A U Thor <same.author@example.com> 1243028202 +0100\trebase finished: refs/heads/rr/renamebranch5 onto c6e3b9fe2da0293f11eae202ec35fb343191a82d\n")
                .getBytes();

        static byte[] twoLineWithAppendInProgress = ("0000000000000000000000000000000000000000 c6734895958052a9dbc396cff4459dc1a25029ab A U Thor <thor@committer.au> 1243028201 -0100\tbranch: Created from rr/renamebranchv4\n"
                + "c6734895958052a9dbc396cff4459dc1a25029ab 54794942a18a237c57a80719afed44bb78172b10 Same A U Thor <same.author@example.com> 1243028202 +0100\trebase finished: refs/heads/rr/renamebranch5 onto c6e3b9fe2da0293f11eae202ec35fb343191a82d\n"
                + "54794942a18a237c57a80719afed44bb78172b10 ")
                .getBytes();

        static byte[] aLine = "1111111111111111111111111111111111111111 3e7549db262d1e836d9bf0af7e22355468f1717c A U Thor <thor@committer.au> 1243028201 -0100\tbranch: change to a\n"
                .getBytes();

        static byte[] masterLine = "2222222222222222222222222222222222222222 3e7549db262d1e836d9bf0af7e22355468f1717c A U Thor <thor@committer.au> 1243028201 -0100\tbranch: change to master\n"
                .getBytes();

        static byte[] headLine = "3333333333333333333333333333333333333333 3e7549db262d1e836d9bf0af7e22355468f1717c A U Thor <thor@committer.au> 1243028201 -0100\tbranch: change to HEAD\n"
                .getBytes();

        [Test]
        public void testReadOneLine()
        {
            setupReflog("logs/refs/heads/master", oneLine);

            ReflogReader reader = new ReflogReader(db, "refs/heads/master");
            ReflogReader.Entry e = reader.getLastEntry();
            Assert.AreEqual(ObjectId
                    .FromString("da85355dfc525c9f6f3927b876f379f46ccf826e"), e
                    .getOldId());
            Assert.AreEqual(ObjectId
                    .FromString("3e7549db262d1e836d9bf0af7e22355468f1717c"), e
                    .getNewId());
            Assert.AreEqual("A O Thor Too", e.getWho().Name);
            Assert.AreEqual("authortoo@wri.tr", e.getWho().EmailAddress);
            Assert.AreEqual(120, e.getWho().TimeZoneOffset);
            Assert.AreEqual("2009-05-22T23:36:40", iso(e.getWho()));
            Assert.AreEqual("commit: Add a toString for debugging to RemoteRefUpdate",
                    e.getComment());
        }

        private String iso(PersonIdent id)
        {
            return id.When.UnixTimeToDateTimeOffset(id.TimeZoneOffset).ToString("yyyy-MM-dd'T'HH:mm:ss");
            //return id.When.ToString("yyyy-MM-dd'T'HH:mm:ss");
            //SimpleDateFormat fmt;
            //fmt = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss");
            //fmt.setTimeZone(id.TimeZoneOffset);
            //return fmt.format(id.When);
        }

        [Test]
        public void testReadTwoLine()
        {
            setupReflog("logs/refs/heads/master", twoLine);

            ReflogReader reader = new ReflogReader(db, "refs/heads/master");
            var reverseEntries = reader.getReverseEntries();
            Assert.AreEqual(2, reverseEntries.Count);
            ReflogReader.Entry e = reverseEntries[0];

            Assert.AreEqual(ObjectId
                    .FromString("c6734895958052a9dbc396cff4459dc1a25029ab"), e
                    .getOldId());

            Assert.AreEqual(ObjectId
                    .FromString("54794942a18a237c57a80719afed44bb78172b10"), e
                    .getNewId());

            Assert.AreEqual("Same A U Thor", e.getWho().Name);
            Assert.AreEqual("same.author@example.com", e.getWho().EmailAddress);
            Assert.AreEqual(60, e.getWho().TimeZoneOffset);
            Assert.AreEqual("2009-05-22T22:36:42", iso(e.getWho()));
            Assert.AreEqual(
                    "rebase finished: refs/heads/rr/renamebranch5 onto c6e3b9fe2da0293f11eae202ec35fb343191a82d",
                    e.getComment());

            e = reverseEntries[1];
            Assert.AreEqual(ObjectId
                    .FromString("0000000000000000000000000000000000000000"), e
                    .getOldId());

            Assert.AreEqual(ObjectId
                    .FromString("c6734895958052a9dbc396cff4459dc1a25029ab"), e
                    .getNewId());

            Assert.AreEqual("A U Thor", e.getWho().Name);
            Assert.AreEqual("thor@committer.au", e.getWho().EmailAddress);
            Assert.AreEqual(-60, e.getWho().TimeZoneOffset);
            Assert.AreEqual("2009-05-22T20:36:41", iso(e.getWho()));
            Assert.AreEqual("branch: Created from rr/renamebranchv4", e.getComment());
        }

        [Test]
        public void testReadWhileAppendIsInProgress()
        {
            setupReflog("logs/refs/heads/master", twoLineWithAppendInProgress);
            ReflogReader reader = new ReflogReader(db, "refs/heads/master");
            var reverseEntries = reader.getReverseEntries();
            Assert.AreEqual(2, reverseEntries.Count);
            ReflogReader.Entry e = reverseEntries[0];

            Assert.AreEqual(ObjectId
                    .FromString("c6734895958052a9dbc396cff4459dc1a25029ab"), e
                    .getOldId());

            Assert.AreEqual(ObjectId
                    .FromString("54794942a18a237c57a80719afed44bb78172b10"), e
                    .getNewId());

            Assert.AreEqual("Same A U Thor", e.getWho().Name);
            Assert.AreEqual("same.author@example.com", e.getWho().EmailAddress);
            Assert.AreEqual(60, e.getWho().TimeZoneOffset);
            Assert.AreEqual("2009-05-22T22:36:42", iso(e.getWho()));
            Assert.AreEqual(
                    "rebase finished: refs/heads/rr/renamebranch5 onto c6e3b9fe2da0293f11eae202ec35fb343191a82d",
                    e.getComment());
            // while similar to testReadTwoLine, we can assume that if we get the last entry
            // right, everything else is too
        }


        [Test]
        public void testReadRightLog()
        {
            setupReflog("logs/refs/heads/a", aLine);
            setupReflog("logs/refs/heads/master", masterLine);
            setupReflog("logs/HEAD", headLine);
            Assert.AreEqual("branch: change to master", db.ReflogReader("master")
                    .getLastEntry().getComment());
            Assert.AreEqual("branch: change to a", db.ReflogReader("a")
                    .getLastEntry().getComment());
            Assert.AreEqual("branch: change to HEAD", db.ReflogReader("HEAD")
                    .getLastEntry().getComment());
        }

        [Test]
        public void testNoLog()
        {
            Assert.AreEqual(0, db.ReflogReader("master").getReverseEntries().Count);
            Assert.IsNull(db.ReflogReader("master").getLastEntry());
        }
    }
}
