/*
 * Copyright (C) 2008, Google Inc.
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * with@out modification, are permitted provided that the following
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
 *   products derived from this software with@out specific prior
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
using GitSharp.Core;
using GitSharp.Core.TreeWalk;
using GitSharp.Core.Util;
using NUnit.Framework;
using FileMode = GitSharp.Core.FileMode;

namespace GitSharp.Core.Tests.TreeWalk
{
	[TestFixture]
	public class CanonicalTreeParserTest
	{
		#region Setup/Teardown

		[SetUp]
		public void setUp()
		{
			tree1 = mktree(Entry(m644, "a", hash_a));
			tree2 = mktree(Entry(m644, "a", hash_a), Entry(m644, "foo", hash_foo));
			tree3 = mktree(Entry(m644, "a", hash_a), Entry(mt, "b_sometree", hash_sometree), Entry(m644, "foo", hash_foo));
		}

		#endregion

		private readonly CanonicalTreeParser ctp = new CanonicalTreeParser();
		private readonly FileMode m644 = FileMode.RegularFile;
		private readonly FileMode mt = FileMode.Tree;
		private readonly ObjectId hash_a = ObjectId.FromString("6b9c715d21d5486e59083fb6071566aa6ecd4d42");
		private readonly ObjectId hash_foo = ObjectId.FromString("a213e8e25bb2442326e86cbfb9ef56319f482869");
		private readonly ObjectId hash_sometree = ObjectId.FromString("daf4bdb0d7bb24319810fe0e73aa317663448c93");

		private byte[] tree1;
		private byte[] tree2;
		private byte[] tree3;

		private static byte[] mktree(params byte[][] data)
		{
			var @out = new MemoryStream();
			foreach (var e in data)
			{
				@out.Write(e, 0, e.Length);
			}
			return @out.ToArray();
		}

		private static byte[] Entry(FileMode mode, string name, AnyObjectId id)
		{
			var @out = new MemoryStream();
			mode.CopyTo(@out);
			@out.WriteByte((byte) ' ');
			byte[] bytes = Constants.encode(name);
			@out.Write(bytes, 0, bytes.Length);
			@out.WriteByte(0);
			id.copyRawTo(@out);
			return @out.ToArray();
		}

		private string Path()
		{
			return RawParseUtils.decode(Constants.CHARSET, ctp.Path, ctp.PathOffset, ctp.PathLen);
		}

		[Test]
		public void testBackwards_ConfusingPathName()
		{
			const string aVeryConfusingName = "confusing 644 entry 755 and others";
			ctp.reset(mktree(Entry(m644, "a", hash_a), Entry(mt, aVeryConfusingName,
			                                                hash_sometree), Entry(m644, "foo", hash_foo)));
			ctp.next(3);
			Assert.IsTrue(ctp.eof());

			ctp.back(2);
			Assert.IsFalse(ctp.eof());
			Assert.AreEqual(mt.Bits, ctp.Mode);
			Assert.AreEqual(aVeryConfusingName, Path());
			Assert.AreEqual(hash_sometree, ctp.getEntryObjectId());

			ctp.back(1);
			Assert.IsFalse(ctp.eof());
			Assert.AreEqual(m644.Bits, ctp.Mode);
			Assert.AreEqual("a", Path());
			Assert.AreEqual(hash_a, ctp.getEntryObjectId());
		}

		[Test]
		public void testEmptyTree_AtEOF()
		{
			ctp.reset(new byte[0]);
			Assert.IsTrue(ctp.eof());
		}

		[Test]
		public void testFreakingHugePathName()
		{
			int n = AbstractTreeIterator.DEFAULT_PATH_SIZE*4;
			var b = new StringBuilder(n);
			for (int i = 0; i < n; i++)
			{
				b.Append('q');
			}
			string name = b.ToString();
			ctp.reset(Entry(m644, name, hash_a));
			Assert.IsFalse(ctp.eof());
			Assert.AreEqual(name, RawParseUtils.decode(Constants.CHARSET, ctp.Path, ctp.PathOffset, ctp.PathLen));
		}

		[Test]
		public void testOneEntry_Backwards()
		{
			ctp.reset(tree1);
			ctp.next(1);
			Assert.IsFalse(ctp.first());
			Assert.IsTrue(ctp.eof());

			ctp.back(1);
			Assert.IsTrue(ctp.first());
			Assert.IsFalse(ctp.eof());
			Assert.AreEqual(m644.Bits, ctp.Mode);
			Assert.AreEqual("a", Path());
			Assert.AreEqual(hash_a, ctp.getEntryObjectId());
		}

		[Test]
		public void testOneEntry_Forward()
		{
			ctp.reset(tree1);

			Assert.IsTrue(ctp.first());
			Assert.IsFalse(ctp.eof());
			Assert.AreEqual(m644.Bits, ctp.Mode);
			Assert.AreEqual("a", Path());
			Assert.AreEqual(hash_a, ctp.getEntryObjectId());

			ctp.next(1);
			Assert.IsFalse(ctp.first());
			Assert.IsTrue(ctp.eof());
		}

		[Test]
		public void testOneEntry_Seek1IsEOF()
		{
			ctp.reset(tree1);
			ctp.next(1);
			Assert.IsTrue(ctp.eof());
		}

		[Test]
		public void testThreeEntries_BackwardsTwo()
		{
			ctp.reset(tree3);
			ctp.next(3);
			Assert.IsTrue(ctp.eof());

			ctp.back(2);
			Assert.IsFalse(ctp.eof());
			Assert.AreEqual(mt.Bits, ctp.Mode);
			Assert.AreEqual("b_sometree", Path());
			Assert.AreEqual(hash_sometree, ctp.getEntryObjectId());

			ctp.next(1);
			Assert.IsFalse(ctp.eof());
			Assert.AreEqual(m644.Bits, ctp.Mode);
			Assert.AreEqual("foo", Path());
			Assert.AreEqual(hash_foo, ctp.getEntryObjectId());

			ctp.next(1);
			Assert.IsTrue(ctp.eof());
		}

		[Test]
		public void testThreeEntries_Seek2()
		{
			ctp.reset(tree3);

			ctp.next(2);
			Assert.IsFalse(ctp.eof());
			Assert.IsFalse(ctp.eof());
			Assert.AreEqual(m644.Bits, ctp.Mode);
			Assert.AreEqual("foo", Path());
			Assert.AreEqual(hash_foo, ctp.getEntryObjectId());

			ctp.next(1);
			Assert.IsTrue(ctp.eof());
		}

		[Test]
		public void testThreeEntries_Seek3IsEOF()
		{
			ctp.reset(tree3);
			ctp.next(3);
			Assert.IsTrue(ctp.eof());
		}

		[Test]
		public void testTwoEntries_BackwardsOneAtATime()
		{
			ctp.reset(tree2);
			ctp.next(2);
			Assert.IsTrue(ctp.eof());

			ctp.back(1);
			Assert.IsFalse(ctp.eof());
			Assert.AreEqual(m644.Bits, ctp.Mode);
			Assert.AreEqual("foo", Path());
			Assert.AreEqual(hash_foo, ctp.getEntryObjectId());

			ctp.back(1);
			Assert.IsFalse(ctp.eof());
			Assert.AreEqual(m644.Bits, ctp.Mode);
			Assert.AreEqual("a", Path());
			Assert.AreEqual(hash_a, ctp.getEntryObjectId());
		}

		[Test]
		public void testTwoEntries_BackwardsTwo()
		{
			ctp.reset(tree2);
			ctp.next(2);
			Assert.IsTrue(ctp.eof());

			ctp.back(2);
			Assert.IsFalse(ctp.eof());
			Assert.AreEqual(m644.Bits, ctp.Mode);
			Assert.AreEqual("a", Path());
			Assert.AreEqual(hash_a, ctp.getEntryObjectId());

			ctp.next(1);
			Assert.IsFalse(ctp.eof());
			Assert.AreEqual(m644.Bits, ctp.Mode);
			Assert.AreEqual("foo", Path());
			Assert.AreEqual(hash_foo, ctp.getEntryObjectId());

			ctp.next(1);
			Assert.IsTrue(ctp.eof());
		}

		[Test]
		public void testTwoEntries_ForwardOneAtATime()
		{
			ctp.reset(tree2);

			Assert.IsTrue(ctp.first());
			Assert.IsFalse(ctp.eof());
			Assert.AreEqual(m644.Bits, ctp.Mode);
			Assert.AreEqual("a", Path());
			Assert.AreEqual(hash_a, ctp.getEntryObjectId());

			ctp.next(1);
			Assert.IsFalse(ctp.eof());
			Assert.AreEqual(m644.Bits, ctp.Mode);
			Assert.AreEqual("foo", Path());
			Assert.AreEqual(hash_foo, ctp.getEntryObjectId());

			ctp.next(1);
			Assert.IsFalse(ctp.first());
			Assert.IsTrue(ctp.eof());
		}

		[Test]
		public void testTwoEntries_Seek2IsEOF()
		{
			ctp.reset(tree2);
			ctp.next(2);
			Assert.IsTrue(ctp.eof());
		}

	    [Test]
	    public void testBackwords_Prebuilts1()
	    {
	        // What is interesting about this test is the ObjectId for the
	        // "darwin-x86" path entry ends in an octal digit (37 == '7').
	        // Thus when scanning backwards we could over scan and consume
	        // part of the SHA-1, and miss the path terminator.
	        //
	        ObjectId common = ObjectId
	            .FromString("af7bf97cb9bce3f60f1d651a0ef862e9447dd8bc");
	        ObjectId darwinx86 = ObjectId
	            .FromString("e927f7398240f78face99e1a738dac54ef738e37");
	        ObjectId linuxx86 = ObjectId
	            .FromString("ac08dd97120c7cb7d06e98cd5b152011183baf21");
	        ObjectId windows = ObjectId
	            .FromString("6c4c64c221a022bb973165192cca4812033479df");

	        ctp.reset(mktree(Entry(mt, "common", common), Entry(mt, "darwin-x86", darwinx86),
                Entry(mt, "linux-x86", linuxx86), Entry(mt, "windows", windows)));
	        ctp.next(3);
	        Assert.AreEqual("windows", ctp.EntryPathString);
	        Assert.AreSame(mt, ctp.EntryFileMode);
	        Assert.AreEqual(windows, ctp.getEntryObjectId());

	        ctp.back(1);
	        Assert.AreEqual("linux-x86", ctp.EntryPathString);
	        Assert.AreSame(mt, ctp.EntryFileMode);
	        Assert.AreEqual(linuxx86, ctp.getEntryObjectId());

	        ctp.next(1);
	        Assert.AreEqual("windows", ctp.EntryPathString);
	        Assert.AreSame(mt, ctp.EntryFileMode);
	        Assert.AreEqual(windows, ctp.getEntryObjectId());
	    }

	    [Test]
	    public void testBackwords_Prebuilts2()
	    {
	        // What is interesting about this test is the ObjectId for the
	        // "darwin-x86" path entry ends in an octal digit (37 == '7').
	        // Thus when scanning backwards we could over scan and consume
	        // part of the SHA-1, and miss the path terminator.
	        //
	        ObjectId common = ObjectId
	            .FromString("af7bf97cb9bce3f60f1d651a0ef862e9447dd8bc");
	        ObjectId darwinx86 = ObjectId
	            .FromString("0000000000000000000000000000000000000037");
	        ObjectId linuxx86 = ObjectId
	            .FromString("ac08dd97120c7cb7d06e98cd5b152011183baf21");
	        ObjectId windows = ObjectId
	            .FromString("6c4c64c221a022bb973165192cca4812033479df");

	        ctp.reset(mktree(Entry(mt, "common", common), 
                Entry(mt, "darwin-x86", darwinx86), Entry(mt, "linux-x86", linuxx86), 
                Entry(mt, "windows", windows)));
	        ctp.next(3);
	        Assert.AreEqual("windows", ctp.EntryPathString);
	        Assert.AreSame(mt, ctp.EntryFileMode);
	        Assert.AreEqual(windows, ctp.getEntryObjectId());

	        ctp.back(1);
	        Assert.AreEqual("linux-x86", ctp.EntryPathString);
	        Assert.AreSame(mt, ctp.EntryFileMode);
	        Assert.AreEqual(linuxx86, ctp.getEntryObjectId());

	        ctp.next(1);
	        Assert.AreEqual("windows", ctp.EntryPathString);
	        Assert.AreSame(mt, ctp.EntryFileMode);
	        Assert.AreEqual(windows, ctp.getEntryObjectId());
	    }

	}
}