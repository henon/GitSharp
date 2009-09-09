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
using Xunit;

namespace GitSharp.Tests.RevWalk
{
    public class RevTagParseTest : RepositoryTestCase
    {
        private readonly Encoding utf8Enc = Encoding.GetEncoding("UTF-8");
        private readonly Encoding isoEnc = Encoding.GetEncoding("ISO-8859-1");
        private readonly Encoding eucJpEnc = Encoding.GetEncoding("EUC-JP");

        [Fact]
        public void testTagBlob()
        {
            testOneType(Constants.OBJ_BLOB);
        }

        [Fact]
        public void testTagTree()
        {
            testOneType(Constants.OBJ_TREE);
        }

        [Fact]
        public void testTagCommit()
        {
            testOneType(Constants.OBJ_COMMIT);
        }

        [Fact]
        public void testTagTag()
        {
            testOneType(Constants.OBJ_TAG);
        }

        private void testOneType(int typeCode)
        {
            ObjectId locId = id("9788669ad918b6fcce64af8882fc9a81cb6aba67");
            var b = new StringBuilder();
            b.Append("object " + locId.Name + "\n");
            b.Append("type " + Constants.typeString(typeCode) + "\n");
            b.Append("tag v1.2.3.4.5\n");
            b.Append("tagger A U. Thor <a_u_thor@example.com> 1218123387 +0700\n");
            b.Append("\n");

            var rw = new GitSharp.RevWalk.RevWalk(db);
            RevTag c;

            c = new RevTag(id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));
            Assert.Null(c.getObject());
            Assert.Null(c.getName());

            c.parseCanonical(rw, utf8Enc.GetBytes(b.ToString()));
            Assert.NotNull(c.getObject());
            Assert.Equal(locId, c.getObject().getId());
            Assert.Same(rw.lookupAny(locId, typeCode), c.getObject());
        }

        [Fact]
        public void testParseAllFields()
        {
            ObjectId treeId = id("9788669ad918b6fcce64af8882fc9a81cb6aba67");
            string name = "v1.2.3.4.5";
            string taggerName = "A U. Thor";
            string taggerEmail = "a_u_thor@example.com";
            int taggerTime = 1218123387;

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
            RevTag c;

            c = new RevTag(id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));
            Assert.Null(c.getObject());
            Assert.Null(c.getName());

            Encoding utf8Enc = Encoding.GetEncoding("UTF-8");
            c.parseCanonical(rw, utf8Enc.GetBytes(body.ToString()));
            Assert.NotNull(c.getObject());
            Assert.Equal(treeId, c.getObject().getId());
            Assert.Same(rw.lookupTree(treeId), c.getObject());

            Assert.NotNull(c.getName());
            Assert.Equal(name, c.getName());
            Assert.Equal("", c.getFullMessage());

            PersonIdent cTagger = c.getTaggerIdent();
            Assert.NotNull(cTagger);
            Assert.Equal(taggerName, cTagger.Name);
            Assert.Equal(taggerEmail, cTagger.EmailAddress);
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

            RevTag c;
            c = new RevTag(id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));

            Encoding utf8Enc = Encoding.GetEncoding("UTF-8");
            c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), utf8Enc.GetBytes(b.ToString()));
            return c;
        }

        [Fact]
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

                c = new RevTag(id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));
                c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), ((MemoryStream) b.BaseStream).ToArray());
            }
            Assert.Equal("F\u00f6r fattare", c.getTaggerIdent().Name);
            Assert.Equal("Sm\u00f6rg\u00e5sbord", c.getShortMessage());
            Assert.Equal("Sm\u00f6rg\u00e5sbord\n\n\u304d\u308c\u3044\n", c.getFullMessage());
        }

        [Fact]
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

                c = new RevTag(id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));
                c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), ((MemoryStream) b.BaseStream).ToArray());
            }
            Assert.Equal("F\u00f6r fattare", c.getTaggerIdent().Name);
            Assert.Equal("Sm\u00f6rg\u00e5sbord", c.getShortMessage());
            Assert.Equal("Sm\u00f6rg\u00e5sbord\n\n\u304d\u308c\u3044\n", c
                                                                                 .getFullMessage());
        }

        /**
         * Test parsing of a commit whose encoding is given and works.
         *
         * @throws Exception
         */

        [Fact]
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

                c = new RevTag(id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));
                c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), ((MemoryStream) b.BaseStream).ToArray());
            }
            Assert.Equal("F\u00f6r fattare", c.getTaggerIdent().Name);
            Assert.Equal("\u304d\u308c\u3044", c.getShortMessage());
            Assert.Equal("\u304d\u308c\u3044\n\nHi\n", c.getFullMessage());
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

        [Fact]
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

                c = new RevTag(id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));
                c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), ((MemoryStream) b.BaseStream).ToArray());
            }

            Assert.Equal("F\u00f6r fattare", c.getTaggerIdent().Name);
            Assert.Equal("\u304d\u308c\u3044", c.getShortMessage());
            Assert.Equal("\u304d\u308c\u3044\n\nHi\n", c.getFullMessage());
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

        [Fact]
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

                c = new RevTag(id("9473095c4cb2f12aefe1db8a355fe3fafba42f67"));
                c.parseCanonical(new GitSharp.RevWalk.RevWalk(db), ((MemoryStream) b.BaseStream).ToArray());
            }
            Assert.Equal("F\u00f6r fattare", c.getTaggerIdent().Name);
            Assert.Equal("\u304d\u308c\u3044", c.getShortMessage());
            Assert.Equal("\u304d\u308c\u3044\n\nHi\n", c.getFullMessage());
        }

        [Fact]
        public void testParse_NoMessage()
        {
            string msg = "";
            RevTag c = create(msg);
            Assert.Equal(msg, c.getFullMessage());
            Assert.Equal(msg, c.getShortMessage());
        }

        [Fact]
        public void testParse_OnlyLFMessage()
        {
            RevTag c = create("\n");
            Assert.Equal("\n", c.getFullMessage());
            Assert.Equal("", c.getShortMessage());
        }

        [Fact]
        public void testParse_ShortLineOnlyNoLF()
        {
            string shortMsg = "This is a short message.";
            RevTag c = create(shortMsg);
            Assert.Equal(shortMsg, c.getFullMessage());
            Assert.Equal(shortMsg, c.getShortMessage());
        }

        [Fact]
        public void testParse_ShortLineOnlyEndLF()
        {
            string shortMsg = "This is a short message.";
            string fullMsg = shortMsg + "\n";
            RevTag c = create(fullMsg);
            Assert.Equal(fullMsg, c.getFullMessage());
            Assert.Equal(shortMsg, c.getShortMessage());
        }

        [Fact]
        public void testParse_ShortLineOnlyEmbeddedLF()
        {
            string fullMsg = "This is a\nshort message.";
            string shortMsg = fullMsg.Replace('\n', ' ');
            RevTag c = create(fullMsg);
            Assert.Equal(fullMsg, c.getFullMessage());
            Assert.Equal(shortMsg, c.getShortMessage());
        }

        [Fact]
        public void testParse_ShortLineOnlyEmbeddedAndEndingLF()
        {
            string fullMsg = "This is a\nshort message.\n";
            string shortMsg = "This is a short message.";
            RevTag c = create(fullMsg);
            Assert.Equal(fullMsg, c.getFullMessage());
            Assert.Equal(shortMsg, c.getShortMessage());
        }

        [Fact]
        public void testParse_GitStyleMessage()
        {
            string shortMsg = "This fixes a bug.";
            string body = "We do it with magic and pixie dust and stuff.\n"
                          + "\n" + "Signed-off-by: A U. Thor <author@example.com>\n";
            string fullMsg = shortMsg + "\n" + "\n" + body;
            RevTag c = create(fullMsg);
            Assert.Equal(fullMsg, c.getFullMessage());
            Assert.Equal(shortMsg, c.getShortMessage());
        }

        private ObjectId id(string str)
        {
            return ObjectId.FromString(str);
        }
    }
}
