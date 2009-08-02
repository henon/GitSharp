/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Google Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using GitSharp;
using GitSharp.Exceptions;

namespace GitSharp.Tests
{
    [TestFixture]
    public class ObjectCheckerTests
    {
        private ObjectChecker checker;

        [SetUp]
        public void setUp()
        {
            //super.setUp();
            checker = new ObjectChecker();
        }

        [Test]
        public void testInvalidType()
        {
            try
            {
                checker.check(Constants.OBJ_BAD, new char[0]);
                Assert.Fail("Did not throw CorruptObjectException");
            }
            catch (CorruptObjectException e)
            {
                String m = e.Message;
                Assert.AreEqual("Invalid object type: " + Constants.OBJ_BAD, m);
            }
        }

        [Test]
        public void testCheckBlob()
        {
            // Any blob should pass...
            checker.checkBlob(new char[0]);
            checker.checkBlob(new char[1]);

            checker.check(Constants.OBJ_BLOB, new char[0]);
            checker.check(Constants.OBJ_BLOB, new char[1]);
        }

        [Test]
        public void testValidCommitNoParent()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("author A. U. Thor <author@localhost> 1 +0000\n");
            b.Append("committer A. U. Thor <author@localhost> 1 +0000\n");

            char[] data = b.ToString().ToCharArray();
            checker.checkCommit(data);
            checker.check(Constants.OBJ_COMMIT, data);
        }

        [Test]
        public void testValidCommitBlankAuthor()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("author <> 0 +0000\n");
            b.Append("committer <> 0 +0000\n");

            char[] data = b.ToString().ToCharArray();
            checker.checkCommit(data);
            checker.check(Constants.OBJ_COMMIT, data);
        }


        [Test]
        public void testValidCommit1Parent()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("parent ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("author A. U. Thor <author@localhost> 1 +0000\n");
            b.Append("committer A. U. Thor <author@localhost> 1 +0000\n");

            char[] data = b.ToString().ToCharArray();
            checker.checkCommit(data);
            checker.check(Constants.OBJ_COMMIT, data);
        }
        [Test]
        public void testValidCommit2Parent()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("parent ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("parent ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("author A. U. Thor <author@localhost> 1 +0000\n");
            b.Append("committer A. U. Thor <author@localhost> 1 +0000\n");

            char[] data = b.ToString().ToCharArray();
            checker.checkCommit(data);
            checker.check(Constants.OBJ_COMMIT, data);
        }
        [Test]
        public void testValidCommit128Parent()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            for (int i = 0; i < 128; i++)
            {
                b.Append("parent ");
                b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
                b.Append('\n');
            }

            b.Append("author A. U. Thor <author@localhost> 1 +0000\n");
            b.Append("committer A. U. Thor <author@localhost> 1 +0000\n");

            char[] data = b.ToString().ToCharArray();
            checker.checkCommit(data);
            checker.check(Constants.OBJ_COMMIT, data);
        }


        [Test]
        public void testValidCommitNormalTime()
        {
            StringBuilder b = new StringBuilder();
            String when = "1222757360 -0730";

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("author A. U. Thor <author@localhost> " + when + "\n");
            b.Append("committer A. U. Thor <author@localhost> " + when + "\n");

            char[] data = b.ToString().ToCharArray();
            checker.checkCommit(data);
            checker.check(Constants.OBJ_COMMIT, data);
        }
        [Test]
        public void testInvalidCommitNoTree1()
        {
            StringBuilder b = new StringBuilder();

            b.Append("parent ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("no tree header", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitNoTree2()
        {
            StringBuilder b = new StringBuilder();

            b.Append("trie ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("no tree header", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitNoTree3()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("no tree header", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitNoTree4()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree\t");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("no tree header", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitInvalidTree1()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("zzzzfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid tree", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitInvalidTree2()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append("z\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid tree", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitInvalidTree3()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9b");
            b.Append("\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid tree", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitInvalidTree4()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree  ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid tree", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitInvalidParent1()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("parent ");
            b.Append("\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid parent", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitInvalidParent2()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("parent ");
            b.Append("zzzzfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append("\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid parent", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitInvalidParent3()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("parent  ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append("\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid parent", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitInvalidParent4()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("parent  ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append("z\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid parent", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitInvalidParent5()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("parent\t");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append("\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                // Yes, really, we complain about author not being
                // found as the invalid parent line wasn't consumed.
                Assert.AreEqual("no author", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitNoAuthor()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("committer A. U. Thor <author@localhost> 1 +0000\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                // Yes, really, we complain about author not being
                // found as the invalid parent line wasn't consumed.
                Assert.AreEqual("no author", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitNoCommitter1()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("author A. U. Thor <author@localhost> 1 +0000\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                // Yes, really, we complain about author not being
                // found as the invalid parent line wasn't consumed.
                Assert.AreEqual("no committer", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitNoCommitter2()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("author A. U. Thor <author@localhost> 1 +0000\n");
            b.Append("\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                // Yes, really, we complain about author not being
                // found as the invalid parent line wasn't consumed.
                Assert.AreEqual("no committer", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitInvalidAuthor1()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("author A. U. Thor <foo 1 +0000\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                // Yes, really, we complain about author not being
                // found as the invalid parent line wasn't consumed.
                Assert.AreEqual("invalid author", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitInvalidAuthor2()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("author A. U. Thor foo> 1 +0000\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                // Yes, really, we complain about author not being
                // found as the invalid parent line wasn't consumed.
                Assert.AreEqual("invalid author", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitInvalidAuthor3()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("author 1 +0000\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                // Yes, really, we complain about author not being
                // found as the invalid parent line wasn't consumed.
                Assert.AreEqual("invalid author", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitInvalidAuthor4()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("author a <b> +0000\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                // Yes, really, we complain about author not being
                // found as the invalid parent line wasn't consumed.
                Assert.AreEqual("invalid author", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitInvalidAuthor5()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("author a <b>\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                // Yes, really, we complain about author not being
                // found as the invalid parent line wasn't consumed.
                Assert.AreEqual("invalid author", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitInvalidAuthor6()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("author a <b> z");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                // Yes, really, we complain about author not being
                // found as the invalid parent line wasn't consumed.
                Assert.AreEqual("invalid author", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitInvalidAuthor7()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("author a <b> 1 z");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                // Yes, really, we complain about author not being
                // found as the invalid parent line wasn't consumed.
                Assert.AreEqual("invalid author", e.Message);
            }
        }
        [Test]
        public void testInvalidCommitInvalidCommitter()
        {
            StringBuilder b = new StringBuilder();

            b.Append("tree ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("author a <b> 1 +0000\n");
            b.Append("committer a <");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkCommit(data);
                Assert.Fail("Did not catch corrupt object");
            }
            catch (CorruptObjectException e)
            {
                // Yes, really, we complain about author not being
                // found as the invalid parent line wasn't consumed.
                Assert.AreEqual("invalid committer", e.Message);
            }
        }
        [Test]
        public void testValidTag()
        {
            StringBuilder b = new StringBuilder();

            b.Append("object ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("type commit\n");
            b.Append("tag test-tag\n");
            b.Append("tagger A. U. Thor <author@localhost> 1 +0000\n");

            char[] data = b.ToString().ToCharArray();
            checker.checkTag(data);
            checker.check(Constants.OBJ_TAG, data);
        }

        [Test]
        public void testInvalidTagNoObject1()
        {
            StringBuilder b = new StringBuilder();

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTag(data);
                Assert.Fail("incorrectly accepted invalid tag");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("no object header", e.Message);
            }
        }
        [Test]
        public void testInvalidTagNoObject2()
        {
            StringBuilder b = new StringBuilder();

            b.Append("object\t");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTag(data);
                Assert.Fail("incorrectly accepted invalid tag");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("no object header", e.Message);
            }
        }
        [Test]
        public void testInvalidTagNoObject3()
        {
            StringBuilder b = new StringBuilder();

            b.Append("obejct ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTag(data);
                Assert.Fail("incorrectly accepted invalid tag");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("no object header", e.Message);
            }
        }
        [Test]
        public void testInvalidTagNoObject4()
        {
            StringBuilder b = new StringBuilder();

            b.Append("object ");
            b.Append("zz9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTag(data);
                Assert.Fail("incorrectly accepted invalid tag");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid object", e.Message);
            }
        }
        [Test]
        public void testInvalidTagNoObject5()
        {
            StringBuilder b = new StringBuilder();

            b.Append("object ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append(" \n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTag(data);
                Assert.Fail("incorrectly accepted invalid tag");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid object", e.Message);
            }
        }
        [Test]
        public void testInvalidTagNoObject6()
        {
            StringBuilder b = new StringBuilder();

            b.Append("object ");
            b.Append("be9");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTag(data);
                Assert.Fail("incorrectly accepted invalid tag");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid object", e.Message);
            }
        }
        [Test]
        public void testInvalidTagNoType1()
        {
            StringBuilder b = new StringBuilder();

            b.Append("object ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTag(data);
                Assert.Fail("incorrectly accepted invalid tag");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("no type header", e.Message);
            }
        }
        [Test]
        public void testInvalidTagNoType2()
        {
            StringBuilder b = new StringBuilder();

            b.Append("object ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("type\tcommit\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTag(data);
                Assert.Fail("incorrectly accepted invalid tag");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("no type header", e.Message);
            }
        }
        [Test]
        public void testInvalidTagNoType3()
        {
            StringBuilder b = new StringBuilder();

            b.Append("object ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("tpye commit\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTag(data);
                Assert.Fail("incorrectly accepted invalid tag");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("no type header", e.Message);
            }
        }
        [Test]
        public void testInvalidTagNoType4()
        {
            StringBuilder b = new StringBuilder();

            b.Append("object ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("type commit");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTag(data);
                Assert.Fail("incorrectly accepted invalid tag");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("no tag header", e.Message);
            }
        }
        [Test]
        public void testInvalidTagNoTagHeader1()
        {
            StringBuilder b = new StringBuilder();

            b.Append("object ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("type commit\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTag(data);
                Assert.Fail("incorrectly accepted invalid tag");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("no tag header", e.Message);
            }
        }
        [Test]
        public void testInvalidTagNoTagHeader2()
        {
            StringBuilder b = new StringBuilder();

            b.Append("object ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("type commit\n");
            b.Append("tag\tfoo\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTag(data);
                Assert.Fail("incorrectly accepted invalid tag");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("no tag header", e.Message);
            }
        }
        [Test]
        public void testInvalidTagNoTagHeader3()
        {
            StringBuilder b = new StringBuilder();

            b.Append("object ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("type commit\n");
            b.Append("tga foo\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTag(data);
                Assert.Fail("incorrectly accepted invalid tag");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("no tag header", e.Message);
            }
        }
        [Test]
        public void testInvalidTagNoTagHeader4()
        {
            StringBuilder b = new StringBuilder();

            b.Append("object ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("type commit\n");
            b.Append("tag foo");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTag(data);
                Assert.Fail("incorrectly accepted invalid tag");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("no tagger header", e.Message);
            }
        }
        [Test]
        public void testInvalidTagNoTaggerHeader1()
        {
            StringBuilder b = new StringBuilder();

            b.Append("object ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("type commit\n");
            b.Append("tag foo\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTag(data);
                Assert.Fail("incorrectly accepted invalid tag");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("no tagger header", e.Message);
            }
        }
        [Test]
        public void testInvalidTagInvalidTaggerHeader1()
        {
            StringBuilder b = new StringBuilder();

            b.Append("object ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("type commit\n");
            b.Append("tag foo\n");
            b.Append("tagger \n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTag(data);
                Assert.Fail("incorrectly accepted invalid tag");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid tagger", e.Message);
            }
        }
        [Test]
        public void testInvalidTagInvalidTaggerHeader3()
        {
            StringBuilder b = new StringBuilder();

            b.Append("object ");
            b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
            b.Append('\n');

            b.Append("type commit\n");
            b.Append("tag foo\n");
            b.Append("tagger a < 1 +000\n");

            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTag(data);
                Assert.Fail("incorrectly accepted invalid tag");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid tagger", e.Message);
            }
        }
        [Test]
        public void testValidEmptyTree()
        {
            checker.checkTree(new char[0]);
            checker.check(Constants.OBJ_TREE, new char[0]);
        }
        [Test]
        public void testValidTree1()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100644 regular-file");
            char[] data = b.ToString().ToCharArray();
            checker.checkTree(data);
        }
        [Test]
        public void testValidTree2()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100755 executable");
            char[] data = b.ToString().ToCharArray();
            checker.checkTree(data);
        }
        [Test]
        public void testValidTree3()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "40000 tree");
            char[] data = b.ToString().ToCharArray();
            checker.checkTree(data);
        }
        [Test]
        public void testValidTree4()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "120000 symlink");
            char[] data = b.ToString().ToCharArray();
            checker.checkTree(data);
        }
        [Test]
        public void testValidTree5()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "160000 git link");
            char[] data = b.ToString().ToCharArray();
            checker.checkTree(data);
        }
        [Test]
        public void testValidTree6()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100644 .a");
            char[] data = b.ToString().ToCharArray();
            checker.checkTree(data);
        }
        [Test]
        public void testValidTreeSorting1()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100644 fooaaa");
            entry(b, "100755 foobar");
            char[] data = b.ToString().ToCharArray();
            checker.checkTree(data);
        }
        [Test]
        public void testValidTreeSorting2()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100755 fooaaa");
            entry(b, "100644 foobar");
            char[] data = b.ToString().ToCharArray();
            checker.checkTree(data);
        }
        [Test]
        public void testValidTreeSorting3()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "40000 a");
            entry(b, "100644 b");
            char[] data = b.ToString().ToCharArray();
            checker.checkTree(data);
        }
        [Test]
        public void testValidTreeSorting4()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100644 a");
            entry(b, "40000 b");
            char[] data = b.ToString().ToCharArray();
            checker.checkTree(data);
        }
        [Test]
        public void testValidTreeSorting5()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100644 a.c");
            entry(b, "40000 a");
            entry(b, "100644 a0c");
            char[] data = b.ToString().ToCharArray();
            checker.checkTree(data);
        }
        [Test]
        public void testValidTreeSorting6()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "40000 a");
            entry(b, "100644 apple");
            char[] data = b.ToString().ToCharArray();
            checker.checkTree(data);
        }
        [Test]
        public void testValidTreeSorting7()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "40000 an orang");
            entry(b, "40000 an orange");
            char[] data = b.ToString().ToCharArray();
            checker.checkTree(data);
        }
        [Test]
        public void testValidTreeSorting8()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100644 a");
            entry(b, "100644 a0c");
            entry(b, "100644 b");
            char[] data = b.ToString().ToCharArray();
            checker.checkTree(data);
        }
        [Test]
        public void testInvalidTreeModeStartsWithZero1()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "0 a");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("mode starts with '0'", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeModeStartsWithZero2()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "0100644 a");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("mode starts with '0'", e.Message);
            }
        }



        [Test]
        public void testInvalidTreeModeStartsWithZero3()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "040000 a");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("mode starts with '0'", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeModeNotOctal1()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "8 a");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid mode character", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeModeNotOctal2()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "Z a");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid mode character", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeModeNotSupportedMode1()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "1 a");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid mode 1", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeModeNotSupportedMode2()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "170000 a");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid mode " + 0170000, e.Message);
            }
        }
        [Test]
        public void testInvalidTreeModeMissingName()
        {
            StringBuilder b = new StringBuilder();
            b.Append("100644");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("truncated in mode", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeNameContainsSlash()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100644 a/b");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("name contains '/'", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeNameIsEmpty()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100644 ");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("zero length name", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeNameIsDot()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100644 .");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid name '.'", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeNameIsDotDot()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100644 ..");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("invalid name '..'", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeTruncatedInName()
        {
            StringBuilder b = new StringBuilder();
            b.Append("100644 b");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("truncated in name", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeTruncatedInObjectId()
        {
            StringBuilder b = new StringBuilder();
            b.Append("100644 b\012");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("truncated in object id", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeBadSorting1()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100644 foobar");
            entry(b, "100644 fooaaa");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("incorrectly sorted", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeBadSorting2()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "40000 a");
            entry(b, "100644 a.c");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("incorrectly sorted", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeBadSorting3()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100644 a0c");
            entry(b, "40000 a");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("incorrectly sorted", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeDuplicateNames1()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100644 a");
            entry(b, "100644 a");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("duplicate entry names", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeDuplicateNames2()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100644 a");
            entry(b, "100755 a");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("duplicate entry names", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeDuplicateNames3()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100644 a");
            entry(b, "40000 a");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("duplicate entry names", e.Message);
            }
        }
        [Test]
        public void testInvalidTreeDuplicateNames4()
        {
            StringBuilder b = new StringBuilder();
            entry(b, "100644 a");
            entry(b, "100644 a.c");
            entry(b, "100644 a.d");
            entry(b, "100644 a.e");
            entry(b, "40000 a");
            entry(b, "100644 zoo");
            char[] data = b.ToString().ToCharArray();
            try
            {
                checker.checkTree(data);
                Assert.Fail("incorrectly accepted an invalid tree");
            }
            catch (CorruptObjectException e)
            {
                Assert.AreEqual("duplicate entry names", e.Message);
            }
        }


        private static void entry(StringBuilder b, String modeName)
        {
            b.Append(modeName);
            b.Append('\0');
            for (int i = 0; i < Constants.OBJECT_ID_LENGTH; i++)
                b.Append((char)i);
        }
    }
}
