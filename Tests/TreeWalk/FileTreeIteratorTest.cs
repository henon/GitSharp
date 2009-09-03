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
using GitSharp.TreeWalk;
using GitSharp.Util;
using NUnit.Framework;

namespace GitSharp.Tests.TreeWalk
{
	[TestFixture]
	public class FileTreeIteratorTest : RepositoryTestCase
	{
		private readonly string[] paths = { "a,", "a,b", "a/b", "a0b" };
		private long[] mtime;

		public override void setUp()
		{
			base.setUp();

			// We build the entries backwards so that on POSIX systems we
			// are likely to get the entries in the trash directory in the
			// opposite order of what they should be in for the iteration.
			// This should stress the sorting code better than doing it in
			// the correct order.
			//
			// [ammachado] Does Windows NTFS works in the same way? AFAIK, it orders by name
			mtime = new long[paths.Length];
			for (int i = paths.Length - 1; i >= 0; i--)
			{
				string s = paths[i];
				FileInfo fi = writeTrashFile(s, s);
				mtime[i] = fi.LastWriteTime.Ticks;
			}
		}

    [Test]
		public virtual void testEmptyIfRootIsFile()
		{
			string path = Path.Combine(trash.FullName, paths[0]);
      DirectoryInfo di = new DirectoryInfo(path);
      FileInfo fi = new FileInfo(path);
			Assert.IsTrue(fi.Exists);

			FileTreeIterator fti = new FileTreeIterator(di);
			Assert.IsTrue(fti.first());
			Assert.IsTrue(fti.eof());
		}

		[Test]
		public virtual void testEmptyIfRootDoesNotExist()
		{
			string path = Path.Combine(trash.FullName, "not-existing-file");
			DirectoryInfo di = new DirectoryInfo(path);
			Assert.IsFalse(di.Exists);

			FileTreeIterator fti = new FileTreeIterator(di);
			Assert.IsTrue(fti.first());
			Assert.IsTrue(fti.eof());
		}

		[Test]
		public virtual void testEmptyIfRootIsEmpty()
		{
			string path = Path.Combine(trash.FullName, "not-existing-file");
			DirectoryInfo di = new DirectoryInfo(path);
			Assert.IsFalse(di.Exists);

			di.Create();
      di.Refresh();
			Assert.IsTrue(di.Exists);

			FileTreeIterator fti = new FileTreeIterator(di);
			Assert.IsTrue(fti.first());
			Assert.IsTrue(fti.eof());
		}

		[Test]
		public virtual void testSimpleIterate()
		{
			FileTreeIterator top = new FileTreeIterator(trash);

			Assert.IsTrue(top.first());
			Assert.IsFalse(top.eof());
			Assert.AreEqual(FileMode.RegularFile.Bits, top.mode);
			Assert.AreEqual(paths[0], nameOf(top));
			Assert.AreEqual(paths[0].Length, top.getEntryLength());
			Assert.AreEqual(mtime[0], top.getEntryLastModified());

			top.next(1);
			Assert.IsFalse(top.first());
			Assert.IsFalse(top.eof());
			Assert.AreEqual(FileMode.RegularFile.Bits, top.mode);
			Assert.AreEqual(paths[1], nameOf(top));
			Assert.AreEqual(paths[1].Length, top.getEntryLength());
			Assert.AreEqual(mtime[1], top.getEntryLastModified());

			top.next(1);
			Assert.IsFalse(top.first());
			Assert.IsFalse(top.eof());
			Assert.AreEqual(FileMode.Tree.Bits, top.mode);

			AbstractTreeIterator sub = top.createSubtreeIterator(db);
			Assert.IsTrue(sub is FileTreeIterator);
			FileTreeIterator subfti = (FileTreeIterator)sub;
			Assert.IsTrue(sub.first());
			Assert.IsFalse(sub.eof());
			Assert.AreEqual(paths[2], nameOf(sub));
			Assert.AreEqual(paths[2].Length, subfti.getEntryLength());
			Assert.AreEqual(mtime[2], subfti.getEntryLastModified());

			sub.next(1);
			Assert.IsTrue(sub.eof());

			top.next(1);
			Assert.IsFalse(top.first());
			Assert.IsFalse(top.eof());
			Assert.AreEqual(FileMode.RegularFile.Bits, top.mode);
			Assert.AreEqual(paths[3], nameOf(top));
			Assert.AreEqual(paths[3].Length, top.getEntryLength());
			Assert.AreEqual(mtime[3], top.getEntryLastModified());

			top.next(1);
			Assert.IsTrue(top.eof());
		}

		[Test]
		public virtual void testComputeFileObjectId()
		{
			FileTreeIterator top = new FileTreeIterator(trash);

			MessageDigest md = Constants.newMessageDigest();
			md.Update(Constants.encodeASCII(Constants.TYPE_BLOB));
			md.Update((byte) ' ');
			md.Update(Constants.encodeASCII(paths[0].Length));
			md.Update((byte) 0);
			md.Update(Constants.encode(paths[0]));
			ObjectId expect = ObjectId.FromRaw(md.Digest());

			Assert.AreEqual(expect, top.getEntryObjectId());

			// Verify it was cached by removing the file and getting it again.
			File.Delete(Path.Combine(trash.FullName, paths[0]));
			Assert.AreEqual(expect, top.getEntryObjectId());
		}

		private static string nameOf(AbstractTreeIterator i)
		{
			return RawParseUtils.decode(Constants.CHARSET, i.path, 0, i.pathLen);
		}
	}
}