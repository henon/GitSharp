/*
 * Copyright (C) 2009, Google Inc.
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

using System.Collections.Generic;
using System.Text;
using GitSharp.RevWalk;
using NUnit.Framework;

namespace GitSharp.Tests.RevWalk
{
	[TestFixture]
	public class FooterLineTest : RepositoryTestCase
	{
		[Test]
		public void testNoFooters_EmptyBody()
		{
			RevCommit commit = Parse(string.Empty);
			IList<FooterLine> footers = commit.GetFooterLines();
			Assert.IsNotNull(footers);
			Assert.AreEqual(0, footers.Count);
		}

		[Test]
		public void testNoFooters_NewlineOnlyBody1()
		{
			RevCommit commit = Parse("\n");
			IList<FooterLine> footers = commit.GetFooterLines();
			Assert.IsNotNull(footers);
			Assert.AreEqual(0, footers.Count);
		}

		[Test]
		public void testNoFooters_NewlineOnlyBody5()
		{
			RevCommit commit = Parse("\n\n\n\n\n");
			IList<FooterLine> footers = commit.GetFooterLines();
			Assert.IsNotNull(footers);
			Assert.AreEqual(0, footers.Count);
		}

		[Test]
		public void testNoFooters_OneLineBodyNoLF()
		{
			RevCommit commit = Parse("this is a commit");
			IList<FooterLine> footers = commit.GetFooterLines();
			Assert.IsNotNull(footers);
			Assert.AreEqual(0, footers.Count);
		}

		[Test]
		public void testNoFooters_OneLineBodyWithLF()
		{
			RevCommit commit = Parse("this is a commit\n");
			IList<FooterLine> footers = commit.GetFooterLines();
			Assert.IsNotNull(footers);
			Assert.AreEqual(0, footers.Count);
		}

		[Test]
		public void testNoFooters_ShortBodyNoLF()
		{
			RevCommit commit = Parse("subject\n\nbody of commit");
			IList<FooterLine> footers = commit.GetFooterLines();
			Assert.IsNotNull(footers);
			Assert.AreEqual(0, footers.Count);
		}

		[Test]
		public virtual void testNoFooters_ShortBodyWithLF()
		{
			RevCommit commit = Parse("subject\n\nbody of commit\n");
			IList<FooterLine> footers = commit.GetFooterLines();
			Assert.IsNotNull(footers);
			Assert.AreEqual(0, footers.Count);
		}

		[Test]
		public void testSignedOffBy_OneUserNoLF()
		{
			RevCommit commit = Parse("subject\n\nbody of commit\n" + "\n" + "Signed-off-by: A. U. Thor <a@example.com>");
			IList<FooterLine> footers = commit.GetFooterLines();

			Assert.IsNotNull(footers);
			Assert.AreEqual(1, footers.Count);

			FooterLine f = footers[0];
			Assert.AreEqual("Signed-off-by", f.Key);
			Assert.AreEqual("A. U. Thor <a@example.com>", f.Value);
			Assert.AreEqual("a@example.com", f.getEmailAddress());
		}

		[Test]
		public void testSignedOffBy_OneUserWithLF()
		{
			RevCommit commit = Parse("subject\n\nbody of commit\n" + "\n" + "Signed-off-by: A. U. Thor <a@example.com>\n");
			IList<FooterLine> footers = commit.GetFooterLines();

			Assert.IsNotNull(footers);
			Assert.AreEqual(1, footers.Count);

			FooterLine f = footers[0];
			Assert.AreEqual("Signed-off-by", f.Key);
			Assert.AreEqual("A. U. Thor <a@example.com>", f.Value);
			Assert.AreEqual("a@example.com", f.getEmailAddress());
		}

		[Test]
		public void testSignedOffBy_IgnoreWhitespace()
		{
			// We only ignore leading whitespace on the value, trailing
			// is assumed part of the value.
			//
			RevCommit commit = Parse("subject\n\nbody of commit\n" + "\n" + "Signed-off-by:   A. U. Thor <a@example.com>  \n");
			IList<FooterLine> footers = commit.GetFooterLines();

			Assert.IsNotNull(footers);
			Assert.AreEqual(1, footers.Count);

			FooterLine f = footers[0];
			Assert.AreEqual("Signed-off-by", f.Key);
			Assert.AreEqual("A. U. Thor <a@example.com>  ", f.Value);
			Assert.AreEqual("a@example.com", f.getEmailAddress());
		}

		[Test]
		public void testEmptyValueNoLF()
		{
			RevCommit commit = Parse("subject\n\nbody of commit\n" + "\n" + "Signed-off-by:");
			IList<FooterLine> footers = commit.GetFooterLines();

			Assert.IsNotNull(footers);
			Assert.AreEqual(1, footers.Count);

			FooterLine f = footers[0];
			Assert.AreEqual("Signed-off-by", f.Key);
			Assert.AreEqual("", f.Value);
			Assert.IsNull(f.getEmailAddress());
		}

		[Test]
		public void testEmptyValueWithLF()
		{
			RevCommit commit = Parse("subject\n\nbody of commit\n" + "\n" + "Signed-off-by:\n");
			IList<FooterLine> footers = commit.GetFooterLines();

			Assert.IsNotNull(footers);
			Assert.AreEqual(1, footers.Count);

			FooterLine f = footers[0];
			Assert.AreEqual("Signed-off-by", f.Key);
			Assert.AreEqual("", f.Value);
			Assert.IsNull(f.getEmailAddress());
		}

		[Test]
		public void testShortKey()
		{
			RevCommit commit = Parse("subject\n\nbody of commit\n" + "\n" + "K:V\n");
			IList<FooterLine> footers = commit.GetFooterLines();

			Assert.IsNotNull(footers);
			Assert.AreEqual(1, footers.Count);

			FooterLine f = footers[0];
			Assert.AreEqual("K", f.Key);
			Assert.AreEqual("V", f.Value);
			Assert.IsNull(f.getEmailAddress());
		}

		[Test]
		public void testNonDelimtedEmail()
		{
			RevCommit commit = Parse("subject\n\nbody of commit\n" + "\n" + "Acked-by: re@example.com\n");
			IList<FooterLine> footers = commit.GetFooterLines();

			Assert.IsNotNull(footers);
			Assert.AreEqual(1, footers.Count);

			FooterLine f = footers[0];
			Assert.AreEqual("Acked-by", f.Key);
			Assert.AreEqual("re@example.com", f.Value);
			Assert.AreEqual("re@example.com", f.getEmailAddress());
		}

		[Test]
		public void testNotEmail()
		{
			RevCommit commit = Parse("subject\n\nbody of commit\n" + "\n" + "Acked-by: Main Tain Er\n");
			IList<FooterLine> footers = commit.GetFooterLines();

			Assert.IsNotNull(footers);
			Assert.AreEqual(1, footers.Count);

			FooterLine f = footers[0];
			Assert.AreEqual("Acked-by", f.Key);
			Assert.AreEqual("Main Tain Er", f.Value);
			Assert.IsNull(f.getEmailAddress());
		}
		
		[Test]
		public void testSignedOffBy_ManyUsers()
		{
			RevCommit commit = Parse("subject\n\nbody of commit\n" + "Not-A-Footer-Line: this line must not be read as a footer\n" + "\n" + "Signed-off-by: A. U. Thor <a@example.com>\n" + "CC:            <some.mailing.list@example.com>\n" + "Acked-by: Some Reviewer <sr@example.com>\n" + "Signed-off-by: Main Tain Er <mte@example.com>\n");
			IList<FooterLine> footers = commit.GetFooterLines();

			Assert.IsNotNull(footers);
			Assert.AreEqual(4, footers.Count);

			FooterLine f = footers[0];
			Assert.AreEqual("Signed-off-by", f.Key);
			Assert.AreEqual("A. U. Thor <a@example.com>", f.Value);
			Assert.AreEqual("a@example.com", f.getEmailAddress());

			f = footers[1];
			Assert.AreEqual("CC", f.Key);
			Assert.AreEqual("<some.mailing.list@example.com>", f.Value);
			Assert.AreEqual("some.mailing.list@example.com", f.getEmailAddress());

			f = footers[2];
			Assert.AreEqual("Acked-by", f.Key);
			Assert.AreEqual("Some Reviewer <sr@example.com>", f.Value);
			Assert.AreEqual("sr@example.com", f.getEmailAddress());

			f = footers[3];
			Assert.AreEqual("Signed-off-by", f.Key);
			Assert.AreEqual("Main Tain Er <mte@example.com>", f.Value);
			Assert.AreEqual("mte@example.com", f.getEmailAddress());
		}

		[Test]
		public void testSignedOffBy_SkipNonFooter()
		{
			RevCommit commit = Parse("subject\n\nbody of commit\n" + "Not-A-Footer-Line: this line must not be read as a footer\n" + "\n" + "Signed-off-by: A. U. Thor <a@example.com>\n" + "CC:            <some.mailing.list@example.com>\n" + "not really a footer line but we'll skip it anyway\n" + "Acked-by: Some Reviewer <sr@example.com>\n" + "Signed-off-by: Main Tain Er <mte@example.com>\n");
			IList<FooterLine> footers = commit.GetFooterLines();

			Assert.IsNotNull(footers);
			Assert.AreEqual(4, footers.Count);

			FooterLine f = footers[0];
			Assert.AreEqual("Signed-off-by", f.Key);
			Assert.AreEqual("A. U. Thor <a@example.com>", f.Value);

			f = footers[1];
			Assert.AreEqual("CC", f.Key);
			Assert.AreEqual("<some.mailing.list@example.com>", f.Value);

			f = footers[2];
			Assert.AreEqual("Acked-by", f.Key);
			Assert.AreEqual("Some Reviewer <sr@example.com>", f.Value);

			f = footers[3];
			Assert.AreEqual("Signed-off-by", f.Key);
			Assert.AreEqual("Main Tain Er <mte@example.com>", f.Value);
		}

		[Test]
		public void testFilterFootersIgnoreCase()
		{
			RevCommit commit = Parse("subject\n\nbody of commit\n" + "Not-A-Footer-Line: this line must not be read as a footer\n" + "\n" + "Signed-Off-By: A. U. Thor <a@example.com>\n" + "CC:            <some.mailing.list@example.com>\n" + "Acked-by: Some Reviewer <sr@example.com>\n" + "signed-off-by: Main Tain Er <mte@example.com>\n");
			IList<string> footers = commit.GetFooterLines("signed-off-by");

			Assert.IsNotNull(footers);
			Assert.AreEqual(2, footers.Count);

			Assert.AreEqual("A. U. Thor <a@example.com>", footers[0]);
			Assert.AreEqual("Main Tain Er <mte@example.com>", footers[1]);
		}

		private RevCommit Parse(string msg)
		{
			var buf = new StringBuilder();
			buf.Append("tree " + ObjectId.ZeroId.Name + "\n");
			buf.Append("author A. U. Thor <a@example.com> 1 +0000\n");
			buf.Append("committer A. U. Thor <a@example.com> 1 +0000\n");
			buf.Append("\n");
			buf.Append(msg);

			var walk = new GitSharp.RevWalk.RevWalk(db);
			walk.setRetainBody(true);
			var c = new RevCommit(ObjectId.ZeroId);
			c.parseCanonical(walk, Constants.encode(buf.ToString()));
			return c;
		}
	}
}