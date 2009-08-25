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

using GitSharp.TreeWalk;
using NUnit.Framework;

namespace GitSharp.Tests.TreeWalk
{
	[TestFixture]
	public class EmptyTreeIteratorTest : RepositoryTestCase
	{
		[Test]
		public virtual void testAtEOF()
		{
			EmptyTreeIterator etp = new EmptyTreeIterator();
			Assert.IsTrue(etp.first());
			Assert.IsTrue(etp.eof());
		}

		[Test]
		public virtual void testCreateSubtreeIterator()
		{
			EmptyTreeIterator etp = new EmptyTreeIterator();
			AbstractTreeIterator sub = etp.createSubtreeIterator(db);
			Assert.IsNotNull(sub);
			Assert.IsTrue(sub.first());
			Assert.IsTrue(sub.eof());
			Assert.IsTrue(sub is EmptyTreeIterator);
		}

		[Test]
		public virtual void testEntryObjectId()
		{
			EmptyTreeIterator etp = new EmptyTreeIterator();
			Assert.AreSame(ObjectId.ZeroId, etp.getEntryObjectId());
			Assert.IsNotNull(etp.idBuffer());
			Assert.AreEqual(0, etp.idOffset());
			Assert.AreEqual(ObjectId.ZeroId, ObjectId.FromRaw(etp.idBuffer()));
		}

		[Test]
		public virtual void testNextDoesNothing()
		{
			EmptyTreeIterator etp = new EmptyTreeIterator();
			etp.next(1);
			Assert.IsTrue(etp.first());
			Assert.IsTrue(etp.eof());
			Assert.AreEqual(ObjectId.ZeroId, ObjectId.FromRaw(etp.idBuffer()));

			etp.next(1);
			Assert.IsTrue(etp.first());
			Assert.IsTrue(etp.eof());
			Assert.AreEqual(ObjectId.ZeroId, ObjectId.FromRaw(etp.idBuffer()));
		}

		[Test]
		public virtual void testBackDoesNothing()
		{
			EmptyTreeIterator etp = new EmptyTreeIterator();
			etp.back(1);
			Assert.IsTrue(etp.first());
			Assert.IsTrue(etp.eof());
			Assert.AreEqual(ObjectId.ZeroId, ObjectId.FromRaw(etp.idBuffer()));

			etp.back(1);
			Assert.IsTrue(etp.first());
			Assert.IsTrue(etp.eof());
			Assert.AreEqual(ObjectId.ZeroId, ObjectId.FromRaw(etp.idBuffer()));
		}

		[Test]
		public virtual void testStopWalkCallsParent()
		{
			bool[] called = new bool[1];
			Assert.IsFalse(called[0]);

			// [ammachado]: Anonymous inner classes are not convertable to .NET:
			EmptyTreeIterator parent = new AnonymousTreeIterator(called);


			parent.createSubtreeIterator(db).stopWalk();
			Assert.IsTrue(called[0]);
		}

		class AnonymousTreeIterator : EmptyTreeIterator
		{
			private readonly bool[] called;

			public AnonymousTreeIterator(bool[] called)
			{
				this.called = called;
			}

			public override void stopWalk()
			{
				called[0] = true;
			}
		}
	}
}
