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

using GitSharp.Tests.Util;
using GitSharp.TreeWalk;
using Xunit;

namespace GitSharp.Tests.TreeWalk
{
	public class EmptyTreeIteratorTest : RepositoryTestCase
	{
		[Fact]
		public virtual void testAtEOF()
		{
			var etp = new EmptyTreeIterator();
			Assert.True(etp.first());
			Assert.True(etp.eof());
		}

		[Fact]
		public virtual void testCreateSubtreeIterator()
		{
			var etp = new EmptyTreeIterator();
			AbstractTreeIterator sub = etp.createSubtreeIterator(db);
			Assert.NotNull(sub);
			Assert.True(sub.first());
			Assert.True(sub.eof());
			Assert.True(sub is EmptyTreeIterator);
		}

		[Fact]
		public virtual void testEntryObjectId()
		{
			var etp = new EmptyTreeIterator();
			Assert.Same(ObjectId.ZeroId, etp.getEntryObjectId());
			Assert.NotNull(etp.idBuffer());
			Assert.Equal(0, etp.idOffset());
			Assert.Equal(ObjectId.ZeroId, ObjectId.FromRaw(etp.idBuffer()));
		}

		[Fact]
		public virtual void testNextDoesNothing()
		{
			var etp = new EmptyTreeIterator();
			etp.next(1);
			Assert.True(etp.first());
			Assert.True(etp.eof());
			Assert.Equal(ObjectId.ZeroId, ObjectId.FromRaw(etp.idBuffer()));

			etp.next(1);
			Assert.True(etp.first());
			Assert.True(etp.eof());
			Assert.Equal(ObjectId.ZeroId, ObjectId.FromRaw(etp.idBuffer()));
		}

		[Fact]
		public virtual void testBackDoesNothing()
		{
			var etp = new EmptyTreeIterator();
			etp.back(1);
			Assert.True(etp.first());
			Assert.True(etp.eof());
			Assert.Equal(ObjectId.ZeroId, ObjectId.FromRaw(etp.idBuffer()));

			etp.back(1);
			Assert.True(etp.first());
			Assert.True(etp.eof());
			Assert.Equal(ObjectId.ZeroId, ObjectId.FromRaw(etp.idBuffer()));
		}

		[Fact]
		public virtual void testStopWalkCallsParent()
		{
			var called = new bool[1];
			Assert.False(called[0]);

			// [ammachado]: Anonymous inner classes are not convertable to .NET:
			EmptyTreeIterator parent = new AnonymousTreeIterator(called);


			parent.createSubtreeIterator(db).stopWalk();
			Assert.True(called[0]);
		}

		#region Nested Types

		class AnonymousTreeIterator : EmptyTreeIterator
		{
			private readonly bool[] _called;

			public AnonymousTreeIterator(bool[] called)
			{
				_called = called;
			}

			public override void stopWalk()
			{
				_called[0] = true;
			}
		}

		#endregion
	}
}
