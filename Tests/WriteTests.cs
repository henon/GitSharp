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
using Gitty.Core.Tests.Util;

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
        public void test009_CreateCommitOldFormat()
        {
            writeTrashFile(".git/config", "[core]\n" + "legacyHeaders=1\n");
            db.Config.Load();

            Tree t = new Tree(db);
            FileTreeEntry f = t.AddFile("i-am-a-file");
            writeTrashFile(f.Name, "and this is the data in me\n");
            t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);
            Assert.AreEqual(ObjectId.FromString("00b1f73724f493096d1ffa0b0f1f1482dbb8c936"),                    t.TreeId);

            Commit c = new Commit(db);
            c.Author = (new PersonIdent(jauthor, 1154236443000L, new TimeSpan(-4 * 60)));
            c.Committer = (new PersonIdent(jcommitter, 1154236443000L, new TimeSpan(-4 * 60)));
            c.Message = ("A Commit\n");
            c.TreeEntry = (t);
            Assert.AreEqual(t.TreeId, c.TreeId);
            c.Save();
            ObjectId cmtid = ObjectId.FromString("803aec4aba175e8ab1d666873c984c0308179099");
            Assert.AreEqual(cmtid, c.CommitId);

            // Verify the commit we just wrote is in the correct format.
            XInputStream xis = new XInputStream(new FileStream(db.ToFile(cmtid).FullName, System.IO.FileMode.Open));
            try
            {
                Assert.AreEqual(0x78, xis.readUInt8());
                Assert.AreEqual(0x9c, xis.readUInt8());
                Assert.IsTrue(0x789c % 31 == 0);
            }
            finally
            {
                xis.Close();
            }

            // Verify we can read it.
            Commit c2 = db.MapCommit(cmtid.ToString());
            Assert.IsNotNull(c2);
            Assert.AreEqual(c.Message, c2.Message);
            Assert.AreEqual(c.TreeId, c2.TreeId);
            Assert.AreEqual(c.Author, c2.Author);
            Assert.AreEqual(c.Committer, c2.Committer);
        }

#if false
        [Test]
        public void test012_SubtreeExternalSorting()
        {
            ObjectId emptyBlob = new ObjectWriter(db).writeBlob(new byte[0]);
            Tree t = new Tree(db);
            FileTreeEntry e0 = t.AddFile("a-");
            FileTreeEntry e1 = t.AddFile("a-b");
            FileTreeEntry e2 = t.AddFile("a/b");
            FileTreeEntry e3 = t.AddFile("a=");
            FileTreeEntry e4 = t.AddFile("a=b");

            e0.Id=(emptyBlob);
            e1.Id=(emptyBlob);
            e2.Id=(emptyBlob);
            e3.Id=(emptyBlob);
            e4.Id=(emptyBlob);

            t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);
            Assert.AreEqual(ObjectId.FromString("b47a8f0a4190f7572e11212769090523e23eb1ea"),
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
            t.Author=(new PersonIdent(jauthor, 1154236443000L, -4 * 60));
            t.Message=("test020 tagged\n");
            t.tag();
            Assert.AreEqual("6759556b09fbb4fd8ae5e315134481cc25d46954", t.getTagId().ToString());

            Tag mapTag = db.mapTag("test020");
            Assert.AreEqual("blob", mapTag.getType());
            Assert.AreEqual("test020 tagged\n", mapTag.Message);
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443000L, -4 * 60), mapTag.Author);
            Assert.AreEqual("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391", mapTag.getObjId().ToString());
        }

        [Test]
        public void test020b_createBlobPlainTag()
        {
            test020_createBlobTag();
            Tag t = new Tag(db);
            t.setTag("test020b");
            t.setObjId(ObjectId.FromString("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391"));
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
            t.Author=(new PersonIdent(jauthor, 1154236443000L, -4 * 60));
            t.Message=("test021 tagged\n");
            t.tag();
            Assert.AreEqual("b0517bc8dbe2096b419d42424cd7030733f4abe5", t.getTagId().ToString());

            Tag mapTag = db.mapTag("test021");
            Assert.AreEqual("tree", mapTag.getType());
            Assert.AreEqual("test021 tagged\n", mapTag.Message);
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443000L, -4 * 60), mapTag.Author);
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
            almostEmptyCommit.Author=(new PersonIdent(jauthor, 1154236443000L, -2 * 60)); // not exactly the same
            almostEmptyCommit.Committer=(new PersonIdent(jauthor, 1154236443000L, -2 * 60));
            almostEmptyCommit.Message=("test022\n");
            almostEmptyCommit.setTreeId(almostEmptyTreeId);
            ObjectId almostEmptyCommitId = new ObjectWriter(db).writeCommit(almostEmptyCommit);
            Tag t = new Tag(db);
            t.setObjId(almostEmptyCommitId);
            t.setType("commit");
            t.setTag("test022");
            t.Author=(new PersonIdent(jauthor, 1154236443000L, -4 * 60));
            t.Message=("test022 tagged\n");
            t.tag();
            Assert.AreEqual("0ce2ebdb36076ef0b38adbe077a07d43b43e3807", t.getTagId().ToString());

            Tag mapTag = db.mapTag("test022");
            Assert.AreEqual("commit", mapTag.getType());
            Assert.AreEqual("test022 tagged\n", mapTag.Message);
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443000L, -4 * 60), mapTag.Author);
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
            commit.Author=(new PersonIdent("Joe H\u00e4cker", "joe@example.com", 4294967295000L, 60));
            commit.Committer=(new PersonIdent("Joe Hacker", "joe2@example.com", 4294967295000L, 60));
            commit.setEncoding("UTF-8");
            commit.Message=("\u00dcbergeeks");
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
            commit.Author=(new PersonIdent("Joe H\u00e4cker", "joe@example.com", 4294967295000L, 60));
            commit.Committer=(new PersonIdent("Joe Hacker", "joe2@example.com", 4294967295000L, 60));
            commit.setEncoding("ISO-8859-1");
            commit.Message=("\u00dcbergeeks");
            ObjectId cid = new ObjectWriter(db).writeCommit(commit);
            Assert.AreEqual("2979b39d385014b33287054b87f77bcb3ecb5ebf", cid.ToString());
        }

        [Test]
        public void test025_packedRefs()
        {
            test020_createBlobTag();
            test021_createTreeTag();
            test022_createCommitTag();

            if (!new DirectoryInfo(db.Directory, "refs/tags/test020").delete()) throw new Error("Cannot delete unpacked tag");
            if (!new DirectoryInfo(db.Directory, "refs/tags/test021").delete()) throw new Error("Cannot delete unpacked tag");
            if (!new DirectoryInfo(db.Directory, "refs/tags/test022").delete()) throw new Error("Cannot delete unpacked tag");

            // We cannot resolve it now, since we have no ref
            Tag mapTag20missing = db.mapTag("test020");
            Assert.IsNull(mapTag20missing);

            // Construct packed refs file
            PrintWriter w = new PrintWriter(new FileWriter(new DirectoryInfo(db.Directory, "packed-refs")));
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
            Assert.AreEqual("test020 tagged\n", mapTag20.Message);
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443000L, -4 * 60), mapTag20.Author);
            Assert.AreEqual("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391", mapTag20.getObjId().ToString());

            Tag mapTag21 = db.mapTag("test021");
            Assert.AreEqual("tree", mapTag21.getType());
            Assert.AreEqual("test021 tagged\n", mapTag21.Message);
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443000L, -4 * 60), mapTag21.Author);
            Assert.AreEqual("417c01c8795a35b8e835113a85a5c0c1c77f67fb", mapTag21.getObjId().ToString());

            Tag mapTag22 = db.mapTag("test022");
            Assert.AreEqual("commit", mapTag22.getType());
            Assert.AreEqual("test022 tagged\n", mapTag22.Message);
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443000L, -4 * 60), mapTag22.Author);
            Assert.AreEqual("b5d3b45a96b340441f5abb9080411705c51cc86c", mapTag22.getObjId().ToString());
        }



        [Test]
        public void test026_CreateCommitMultipleparents()
        {
            db.Config.Load();

            Tree t = new Tree(db);
            FileTreeEntry f = t.AddFile("i-am-a-file");
            writeTrashFile(f.Name, "and this is the data in me\n");
            t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);
            Assert.AreEqual(ObjectId.FromString("00b1f73724f493096d1ffa0b0f1f1482dbb8c936"),
                    t.TreeId);

            Commit c1 = new Commit(db);
            c1.Author=(new PersonIdent(jauthor, 1154236443000L, -4 * 60));
            c1.Committer=(new PersonIdent(jcommitter, 1154236443000L, -4 * 60));
            c1.Message=("A Commit\n");
            c1.TreeEntry=(t);
            Assert.AreEqual(t.TreeId, c1.TreeId);
            c1.commit();
            ObjectId cmtid1 = ObjectId.FromString(
                   "803aec4aba175e8ab1d666873c984c0308179099");
            Assert.AreEqual(cmtid1, c1.CommitId);

            Commit c2 = new Commit(db);
            c2.Author=(new PersonIdent(jauthor, 1154236443000L, -4 * 60));
            c2.Committer=(new PersonIdent(jcommitter, 1154236443000L, -4 * 60));
            c2.Message=("A Commit 2\n");
            c2.TreeEntry=(t);
            Assert.AreEqual(t.TreeId, c2.TreeId);
            c2.setParentIds(new ObjectId[] { c1.CommitId });
            c2.commit();
            ObjectId cmtid2 = ObjectId.FromString(
                   "95d068687c91c5c044fb8c77c5154d5247901553");
            Assert.AreEqual(cmtid2, c2.CommitId);

            Commit rm2 = db.MapCommit(cmtid2);
            Assert.AreNotSame(c2, rm2); // assert the parsed objects is not from the cache
            Assert.AreEqual(c2.Author, rm2.Author);
            Assert.AreEqual(c2.CommitId, rm2.CommitId);
            Assert.AreEqual(c2.Message, rm2.Message);
            Assert.AreEqual(c2.getTree().TreeId, rm2.getTree().TreeId);
            Assert.AreEqual(1, rm2.getParentIds().Length);
            Assert.AreEqual(c1.CommitId, rm2.getParentIds()[0]);

            Commit c3 = new Commit(db);
            c3.Author=(new PersonIdent(jauthor, 1154236443000L, -4 * 60));
            c3.Committer=(new PersonIdent(jcommitter, 1154236443000L, -4 * 60));
            c3.Message=("A Commit 3\n");
            c3.TreeEntry=(t);
            Assert.AreEqual(t.TreeId, c3.TreeId);
            c3.setParentIds(new ObjectId[] { c1.CommitId, c2.CommitId });
            c3.commit();
            ObjectId cmtid3 = ObjectId.FromString(
                   "ce6e1ce48fbeeb15a83f628dc8dc2debefa066f4");
            Assert.AreEqual(cmtid3, c3.CommitId);

            Commit rm3 = db.MapCommit(cmtid3);
            Assert.AreNotSame(c3, rm3); // assert the parsed objects is not from the cache
            Assert.AreEqual(c3.Author, rm3.Author);
            Assert.AreEqual(c3.CommitId, rm3.CommitId);
            Assert.AreEqual(c3.Message, rm3.Message);
            Assert.AreEqual(c3.getTree().TreeId, rm3.getTree().TreeId);
            Assert.AreEqual(2, rm3.getParentIds().Length);
            Assert.AreEqual(c1.CommitId, rm3.getParentIds()[0]);
            Assert.AreEqual(c2.CommitId, rm3.getParentIds()[1]);

            Commit c4 = new Commit(db);
            c4.Author=(new PersonIdent(jauthor, 1154236443000L, -4 * 60));
            c4.Committer=(new PersonIdent(jcommitter, 1154236443000L, -4 * 60));
            c4.Message=("A Commit 4\n");
            c4.TreeEntry=(t);
            Assert.AreEqual(t.TreeId, c3.TreeId);
            c4.setParentIds(new ObjectId[] { c1.CommitId, c2.CommitId, c3.CommitId });
            c4.commit();
            ObjectId cmtid4 = ObjectId.FromString(
                   "d1fca9fe3fef54e5212eb67902c8ed3e79736e27");
            Assert.AreEqual(cmtid4, c4.CommitId);

            Commit rm4 = db.MapCommit(cmtid4);
            Assert.AreNotSame(c4, rm3); // assert the parsed objects is not from the cache
            Assert.AreEqual(c4.Author, rm4.Author);
            Assert.AreEqual(c4.CommitId, rm4.CommitId);
            Assert.AreEqual(c4.Message, rm4.Message);
            Assert.AreEqual(c4.getTree().TreeId, rm4.getTree().TreeId);
            Assert.AreEqual(3, rm4.getParentIds().Length);
            Assert.AreEqual(c1.CommitId, rm4.getParentIds()[0]);
            Assert.AreEqual(c2.CommitId, rm4.getParentIds()[1]);
            Assert.AreEqual(c3.CommitId, rm4.getParentIds()[2]);
        }

        [Test]
        public void test027_UnpackedRefHigherPriorityThanPacked()
        {
            PrintWriter writer = new PrintWriter(new FileWriter(new DirectoryInfo(db.Directory, "refs/heads/a")));
            string unpackedId = "7f822839a2fe9760f386cbbbcb3f92c5fe81def7";
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
            ObjectId newId = ObjectId.FromString("07f822839a2fe9760f386cbbbcb3f92c5fe81def");
            lockRef.setNewObjectId(newId);
            Assert.AreEqual(RefUpdate.Result.FORCED, lockRef.forceUpdate());

            Assert.IsTrue(new DirectoryInfo(db.Directory, "refs/heads/foobar").exists());
            Assert.AreEqual(newId, db.resolve("refs/heads/foobar"));

            // Again. The ref already exists
            RefUpdate lockRef2 = db.updateRef("HEAD");
            ObjectId newId2 = ObjectId.FromString("7f822839a2fe9760f386cbbbcb3f92c5fe81def7");
            lockRef2.setNewObjectId(newId2);
            Assert.AreEqual(RefUpdate.Result.FORCED, lockRef2.forceUpdate());

            Assert.IsTrue(new DirectoryInfo(db.Directory, "refs/heads/foobar").exists());
            Assert.AreEqual(newId2, db.resolve("refs/heads/foobar"));
        }

        [Test]
        public void test029_mapObject()
        {
            Assert.AreEqual(new byte[0].GetType(), db.mapObject(ObjectId.FromString("5b6e7c66c276e7610d4a73c70ec1a1f7c1003259"), null).GetType());
            Assert.AreEqual(typeof(Commit), db.mapObject(ObjectId.FromString("540a36d136cf413e4b064c2b0e0a4db60f77feab"), null).GetType());
            Assert.AreEqual(typeof(Tree), db.mapObject(ObjectId.FromString("aabf2ffaec9b497f0950352b3e582d73035c2035"), null).GetType());
            Assert.AreEqual(typeof(Tag), db.mapObject(ObjectId.FromString("17768080a2318cd89bba4c8b87834401e2095703"), null).GetType());

        }
#endif
    }
}
