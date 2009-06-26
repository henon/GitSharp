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
using GitSharp.Tests.Util;
using GitSharp.Util;

namespace GitSharp.Tests
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
        public void Compute_SHA()
        {
            byte[] data = Encoding.GetEncoding("ISO-8859-1").GetBytes("test025 some data, more than 16 bytes to get good coverage");
            ObjectId id = new ObjectWriter(db).ComputeBlobSha1(data.Length, new MemoryStream(data));
            Assert.AreEqual("4f561df5ecf0dfbd53a0dc0f37262fef075d9dde", id.ToString());
        }

        [Test]
        public void Write_Blob()
        {
            ObjectId id = new ObjectWriter(db).WriteBlob(new FileInfo("Resources/single_file_commit/i-am-a-file"));
            Assert.AreEqual("95ea6a6859af6791464bd8b6de76ad5a6f9fad81", id.ToString());
            Assert.AreEqual(Inspector.Inspect("Resources/single_file_commit", "95ea6a6859af6791464bd8b6de76ad5a6f9fad81"), new Inspector(db).Inspect(id));

            writeTrashFile("i-am-a-file", "and this is the data in me\r\n\r\n");
            id = new ObjectWriter(db).WriteBlob(new FileInfo(trash+"/i-am-a-file"));
            Assert.AreEqual("95ea6a6859af6791464bd8b6de76ad5a6f9fad81", id.ToString());
        }

        [Test]
        public void Write_Tree()
        {
            Tree t = new Tree(db);
            FileTreeEntry f = t.AddFile("i-am-a-file");
            writeTrashFile(f.Name, "and this is the data in me\r\n\r\n");
            Assert.AreEqual(File.ReadAllText("Resources/single_file_commit/i-am-a-file"), File.ReadAllText(trash + "/i-am-a-file"));
            t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);
            var id = t.Id;

            var b1 = new BinaryReader(new Inspector(db).ContentStream(id));
            b1.BaseStream.Position = b1.BaseStream.Length - 21;

            var b2 = new BinaryReader(Inspector.ContentStream("Resources/single_file_commit", "917c130bd4fa5bf2df0c399dc1b03401860aa448"));
            b2.BaseStream.Position = b2.BaseStream.Length - 21;
            Assert.AreEqual(b2.ReadByte(), b1.ReadByte());

            var git_w1=b2.ReadInt32();
            var git_w2=b2.ReadInt32();
            var git_w3 = b2.ReadInt32();
            var git_w4 = b2.ReadInt32();
            var git_w5 = b2.ReadInt32(); 
            b2.BaseStream.Position = b2.BaseStream.Length-20;
            var git_id = ObjectId.FromRaw(b2.ReadBytes(20));
            var w1 = b1.ReadInt32();
            var w2= b1.ReadInt32();
            b1.Close();
            b2.Close();

            Assert.AreEqual(git_w1,w1);
            Assert.AreEqual(git_w2, w2);

            Assert.AreEqual("917c130bd4fa5bf2df0c399dc1b03401860aa448", id.ToString());
            var s_git = Inspector.Inspect("Resources/single_file_commit", "917c130bd4fa5bf2df0c399dc1b03401860aa448");
            var s = new Inspector(db).Inspect(id);
            Assert.AreEqual(s_git, s);
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
            //var s = Encoding.UTF8.GetString(db.OpenObject(t.Id).getBytes());
            //var s1 = File.ReadAllText(trash_git + "/objects/71/01da2d239567432e3d10a0c45bb81b58f25be6");
            Assert.AreEqual("7bb943559a305bdd6bdee2cef6e5df2413c3d30a", t.Id.ToString());

            var o = new FileInfo(trash_git + "/objects/7b/b943559a305bdd6bdee2cef6e5df2413c3d30a");
            Assert.IsTrue(o.Exists);
            Assert.IsTrue(o.IsReadOnly);

            o = new FileInfo(trash_git + "/objects/e6/9de29bb2d1d6434b8b29ae775ad8c2e48c5391");
            Assert.IsTrue(o.Exists);
            Assert.IsTrue(o.IsReadOnly);
        }

        [Test]
        public void Write_Simple_Commit()
        {
            Tree t = new Tree(db);
            FileTreeEntry f = t.AddFile("i-am-a-file");
            writeTrashFile(f.Name, "and this is the data in me\r\n\r\n");
            t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);
            //new ObjectChecker().checkBlob(Encoding.UTF8.GetString(db.OpenObject(t.TreeId).getBytes()).ToCharArray());



            string s = new Inspector(db).Inspect(t.Id);
            string s1 = Inspector.Inspect("Resources/single_file_commit", "16c0beaf7523eb3ef5df45bd42dd4fc6343de864");
            string s2 = Inspector.Inspect("Resources/single_file_commit", "917c130bd4fa5bf2df0c399dc1b03401860aa448");
            string s3 = Inspector.Inspect("Resources/single_file_commit", "95ea6a6859af6791464bd8b6de76ad5a6f9fad81");

            //tree 917c130bd4fa5bf2df0c399dc1b03401860aa448\nauthor henon <meinrad.recheis@gmail.com> 1245946742 +0200\ncommitter henon <meinrad.recheis@gmail.com> 1245946742 +0200\n\nA Commit\n"

            Assert.AreEqual(ObjectId.FromString("917c130bd4fa5bf2df0c399dc1b03401860aa448"), t.Id);


            Commit c = new Commit(db);
            c.Author = (new PersonIdent("henon", "meinrad.recheis@gmail.com", 1245946742, 2 * 60));
            c.Committer = (new PersonIdent("henon", "meinrad.recheis@gmail.com", 1245946742, 2 * 60));
            c.Message = ("A Commit\n");
            c.TreeEntry = (t);
            Assert.AreEqual(t.TreeId, c.TreeId);
            c.Save();

            string s_c = new Inspector(db).Inspect(c.CommitId);
            ObjectId cmtid = ObjectId.FromString("16c0beaf7523eb3ef5df45bd42dd4fc6343de864");
            Assert.AreEqual(cmtid, c.CommitId);

            // Verify the commit we just wrote is in the correct format.
            //using (var xis = new XInputStream(new FileStream(db.ToFile(cmtid).FullName, System.IO.FileMode.Open, FileAccess.Read)))
            //{
            //    Assert.AreEqual(0x78, xis.readUInt8());
            //    Assert.AreEqual(0x9c, xis.readUInt8());
            //    Assert.IsTrue(0x789c % 31 == 0);
            //}

            // Verify we can read it.
            Commit c2 = db.MapCommit(cmtid.ToString());
            Assert.IsNotNull(c2);
            Assert.AreEqual(c.Message, c2.Message);
            Assert.AreEqual(c.TreeId, c2.TreeId);
            Assert.AreEqual(c.Author, c2.Author);
            Assert.AreEqual(c.Committer, c2.Committer);
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

            Assert.AreEqual(ObjectId.FromString("00b1f73724f493096d1ffa0b0f1f1482dbb8c936"), t.Id);
            //new ObjectChecker().checkBlob(Encoding.UTF8.GetString(db.OpenObject(t.TreeId).getBytes()).ToCharArray());

            Commit c = new Commit(db);
            c.Author = (new PersonIdent(jauthor, 1154236443L, -4 * 60));
            c.Committer = (new PersonIdent(jcommitter, 1154236443L, -4 * 60));
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


        [Test]
        public void test012_SubtreeExternalSorting()
        {
            ObjectId emptyBlob = new ObjectWriter(db).WriteBlob(new byte[0]);
            Tree t = new Tree(db);
            FileTreeEntry e0 = t.AddFile("a-");
            FileTreeEntry e1 = t.AddFile("a-b");
            FileTreeEntry e2 = t.AddFile("a/b");
            FileTreeEntry e3 = t.AddFile("a=");
            FileTreeEntry e4 = t.AddFile("a=b");

            e0.Id = (emptyBlob);
            e1.Id = (emptyBlob);
            e2.Id = (emptyBlob);
            e3.Id = (emptyBlob);
            e4.Id = (emptyBlob);

            t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);
            Assert.AreEqual(ObjectId.FromString("b47a8f0a4190f7572e11212769090523e23eb1ea"), t.Id);
        }


        [Test]
        public void test020_createBlobTag()
        {
            ObjectId emptyId = new ObjectWriter(db).WriteBlob(new byte[0]);
            Tag t = new Tag(db);
            t.Id = (emptyId);
            t.TagType = ("blob");
            t.TagName = ("test020");
            t.Author = (new PersonIdent(jauthor, 1154236443L, -4 * 60));
            t.Message = ("test020 tagged\n");
            t.Save();
            Assert.AreEqual("6759556b09fbb4fd8ae5e315134481cc25d46954", t.TagId.ToString());

            Tag MapTag = db.MapTag("test020");
            Assert.AreEqual("blob", MapTag.TagType);
            Assert.AreEqual("test020 tagged\n", MapTag.Message);
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443L, -4 * 60), MapTag.Author);
            Assert.AreEqual("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391", MapTag.Id.ToString());
        }

        [Test]
        public void test020b_createBlobPlainTag()
        {
            test020_createBlobTag();
            Tag t = new Tag(db);
            t.TagName = ("test020b");
            t.Id = (ObjectId.FromString("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391"));
            t.Save();

            Tag MapTag = db.MapTag("test020b");
            Assert.AreEqual("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391", MapTag.Id.ToString());

            // We do not repeat the plain tag test for other object types
        }

        [Test]
        public void test021_createTreeTag()
        {
            ObjectId emptyId = new ObjectWriter(db).WriteBlob(new byte[0]);
            Tree almostEmptyTree = new Tree(db);
            almostEmptyTree.AddEntry(new FileTreeEntry(almostEmptyTree, emptyId, Encoding.ASCII.GetBytes("empty"), false));
            ObjectId almostEmptyTreeId = new ObjectWriter(db).WriteTree(almostEmptyTree);
            Tag t = new Tag(db);
            t.Id = (almostEmptyTreeId);
            t.TagType = ("tree");
            t.TagName = ("test021");
            t.Author = (new PersonIdent(jauthor, 1154236443L, -4 * 60));
            t.Message = ("test021 tagged\n");
            t.Save();
            Assert.AreEqual("b0517bc8dbe2096b419d42424cd7030733f4abe5", t.TagId.ToString());

            Tag MapTag = db.MapTag("test021");
            Assert.AreEqual("tree", MapTag.TagType);
            Assert.AreEqual("test021 tagged\n", MapTag.Message);
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443L, -4 * 60), MapTag.Author);
            Assert.AreEqual("417c01c8795a35b8e835113a85a5c0c1c77f67fb", MapTag.Id.ToString());
        }

        [Test]
        public void test022_createCommitTag()
        {
            ObjectId emptyId = new ObjectWriter(db).WriteBlob(new byte[0]);
            Tree almostEmptyTree = new Tree(db);
            almostEmptyTree.AddEntry(new FileTreeEntry(almostEmptyTree, emptyId, Encoding.ASCII.GetBytes("empty"), false));
            ObjectId almostEmptyTreeId = new ObjectWriter(db).WriteTree(almostEmptyTree);
            Commit almostEmptyCommit = new Commit(db);
            almostEmptyCommit.Author = (new PersonIdent(jauthor, 1154236443L, -2 * 60)); // not exactly the same
            almostEmptyCommit.Committer = (new PersonIdent(jauthor, 1154236443L, -2 * 60));
            almostEmptyCommit.Message = ("test022\n");
            almostEmptyCommit.TreeId = (almostEmptyTreeId);
            ObjectId almostEmptyCommitId = new ObjectWriter(db).WriteCommit(almostEmptyCommit);
            Tag t = new Tag(db);
            t.Id = (almostEmptyCommitId);
            t.TagType = ("commit");
            t.TagName = ("test022");
            t.Author = (new PersonIdent(jauthor, 1154236443L, -4 * 60));
            t.Message = ("test022 tagged\n");
            t.Save();
            Assert.AreEqual("0ce2ebdb36076ef0b38adbe077a07d43b43e3807", t.TagId.ToString());

            Tag MapTag = db.MapTag("test022");
            Assert.AreEqual("commit", MapTag.TagType);
            Assert.AreEqual("test022 tagged\n", MapTag.Message);
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443L, -4 * 60), MapTag.Author);
            Assert.AreEqual("b5d3b45a96b340441f5abb9080411705c51cc86c", MapTag.Id.ToString());
        }

        [Test]
        public void test023_createCommitNonAnullii()
        {
            ObjectId emptyId = new ObjectWriter(db).WriteBlob(new byte[0]);
            Tree almostEmptyTree = new Tree(db);
            almostEmptyTree.AddEntry(new FileTreeEntry(almostEmptyTree, emptyId, Encoding.ASCII.GetBytes("empty"), false));
            ObjectId almostEmptyTreeId = new ObjectWriter(db).WriteTree(almostEmptyTree);
            Commit commit = new Commit(db);
            commit.TreeId = (almostEmptyTreeId);
            commit.Author = (new PersonIdent("Joe H\u00e4cker", "joe@example.com", 4294967295L, 60));
            commit.Committer = (new PersonIdent("Joe Hacker", "joe2@example.com", 4294967295L, 60));
            commit.setEncoding("UTF-8");
            commit.Message = ("\u00dcbergeeks");
            ObjectId cid = new ObjectWriter(db).WriteCommit(commit);
            Assert.AreEqual("4680908112778718f37e686cbebcc912730b3154", cid.ToString());
        }

        [Test]
        public void test024_createCommitNonAscii()
        {
            ObjectId emptyId = new ObjectWriter(db).WriteBlob(new byte[0]);
            Tree almostEmptyTree = new Tree(db);
            almostEmptyTree.AddEntry(new FileTreeEntry(almostEmptyTree, emptyId, Encoding.ASCII.GetBytes("empty"), false));
            ObjectId almostEmptyTreeId = new ObjectWriter(db).WriteTree(almostEmptyTree);
            Commit commit = new Commit(db);
            commit.TreeId = (almostEmptyTreeId);
            commit.Author = (new PersonIdent("Joe H\u00e4cker", "joe@example.com", 4294967295L, 60));
            commit.Committer = (new PersonIdent("Joe Hacker", "joe2@example.com", 4294967295L, 60));
            commit.setEncoding("ISO-8859-1");
            commit.Message = ("\u00dcbergeeks");
            ObjectId cid = new ObjectWriter(db).WriteCommit(commit);
            Assert.AreEqual("2979b39d385014b33287054b87f77bcb3ecb5ebf", cid.ToString());
        }

        [Test]
        public void test025_packedRefs()
        {
            test020_createBlobTag();
            test021_createTreeTag();
            test022_createCommitTag();

            new DirectoryInfo(db.Directory.FullName + "/refs/tags/test020").Delete();
            if (new DirectoryInfo(db.Directory.FullName + "/refs/tags/test020").Exists) throw new IOException("Cannot delete unpacked tag");
            new DirectoryInfo(db.Directory.FullName + "/refs/tags/test021").Delete();
            if (new DirectoryInfo(db.Directory.FullName + "/refs/tags/test021").Exists) throw new IOException("Cannot delete unpacked tag");
            new DirectoryInfo(db.Directory.FullName + "/refs/tags/test022").Delete();
            if (new DirectoryInfo(db.Directory.FullName + "/refs/tags/test022").Exists) throw new IOException("Cannot delete unpacked tag");

            // We cannot Resolve it now, since we have no ref
            Tag mapTag20missing = db.MapTag("test020");
            Assert.IsNull(mapTag20missing);

            // Construct packed refs file
            var w = new StreamWriter(new FileStream(db.Directory.FullName + "/packed-refs", System.IO.FileMode.CreateNew));
            w.WriteLine("# packed-refs with: peeled");
            w.WriteLine("6759556b09fbb4fd8ae5e315134481cc25d46954 refs/tags/test020");
            w.WriteLine("^e69de29bb2d1d6434b8b29ae775ad8c2e48c5391");
            w.WriteLine("b0517bc8dbe2096b419d42424cd7030733f4abe5 refs/tags/test021");
            w.WriteLine("^417c01c8795a35b8e835113a85a5c0c1c77f67fb");
            w.WriteLine("0ce2ebdb36076ef0b38adbe077a07d43b43e3807 refs/tags/test022");
            w.WriteLine("^b5d3b45a96b340441f5abb9080411705c51cc86c");
            w.Close();

            Tag mapTag20 = db.MapTag("test020");
            Assert.IsNotNull(mapTag20);
            Assert.AreEqual("blob", mapTag20.TagType);
            Assert.AreEqual("test020 tagged\n", mapTag20.Message);
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443L, -4 * 60), mapTag20.Author);
            Assert.AreEqual("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391", mapTag20.Id.ToString());

            Tag mapTag21 = db.MapTag("test021");
            Assert.AreEqual("tree", mapTag21.TagType);
            Assert.AreEqual("test021 tagged\n", mapTag21.Message);
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443L, -4 * 60), mapTag21.Author);
            Assert.AreEqual("417c01c8795a35b8e835113a85a5c0c1c77f67fb", mapTag21.Id.ToString());

            Tag mapTag22 = db.MapTag("test022");
            Assert.AreEqual("commit", mapTag22.TagType);
            Assert.AreEqual("test022 tagged\n", mapTag22.Message);
            Assert.AreEqual(new PersonIdent(jauthor, 1154236443L, -4 * 60), mapTag22.Author);
            Assert.AreEqual("b5d3b45a96b340441f5abb9080411705c51cc86c", mapTag22.Id.ToString());
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
            c1.Author = (new PersonIdent(jauthor, 1154236443L, -4 * 60));
            c1.Committer = (new PersonIdent(jcommitter, 1154236443L, -4 * 60));
            c1.Message = ("A Commit\n");
            c1.TreeEntry = (t);
            Assert.AreEqual(t.TreeId, c1.TreeId);
            c1.Save();
            ObjectId cmtid1 = ObjectId.FromString(
                   "803aec4aba175e8ab1d666873c984c0308179099");
            Assert.AreEqual(cmtid1, c1.CommitId);

            Commit c2 = new Commit(db);
            c2.Author = (new PersonIdent(jauthor, 1154236443L, -4 * 60));
            c2.Committer = (new PersonIdent(jcommitter, 1154236443L, -4 * 60));
            c2.Message = ("A Commit 2\n");
            c2.TreeEntry = (t);
            Assert.AreEqual(t.TreeId, c2.TreeId);
            c2.ParentIds = (new ObjectId[] { c1.CommitId });
            c2.Save();
            ObjectId cmtid2 = ObjectId.FromString(
                   "95d068687c91c5c044fb8c77c5154d5247901553");
            Assert.AreEqual(cmtid2, c2.CommitId);

            Commit rm2 = db.MapCommit(cmtid2);
            Assert.AreNotSame(c2, rm2); // assert the parsed objects is not from the cache
            Assert.AreEqual(c2.Author, rm2.Author);
            Assert.AreEqual(c2.CommitId, rm2.CommitId);
            Assert.AreEqual(c2.Message, rm2.Message);
            Assert.AreEqual(c2.TreeEntry.TreeId, rm2.TreeEntry.TreeId);
            Assert.AreEqual(1, rm2.ParentIds.Length);
            Assert.AreEqual(c1.CommitId, rm2.ParentIds[0]);

            Commit c3 = new Commit(db);
            c3.Author = (new PersonIdent(jauthor, 1154236443L, -4 * 60));
            c3.Committer = (new PersonIdent(jcommitter, 1154236443L, -4 * 60));
            c3.Message = ("A Commit 3\n");
            c3.TreeEntry = (t);
            Assert.AreEqual(t.TreeId, c3.TreeId);
            c3.ParentIds = (new ObjectId[] { c1.CommitId, c2.CommitId });
            c3.Save();
            ObjectId cmtid3 = ObjectId.FromString(
                   "ce6e1ce48fbeeb15a83f628dc8dc2debefa066f4");
            Assert.AreEqual(cmtid3, c3.CommitId);

            Commit rm3 = db.MapCommit(cmtid3);
            Assert.AreNotSame(c3, rm3); // assert the parsed objects is not from the cache
            Assert.AreEqual(c3.Author, rm3.Author);
            Assert.AreEqual(c3.CommitId, rm3.CommitId);
            Assert.AreEqual(c3.Message, rm3.Message);
            Assert.AreEqual(c3.TreeEntry.TreeId, rm3.TreeEntry.TreeId);
            Assert.AreEqual(2, rm3.ParentIds.Length);
            Assert.AreEqual(c1.CommitId, rm3.ParentIds[0]);
            Assert.AreEqual(c2.CommitId, rm3.ParentIds[1]);

            Commit c4 = new Commit(db);
            c4.Author = (new PersonIdent(jauthor, 1154236443L, -4 * 60));
            c4.Committer = (new PersonIdent(jcommitter, 1154236443L, -4 * 60));
            c4.Message = ("A Commit 4\n");
            c4.TreeEntry = (t);
            Assert.AreEqual(t.TreeId, c3.TreeId);
            c4.ParentIds = (new ObjectId[] { c1.CommitId, c2.CommitId, c3.CommitId });
            c4.Save();
            ObjectId cmtid4 = ObjectId.FromString(
                   "d1fca9fe3fef54e5212eb67902c8ed3e79736e27");
            Assert.AreEqual(cmtid4, c4.CommitId);

            Commit rm4 = db.MapCommit(cmtid4);
            Assert.AreNotSame(c4, rm3); // assert the parsed objects is not from the cache
            Assert.AreEqual(c4.Author, rm4.Author);
            Assert.AreEqual(c4.CommitId, rm4.CommitId);
            Assert.AreEqual(c4.Message, rm4.Message);
            Assert.AreEqual(c4.TreeEntry.TreeId, rm4.TreeEntry.TreeId);
            Assert.AreEqual(3, rm4.ParentIds.Length);
            Assert.AreEqual(c1.CommitId, rm4.ParentIds[0]);
            Assert.AreEqual(c2.CommitId, rm4.ParentIds[1]);
            Assert.AreEqual(c3.CommitId, rm4.ParentIds[2]);
        }

        [Test]
        public void test027_UnpackedRefHigherPriorityThanPacked()
        {
            var writer = new StreamWriter(new FileStream(db.Directory.FullName + "/refs/heads/a", System.IO.FileMode.CreateNew));
            string unpackedId = "7f822839a2fe9760f386cbbbcb3f92c5fe81def7";
            writer.WriteLine(unpackedId);
            writer.Close();

            ObjectId resolved = db.Resolve("refs/heads/a");
            Assert.AreEqual(unpackedId, resolved.ToString());
        }

        [Test]
        public void test028_LockPackedRef()
        {
            writeTrashFile(".git/packed-refs", "7f822839a2fe9760f386cbbbcb3f92c5fe81def7 refs/heads/foobar");
            writeTrashFile(".git/HEAD", "ref: refs/heads/foobar\n");

            ObjectId Resolve = db.Resolve("HEAD");
            Assert.AreEqual("7f822839a2fe9760f386cbbbcb3f92c5fe81def7", Resolve.ToString());

            RefUpdate lockRef = db.UpdateRef("HEAD");
            ObjectId newId = ObjectId.FromString("07f822839a2fe9760f386cbbbcb3f92c5fe81def");
            lockRef.NewObjectId = (newId);
            Assert.AreEqual(RefUpdate.RefUpdateResult.Forced, lockRef.ForceUpdate());

            Assert.IsTrue(new DirectoryInfo(db.Directory.FullName + "/refs/heads/foobar").Exists);
            Assert.AreEqual(newId, db.Resolve("refs/heads/foobar"));

            // Again. The ref already exists
            RefUpdate lockRef2 = db.UpdateRef("HEAD");
            ObjectId newId2 = ObjectId.FromString("7f822839a2fe9760f386cbbbcb3f92c5fe81def7");
            lockRef2.NewObjectId = (newId2);
            Assert.AreEqual(RefUpdate.RefUpdateResult.Forced, lockRef2.ForceUpdate());

            Assert.IsTrue(new DirectoryInfo(db.Directory.FullName + "refs/heads/foobar").Exists);
            Assert.AreEqual(newId2, db.Resolve("refs/heads/foobar"));
        }

        [Test]
        public void test029_mapObject()
        {
            Assert.AreEqual(new byte[0].GetType(), db.MapObject(ObjectId.FromString("5b6e7c66c276e7610d4a73c70ec1a1f7c1003259"), null).GetType());
            Assert.AreEqual(typeof(Commit), db.MapObject(ObjectId.FromString("540a36d136cf413e4b064c2b0e0a4db60f77feab"), null).GetType());
            Assert.AreEqual(typeof(Tree), db.MapObject(ObjectId.FromString("aabf2ffaec9b497f0950352b3e582d73035c2035"), null).GetType());
            Assert.AreEqual(typeof(Tag), db.MapObject(ObjectId.FromString("17768080a2318cd89bba4c8b87834401e2095703"), null).GetType());

        }

    }
}
