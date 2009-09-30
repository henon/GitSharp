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
using GitSharp.Core;
using GitSharp.Core.TreeWalk;
using GitSharp.Core.Util;
using NUnit.Framework;
using FileMode=GitSharp.Core.FileMode;

namespace GitSharp.Tests.TreeWalk
{
	[TestFixture]
	public class FileTreeIteratorTest : RepositoryTestCase
	{
		private static readonly string[] Paths = { "a,", "a,b", "a/b", "a0b" };
		private long[] _mtime;

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
			_mtime = new long[Paths.Length];
			for (int i = Paths.Length - 1; i >= 0; i--)
			{
				string s = Paths[i];
				FileInfo fi = writeTrashFile(s, s);
				_mtime[i] = fi.LastWriteTime.Ticks;
			}
		}

		[Test]
		public void testEmptyIfRootIsFile()
		{
			string path = Path.Combine(trash.FullName, Paths[0]);
			var di = new DirectoryInfo(path);
			var fi = new FileInfo(path);
			Assert.IsTrue(fi.Exists);

			var fti = new FileTreeIterator(di);
			Assert.IsTrue(fti.first());
			Assert.IsTrue(fti.eof(),"Test fails under mono due to http://bugzilla.novell.com/show_bug.cgi?id=539791,Fixed upstream");
		}

		[Test]
		public void testEmptyIfRootDoesNotExist()
		{
			string path = Path.Combine(trash.FullName, "not-existing-File");
			var di = new DirectoryInfo(path);
			Assert.IsFalse(di.Exists);

			var fti = new FileTreeIterator(di);
			Assert.IsTrue(fti.first());
			Assert.IsTrue(fti.eof());
		}

		[Test]
		public void testEmptyIfRootIsEmpty()
		{
			string path = Path.Combine(trash.FullName, "not-existing-File");
			var di = new DirectoryInfo(path);
			Assert.IsFalse(di.Exists);

			di.Mkdirs();
            di.Refresh();
			Assert.IsTrue(di.Exists);

			var fti = new FileTreeIterator(di);
			Assert.IsTrue(fti.first());
			Assert.IsTrue(fti.eof());
		}

		[Test]
		public void testSimpleIterate()
		{
			var top = new FileTreeIterator(trash);

			Assert.IsTrue(top.first());
			Assert.IsFalse(top.eof());
			Assert.IsTrue(FileMode.RegularFile == top.EntryFileMode);
			Assert.AreEqual(Paths[0], NameOf(top));
			Assert.AreEqual(Paths[0].Length, top.getEntryLength());
			Assert.AreEqual(_mtime[0], top.getEntryLastModified());

			top.next(1);
			Assert.IsFalse(top.first());
			Assert.IsFalse(top.eof());
			Assert.IsTrue(FileMode.RegularFile == top.EntryFileMode);
			Assert.AreEqual(Paths[1], NameOf(top));
			Assert.AreEqual(Paths[1].Length, top.getEntryLength());
			Assert.AreEqual(_mtime[1], top.getEntryLastModified());

			top.next(1);
			Assert.IsFalse(top.first());
			Assert.IsFalse(top.eof());
			Assert.IsTrue(FileMode.Tree == top.EntryFileMode);

			AbstractTreeIterator sub = top.createSubtreeIterator(db);
			Assert.IsTrue(sub is FileTreeIterator);
			var subfti = (FileTreeIterator)sub;
			Assert.IsTrue(sub.first());
			Assert.IsFalse(sub.eof());
			Assert.AreEqual(Paths[2], NameOf(sub));
			Assert.AreEqual(Paths[2].Length, subfti.getEntryLength());
			Assert.AreEqual(_mtime[2], subfti.getEntryLastModified());

			sub.next(1);
			Assert.IsTrue(sub.eof());

			top.next(1);
			Assert.IsFalse(top.first());
			Assert.IsFalse(top.eof());
			Assert.IsTrue(FileMode.RegularFile == top.EntryFileMode);
			Assert.AreEqual(Paths[3], NameOf(top));
			Assert.AreEqual(Paths[3].Length, top.getEntryLength());
			Assert.AreEqual(_mtime[3], top.getEntryLastModified());

			top.next(1);
			Assert.IsTrue(top.eof());
		}

		[Test]
		public void testComputeFileObjectId()
		{
			var top = new FileTreeIterator(trash);

			MessageDigest md = Constants.newMessageDigest();
			md.Update(Constants.encodeASCII(Constants.TYPE_BLOB));
			md.Update((byte)' ');
			md.Update(Constants.encodeASCII(Paths[0].Length));
			md.Update(0);
			md.Update(Constants.encode(Paths[0]));
			ObjectId expect = ObjectId.FromRaw(md.Digest());

			Assert.AreEqual(expect, top.getEntryObjectId());

			// Verify it was cached by removing the File and getting it again.
			File.Delete(Path.Combine(trash.FullName, Paths[0]));
			Assert.AreEqual(expect, top.getEntryObjectId());
		}

		private static string NameOf(AbstractTreeIterator i)
		{
			return RawParseUtils.decode(Constants.CHARSET, i.Path, 0, i.PathLen);
		}
	}
}