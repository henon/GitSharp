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

using System.IO;
using System.Text;
using GitSharp.Tests.Util;
using GitSharp.Util;
using Xunit;

namespace GitSharp.Tests
{
	public class WriteTests : RepositoryTestCase // [henon] was BasicTests but I think this name is better
	{
		[Fact]
		public void test001_Initalize()
		{
			var gitdir = new DirectoryInfo(trash.FullName + "/.git");
			var objects = new DirectoryInfo(gitdir.FullName + "/objects");
			var objectsPack = new DirectoryInfo(objects.FullName + "/pack");
			var objectsInfo = new DirectoryInfo(objects.FullName + "/info");
			var refs = new DirectoryInfo(gitdir.FullName + "/refs");
			var refsHeads = new DirectoryInfo(refs.FullName + "/heads");
			var refsTags = new DirectoryInfo(refs.FullName + "/tags");
			var HEAD = new FileInfo(gitdir.FullName + "/HEAD");

			Assert.True(trash.Exists);
			Assert.True(objects.Exists);
			Assert.True(objectsPack.Exists);
			Assert.True(objectsInfo.Exists);
			Assert.Equal(2, objects.GetDirectories().Length);
			Assert.True(refs.Exists);
			Assert.True(refsHeads.Exists);
			Assert.True(refsTags.Exists);
			Assert.True(HEAD.Exists);
			Assert.Equal(23, HEAD.Length);
		}


		[Fact]
		public void ComputeSha()
		{
			byte[] data = Encoding.GetEncoding("ISO-8859-1").GetBytes("test025 some data, more than 16 bytes to get good coverage");
			ObjectId id = new ObjectWriter(db).ComputeBlobSha1(data.Length, new MemoryStream(data));
			Assert.Equal("4f561df5ecf0dfbd53a0dc0f37262fef075d9dde", id.ToString());
		}

		[Fact]
		public void WriteBlob()
		{
			ObjectId id = new ObjectWriter(db).WriteBlob(new FileInfo("Resources/single_file_commit/i-am-a-file"));
			Assert.Equal("95ea6a6859af6791464bd8b6de76ad5a6f9fad81", id.ToString());
			Assert.Equal(Inspector.Inspect("Resources/single_file_commit", "95ea6a6859af6791464bd8b6de76ad5a6f9fad81"), new Inspector(db).Inspect(id));

			writeTrashFile("i-am-a-file", "and this is the data in me\r\n\r\n");
			id = new ObjectWriter(db).WriteBlob(new FileInfo(trash + "/i-am-a-file"));
			Assert.Equal("95ea6a6859af6791464bd8b6de76ad5a6f9fad81", id.ToString());
		}

		[Fact]
		public void WriteTree()
		{
			var t = new Tree(db);
			FileTreeEntry f = t.AddFile("i-am-a-file");
			writeTrashFile(f.Name, "and this is the data in me\r\n\r\n");
			Assert.Equal(File.ReadAllText("Resources/single_file_commit/i-am-a-file"), File.ReadAllText(trash + "/i-am-a-file"));
			t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);
			var id = t.Id;

			var b1 = new BinaryReader(new Inspector(db).ContentStream(id));
			b1.BaseStream.Position = b1.BaseStream.Length - 21;

			var b2 = new BinaryReader(Inspector.ContentStream("Resources/single_file_commit", "917c130bd4fa5bf2df0c399dc1b03401860aa448"));
			b2.BaseStream.Position = b2.BaseStream.Length - 21;
			Assert.Equal(b2.ReadByte(), b1.ReadByte());

			var gitW1 = b2.ReadInt32();
			var gitW2 = b2.ReadInt32();
			var gitW3 = b2.ReadInt32();
			var gitW4 = b2.ReadInt32();
			var gitW5 = b2.ReadInt32();
			b2.BaseStream.Position = b2.BaseStream.Length - 20;
			var gitId = ObjectId.FromRaw(b2.ReadBytes(20));
			var w1 = b1.ReadInt32();
			var w2 = b1.ReadInt32();
			b1.Close();
			b2.Close();

			Assert.Equal(gitW1, w1);
			Assert.Equal(gitW2, w2);

			Assert.Equal("917c130bd4fa5bf2df0c399dc1b03401860aa448", id.ToString());
			var sGit = Inspector.Inspect("Resources/single_file_commit", "917c130bd4fa5bf2df0c399dc1b03401860aa448");
			var s = new Inspector(db).Inspect(id);
			Assert.Equal(sGit, s);
		}

		[Fact]
		public void test002_WriteEmptyTree()
		{
			// One of our test packs contains the empty tree object. If the pack is
			// open when we Create it we won't write the object file out as a loose
			// object (as it already exists in the pack).
			//
			Repository newdb = createNewEmptyRepo();
			var t = new Tree(newdb);
			t.Accept(new WriteTree(trash, newdb), TreeEntry.MODIFIED_ONLY);
			Assert.Equal("4b825dc642cb6eb9a060e54bf8d69288fbee4904", t.Id.ToString());
			var o = new FileInfo(newdb.Directory + "/objects/4b/825dc642cb6eb9a060e54bf8d69288fbee4904");
			Assert.True(o.Exists);
			Assert.True(o.IsReadOnly);
		}

		[Fact]
		public void test002_WriteEmptyTree2()
		{
			// File shouldn't exist as it is in a test pack.
			//
			var t = new Tree(db);
			t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);
			Assert.Equal("4b825dc642cb6eb9a060e54bf8d69288fbee4904", t.Id.ToString());
			var o = new FileInfo(trash_git + "/objects/4b/825dc642cb6eb9a060e54bf8d69288fbee4904");
			Assert.False(o.Exists);
		}

		[Fact]
		public void test003_WriteShouldBeEmptyTree()
		{
			var t = new Tree(db);
			ObjectId emptyId = new ObjectWriter(db).WriteBlob(new byte[0]);
			t.AddFile("should-be-empty").Id = (emptyId);
			t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);
			//var s = Constants.CHARSET.GetString(db.OpenObject(t.Id).getBytes());
			//var s1 = File.ReadAllText(trash_git + "/objects/71/01da2d239567432e3d10a0c45bb81b58f25be6");
			Assert.Equal("7bb943559a305bdd6bdee2cef6e5df2413c3d30a", t.Id.ToString());

			var o = new FileInfo(trash_git + "/objects/7b/b943559a305bdd6bdee2cef6e5df2413c3d30a");
			Assert.True(o.Exists);
			Assert.True(o.IsReadOnly);

			o = new FileInfo(trash_git + "/objects/e6/9de29bb2d1d6434b8b29ae775ad8c2e48c5391");
			Assert.True(o.Exists);
			Assert.True(o.IsReadOnly);
		}

		[Fact]
		public void Write_Simple_Commit()
		{
			var t = new Tree(db);
			FileTreeEntry f = t.AddFile("i-am-a-file");
			writeTrashFile(f.Name, "and this is the data in me\r\n\r\n");
			t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);
			//new ObjectChecker().checkBlob(Constants.CHARSET.GetString(db.OpenObject(t.TreeId).getBytes()).ToCharArray());

			string s = new Inspector(db).Inspect(t.Id);
			string s1 = Inspector.Inspect("Resources/single_file_commit", "16c0beaf7523eb3ef5df45bd42dd4fc6343de864");
			string s2 = Inspector.Inspect("Resources/single_file_commit", "917c130bd4fa5bf2df0c399dc1b03401860aa448");
			string s3 = Inspector.Inspect("Resources/single_file_commit", "95ea6a6859af6791464bd8b6de76ad5a6f9fad81");

			//tree 917c130bd4fa5bf2df0c399dc1b03401860aa448\nauthor henon <meinrad.recheis@gmail.com> 1245946742 +0200\ncommitter henon <meinrad.recheis@gmail.com> 1245946742 +0200\n\nA Commit\n"

			Assert.Equal(ObjectId.FromString("917c130bd4fa5bf2df0c399dc1b03401860aa448"), t.Id);


			var c = new Commit(db)
						{
							Author = (new PersonIdent("henon", "meinrad.recheis@gmail.com", 1245946742, 2 * 60)),
							Committer = (new PersonIdent("henon", "meinrad.recheis@gmail.com", 1245946742, 2 * 60)),
							Message = ("A Commit\n"),
							TreeEntry = (t)
						};
			Assert.Equal(t.TreeId, c.TreeId);
			c.Save();

			string sC = new Inspector(db).Inspect(c.CommitId);
			ObjectId cmtid = ObjectId.FromString("16c0beaf7523eb3ef5df45bd42dd4fc6343de864");
			Assert.Equal(cmtid, c.CommitId);

			// Verify the commit we just wrote is in the correct format.
			//using (var xis = new XInputStream(new FileStream(db.ToFile(cmtid).FullName, System.IO.FileMode.Open, FileAccess.Read)))
			//{
			//    Assert.Equal(0x78, xis.ReadUInt8());
			//    Assert.Equal(0x9c, xis.ReadUInt8());
			//    Assert.True(0x789c % 31 == 0);
			//}

			// Verify we can Read it.
			Commit c2 = db.MapCommit(cmtid.ToString());
			Assert.NotNull(c2);
			Assert.Equal(c.Message, c2.Message);
			Assert.Equal(c.TreeId, c2.TreeId);
			Assert.Equal(c.Author, c2.Author);
			Assert.Equal(c.Committer, c2.Committer);
		}

		[Fact]
		public void test009_CreateCommitOldFormat()
		{
			writeTrashFile(".git/config", "[core]\n" + "legacyHeaders=1\n");
			db.Config.load();
			Assert.Equal(db.Config.getBoolean("core", "legacyHeaders", false), true);

			var t = new Tree(db);
			FileTreeEntry f = t.AddFile("i-am-a-file");
			writeTrashFile(f.Name, "and this is the data in me\n");
			t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);

			var s = new Inspector(db).Inspect(t.Id);

			Assert.Equal(ObjectId.FromString("00b1f73724f493096d1ffa0b0f1f1482dbb8c936"), t.Id);


			var c = new Commit(db)
						{
							Author = (new PersonIdent(jauthor, 1154236443L, -4 * 60)),
							Committer = (new PersonIdent(jcommitter, 1154236443L, -4 * 60)),
							Message = ("A Commit\n"),
							TreeEntry = (t)
						};

			Assert.Equal(t.TreeId, c.TreeId);
			c.Save();

			var sc = new Inspector(db).Inspect(c.CommitId);

			ObjectId cmtid = ObjectId.FromString("803aec4aba175e8ab1d666873c984c0308179099");
			Assert.Equal(cmtid, c.CommitId);

			// Verify the commit we just wrote is in the correct format.
			var xis = new XInputStream(new FileStream(db.ToFile(cmtid).FullName, System.IO.FileMode.Open, FileAccess.Read));
			try
			{
				Assert.Equal(0x78, xis.ReadUInt8());
				Assert.Equal(0x9c, xis.ReadUInt8());
				Assert.True(0x789c % 31 == 0);
			}
			finally
			{
				xis.Close();
			}

			// Verify we can Read it.
			Commit c2 = db.MapCommit(cmtid.ToString());
			Assert.NotNull(c2);
			Assert.Equal(c.Message, c2.Message);
			Assert.Equal(c.TreeId, c2.TreeId);
			Assert.Equal(c.Author, c2.Author);
			Assert.Equal(c.Committer, c2.Committer);
		}

		[Fact]
		public void test012_SubtreeExternalSorting()
		{
			ObjectId emptyBlob = new ObjectWriter(db).WriteBlob(new byte[0]);
			var t = new Tree(db);
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
			Assert.Equal(ObjectId.FromString("b47a8f0a4190f7572e11212769090523e23eb1ea"), t.Id);
		}

		[Fact]
		public void test020_createBlobTag()
		{
			ObjectId emptyId = new ObjectWriter(db).WriteBlob(new byte[0]);
			var t = new Tag(db)
						{
							Id = (emptyId),
							TagType = ("blob"),
							TagName = ("test020"),
							Author = (new PersonIdent(jauthor, 1154236443L, -4 * 60)),
							Message = ("test020 tagged\n")
						};

			t.Save();
			Assert.Equal("6759556b09fbb4fd8ae5e315134481cc25d46954", t.TagId.ToString());

			Tag mapTag = db.MapTag("test020");
			Assert.Equal("blob", mapTag.TagType);
			Assert.Equal("test020 tagged\n", mapTag.Message);
			Assert.Equal(new PersonIdent(jauthor, 1154236443L, -4 * 60), mapTag.Author);
			Assert.Equal("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391", mapTag.Id.ToString());
		}

		[Fact]
		public void test020b_createBlobPlainTag()
		{
			test020_createBlobTag();
			var t = new Tag(db)
						{
							TagName = ("test020b"),
							Id = (ObjectId.FromString("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391"))
						};
			t.Save();

			Tag mapTag = db.MapTag("test020b");
			Assert.Equal("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391", mapTag.Id.ToString());

			// We do not repeat the plain tag test for other object types
		}

		[Fact]
		public void test021_createTreeTag()
		{
			ObjectId emptyId = new ObjectWriter(db).WriteBlob(new byte[0]);
			var almostEmptyTree = new Tree(db);
			almostEmptyTree.AddEntry(new FileTreeEntry(almostEmptyTree, emptyId, Encoding.ASCII.GetBytes("empty"), false));
			ObjectId almostEmptyTreeId = new ObjectWriter(db).WriteTree(almostEmptyTree);

			var t = new Tag(db)
						{
							Id = almostEmptyTreeId,
							TagType = "tree",
							TagName = "test021",
							Author = new PersonIdent(jauthor, 1154236443L, -4 * 60),
							Message = "test021 tagged\n"
						};

			t.Save();
			Assert.Equal("b0517bc8dbe2096b419d42424cd7030733f4abe5", t.TagId.ToString());

			Tag mapTag = db.MapTag("test021");
			Assert.Equal("tree", mapTag.TagType);
			Assert.Equal("test021 tagged\n", mapTag.Message);
			Assert.Equal(new PersonIdent(jauthor, 1154236443L, -4 * 60), mapTag.Author);
			Assert.Equal("417c01c8795a35b8e835113a85a5c0c1c77f67fb", mapTag.Id.ToString());
		}

		[Fact]
		public void test022_createCommitTag()
		{
			ObjectId emptyId = new ObjectWriter(db).WriteBlob(new byte[0]);
			var almostEmptyTree = new Tree(db);
			almostEmptyTree.AddEntry(new FileTreeEntry(almostEmptyTree, emptyId, Constants.encodeASCII("empty"), false));
			ObjectId almostEmptyTreeId = new ObjectWriter(db).WriteTree(almostEmptyTree);

			var almostEmptyCommit = new Commit(db)
										{
											Author = new PersonIdent(jauthor, 1154236443L, -2 * 60),
											Committer = new PersonIdent(jauthor, 1154236443L, -2 * 60),
											Message = "test022\n",
											TreeId = almostEmptyTreeId
										};

			ObjectId almostEmptyCommitId = new ObjectWriter(db).WriteCommit(almostEmptyCommit);

			var t = new Tag(db)
						{
							Id = almostEmptyCommitId,
							TagType = "commit",
							TagName = "test022",
							Author = new PersonIdent(jauthor, 1154236443L, -4 * 60),
							Message = "test022 tagged\n"
						};

			t.Save();
			Assert.Equal("0ce2ebdb36076ef0b38adbe077a07d43b43e3807", t.TagId.ToString());

			Tag mapTag = db.MapTag("test022");
			Assert.Equal("commit", mapTag.TagType);
			Assert.Equal("test022 tagged\n", mapTag.Message);
			Assert.Equal(new PersonIdent(jauthor, 1154236443L, -4 * 60), mapTag.Author);
			Assert.Equal("b5d3b45a96b340441f5abb9080411705c51cc86c", mapTag.Id.ToString());
		}

		[Fact]
		public void test023_createCommitNonAnullii()
		{
			ObjectId emptyId = new ObjectWriter(db).WriteBlob(new byte[0]);
			var almostEmptyTree = new Tree(db);
			almostEmptyTree.AddEntry(new FileTreeEntry(almostEmptyTree, emptyId, Constants.encodeASCII("empty"), false));
			ObjectId almostEmptyTreeId = new ObjectWriter(db).WriteTree(almostEmptyTree);

			var commit = new Commit(db)
							{
								TreeId = almostEmptyTreeId,
								Author = new PersonIdent("Joe H\u00e4cker", "joe@example.com", 4294967295L, 60),
								Committer = new PersonIdent("Joe Hacker", "joe2@example.com", 4294967295L, 60),
								Encoding = Constants.CHARSET,
								Message = "\u00dcbergeeks"
							};

			ObjectId cid = new ObjectWriter(db).WriteCommit(commit);
			Assert.Equal("4680908112778718f37e686cbebcc912730b3154", cid.ToString());
		}

		[Fact]
		public void test024_createCommitNonAscii()
		{
			ObjectId emptyId = new ObjectWriter(db).WriteBlob(new byte[0]);
			var almostEmptyTree = new Tree(db);
			almostEmptyTree.AddEntry(new FileTreeEntry(almostEmptyTree, emptyId, Constants.encodeASCII("empty"), false));
			ObjectId almostEmptyTreeId = new ObjectWriter(db).WriteTree(almostEmptyTree);
			
			var commit = new Commit(db)
							{
								TreeId = almostEmptyTreeId,
								Author = new PersonIdent("Joe H\u00e4cker", "joe@example.com", 4294967295L, 60),
								Committer = new PersonIdent("Joe Hacker", "joe2@example.com", 4294967295L, 60),
								Encoding = Encoding.GetEncoding("ISO-8859-1"),
								Message = "\u00dcbergeeks"
							};

			ObjectId cid = new ObjectWriter(db).WriteCommit(commit);
			var s = new Inspector(db).Inspect(cid);
			Assert.Equal("2979b39d385014b33287054b87f77bcb3ecb5ebf", cid.ToString());
		}

		[Fact]
		public void test025_packedRefs()
		{
			test020_createBlobTag();
			test021_createTreeTag();
			test022_createCommitTag();

			new FileInfo(db.Directory.FullName + "/refs/tags/test020").Delete();
			if (new FileInfo(db.Directory.FullName + "/refs/tags/test020").Exists) throw new IOException("Cannot delete unpacked tag");
			new FileInfo(db.Directory.FullName + "/refs/tags/test021").Delete();
			if (new FileInfo(db.Directory.FullName + "/refs/tags/test021").Exists) throw new IOException("Cannot delete unpacked tag");
			new FileInfo(db.Directory.FullName + "/refs/tags/test022").Delete();
			if (new FileInfo(db.Directory.FullName + "/refs/tags/test022").Exists) throw new IOException("Cannot delete unpacked tag");

			// We cannot Resolve it now, since we have no ref
			Tag mapTag20Missing = db.MapTag("test020");
			Assert.Null(mapTag20Missing);

			// Construct packed refs file
			var fs = new FileStream(db.Directory.FullName + "/packed-refs", System.IO.FileMode.Create);
			var w = new StreamWriter(fs);
			w.WriteLine("# packed-refs with: peeled");
			w.WriteLine("6759556b09fbb4fd8ae5e315134481cc25d46954 refs/tags/test020");
			w.WriteLine("^e69de29bb2d1d6434b8b29ae775ad8c2e48c5391");
			w.WriteLine("b0517bc8dbe2096b419d42424cd7030733f4abe5 refs/tags/test021");
			w.WriteLine("^417c01c8795a35b8e835113a85a5c0c1c77f67fb");
			w.WriteLine("0ce2ebdb36076ef0b38adbe077a07d43b43e3807 refs/tags/test022");
			w.WriteLine("^b5d3b45a96b340441f5abb9080411705c51cc86c");
			w.Close();

			Tag mapTag20 = db.MapTag("test020");
			Assert.NotNull(mapTag20);
			Assert.Equal("blob", mapTag20.TagType);
			Assert.Equal("test020 tagged\n", mapTag20.Message);
			Assert.Equal(new PersonIdent(jauthor, 1154236443L, -4 * 60), mapTag20.Author);
			Assert.Equal("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391", mapTag20.Id.ToString());

			Tag mapTag21 = db.MapTag("test021");
			Assert.Equal("tree", mapTag21.TagType);
			Assert.Equal("test021 tagged\n", mapTag21.Message);
			Assert.Equal(new PersonIdent(jauthor, 1154236443L, -4 * 60), mapTag21.Author);
			Assert.Equal("417c01c8795a35b8e835113a85a5c0c1c77f67fb", mapTag21.Id.ToString());

			Tag mapTag22 = db.MapTag("test022");
			Assert.Equal("commit", mapTag22.TagType);
			Assert.Equal("test022 tagged\n", mapTag22.Message);
			Assert.Equal(new PersonIdent(jauthor, 1154236443L, -4 * 60), mapTag22.Author);
			Assert.Equal("b5d3b45a96b340441f5abb9080411705c51cc86c", mapTag22.Id.ToString());
		}

		[Fact]
		public void test026_CreateCommitMultipleparents()
		{
			db.Config.load();

			var t = new Tree(db);
			FileTreeEntry f = t.AddFile("i-am-a-file");
			writeTrashFile(f.Name, "and this is the data in me\n");
			t.Accept(new WriteTree(trash, db), TreeEntry.MODIFIED_ONLY);
			Assert.Equal(ObjectId.FromString("00b1f73724f493096d1ffa0b0f1f1482dbb8c936"),
					t.TreeId);

			var c1 = new Commit(db)
						{
							Author = new PersonIdent(jauthor, 1154236443L, -4 * 60),
							Committer = new PersonIdent(jcommitter, 1154236443L, -4 * 60),
							Message = "A Commit\n",
							TreeEntry = t
						};

			Assert.Equal(t.TreeId, c1.TreeId);
			c1.Save();
			ObjectId cmtid1 = ObjectId.FromString("803aec4aba175e8ab1d666873c984c0308179099");
			Assert.Equal(cmtid1, c1.CommitId);

			var c2 = new Commit(db)
						{
							Author = new PersonIdent(jauthor, 1154236443L, -4 * 60),
							Committer = new PersonIdent(jcommitter, 1154236443L, -4 * 60),
							Message = "A Commit 2\n",
							TreeEntry = t
						};

			Assert.Equal(t.TreeId, c2.TreeId);
			c2.ParentIds = new[] { c1.CommitId };
			c2.Save();
			ObjectId cmtid2 = ObjectId.FromString("95d068687c91c5c044fb8c77c5154d5247901553");
			Assert.Equal(cmtid2, c2.CommitId);

			Commit rm2 = db.MapCommit(cmtid2);
			Assert.NotSame(c2, rm2); // assert the parsed objects is not from the cache
			Assert.Equal(c2.Author, rm2.Author);
			Assert.Equal(c2.CommitId, rm2.CommitId);
			Assert.Equal(c2.Message, rm2.Message);
			Assert.Equal(c2.TreeEntry.TreeId, rm2.TreeEntry.TreeId);
			Assert.Equal(1, rm2.ParentIds.Length);
			Assert.Equal(c1.CommitId, rm2.ParentIds[0]);

			var c3 = new Commit(db)
						{
							Author = new PersonIdent(jauthor, 1154236443L, -4 * 60),
							Committer = new PersonIdent(jcommitter, 1154236443L, -4 * 60),
							Message = "A Commit 3\n",
							TreeEntry = t
						};

			Assert.Equal(t.TreeId, c3.TreeId);
			c3.ParentIds = new[] { c1.CommitId, c2.CommitId };
			c3.Save();
			ObjectId cmtid3 = ObjectId.FromString("ce6e1ce48fbeeb15a83f628dc8dc2debefa066f4");
			Assert.Equal(cmtid3, c3.CommitId);

			Commit rm3 = db.MapCommit(cmtid3);
			Assert.NotSame(c3, rm3); // assert the parsed objects is not from the cache
			Assert.Equal(c3.Author, rm3.Author);
			Assert.Equal(c3.CommitId, rm3.CommitId);
			Assert.Equal(c3.Message, rm3.Message);
			Assert.Equal(c3.TreeEntry.TreeId, rm3.TreeEntry.TreeId);
			Assert.Equal(2, rm3.ParentIds.Length);
			Assert.Equal(c1.CommitId, rm3.ParentIds[0]);
			Assert.Equal(c2.CommitId, rm3.ParentIds[1]);

			var c4 = new Commit(db)
						{
							Author = new PersonIdent(jauthor, 1154236443L, -4 * 60),
							Committer = new PersonIdent(jcommitter, 1154236443L, -4 * 60),
							Message = "A Commit 4\n",
							TreeEntry = t
						};

			Assert.Equal(t.TreeId, c3.TreeId);
			c4.ParentIds = new[] { c1.CommitId, c2.CommitId, c3.CommitId };
			c4.Save();
			ObjectId cmtid4 = ObjectId.FromString("d1fca9fe3fef54e5212eb67902c8ed3e79736e27");
			Assert.Equal(cmtid4, c4.CommitId);

			Commit rm4 = db.MapCommit(cmtid4);
			Assert.NotSame(c4, rm3); // assert the parsed objects is not from the cache
			Assert.Equal(c4.Author, rm4.Author);
			Assert.Equal(c4.CommitId, rm4.CommitId);
			Assert.Equal(c4.Message, rm4.Message);
			Assert.Equal(c4.TreeEntry.TreeId, rm4.TreeEntry.TreeId);
			Assert.Equal(3, rm4.ParentIds.Length);
			Assert.Equal(c1.CommitId, rm4.ParentIds[0]);
			Assert.Equal(c2.CommitId, rm4.ParentIds[1]);
			Assert.Equal(c3.CommitId, rm4.ParentIds[2]);
		}

		[Fact]
		public void test027_UnpackedRefHigherPriorityThanPacked()
		{
			const string unpackedId = "7f822839a2fe9760f386cbbbcb3f92c5fe81def7";
			var writer = new StreamWriter(new FileStream(db.Directory.FullName + "/refs/heads/a", System.IO.FileMode.CreateNew));
			writer.WriteLine(unpackedId);
			writer.Close();

			ObjectId resolved = db.Resolve("refs/heads/a");
			Assert.Equal(unpackedId, resolved.ToString());
		}

		[Fact]
		public void test028_LockPackedRef()
		{
			writeTrashFile(".git/packed-refs", "7f822839a2fe9760f386cbbbcb3f92c5fe81def7 refs/heads/foobar");
			writeTrashFile(".git/HEAD", "ref: refs/heads/foobar\n");

			ObjectId resolve = db.Resolve("HEAD");
			Assert.Equal("7f822839a2fe9760f386cbbbcb3f92c5fe81def7", resolve.ToString());

			RefUpdate lockRef = db.UpdateRef("HEAD");
			ObjectId newId = ObjectId.FromString("07f822839a2fe9760f386cbbbcb3f92c5fe81def");
			lockRef.NewObjectId = newId;
			Assert.Equal(RefUpdate.RefUpdateResult.Forced, lockRef.ForceUpdate());

			Assert.True(new FileInfo(db.Directory.FullName + "/refs/heads/foobar").Exists);
			Assert.Equal(newId, db.Resolve("refs/heads/foobar"));

			// Again. The ref already exists
			RefUpdate lockRef2 = db.UpdateRef("HEAD");
			ObjectId newId2 = ObjectId.FromString("7f822839a2fe9760f386cbbbcb3f92c5fe81def7");
			lockRef2.NewObjectId = newId2;
			Assert.Equal(RefUpdate.RefUpdateResult.Forced, lockRef2.ForceUpdate());

			Assert.True(new FileInfo(db.Directory.FullName + "/refs/heads/foobar").Exists);
			Assert.Equal(newId2, db.Resolve("refs/heads/foobar"));
		}

		[Fact(Timeout = 30000)]
		public void test029_mapObject()
		{
			Assert.Equal((new byte[0].GetType()), db.MapObject(ObjectId.FromString("5b6e7c66c276e7610d4a73c70ec1a1f7c1003259"), null).GetType());
			Assert.Equal(typeof(Commit), db.MapObject(ObjectId.FromString("540a36d136cf413e4b064c2b0e0a4db60f77feab"), null).GetType());
			Assert.Equal(typeof(Tree), db.MapObject(ObjectId.FromString("aabf2ffaec9b497f0950352b3e582d73035c2035"), null).GetType());
			Assert.Equal(typeof(Tag), db.MapObject(ObjectId.FromString("17768080a2318cd89bba4c8b87834401e2095703"), null).GetType());
		}
	}
}
