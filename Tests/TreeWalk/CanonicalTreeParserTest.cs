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
using GitSharp.TreeWalk;
using GitSharp.Util;
using Xunit;

namespace GitSharp.Tests.TreeWalk
{
	public class CanonicalTreeParserTest : XunitBaseFact
	{
		private readonly CanonicalTreeParser _ctp;
		private readonly FileMode _m644;
		private readonly FileMode _mt;
		private readonly ObjectId _hashA;
		private readonly ObjectId _hashFoo;
		private readonly ObjectId _hashSometree;

		private byte[] tree1;
		private byte[] tree2;
		private byte[] tree3;

		#region Setup/Teardown

		public CanonicalTreeParserTest()
		{
			_ctp = new CanonicalTreeParser();
			_m644 = FileMode.RegularFile;
			_mt = FileMode.Tree;
			_hashA = ObjectId.FromString("6b9c715d21d5486e59083fb6071566aa6ecd4d42");
			_hashFoo = ObjectId.FromString("a213e8e25bb2442326e86cbfb9ef56319f482869");
			_hashSometree = ObjectId.FromString("daf4bdb0d7bb24319810fe0e73aa317663448c93");
		}

		protected override void SetUp()
		{
			tree1 = MkTree(Entry(_m644, "a", _hashA));
			tree2 = MkTree(Entry(_m644, "a", _hashA), Entry(_m644, "foo", _hashFoo));
			tree3 = MkTree(Entry(_m644, "a", _hashA), Entry(_mt, "b_sometree", _hashSometree), Entry(_m644, "foo", _hashFoo));
		}

		#endregion

		private static byte[] MkTree(params byte[][] data)
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
			return RawParseUtils.decode(Constants.CHARSET, _ctp.Path, _ctp.PathOffset, _ctp.PathLen);
		}

		[Fact]
		public void testBackwards_ConfusingPathName()
		{
			const string aVeryConfusingName = "confusing 644 entry 755 and others";
			_ctp.reset(MkTree(Entry(_m644, "a", _hashA), Entry(_mt, aVeryConfusingName,
			                                                _hashSometree), Entry(_m644, "foo", _hashFoo)));
			_ctp.next(3);
			Assert.True(_ctp.eof());

			_ctp.back(2);
			Assert.False(_ctp.eof());
			Assert.Equal(_mt.Bits, _ctp.Mode);
			Assert.Equal(aVeryConfusingName, Path());
			Assert.Equal(_hashSometree, _ctp.getEntryObjectId());

			_ctp.back(1);
			Assert.False(_ctp.eof());
			Assert.Equal(_m644.Bits, _ctp.Mode);
			Assert.Equal("a", Path());
			Assert.Equal(_hashA, _ctp.getEntryObjectId());
		}

		[Fact]
		public void testEmptyTree_AtEOF()
		{
			_ctp.reset(new byte[0]);
			Assert.True(_ctp.eof());
		}

		[Fact]
		public void testFreakingHugePathName()
		{
			int n = AbstractTreeIterator.DEFAULT_PATH_SIZE*4;
			var b = new StringBuilder(n);
			for (int i = 0; i < n; i++)
			{
				b.Append('q');
			}
			string name = b.ToString();
			_ctp.reset(Entry(_m644, name, _hashA));
			Assert.False(_ctp.eof());
			Assert.Equal(name, RawParseUtils.decode(Constants.CHARSET, _ctp.Path, _ctp.PathOffset, _ctp.PathLen));
		}

		[Fact]
		public void testOneEntry_Backwards()
		{
			_ctp.reset(tree1);
			_ctp.next(1);
			Assert.False(_ctp.first());
			Assert.True(_ctp.eof());

			_ctp.back(1);
			Assert.True(_ctp.first());
			Assert.False(_ctp.eof());
			Assert.Equal(_m644.Bits, _ctp.Mode);
			Assert.Equal("a", Path());
			Assert.Equal(_hashA, _ctp.getEntryObjectId());
		}

		[Fact]
		public void testOneEntry_Forward()
		{
			_ctp.reset(tree1);

			Assert.True(_ctp.first());
			Assert.False(_ctp.eof());
			Assert.Equal(_m644.Bits, _ctp.Mode);
			Assert.Equal("a", Path());
			Assert.Equal(_hashA, _ctp.getEntryObjectId());

			_ctp.next(1);
			Assert.False(_ctp.first());
			Assert.True(_ctp.eof());
		}

		[Fact]
		public void testOneEntry_Seek1IsEOF()
		{
			_ctp.reset(tree1);
			_ctp.next(1);
			Assert.True(_ctp.eof());
		}

		[Fact]
		public void testThreeEntries_BackwardsTwo()
		{
			_ctp.reset(tree3);
			_ctp.next(3);
			Assert.True(_ctp.eof());

			_ctp.back(2);
			Assert.False(_ctp.eof());
			Assert.Equal(_mt.Bits, _ctp.Mode);
			Assert.Equal("b_sometree", Path());
			Assert.Equal(_hashSometree, _ctp.getEntryObjectId());

			_ctp.next(1);
			Assert.False(_ctp.eof());
			Assert.Equal(_m644.Bits, _ctp.Mode);
			Assert.Equal("foo", Path());
			Assert.Equal(_hashFoo, _ctp.getEntryObjectId());

			_ctp.next(1);
			Assert.True(_ctp.eof());
		}

		[Fact]
		public void testThreeEntries_Seek2()
		{
			_ctp.reset(tree3);

			_ctp.next(2);
			Assert.False(_ctp.eof());
			Assert.False(_ctp.eof());
			Assert.Equal(_m644.Bits, _ctp.Mode);
			Assert.Equal("foo", Path());
			Assert.Equal(_hashFoo, _ctp.getEntryObjectId());

			_ctp.next(1);
			Assert.True(_ctp.eof());
		}

		[Fact]
		public void testThreeEntries_Seek3IsEOF()
		{
			_ctp.reset(tree3);
			_ctp.next(3);
			Assert.True(_ctp.eof());
		}

		[Fact]
		public void testTwoEntries_BackwardsOneAtATime()
		{
			_ctp.reset(tree2);
			_ctp.next(2);
			Assert.True(_ctp.eof());

			_ctp.back(1);
			Assert.False(_ctp.eof());
			Assert.Equal(_m644.Bits, _ctp.Mode);
			Assert.Equal("foo", Path());
			Assert.Equal(_hashFoo, _ctp.getEntryObjectId());

			_ctp.back(1);
			Assert.False(_ctp.eof());
			Assert.Equal(_m644.Bits, _ctp.Mode);
			Assert.Equal("a", Path());
			Assert.Equal(_hashA, _ctp.getEntryObjectId());
		}

		[Fact]
		public void testTwoEntries_BackwardsTwo()
		{
			_ctp.reset(tree2);
			_ctp.next(2);
			Assert.True(_ctp.eof());

			_ctp.back(2);
			Assert.False(_ctp.eof());
			Assert.Equal(_m644.Bits, _ctp.Mode);
			Assert.Equal("a", Path());
			Assert.Equal(_hashA, _ctp.getEntryObjectId());

			_ctp.next(1);
			Assert.False(_ctp.eof());
			Assert.Equal(_m644.Bits, _ctp.Mode);
			Assert.Equal("foo", Path());
			Assert.Equal(_hashFoo, _ctp.getEntryObjectId());

			_ctp.next(1);
			Assert.True(_ctp.eof());
		}

		[Fact]
		public void testTwoEntries_ForwardOneAtATime()
		{
			_ctp.reset(tree2);

			Assert.True(_ctp.first());
			Assert.False(_ctp.eof());
			Assert.Equal(_m644.Bits, _ctp.Mode);
			Assert.Equal("a", Path());
			Assert.Equal(_hashA, _ctp.getEntryObjectId());

			_ctp.next(1);
			Assert.False(_ctp.eof());
			Assert.Equal(_m644.Bits, _ctp.Mode);
			Assert.Equal("foo", Path());
			Assert.Equal(_hashFoo, _ctp.getEntryObjectId());

			_ctp.next(1);
			Assert.False(_ctp.first());
			Assert.True(_ctp.eof());
		}

		[Fact]
		public void testTwoEntries_Seek2IsEOF()
		{
			_ctp.reset(tree2);
			_ctp.next(2);
			Assert.True(_ctp.eof());
		}
	}
}