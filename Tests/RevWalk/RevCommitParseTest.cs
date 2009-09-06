/*
 * Copyright (C) 2008, Google Inc.
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
using System.Text;
using GitSharp.RevWalk;
using NUnit.Framework;

namespace GitSharp.Tests.RevWalk
{
    [TestFixture]
    public class RevCommitParseTest : RepositoryTestCase
    {
        private readonly Encoding utf8Enc = Encoding.GetEncoding("UTF-8");
        private readonly Encoding isoEnc = Encoding.GetEncoding("ISO-8859-1");
        private readonly Encoding eucJpEnc = Encoding.GetEncoding("EUC-JP");

        [Test]
        public void testParse_NoParents()
        {
            ObjectId treeId = id("9788669ad918b6fcce64af8882fc9a81cb6aba67");
            string authorName = "A U. Thor";
            string authorEmail = "a_u_thor@example.com";
            int authorTime = 1218123387;

            string committerName = "C O. Miter";
            string committerEmail = "comiter@example.com";
            int committerTime = 1218123390;
            var body = new StringBuilder();

            body.Append("tree ");
            body.Append(treeId.Name);
            body.Append("\n");

            body.Append("author ");
            body.Append(authorName);
            body.Append(" <");
            body.Append(authorEmail);
            body.Append("> ");
            body.Append(authorTime);
            body.Append(" +0700\n");

            body.Append("committer ");
            body.Append(committerName);
            body.Append(" <");
            body.Append(committerEmail);
            body.Append("> ");
            body.Append(committerTime);
            body.Append(" -0500\n");

            body.Append("\n");

            var rw = new GitSharp.RevWalk.RevWalk(db);
            RevCommit c;

            c = new RevCommit(id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));
            Assert.IsNull(c.getTree());
            Assert.IsNull(c.parents);

            c.parseCanonical(rw, utf8Enc.GetBytes(body.ToString()));
            Assert.IsNotNull(c.getTree());
            Assert.AreEqual(treeId, c.getTree().getId());
            Assert.AreSame(rw.lookupTree(treeId), c.getTree());

            Assert.IsNotNull(c.parents);
            Assert.AreEqual(0, c.parents.Length);
            Assert.AreEqual("", c.getFullMessage());

            PersonIdent cAuthor = c.getAuthorIdent();
            Assert.IsNotNull(cAuthor);
            Assert.AreEqual(authorName, cAuthor.Name);
            Assert.AreEqual(authorEmail, cAuthor.EmailAddress);

            PersonIdent cCommitter = c.getCommitterIdent();
            Assert.IsNotNull(cCommitter);
            Assert.AreEqual(committerName, cCommitter.Name);
            Assert.AreEqual(committerEmail, cCommitter.EmailAddress);
        }

        private RevCommit create(string msg)
        {
            var b = new StringBuilder();
            b.Append("tree 9788669ad918b6fcce64af8882fc9a81cb6aba67\n");
            b.Append("author A U. Thor <a_u_thor@example.com> 1218123387 +0700\n");
            b.Append("committer C O. Miter <c@example.com> 1218123390 -0500\n");
            b.Append("\n");
            b.Append(msg);

            RevCommit c;
            c = new RevCommit(id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));

            c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), utf8Enc.GetBytes(b.ToString()));
            return c;
        }

        [Test]
        public void testParse_WeirdHeaderOnlyCommit()
        {
            var b = new StringBuilder();
            b.Append("tree 9788669ad918b6fcce64af8882fc9a81cb6aba67\n");
            b.Append("author A U. Thor <a_u_thor@example.com> 1218123387 +0700\n");
            b.Append("committer C O. Miter <c@example.com> 1218123390 -0500\n");

            RevCommit c;
            c = new RevCommit(id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));

            c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), utf8Enc.GetBytes(b.ToString()));

            Assert.AreEqual("", c.getFullMessage());
            Assert.AreEqual("", c.getShortMessage());
        }

        [Test]
        public void testParse_implicit_UTF8_encoded()
        {
            RevCommit c;
            using (var b = new BinaryWriter(new MemoryStream()))
            {
                b.Write(utf8Enc.GetBytes("tree 9788669ad918b6fcce64af8882fc9a81cb6aba67\n"));
                b.Write(utf8Enc.GetBytes("author F\u00f6r fattare <a_u_thor@example.com> 1218123387 +0700\n"));
                b.Write(utf8Enc.GetBytes("committer C O. Miter <c@example.com> 1218123390 -0500\n"));
                b.Write(utf8Enc.GetBytes("\n"));
                b.Write(utf8Enc.GetBytes("Sm\u00f6rg\u00e5sbord\n"));
                b.Write(utf8Enc.GetBytes("\n"));
                b.Write(utf8Enc.GetBytes("\u304d\u308c\u3044\n"));
                c = new RevCommit(id("9473095c4cb2f12aefe1db8a355fe3fafba42f67")); // bogus id
                c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), ((MemoryStream) b.BaseStream).ToArray());
            }

            Assert.AreEqual("F\u00f6r fattare", c.getAuthorIdent().Name);
            Assert.AreEqual("Sm\u00f6rg\u00e5sbord", c.getShortMessage());
            Assert.AreEqual("Sm\u00f6rg\u00e5sbord\n\n\u304d\u308c\u3044\n", c.getFullMessage());
        }

        [Test]
        public void testParse_implicit_mixed_encoded()
        {
            RevCommit c;
            using (var b = new BinaryWriter(new MemoryStream()))
            {
                b.Write(utf8Enc.GetBytes("tree 9788669ad918b6fcce64af8882fc9a81cb6aba67\n"));
                b.Write(isoEnc.GetBytes("author F\u00f6r fattare <a_u_thor@example.com> 1218123387 +0700\n"));
                b.Write(utf8Enc.GetBytes("committer C O. Miter <c@example.com> 1218123390 -0500\n"));
                b.Write(utf8Enc.GetBytes("\n"));
                b.Write(utf8Enc.GetBytes("Sm\u00f6rg\u00e5sbord\n"));
                b.Write(utf8Enc.GetBytes("\n"));
                b.Write(utf8Enc.GetBytes("\u304d\u308c\u3044\n"));

                c = new RevCommit(id("9473095c4cb2f12aefe1db8a355fe3fafba42f67")); // bogus id
                c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), ((MemoryStream) b.BaseStream).ToArray());
            }

            Assert.AreEqual("F\u00f6r fattare", c.getAuthorIdent().Name);
            Assert.AreEqual("Sm\u00f6rg\u00e5sbord", c.getShortMessage());
            Assert.AreEqual("Sm\u00f6rg\u00e5sbord\n\n\u304d\u308c\u3044\n", c.getFullMessage());
        }

        /**
         * Test parsing of a commit whose encoding is given and works.
         *
         * @throws Exception
         */

        [Test]
        public void testParse_explicit_encoded()
        {
            RevCommit c;
            using (var b = new BinaryWriter(new MemoryStream()))
            {
                b.Write(eucJpEnc.GetBytes("tree 9788669ad918b6fcce64af8882fc9a81cb6aba67\n"));
                b.Write(eucJpEnc.GetBytes("author F\u00f6r fattare <a_u_thor@example.com> 1218123387 +0700\n"));
                b.Write(eucJpEnc.GetBytes("committer C O. Miter <c@example.com> 1218123390 -0500\n"));
                b.Write(eucJpEnc.GetBytes("encoding euc_JP\n"));
                b.Write(eucJpEnc.GetBytes("\n"));
                b.Write(eucJpEnc.GetBytes("\u304d\u308c\u3044\n"));
                b.Write(eucJpEnc.GetBytes("\n"));
                b.Write(eucJpEnc.GetBytes("Hi\n"));

                c = new RevCommit(id("9473095c4cb2f12aefe1db8a355fe3fafba42f67")); // bogus id
                c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), ((MemoryStream) b.BaseStream).ToArray());
            }
            Assert.AreEqual("F\u00f6r fattare", c.getAuthorIdent().Name);
            Assert.AreEqual("\u304d\u308c\u3044", c.getShortMessage());
            Assert.AreEqual("\u304d\u308c\u3044\n\nHi\n", c.getFullMessage());
        }

        /**
         * This is a twisted case, but show what we expect here. We can revise the
         * expectations provided this case is updated.
         *
         * What happens here is that an encoding us given, but data is not encoded
         * that way (and we can detect it), so we try other encodings.
         *
         * @throws Exception
         */

        [Test]
        public void testParse_explicit_bad_encoded()
        {
            RevCommit c;
            using (var b = new BinaryWriter(new MemoryStream()))
            {
                b.Write(utf8Enc.GetBytes("tree 9788669ad918b6fcce64af8882fc9a81cb6aba67\n"));
                b.Write(isoEnc.GetBytes("author F\u00f6r fattare <a_u_thor@example.com> 1218123387 +0700\n"));
                b.Write(utf8Enc.GetBytes("committer C O. Miter <c@example.com> 1218123390 -0500\n"));
                b.Write(utf8Enc.GetBytes("encoding EUC-JP\n"));
                b.Write(utf8Enc.GetBytes("\n"));
                b.Write(utf8Enc.GetBytes("\u304d\u308c\u3044\n"));
                b.Write(utf8Enc.GetBytes("\n"));
                b.Write(utf8Enc.GetBytes("Hi\n"));

                c = new RevCommit(id("9473095c4cb2f12aefe1db8a355fe3fafba42f67")); // bogus id
                c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), ((MemoryStream) b.BaseStream).ToArray());
            }
            Assert.AreEqual("F\u00f6r fattare", c.getAuthorIdent().Name);
            Assert.AreEqual("\u304d\u308c\u3044", c.getShortMessage());
            Assert.AreEqual("\u304d\u308c\u3044\n\nHi\n", c.getFullMessage());
        }

        /**
         * This is a twisted case too, but show what we expect here. We can revise the
         * expectations provided this case is updated.
         *
         * What happens here is that an encoding us given, but data is not encoded
         * that way (and we can detect it), so we try other encodings. Here data could
         * actually be decoded in the stated encoding, but we override using UTF-8.
         *
         * @throws Exception
         */

        [Test]
        public void testParse_explicit_bad_encoded2()
        {
            RevCommit c;
            using (var b = new BinaryWriter(new MemoryStream()))
            {
                b.Write(utf8Enc.GetBytes("tree 9788669ad918b6fcce64af8882fc9a81cb6aba67\n"));
                b.Write(utf8Enc.GetBytes("author F\u00f6r fattare <a_u_thor@example.com> 1218123387 +0700\n"));
                b.Write(utf8Enc.GetBytes("committer C O. Miter <c@example.com> 1218123390 -0500\n"));
                b.Write(utf8Enc.GetBytes("encoding ISO-8859-1\n"));
                b.Write(utf8Enc.GetBytes("\n"));
                b.Write(utf8Enc.GetBytes("\u304d\u308c\u3044\n"));
                b.Write(utf8Enc.GetBytes("\n"));
                b.Write(utf8Enc.GetBytes("Hi\n"));

                c = new RevCommit(id("9473095c4cb2f12aefe1db8a355fe3fafba42f67")); // bogus id
                c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), ((MemoryStream) b.BaseStream).ToArray());
            }
            Assert.AreEqual("F\u00f6r fattare", c.getAuthorIdent().Name);
            Assert.AreEqual("\u304d\u308c\u3044", c.getShortMessage());
            Assert.AreEqual("\u304d\u308c\u3044\n\nHi\n", c.getFullMessage());
        }

        [Test]
        public void testParse_NoMessage()
        {
            string msg = "";
            RevCommit c = create(msg);
            Assert.AreEqual(msg, c.getFullMessage());
            Assert.AreEqual(msg, c.getShortMessage());
        }

        [Test]
        public void testParse_OnlyLFMessage()
        {
            RevCommit c = create("\n");
            Assert.AreEqual("\n", c.getFullMessage());
            Assert.AreEqual("", c.getShortMessage());
        }

        [Test]
        public void testParse_ShortLineOnlyNoLF()
        {
            string shortMsg = "This is a short message.";
            RevCommit c = create(shortMsg);
            Assert.AreEqual(shortMsg, c.getFullMessage());
            Assert.AreEqual(shortMsg, c.getShortMessage());
        }

        [Test]
        public void testParse_ShortLineOnlyEndLF()
        {
            string shortMsg = "This is a short message.";
            string fullMsg = shortMsg + "\n";
            RevCommit c = create(fullMsg);
            Assert.AreEqual(fullMsg, c.getFullMessage());
            Assert.AreEqual(shortMsg, c.getShortMessage());
        }

        [Test]
        public void testParse_ShortLineOnlyEmbeddedLF()
        {
            string fullMsg = "This is a\nshort message.";
            string shortMsg = fullMsg.Replace('\n', ' ');
            RevCommit c = create(fullMsg);
            Assert.AreEqual(fullMsg, c.getFullMessage());
            Assert.AreEqual(shortMsg, c.getShortMessage());
        }

        [Test]
        public void testParse_ShortLineOnlyEmbeddedAndEndingLF()
        {
            string fullMsg = "This is a\nshort message.\n";
            string shortMsg = "This is a short message.";
            RevCommit c = create(fullMsg);
            Assert.AreEqual(fullMsg, c.getFullMessage());
            Assert.AreEqual(shortMsg, c.getShortMessage());
        }

        [Test]
        public void testParse_GitStyleMessage()
        {
            string shortMsg = "This fixes a bug.";
            string body = "We do it with magic and pixie dust and stuff.\n"
                          + "\n" + "Signed-off-by: A U. Thor <author@example.com>\n";
            string fullMsg = shortMsg + "\n" + "\n" + body;
            RevCommit c = create(fullMsg);
            Assert.AreEqual(fullMsg, c.getFullMessage());
            Assert.AreEqual(shortMsg, c.getShortMessage());
        }

        private static ObjectId id(string str)
        {
            return ObjectId.FromString(str);
        }
    }
}