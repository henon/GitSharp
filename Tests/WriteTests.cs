/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using System.IO;
using NUnit.Framework;

namespace Gitty.Core.Tests
{
    [TestFixture]
    public class WriteTests : RepositoryTestCase // [henon] was BasicTests but I think this name is better
    {
        [Test]
        public void test001_Initalize()
        {
            var gitdir = new DirectoryInfo(trash.FullName + "/.git");
            var objects = new DirectoryInfo(gitdir.FullName + "/objects");
            var objects_pack = new DirectoryInfo(objects.FullName + "/pack");
            var objects_info = new DirectoryInfo(objects.FullName + "/info");
            var refs = new DirectoryInfo(gitdir.FullName + "/refs");
            var refs_heads = new DirectoryInfo(refs.FullName + "/heads");
            var refs_tags = new DirectoryInfo(refs.FullName + "/tags");
            var HEAD = new FileInfo(gitdir.FullName + "/HEAD");

            Assert.IsTrue(trash.Exists);
            Assert.IsTrue(objects.Exists);
            Assert.IsTrue(objects_pack.Exists);
            Assert.IsTrue(objects_info.Exists);
            Assert.AreEqual(2, objects.GetDirectories().Length);
            Assert.IsTrue(refs.Exists);
            Assert.IsTrue(refs_heads.Exists);
            Assert.IsTrue(refs_tags.Exists);
            Assert.IsTrue(HEAD.Exists);
            Assert.AreEqual(23, HEAD.Length);
        }

        [Test]
        public void test002_WriteEmptyTree()
        {
            // One of our test packs contains the empty tree object. If the pack is
            // open when we create it we won't write the object file out as a loose
            // object (as it already exists in the pack).
            //
            Repository newdb = createNewEmptyRepo();
            Tree t = new Tree(newdb);
            t.Accept(new WriteTree(trash, newdb), TreeEntry.MODIFIED_ONLY);
            Assert.AreEqual("4b825dc642cb6eb9a060e54bf8d69288fbee4904", t.Id.ToString());
            var o = new FileInfo(newdb.Directory + "/objects/4b/825dc642cb6eb9a060e54bf8d69288fbee4904");
            Assert.IsTrue(o.Exists);
            Assert.IsTrue(o.IsReadOnly);
        }

        [Test]
        public void test002_WriteEmptyTree2()
        {
            // File shouldn't exist as it is in a test pack.
            //
            Tree t = new Tree(db);
            t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);
            Assert.AreEqual("4b825dc642cb6eb9a060e54bf8d69288fbee4904", t.Id.ToString());
            var o = new FileInfo(trash_git + "/objects/4b/825dc642cb6eb9a060e54bf8d69288fbee4904");
            Assert.IsFalse(o.Exists);
        }

        [Test]
        public void test003_WriteShouldBeEmptyTree()
        {
            Tree t = new Tree(db);
            ObjectId emptyId = new ObjectWriter(db).WriteBlob(new byte[0]);
            t.AddFile("should-be-empty").Id = (emptyId);
            t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);
            Assert.AreEqual("7bb943559a305bdd6bdee2cef6e5df2413c3d30a", t.Id.ToString());

            var o = new FileInfo(trash_git + "/objects/7b/b943559a305bdd6bdee2cef6e5df2413c3d30a");
            Assert.IsTrue(o.Exists);
            Assert.IsTrue(o.IsReadOnly);

            o = new FileInfo(trash_git + "/objects/e6/9de29bb2d1d6434b8b29ae775ad8c2e48c5391");
            Assert.IsTrue(o.Exists);
            Assert.IsTrue(o.IsReadOnly);
        }

        [Test]
        public void test025_computeSha1NoStore()
        {
            byte[] data = Encoding.GetEncoding("ISO-8859-1").GetBytes("test025 some data, more than 16 bytes to get good coverage");
            // TODO: but we do not test legacy header writing
            ObjectId id = new ObjectWriter(db).ComputeBlobSha1(data.Length, new MemoryStream(data));
            Assert.AreEqual("4f561df5ecf0dfbd53a0dc0f37262fef075d9dde", id.ToString());
        }

        [Test]
        public void test004_CheckNewConfig()
        {
            RepositoryConfig c = db.Config;
            Assert.IsNotNull(c);
            Assert.AreEqual("0", c.GetString("core", null, "repositoryformatversion"));
            Assert.AreEqual("0", c.GetString("CoRe", null, "REPOSITORYFoRmAtVeRsIoN"));
            Assert.AreEqual("true", c.GetString("core", null, "filemode"));
            Assert.AreEqual("true", c.GetString("cOrE", null, "fIlEModE"));
            Assert.IsNull(c.GetString("notavalue", null, "reallyNotAValue"));
            c.Load();
        }


        [Test]
        public void test005_ReadSimpleConfig()
        {
            RepositoryConfig c = db.Config;
            Assert.IsNotNull(c);
            c.Load();
            Assert.AreEqual("0", c.GetString("core", null, "repositoryformatversion"));
            Assert.AreEqual("0", c.GetString("CoRe", null, "REPOSITORYFoRmAtVeRsIoN"));
            Assert.AreEqual("true", c.GetString("core", null, "filemode"));
            Assert.AreEqual("true", c.GetString("cOrE", null, "fIlEModE"));
            Assert.IsNull(c.GetString("notavalue", null, "reallyNotAValue"));
        }
#if false
        [Test]
        public void test006_ReadUglyConfig()
        {
            RepositoryConfig c = db.getConfig();
            DirectoryInfo cfg = new DirectoryInfo(db.getDirectory(), "config");
            FileWriter pw = new FileWriter(cfg);
            String configStr = "  [core];comment\n\tfilemode = yes\n"
                   + "[user]\n"
                   + "  email = A U Thor <thor@example.com> # Just an example...\n"
                   + " name = \"A  Thor \\\\ \\\"\\t \"\n"
                   + "    defaultCheckInComment = a many line\\n\\\ncomment\\n\\\n"
                   + " to test\n";
            pw.write(configStr);
            pw.close();
            c.Load();
            Assert.AreEqual("yes", c.GetString("core", null, "filemode"));
            Assert.AreEqual("A U Thor <thor@example.com>", c
                    .GetString("user", null, "email"));
            Assert.AreEqual("A  Thor \\ \"\t ", c.GetString("user", null, "name"));
            Assert.AreEqual("a many line\ncomment\n to test", c.GetString("user",
                    null, "defaultCheckInComment"));
            c.save();
            FileReader fr = new FileReader(cfg);
            char[] cbuf = new char[configStr.Length()];
            fr.read(cbuf);
            fr.close();
            Assert.AreEqual(configStr, new String(cbuf));
        }

        [Test]
        public void test007_Open()
        {
            Repository db2 = new Repository(db.getDirectory());
            Assert.AreEqual(db.getDirectory(), db2.getDirectory());
            Assert.AreEqual(db.getObjectsDirectory(), db2.getObjectsDirectory());
            assertNotSame(db.getConfig(), db2.getConfig());
        }

        [Test]
        public void test008_FailOnWrongVersion()
        {
            DirectoryInfo cfg = new DirectoryInfo(db.getDirectory(), "config");
            FileWriter pw = new FileWriter(cfg);
            String badvers = "ihopethisisneveraversion";
            String configStr = "[core]\n" + "\trepositoryFormatVersion="
                   + badvers + "\n";
            pw.write(configStr);
            pw.close();

            try
            {
                new Repository(db.getDirectory());
                fail("incorrectly opened a bad repository");
            }
            catch (IOException ioe)
            {
                Assert.IsTrue(ioe.getMessage().indexOf("format") > 0);
                Assert.IsTrue(ioe.getMessage().indexOf(badvers) > 0);
            }
        }

        [Test]
        public void test009_CreateCommitOldFormat()
        {
            writeTrashFile(".git/config", "[core]\n" + "legacyHeaders=1\n");
            db.getConfig().Load();

            Tree t = new Tree(db);
            FileTreeEntry f = t.addFile("i-am-a-file");
            writeTrashFile(f.getName(), "and this is the data in me\n");
            t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);
            Assert.AreEqual(ObjectId.fromString("00b1f73724f493096d1ffa0b0f1f1482dbb8c936"),
                    t.getTreeId());

            Commit c = new Commit(db);
            c.setAuthor(new PersonIdent(jauthor, 1154236443000L, -4 * 60));
            c.setCommitter(new PersonIdent(jcommitter, 1154236443000L, -4 * 60));
            c.setMessage("A Commit\n");
            c.setTree(t);
            Assert.AreEqual(t.getTreeId(), c.getTreeId());
            c.commit();
            ObjectId cmtid = ObjectId.fromString(
                   "803aec4aba175e8ab1d666873c984c0308179099");
            Assert.AreEqual(cmtid, c.getCommitId());

            // Verify the commit we just wrote is in the correct format.
            XInputStream xis = new XInputStream(new FileInputStream(db
                   .toFile(cmtid)));
            try
            {
                Assert.AreEqual(0x78, xis.readUInt8());
                Assert.AreEqual(0x9c, xis.readUInt8());
                Assert.IsTrue(0x789c % 31 == 0);
            }
            finally
            {
                xis.close();
            }

            // Verify we can read it.
            Commit c2 = db.mapCommit(cmtid);
            Assert.IsNotNull(c2);
            Assert.AreEqual(c.getMessage(), c2.getMessage());
            Assert.AreEqual(c.getTreeId(), c2.getTreeId());
            Assert.AreEqual(c.getAuthor(), c2.getAuthor());
            Assert.AreEqual(c.getCommitter(), c2.getCommitter());
        }

        [Test]
        public void test012_SubtreeExternalSorting()
        {
            ObjectId emptyBlob = new ObjectWriter(db).writeBlob(new byte[0]);
            Tree t = new Tree(db);
            FileTreeEntry e0 = t.addFile("a-");
            FileTreeEntry e1 = t.addFile("a-b");
            FileTreeEntry e2 = t.addFile("a/b");
            FileTreeEntry e3 = t.addFile("a=");
            FileTreeEntry e4 = t.addFile("a=b");

            e0.Id=(emptyBlob);
            e1.Id=(emptyBlob);
            e2.Id=(emptyBlob);
            e3.Id=(emptyBlob);
            e4.Id=(emptyBlob);

            t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);
            Assert.AreEqual(ObjectId.fromString("b47a8f0a4190f7572e11212769090523e23eb1ea"),
                    t.Id);
        }

        [Test]
        public void test020_createBlobTag()
        {
            ObjectId emptyId = new ObjectWriter(db).writeBlob(new byte[0]);
            Tag t = new Tag(db);
            t.setObjId(emptyId);
            t.setType("blob");
            t.setTag("test020");
            t.setAuthor(new PersonIdent(jauthor, 1154236443000L, -4 * 60));
            t.setMessage("test020 tagged\n");
            t.tag();
            Assert.AreEqual("6759556b09fbb4fd8ae5e315134481cc25d46954", t.getTagId().ToString());

            Tag mapTag = db.mapTag("test020");
            Assert.AreEqual("blob", mapTag.getType());
            Assert.AreEqual("test020 tagged\n", mapTag.getMessage());
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443000L, -4 * 60), mapTag.getAuthor());
            Assert.AreEqual("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391", mapTag.getObjId().ToString());
        }

        [Test]
        public void test020b_createBlobPlainTag()
        {
            test020_createBlobTag();
            Tag t = new Tag(db);
            t.setTag("test020b");
            t.setObjId(ObjectId.fromString("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391"));
            t.tag();

            Tag mapTag = db.mapTag("test020b");
            Assert.AreEqual("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391", mapTag.getObjId().ToString());

            // We do not repeat the plain tag test for other object types
        }

        [Test]
        public void test021_createTreeTag()
        {
            ObjectId emptyId = new ObjectWriter(db).writeBlob(new byte[0]);
            Tree almostEmptyTree = new Tree(db);
            almostEmptyTree.addEntry(new FileTreeEntry(almostEmptyTree, emptyId, "empty".getBytes(), false));
            ObjectId almostEmptyTreeId = new ObjectWriter(db).writeTree(almostEmptyTree);
            Tag t = new Tag(db);
            t.setObjId(almostEmptyTreeId);
            t.setType("tree");
            t.setTag("test021");
            t.setAuthor(new PersonIdent(jauthor, 1154236443000L, -4 * 60));
            t.setMessage("test021 tagged\n");
            t.tag();
            Assert.AreEqual("b0517bc8dbe2096b419d42424cd7030733f4abe5", t.getTagId().ToString());

            Tag mapTag = db.mapTag("test021");
            Assert.AreEqual("tree", mapTag.getType());
            Assert.AreEqual("test021 tagged\n", mapTag.getMessage());
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443000L, -4 * 60), mapTag.getAuthor());
            Assert.AreEqual("417c01c8795a35b8e835113a85a5c0c1c77f67fb", mapTag.getObjId().ToString());
        }

        [Test]
        public void test022_createCommitTag()
        {
            ObjectId emptyId = new ObjectWriter(db).writeBlob(new byte[0]);
            Tree almostEmptyTree = new Tree(db);
            almostEmptyTree.addEntry(new FileTreeEntry(almostEmptyTree, emptyId, "empty".getBytes(), false));
            ObjectId almostEmptyTreeId = new ObjectWriter(db).writeTree(almostEmptyTree);
            Commit almostEmptyCommit = new Commit(db);
            almostEmptyCommit.setAuthor(new PersonIdent(jauthor, 1154236443000L, -2 * 60)); // not exactly the same
            almostEmptyCommit.setCommitter(new PersonIdent(jauthor, 1154236443000L, -2 * 60));
            almostEmptyCommit.setMessage("test022\n");
            almostEmptyCommit.setTreeId(almostEmptyTreeId);
            ObjectId almostEmptyCommitId = new ObjectWriter(db).writeCommit(almostEmptyCommit);
            Tag t = new Tag(db);
            t.setObjId(almostEmptyCommitId);
            t.setType("commit");
            t.setTag("test022");
            t.setAuthor(new PersonIdent(jauthor, 1154236443000L, -4 * 60));
            t.setMessage("test022 tagged\n");
            t.tag();
            Assert.AreEqual("0ce2ebdb36076ef0b38adbe077a07d43b43e3807", t.getTagId().ToString());

            Tag mapTag = db.mapTag("test022");
            Assert.AreEqual("commit", mapTag.getType());
            Assert.AreEqual("test022 tagged\n", mapTag.getMessage());
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443000L, -4 * 60), mapTag.getAuthor());
            Assert.AreEqual("b5d3b45a96b340441f5abb9080411705c51cc86c", mapTag.getObjId().ToString());
        }

        [Test]
        public void test023_createCommitNonAnullii()
        {
            ObjectId emptyId = new ObjectWriter(db).writeBlob(new byte[0]);
            Tree almostEmptyTree = new Tree(db);
            almostEmptyTree.addEntry(new FileTreeEntry(almostEmptyTree, emptyId, "empty".getBytes(), false));
            ObjectId almostEmptyTreeId = new ObjectWriter(db).writeTree(almostEmptyTree);
            Commit commit = new Commit(db);
            commit.setTreeId(almostEmptyTreeId);
            commit.setAuthor(new PersonIdent("Joe H\u00e4cker", "joe@example.com", 4294967295000L, 60));
            commit.setCommitter(new PersonIdent("Joe Hacker", "joe2@example.com", 4294967295000L, 60));
            commit.setEncoding("UTF-8");
            commit.setMessage("\u00dcbergeeks");
            ObjectId cid = new ObjectWriter(db).writeCommit(commit);
            Assert.AreEqual("4680908112778718f37e686cbebcc912730b3154", cid.ToString());
        }

        [Test]
        public void test024_createCommitNonAscii()
        {
            ObjectId emptyId = new ObjectWriter(db).writeBlob(new byte[0]);
            Tree almostEmptyTree = new Tree(db);
            almostEmptyTree.addEntry(new FileTreeEntry(almostEmptyTree, emptyId, "empty".getBytes(), false));
            ObjectId almostEmptyTreeId = new ObjectWriter(db).writeTree(almostEmptyTree);
            Commit commit = new Commit(db);
            commit.setTreeId(almostEmptyTreeId);
            commit.setAuthor(new PersonIdent("Joe H\u00e4cker", "joe@example.com", 4294967295000L, 60));
            commit.setCommitter(new PersonIdent("Joe Hacker", "joe2@example.com", 4294967295000L, 60));
            commit.setEncoding("ISO-8859-1");
            commit.setMessage("\u00dcbergeeks");
            ObjectId cid = new ObjectWriter(db).writeCommit(commit);
            Assert.AreEqual("2979b39d385014b33287054b87f77bcb3ecb5ebf", cid.ToString());
        }

        [Test]
        public void test025_packedRefs()
        {
            test020_createBlobTag();
            test021_createTreeTag();
            test022_createCommitTag();

            if (!new DirectoryInfo(db.getDirectory(), "refs/tags/test020").delete()) throw new Error("Cannot delete unpacked tag");
            if (!new DirectoryInfo(db.getDirectory(), "refs/tags/test021").delete()) throw new Error("Cannot delete unpacked tag");
            if (!new DirectoryInfo(db.getDirectory(), "refs/tags/test022").delete()) throw new Error("Cannot delete unpacked tag");

            // We cannot resolve it now, since we have no ref
            Tag mapTag20missing = db.mapTag("test020");
            Assert.IsNull(mapTag20missing);

            // Construct packed refs file
            PrintWriter w = new PrintWriter(new FileWriter(new DirectoryInfo(db.getDirectory(), "packed-refs")));
            w.println("# packed-refs with: peeled");
            w.println("6759556b09fbb4fd8ae5e315134481cc25d46954 refs/tags/test020");
            w.println("^e69de29bb2d1d6434b8b29ae775ad8c2e48c5391");
            w.println("b0517bc8dbe2096b419d42424cd7030733f4abe5 refs/tags/test021");
            w.println("^417c01c8795a35b8e835113a85a5c0c1c77f67fb");
            w.println("0ce2ebdb36076ef0b38adbe077a07d43b43e3807 refs/tags/test022");
            w.println("^b5d3b45a96b340441f5abb9080411705c51cc86c");
            w.close();

            Tag mapTag20 = db.mapTag("test020");
            Assert.IsNotNull("have tag test020", mapTag20);
            Assert.AreEqual("blob", mapTag20.getType());
            Assert.AreEqual("test020 tagged\n", mapTag20.getMessage());
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443000L, -4 * 60), mapTag20.getAuthor());
            Assert.AreEqual("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391", mapTag20.getObjId().ToString());

            Tag mapTag21 = db.mapTag("test021");
            Assert.AreEqual("tree", mapTag21.getType());
            Assert.AreEqual("test021 tagged\n", mapTag21.getMessage());
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443000L, -4 * 60), mapTag21.getAuthor());
            Assert.AreEqual("417c01c8795a35b8e835113a85a5c0c1c77f67fb", mapTag21.getObjId().ToString());

            Tag mapTag22 = db.mapTag("test022");
            Assert.AreEqual("commit", mapTag22.getType());
            Assert.AreEqual("test022 tagged\n", mapTag22.getMessage());
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443000L, -4 * 60), mapTag22.getAuthor());
            Assert.AreEqual("b5d3b45a96b340441f5abb9080411705c51cc86c", mapTag22.getObjId().ToString());
        }



        [Test]
        public void test026_CreateCommitMultipleparents()
        {
            db.getConfig().Load();

            Tree t = new Tree(db);
            FileTreeEntry f = t.addFile("i-am-a-file");
            writeTrashFile(f.getName(), "and this is the data in me\n");
            t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);
            Assert.AreEqual(ObjectId.fromString("00b1f73724f493096d1ffa0b0f1f1482dbb8c936"),
                    t.getTreeId());

            Commit c1 = new Commit(db);
            c1.setAuthor(new PersonIdent(jauthor, 1154236443000L, -4 * 60));
            c1.setCommitter(new PersonIdent(jcommitter, 1154236443000L, -4 * 60));
            c1.setMessage("A Commit\n");
            c1.setTree(t);
            Assert.AreEqual(t.getTreeId(), c1.getTreeId());
            c1.commit();
            ObjectId cmtid1 = ObjectId.fromString(
                   "803aec4aba175e8ab1d666873c984c0308179099");
            Assert.AreEqual(cmtid1, c1.getCommitId());

            Commit c2 = new Commit(db);
            c2.setAuthor(new PersonIdent(jauthor, 1154236443000L, -4 * 60));
            c2.setCommitter(new PersonIdent(jcommitter, 1154236443000L, -4 * 60));
            c2.setMessage("A Commit 2\n");
            c2.setTree(t);
            Assert.AreEqual(t.getTreeId(), c2.getTreeId());
            c2.setParentIds(new ObjectId[] { c1.getCommitId() });
            c2.commit();
            ObjectId cmtid2 = ObjectId.fromString(
                   "95d068687c91c5c044fb8c77c5154d5247901553");
            Assert.AreEqual(cmtid2, c2.getCommitId());

            Commit rm2 = db.mapCommit(cmtid2);
            assertNotSame(c2, rm2); // assert the parsed objects is not from the cache
            Assert.AreEqual(c2.getAuthor(), rm2.getAuthor());
            Assert.AreEqual(c2.getCommitId(), rm2.getCommitId());
            Assert.AreEqual(c2.getMessage(), rm2.getMessage());
            Assert.AreEqual(c2.getTree().getTreeId(), rm2.getTree().getTreeId());
            Assert.AreEqual(1, rm2.getParentIds().Length);
            Assert.AreEqual(c1.getCommitId(), rm2.getParentIds()[0]);

            Commit c3 = new Commit(db);
            c3.setAuthor(new PersonIdent(jauthor, 1154236443000L, -4 * 60));
            c3.setCommitter(new PersonIdent(jcommitter, 1154236443000L, -4 * 60));
            c3.setMessage("A Commit 3\n");
            c3.setTree(t);
            Assert.AreEqual(t.getTreeId(), c3.getTreeId());
            c3.setParentIds(new ObjectId[] { c1.getCommitId(), c2.getCommitId() });
            c3.commit();
            ObjectId cmtid3 = ObjectId.fromString(
                   "ce6e1ce48fbeeb15a83f628dc8dc2debefa066f4");
            Assert.AreEqual(cmtid3, c3.getCommitId());

            Commit rm3 = db.mapCommit(cmtid3);
            assertNotSame(c3, rm3); // assert the parsed objects is not from the cache
            Assert.AreEqual(c3.getAuthor(), rm3.getAuthor());
            Assert.AreEqual(c3.getCommitId(), rm3.getCommitId());
            Assert.AreEqual(c3.getMessage(), rm3.getMessage());
            Assert.AreEqual(c3.getTree().getTreeId(), rm3.getTree().getTreeId());
            Assert.AreEqual(2, rm3.getParentIds().Length);
            Assert.AreEqual(c1.getCommitId(), rm3.getParentIds()[0]);
            Assert.AreEqual(c2.getCommitId(), rm3.getParentIds()[1]);

            Commit c4 = new Commit(db);
            c4.setAuthor(new PersonIdent(jauthor, 1154236443000L, -4 * 60));
            c4.setCommitter(new PersonIdent(jcommitter, 1154236443000L, -4 * 60));
            c4.setMessage("A Commit 4\n");
            c4.setTree(t);
            Assert.AreEqual(t.getTreeId(), c3.getTreeId());
            c4.setParentIds(new ObjectId[] { c1.getCommitId(), c2.getCommitId(), c3.getCommitId() });
            c4.commit();
            ObjectId cmtid4 = ObjectId.fromString(
                   "d1fca9fe3fef54e5212eb67902c8ed3e79736e27");
            Assert.AreEqual(cmtid4, c4.getCommitId());

            Commit rm4 = db.mapCommit(cmtid4);
            assertNotSame(c4, rm3); // assert the parsed objects is not from the cache
            Assert.AreEqual(c4.getAuthor(), rm4.getAuthor());
            Assert.AreEqual(c4.getCommitId(), rm4.getCommitId());
            Assert.AreEqual(c4.getMessage(), rm4.getMessage());
            Assert.AreEqual(c4.getTree().getTreeId(), rm4.getTree().getTreeId());
            Assert.AreEqual(3, rm4.getParentIds().Length);
            Assert.AreEqual(c1.getCommitId(), rm4.getParentIds()[0]);
            Assert.AreEqual(c2.getCommitId(), rm4.getParentIds()[1]);
            Assert.AreEqual(c3.getCommitId(), rm4.getParentIds()[2]);
        }

        [Test]
        public void test027_UnpackedRefHigherPriorityThanPacked()
        {
            PrintWriter writer = new PrintWriter(new FileWriter(new DirectoryInfo(db.getDirectory(), "refs/heads/a")));
            String unpackedId = "7f822839a2fe9760f386cbbbcb3f92c5fe81def7";
            writer.println(unpackedId);
            writer.close();

            ObjectId resolved = db.resolve("refs/heads/a");
            Assert.AreEqual(unpackedId, resolved.ToString());
        }

        [Test]
        public void test028_LockPackedRef()
        {
            writeTrashFile(".git/packed-refs", "7f822839a2fe9760f386cbbbcb3f92c5fe81def7 refs/heads/foobar");
            writeTrashFile(".git/HEAD", "ref: refs/heads/foobar\n");

            ObjectId resolve = db.resolve("HEAD");
            Assert.AreEqual("7f822839a2fe9760f386cbbbcb3f92c5fe81def7", resolve.ToString());

            RefUpdate lockRef = db.updateRef("HEAD");
            ObjectId newId = ObjectId.fromString("07f822839a2fe9760f386cbbbcb3f92c5fe81def");
            lockRef.setNewObjectId(newId);
            Assert.AreEqual(RefUpdate.Result.FORCED, lockRef.forceUpdate());

            Assert.IsTrue(new DirectoryInfo(db.getDirectory(), "refs/heads/foobar").exists());
            Assert.AreEqual(newId, db.resolve("refs/heads/foobar"));

            // Again. The ref already exists
            RefUpdate lockRef2 = db.updateRef("HEAD");
            ObjectId newId2 = ObjectId.fromString("7f822839a2fe9760f386cbbbcb3f92c5fe81def7");
            lockRef2.setNewObjectId(newId2);
            Assert.AreEqual(RefUpdate.Result.FORCED, lockRef2.forceUpdate());

            Assert.IsTrue(new DirectoryInfo(db.getDirectory(), "refs/heads/foobar").exists());
            Assert.AreEqual(newId2, db.resolve("refs/heads/foobar"));
        }

        [Test]
        public void test029_mapObject()
        {
            Assert.AreEqual(new byte[0].GetType(), db.mapObject(ObjectId.fromString("5b6e7c66c276e7610d4a73c70ec1a1f7c1003259"), null).GetType());
            Assert.AreEqual(typeof(Commit), db.mapObject(ObjectId.fromString("540a36d136cf413e4b064c2b0e0a4db60f77feab"), null).GetType());
            Assert.AreEqual(typeof(Tree), db.mapObject(ObjectId.fromString("aabf2ffaec9b497f0950352b3e582d73035c2035"), null).GetType());
            Assert.AreEqual(typeof(Tag), db.mapObject(ObjectId.fromString("17768080a2318cd89bba4c8b87834401e2095703"), null).GetType());

        }
#endif
    }
}
