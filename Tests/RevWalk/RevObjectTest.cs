/*
 * Copyright (C) 2009, Google Inc.
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

using GitSharp.RevWalk;
using Xunit;

namespace GitSharp.Tests.RevWalk
{
	public class RevObjectTest : RevWalkTestCase
	{
		[Fact]
		public void testId()
		{
			RevCommit a = commit();
			Assert.Same(a, a.getId());
		}

		[Fact]
		public void testEqualsIsIdentity()
		{
			RevCommit a1 = commit();
			RevCommit b1 = commit();

			Assert.True(a1.Equals(a1));
			Assert.True(a1.Equals((object)a1));
			Assert.False(a1.Equals(b1));

			Assert.False(a1.Equals(a1.Copy()));
			Assert.False(a1.Equals((object)a1.Copy()));
			Assert.False(a1.Equals(string.Empty));

			var rw2 = new GitSharp.RevWalk.RevWalk(db);
			RevCommit a2 = rw2.parseCommit(a1);
			RevCommit b2 = rw2.parseCommit(b1);
			Assert.NotSame(a1, a2);
			Assert.NotSame(b1, b2);

			Assert.False(a1.Equals(a2));
			Assert.False(b1.Equals(b2));

			Assert.Equal(a1.GetHashCode(), a2.GetHashCode());
			Assert.Equal(b1.GetHashCode(), b2.GetHashCode());

			Assert.True(Equals(a1, a2));
			Assert.True(Equals(b1, b2));
		}

		[Fact]
		public void testRevObjectTypes()
		{
			Assert.Equal(Constants.OBJ_TREE, emptyTree.Type);
			Assert.Equal(Constants.OBJ_COMMIT, commit().Type);
			Assert.Equal(Constants.OBJ_BLOB, blob("").Type);
			Assert.Equal(Constants.OBJ_TAG, tag("emptyTree", emptyTree).Type);
		}

		[Fact]
		public void testHasRevFlag()
		{
			RevCommit a = commit();
			Assert.False(a.has(RevFlag.UNINTERESTING));
			a.Flags |= GitSharp.RevWalk.RevWalk.UNINTERESTING;
			Assert.True(a.has(RevFlag.UNINTERESTING));
		}

		[Fact]
		public void testHasAnyFlag()
		{
			RevCommit a = commit();
			RevFlag flag1 = rw.newFlag("flag1");
			RevFlag flag2 = rw.newFlag("flag2");
			var s = new RevFlagSet { flag1, flag2 };

			Assert.False(a.hasAny(s));
			a.Flags |= flag1.Mask;
			Assert.True(a.hasAny(s));
		}

		[Fact]
		public void testHasAllFlag()
		{
			RevCommit a = commit();
			RevFlag flag1 = rw.newFlag("flag1");
			RevFlag flag2 = rw.newFlag("flag2");
			var s = new RevFlagSet { flag1, flag2 };

			Assert.False(a.hasAll(s));
			a.Flags |= flag1.Mask;
			Assert.False(a.hasAll(s));
			a.Flags |= flag2.Mask;
			Assert.True(a.hasAll(s));
		}

		[Fact]
		public void testAddRevFlag()
		{
			RevCommit a = commit();
			RevFlag flag1 = rw.newFlag("flag1");
			RevFlag flag2 = rw.newFlag("flag2");
			Assert.Equal(0, a.Flags);

			a.add(flag1);
			Assert.Equal(flag1.Mask, a.Flags);

			a.add(flag2);
			Assert.Equal(flag1.Mask | flag2.Mask, a.Flags);
		}

		[Fact]
		public void testAddRevFlagSet()
		{
			RevCommit a = commit();
			RevFlag flag1 = rw.newFlag("flag1");
			RevFlag flag2 = rw.newFlag("flag2");
			var s = new RevFlagSet { flag1, flag2 };

			Assert.Equal(0, a.Flags);

			a.add(s);
			Assert.Equal(flag1.Mask | flag2.Mask, a.Flags);
		}

		[Fact]
		public void testRemoveRevFlag()
		{
			RevCommit a = commit();
			RevFlag flag1 = rw.newFlag("flag1");
			RevFlag flag2 = rw.newFlag("flag2");
			a.add(flag1);
			a.add(flag2);
			Assert.Equal(flag1.Mask | flag2.Mask, a.Flags);
			a.remove(flag2);
			Assert.Equal(flag1.Mask, a.Flags);
		}

		[Fact]
		public void testRemoveRevFlagSet()
		{
			RevCommit a = commit();
			RevFlag flag1 = rw.newFlag("flag1");
			RevFlag flag2 = rw.newFlag("flag2");
			RevFlag flag3 = rw.newFlag("flag3");
			var s = new RevFlagSet { flag1, flag2 };
			a.add(flag3);
			a.add(s);
			Assert.Equal(flag1.Mask | flag2.Mask | flag3.Mask, a.Flags);
			a.remove(s);
			Assert.Equal(flag3.Mask, a.Flags);
		}
	}
}
