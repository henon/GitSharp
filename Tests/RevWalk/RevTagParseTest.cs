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
    public class RevTagParseTest : RepositoryTestCase
    {
        private readonly Encoding utf8Enc = Constants.CHARSET;
        private readonly Encoding isoEnc = Encoding.GetEncoding("ISO-8859-1");
        private readonly Encoding eucJpEnc = Encoding.GetEncoding("EUC-JP");

        [Test]
        public void testTagBlob()
        {
            testOneType(Constants.OBJ_BLOB);
        }

        [Test]
        public void testTagTree()
        {
            testOneType(Constants.OBJ_TREE);
        }

        [Test]
        public void testTagCommit()
        {
            testOneType(Constants.OBJ_COMMIT);
        }

        [Test]
        public void testTagTag()
        {
            testOneType(Constants.OBJ_TAG);
        }

        private void testOneType(int typeCode)
        {
            ObjectId locId = Id("9788669ad918b6fcce64af8882fc9a81cb6aba67");
            var b = new StringBuilder();
            b.Append("object " + locId.Name + "\n");
            b.Append("type " + Constants.typeString(typeCode) + "\n");
            b.Append("tag v1.2.3.4.5\n");
            b.Append("tagger A U. Thor <a_u_thor@example.com> 1218123387 +0700\n");
            b.Append("\n");

            var rw = new GitSharp.RevWalk.RevWalk(db);

        	var c = new RevTag(Id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));
            Assert.IsNull(c.getObject());
            Assert.IsNull(c.getName());

            c.parseCanonical(rw, utf8Enc.GetBytes(b.ToString()));
            Assert.IsNotNull(c.getObject());
            Assert.AreEqual(locId, c.getObject().getId());
            Assert.AreSame(rw.lookupAny(locId, typeCode), c.getObject());
        }

        [Test]
        public void testParseAllFields()
        {
            ObjectId treeId = Id("9788669ad918b6fcce64af8882fc9a81cb6aba67");
            const string name = "v1.2.3.4.5";
            const string taggerName = "A U. Thor";
            const string taggerEmail = "a_u_thor@example.com";
            const int taggerTime = 1218123387;

            var body = new StringBuilder();

            body.Append("object ");
            body.Append(treeId.Name);
            body.Append("\n");

            body.Append("type tree\n");

            body.Append("tag ");
            body.Append(name);
            body.Append("\n");

            body.Append("tagger ");
            body.Append(taggerName);
            body.Append(" <");
            body.Append(taggerEmail);
            body.Append("> ");
            body.Append(taggerTime);
            body.Append(" +0700\n");

            body.Append("\n");

            var rw = new GitSharp.RevWalk.RevWalk(db);

        	var c = new RevTag(Id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));
            Assert.IsNull(c.getObject());
            Assert.IsNull(c.getName());

            c.parseCanonical(rw, utf8Enc.GetBytes(body.ToString()));
            Assert.IsNotNull(c.getObject());
            Assert.AreEqual(treeId, c.getObject().getId());
            Assert.AreSame(rw.lookupTree(treeId), c.getObject());

            Assert.IsNotNull(c.getName());
            Assert.AreEqual(name, c.getName());
            Assert.AreEqual(string.Empty, c.getFullMessage());

            PersonIdent cTagger = c.getTaggerIdent();
            Assert.IsNotNull(cTagger);
            Assert.AreEqual(taggerName, cTagger.Name);
            Assert.AreEqual(taggerEmail, cTagger.EmailAddress);
        }

        private RevTag create(string msg)
        {
            var b = new StringBuilder();
            b.Append("object 9788669ad918b6fcce64af8882fc9a81cb6aba67\n");
            b.Append("type tree\n");
            b.Append("tag v1.2.3.4.5\n");
            b.Append("tagger A U. Thor <a_u_thor@example.com> 1218123387 +0700\n");
            b.Append("\n");
            b.Append(msg);

        	var c = new RevTag(Id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));

            c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), utf8Enc.GetBytes(b.ToString()));
            return c;
        }

        [Test]
        public void testParse_implicit_UTF8_encoded()
        {
            RevTag c;
            using (var b = new BinaryWriter(new MemoryStream()))
            {
                b.Write(utf8Enc.GetBytes("object 9788669ad918b6fcce64af8882fc9a81cb6aba67\n"));
                b.Write(utf8Enc.GetBytes("type tree\n"));
                b.Write(utf8Enc.GetBytes("tag v1.2.3.4.5\n"));
                b.Write(utf8Enc.GetBytes("tagger F\u00f6r fattare <a_u_thor@example.com> 1218123387 +0700\n"));
                b.Write(utf8Enc.GetBytes("\n"));
                b.Write(utf8Enc.GetBytes("Sm\u00f6rg\u00e5sbord\n"));
                b.Write(utf8Enc.GetBytes("\n"));
                b.Write(utf8Enc.GetBytes("\u304d\u308c\u3044\n"));

                c = new RevTag(Id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));
                c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), ((MemoryStream) b.BaseStream).ToArray());
            }
            Assert.AreEqual("F\u00f6r fattare", c.getTaggerIdent().Name);
            Assert.AreEqual("Sm\u00f6rg\u00e5sbord", c.getShortMessage());
            Assert.AreEqual("Sm\u00f6rg\u00e5sbord\n\n\u304d\u308c\u3044\n", c.getFullMessage());
        }

        [Test]
        public void testParse_implicit_mixed_encoded()
        {
            RevTag c;
            using (var b = new BinaryWriter(new MemoryStream()))
            {
                b.Write(utf8Enc.GetBytes("object 9788669ad918b6fcce64af8882fc9a81cb6aba67\n"));
                b.Write(utf8Enc.GetBytes("type tree\n"));
                b.Write(utf8Enc.GetBytes("tag v1.2.3.4.5\n"));
                b.Write(isoEnc.GetBytes("tagger F\u00f6r fattare <a_u_thor@example.com> 1218123387 +0700\n"));
                b.Write(utf8Enc.GetBytes("\n"));
                b.Write(utf8Enc.GetBytes("Sm\u00f6rg\u00e5sbord\n"));
                b.Write(utf8Enc.GetBytes("\n"));
                b.Write(utf8Enc.GetBytes("\u304d\u308c\u3044\n"));

                c = new RevTag(Id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));
                c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), ((MemoryStream) b.BaseStream).ToArray());
            }
            Assert.AreEqual("F\u00f6r fattare", c.getTaggerIdent().Name);
            Assert.AreEqual("Sm\u00f6rg\u00e5sbord", c.getShortMessage());
            Assert.AreEqual("Sm\u00f6rg\u00e5sbord\n\n\u304d\u308c\u3044\n", c
                                                                                 .getFullMessage());
        }

        /**
         * Test parsing of a commit whose encoding is given and works.
         *
         * @throws Exception
         */

        [Test]
        public void testParse_explicit_encoded()
        {
            RevTag c;
            using (var b = new BinaryWriter(new MemoryStream()))
            {
                b.Write(eucJpEnc.GetBytes("object 9788669ad918b6fcce64af8882fc9a81cb6aba67\n"));
                b.Write(eucJpEnc.GetBytes("type tree\n"));
                b.Write(eucJpEnc.GetBytes("tag v1.2.3.4.5\n"));
                b.Write(eucJpEnc.GetBytes("tagger F\u00f6r fattare <a_u_thor@example.com> 1218123387 +0700\n"));
                b.Write(eucJpEnc.GetBytes("encoding euc_JP\n"));
                b.Write(eucJpEnc.GetBytes("\n"));
                b.Write(eucJpEnc.GetBytes("\u304d\u308c\u3044\n"));
                b.Write(eucJpEnc.GetBytes("\n"));
                b.Write(eucJpEnc.GetBytes("Hi\n"));

                c = new RevTag(Id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));
                c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), ((MemoryStream) b.BaseStream).ToArray());
            }
            Assert.AreEqual("F\u00f6r fattare", c.getTaggerIdent().Name);
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
            RevTag c;
            using (var b = new BinaryWriter(new MemoryStream()))
            {
                b.Write(utf8Enc.GetBytes("object 9788669ad918b6fcce64af8882fc9a81cb6aba67\n"));
                b.Write(utf8Enc.GetBytes("type tree\n"));
                b.Write(utf8Enc.GetBytes("tag v1.2.3.4.5\n"));
                b.Write(isoEnc.GetBytes("tagger F\u00f6r fattare <a_u_thor@example.com> 1218123387 +0700\n"));
                b.Write(utf8Enc.GetBytes("encoding EUC-JP\n"));
                b.Write(utf8Enc.GetBytes("\n"));
                b.Write(utf8Enc.GetBytes("\u304d\u308c\u3044\n"));
                b.Write(utf8Enc.GetBytes("\n"));
                b.Write(utf8Enc.GetBytes("Hi\n"));

                c = new RevTag(Id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));
                c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), ((MemoryStream) b.BaseStream).ToArray());
            }

            Assert.AreEqual("F\u00f6r fattare", c.getTaggerIdent().Name);
            Assert.AreEqual("\u304d\u308c\u3044", c.getShortMessage());
            Assert.AreEqual("\u304d\u308c\u3044\n\nHi\n", c.getFullMessage());
        }

	    /**
	     * This is a twisted case too, but show what we expect here. We can revise
	     * the expectations provided this case is updated.
	     *
	     * What happens here is that an encoding us given, but data is not encoded
	     * that way (and we can detect it), so we try other encodings. Here data
	     * could actually be decoded in the stated encoding, but we override using
	     * UTF-8.
	     *
	     * @throws Exception
	     */

        [Test]
        public void testParse_explicit_bad_encoded2()
        {
            RevTag c;
            using (var b = new BinaryWriter(new MemoryStream()))
            {
                b.Write(utf8Enc.GetBytes("object 9788669ad918b6fcce64af8882fc9a81cb6aba67\n"));
                b.Write(utf8Enc.GetBytes("type tree\n"));
                b.Write(utf8Enc.GetBytes("tag v1.2.3.4.5\n"));
                b.Write(utf8Enc.GetBytes("tagger F\u00f6r fattare <a_u_thor@example.com> 1218123387 +0700\n"));
                b.Write(utf8Enc.GetBytes("encoding ISO-8859-1\n"));
                b.Write(utf8Enc.GetBytes("\n"));
                b.Write(utf8Enc.GetBytes("\u304d\u308c\u3044\n"));
                b.Write(utf8Enc.GetBytes("\n"));
                b.Write(utf8Enc.GetBytes("Hi\n"));

                c = new RevTag(Id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));
                c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), ((MemoryStream) b.BaseStream).ToArray());
            }
            Assert.AreEqual("F\u00f6r fattare", c.getTaggerIdent().Name);
            Assert.AreEqual("\u304d\u308c\u3044", c.getShortMessage());
            Assert.AreEqual("\u304d\u308c\u3044\n\nHi\n", c.getFullMessage());
        }

        [Test]
        public void testParse_NoMessage()
        {
            const string msg = "";
            RevTag c = create(msg);
            Assert.AreEqual(msg, c.getFullMessage());
            Assert.AreEqual(msg, c.getShortMessage());
        }

        [Test]
        public void testParse_OnlyLFMessage()
        {
            RevTag c = create("\n");
            Assert.AreEqual("\n", c.getFullMessage());
            Assert.AreEqual(string.Empty, c.getShortMessage());
        }

        [Test]
        public void testParse_ShortLineOnlyNoLF()
        {
            const string shortMsg = "This is a short message.";
            RevTag c = create(shortMsg);
            Assert.AreEqual(shortMsg, c.getFullMessage());
            Assert.AreEqual(shortMsg, c.getShortMessage());
        }

        [Test]
        public void testParse_ShortLineOnlyEndLF()
        {
            const string shortMsg = "This is a short message.";
            const string fullMsg = shortMsg + "\n";
            RevTag c = create(fullMsg);
            Assert.AreEqual(fullMsg, c.getFullMessage());
            Assert.AreEqual(shortMsg, c.getShortMessage());
        }

        [Test]
        public void testParse_ShortLineOnlyEmbeddedLF()
        {
            const string fullMsg = "This is a\nshort message.";
            string shortMsg = fullMsg.Replace('\n', ' ');
            RevTag c = create(fullMsg);
            Assert.AreEqual(fullMsg, c.getFullMessage());
            Assert.AreEqual(shortMsg, c.getShortMessage());
        }

        [Test]
        public void testParse_ShortLineOnlyEmbeddedAndEndingLF()
        {
            const string fullMsg = "This is a\nshort message.\n";
            const string shortMsg = "This is a short message.";
            RevTag c = create(fullMsg);
            Assert.AreEqual(fullMsg, c.getFullMessage());
            Assert.AreEqual(shortMsg, c.getShortMessage());
        }

        [Test]
        public void testParse_GitStyleMessage()
        {
            const string shortMsg = "This fixes a bug.";
            const string body = "We do it with magic and pixie dust and stuff.\n"
                                + "\n" + "Signed-off-by: A U. Thor <author@example.com>\n";
            const string fullMsg = shortMsg + "\n" + "\n" + body;
            RevTag c = create(fullMsg);
            Assert.AreEqual(fullMsg, c.getFullMessage());
            Assert.AreEqual(shortMsg, c.getShortMessage());
        }

        private static ObjectId Id(string str)
        {
            return ObjectId.FromString(str);
        }
    }
}
