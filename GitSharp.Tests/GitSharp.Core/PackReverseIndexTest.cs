/*
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
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
using System.IO;
using GitSharp.Core;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Tests.Util;
using GitSharp.Tests.GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Core.Tests
{
	[TestFixture]
	public class PackReverseIndexTest : RepositoryTestCase
	{
		private PackIndex _idx;
		private PackReverseIndex _reverseIdx;

		///	<summary>
		/// Set up tested class instance, test constructor by the way.
		/// </summary>
		public override void setUp()
		{
			base.setUp();

			// index with both small (< 2^31) and big offsets
			var fi = new FileInfo("Resources/pack-huge.idx");
			Assert.IsTrue(fi.Exists, "Does the index exist");
			_idx = PackIndex.Open(fi);
			_reverseIdx = new PackReverseIndex(_idx);
		}

		///	<summary>
		/// Test findObject() for all index entries.
		/// </summary>
		[Test]
		public void testFindObject()
		{
			foreach (PackIndex.MutableEntry me in _idx)
			{
				Assert.AreEqual(me.ToObjectId(), _reverseIdx.FindObject(me.Offset));
			}
		}

		///	<summary>
		/// Test findObject() with illegal argument.
		/// </summary>
		[Test]
		public void testFindObjectWrongOffset()
		{
			Assert.IsNull(_reverseIdx.FindObject(0));
		}

		///	<summary>
		/// Test findNextOffset() for all index entries.
		///	</summary>
		[Test]
		public void testFindNextOffset()
		{
			long offset = FindFirstOffset();
			Assert.IsTrue(offset > 0);

			for (int i = 0; i < _idx.ObjectCount; i++)
			{
				long newOffset = _reverseIdx.FindNextOffset(offset, long.MaxValue);
				Assert.IsTrue(newOffset > offset);

				if (i == _idx.ObjectCount - 1)
				{
					Assert.AreEqual(newOffset, long.MaxValue);
				}
				else
				{
					Assert.AreEqual(newOffset, _idx.FindOffset(_reverseIdx.FindObject(newOffset)));
				}

				offset = newOffset;
			}
		}

		///	<summary>
		/// Test findNextOffset() with wrong illegal argument as offset.
		/// </summary>
		[Test]
		public void testFindNextOffsetWrongOffset()
		{
			AssertHelper.Throws<CorruptObjectException>(() => _reverseIdx.FindNextOffset(0, long.MaxValue));
		}

		private long FindFirstOffset()
		{
			long min = long.MaxValue;
			foreach (PackIndex.MutableEntry me in _idx)
			{
				min = Math.Min(min, me.Offset);
			}
			return min;
		}
	}
}