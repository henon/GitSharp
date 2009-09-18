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

using GitSharp.Tests.Util;
using GitSharp.Util;
using Xunit;

namespace GitSharp.Tests
{
	public class ReflogReaderTest : RepositoryTestCase
	{
		static readonly byte[] OneLine = "da85355dfc525c9f6f3927b876f379f46ccf826e 3e7549db262d1e836d9bf0af7e22355468f1717c A O Thor Too <authortoo@wri.tr> 1243028200 +0200\tcommit: Add a toString for debugging to RemoteRefUpdate\n"
				.getBytes();

		static readonly byte[] TwoLine = ("0000000000000000000000000000000000000000 c6734895958052a9dbc396cff4459dc1a25029ab A U Thor <thor@committer.au> 1243028201 -0100\tbranch: Created from rr/renamebranchv4\n"
				+ "c6734895958052a9dbc396cff4459dc1a25029ab 54794942a18a237c57a80719afed44bb78172b10 Same A U Thor <same.author@example.com> 1243028202 +0100\trebase finished: refs/heads/rr/renamebranch5 onto c6e3b9fe2da0293f11eae202ec35fb343191a82d\n")
				.getBytes();

		static readonly byte[] TwoLineWithAppendInProgress = ("0000000000000000000000000000000000000000 c6734895958052a9dbc396cff4459dc1a25029ab A U Thor <thor@committer.au> 1243028201 -0100\tbranch: Created from rr/renamebranchv4\n"
				+ "c6734895958052a9dbc396cff4459dc1a25029ab 54794942a18a237c57a80719afed44bb78172b10 Same A U Thor <same.author@example.com> 1243028202 +0100\trebase finished: refs/heads/rr/renamebranch5 onto c6e3b9fe2da0293f11eae202ec35fb343191a82d\n"
				+ "54794942a18a237c57a80719afed44bb78172b10 ")
				.getBytes();

		static readonly byte[] ALine = "1111111111111111111111111111111111111111 3e7549db262d1e836d9bf0af7e22355468f1717c A U Thor <thor@committer.au> 1243028201 -0100\tbranch: change to a\n"
				.getBytes();

		static readonly byte[] MasterLine = "2222222222222222222222222222222222222222 3e7549db262d1e836d9bf0af7e22355468f1717c A U Thor <thor@committer.au> 1243028201 -0100\tbranch: change to master\n"
				.getBytes();

		static readonly byte[] HeadLine = "3333333333333333333333333333333333333333 3e7549db262d1e836d9bf0af7e22355468f1717c A U Thor <thor@committer.au> 1243028201 -0100\tbranch: change to HEAD\n"
				.getBytes();

		[Fact]
		public void testReadOneLine()
		{
			SetupReflog("logs/refs/heads/master", OneLine);

			var reader = new ReflogReader(db, "refs/heads/master");
			ReflogReader.Entry e = reader.getLastEntry();

			Assert.Equal(ObjectId.FromString("da85355dfc525c9f6f3927b876f379f46ccf826e"), e.OldId);

			Assert.Equal(ObjectId.FromString("3e7549db262d1e836d9bf0af7e22355468f1717c"), e.NewId);

			Assert.Equal("A O Thor Too", e.Who.Name);
			Assert.Equal("authortoo@wri.tr", e.Who.EmailAddress);
			Assert.Equal("120", e.Who.TimeZone.ToString());
			Assert.Equal("2009-05-22T23:36:40", e.Who.When.ToIsoFormatDate());

			Assert.Equal("commit: Add a toString for debugging to RemoteRefUpdate", e.Comment);
		}

		[Fact]
		public void testReadTwoLine()
		{
			SetupReflog("logs/refs/heads/master", TwoLine);

			var reader = new ReflogReader(db, "refs/heads/master");
			var reverseEntries = reader.getReverseEntries();
			Assert.Equal(2, reverseEntries.Count);
			ReflogReader.Entry e = reverseEntries[0];

			Assert.Equal(ObjectId.FromString("c6734895958052a9dbc396cff4459dc1a25029ab"), e.OldId);

			Assert.Equal(ObjectId.FromString("54794942a18a237c57a80719afed44bb78172b10"), e.NewId);

			Assert.Equal("Same A U Thor", e.Who.Name);
			Assert.Equal("same.author@example.com", e.Who.EmailAddress);
			Assert.Equal("60", e.Who.TimeZone.ToString());
			Assert.Equal("2009-05-22T22:36:42", e.Who.When.ToIsoFormatDate());
			Assert.Equal("rebase finished: refs/heads/rr/renamebranch5 onto c6e3b9fe2da0293f11eae202ec35fb343191a82d",
				e.Comment);

			e = reverseEntries[1];

			Assert.Equal(ObjectId.FromString("0000000000000000000000000000000000000000"), e.OldId);

			Assert.Equal(ObjectId.FromString("c6734895958052a9dbc396cff4459dc1a25029ab"), e.NewId);

			Assert.Equal("A U Thor", e.Who.Name);
			Assert.Equal("thor@committer.au", e.Who.EmailAddress);
			Assert.Equal("-60", e.Who.TimeZone.ToString());
			Assert.Equal("2009-05-22T20:36:41", e.Who.When.ToIsoFormatDate());
			Assert.Equal("branch: Created from rr/renamebranchv4", e.Comment);
		}

		[Fact]
		public void testReadWhileAppendIsInProgress()
		{
			SetupReflog("logs/refs/heads/master", TwoLineWithAppendInProgress);
			var reader = new ReflogReader(db, "refs/heads/master");
			var reverseEntries = reader.getReverseEntries();
			Assert.Equal(2, reverseEntries.Count);
			ReflogReader.Entry e = reverseEntries[0];

			Assert.Equal(ObjectId.FromString("c6734895958052a9dbc396cff4459dc1a25029ab"), e.OldId);

			Assert.Equal(ObjectId.FromString("54794942a18a237c57a80719afed44bb78172b10"), e.NewId);

			Assert.Equal("Same A U Thor", e.Who.Name);
			Assert.Equal("same.author@example.com", e.Who.EmailAddress);
			Assert.Equal("60", e.Who.TimeZone.ToString());
			Assert.Equal("2009-05-22T22:36:42", e.Who.When.ToIsoFormatDate());
			Assert.Equal("rebase finished: refs/heads/rr/renamebranch5 onto c6e3b9fe2da0293f11eae202ec35fb343191a82d",
				e.Comment);

			// while similar to testReadTwoLine, we can assume that if we get the last entry
			// right, everything else is too
		}

		[Fact]
		public void testReadRightLog()
		{
			SetupReflog("logs/refs/heads/a", ALine);
			SetupReflog("logs/refs/heads/master", MasterLine);
			SetupReflog("logs/HEAD", HeadLine);
			Assert.Equal("branch: change to master", db.ReflogReader("master").getLastEntry().Comment);
			Assert.Equal("branch: change to a", db.ReflogReader("a").getLastEntry().Comment);
			Assert.Equal("branch: change to HEAD", db.ReflogReader("HEAD").getLastEntry().Comment);
		}

		[Fact]
		public void testNoLog()
		{
			Assert.Equal(0, db.ReflogReader("master").getReverseEntries().Count);
			Assert.Null(db.ReflogReader("master").getLastEntry());
		}
	}
}
