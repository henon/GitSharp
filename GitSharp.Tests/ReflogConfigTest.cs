/*
 * Copyright (C) 2009, Christian Halstrick, Matthias Sohn, SAP AG
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
 * - Neither the name of the Eclipse Foundation, Inc. nor the
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
using GitSharp.Core;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests
{
    [TestFixture]
    public class ReflogConfigTest : RepositoryTestCase
    {
        [Test]
        public void testlogAllRefUpdates() {
            long commitTime = 1154236443000L;
            int tz = -4 * 60;

            // check that there are no entries in the reflog and turn off writing
            // reflogs
            Assert.IsTrue(db.ReflogReader(Constants.HEAD).getReverseEntries().Count == 0, "there should be no entries in reflog");
            db.Config.setBoolean("core", null, "logallrefupdates", false);

            // do one commit and check that reflog size is 0: no reflogs should be
            // written
            Core.Tree t = new Core.Tree(db);
            addFileToTree(t, "i-am-a-file", "and this is the data in me\n");
            commit(t, "A Commit\n", new PersonIdent(author, commitTime, tz), new PersonIdent(committer, commitTime, tz));
            commitTime += 100;
            Assert.IsTrue(
				
                db.ReflogReader(Constants.HEAD).getReverseEntries().Count == 0, "Reflog for HEAD still contain no entry");

            // set the logAllRefUpdates parameter to true and check it
            db.Config.setBoolean("core", null, "logallrefupdates", true);
            Assert.IsTrue(db.Config.getCore().isLogAllRefUpdates());

            // do one commit and check that reflog size is increased to 1
            addFileToTree(t, "i-am-another-file", "and this is other data in me\n");
            commit(t, "A Commit\n", new PersonIdent(author, commitTime, tz), new PersonIdent(committer, commitTime, tz));
            commitTime += 100;
            Assert.IsTrue(db.ReflogReader(Constants.HEAD).getReverseEntries().Count == 1, "Reflog for HEAD should contain one entry");

            // set the logAllRefUpdates parameter to false and check it
            db.Config.setBoolean("core", null, "logallrefupdates", false);
            Assert.IsFalse(db.Config.getCore().isLogAllRefUpdates());

            // do one commit and check that reflog size is 2
            addFileToTree(t, "i-am-anotheranother-file", "and this is other other data in me\n");
            commit(t, "A Commit\n", new PersonIdent(author, commitTime, tz), new PersonIdent(committer, commitTime, tz));
            Assert.IsTrue(db.ReflogReader(Constants.HEAD).getReverseEntries().Count == 2, "Reflog for HEAD should contain two entries");
        }


        [Test]
        [Ignore]
        public void testEnsureCommitTimeAndTimeZoneOffsetArePreserved()
        {
            long commitTime = 1154236443000L;
            int tz = -4 * 60;

            Core.Tree t = new Core.Tree(db);
            addFileToTree(t, "i-am-a-file", "and this is the data in me\n");
            commit(t, "A Commit\n", new PersonIdent(author, commitTime, tz), new PersonIdent(committer, commitTime, tz));
            IList<ReflogReader.Entry> entries = db.ReflogReader(Constants.HEAD).getReverseEntries();
            Assert.AreEqual(1, entries.Count);

            var entry = entries[0];

            Assert.AreEqual(commitTime, entry.getWho().When);
            Assert.AreEqual(tz, entry.getWho().TimeZoneOffset);
        }

        private void addFileToTree(Core.Tree t, string filename, string content)
        {
            FileTreeEntry f = t.AddFile(filename);
            writeTrashFile(f.Name, content);
            t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);
        }

        private void commit(Core.Tree t, string commitMsg, PersonIdent author,
                            PersonIdent committer) {
            Core.Commit commit = new Core.Commit(db);
            commit.Author = (author);
            commit.Committer = (committer);
            commit.Message = (commitMsg);
            commit.TreeEntry = (t);
            //ObjectWriter writer = new ObjectWriter(db);
            //commit.CommitId = (writer.WriteCommit(commit));
            commit.Save();

            int nl = commitMsg.IndexOf('\n');
            RefUpdate ru = db.UpdateRef(Constants.HEAD);
            ru.NewObjectId = (commit.CommitId);
            ru.SetRefLogMessage("commit : "
                                + ((nl == -1) ? commitMsg : commitMsg.Slice(0, nl)), false);
            ru.ForceUpdate();
                            }
    }
}