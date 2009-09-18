/*
 * Copyright (C) 2008, Google Inc.
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

using GitSharp.Tests.Util;
using GitSharp.TreeWalk.Filter;
using Xunit;

namespace GitSharp.Tests.TreeWalk.Filter
{
	public class NotTreeFilterTest : RepositoryTestCase
	{
		[StrictFactAttribute]
		public void testWrap()
		{
			var tw = new GitSharp.TreeWalk.TreeWalk(db);
			TreeFilter a = TreeFilter.ALL;
			TreeFilter n = NotTreeFilter.create(a);
			Assert.NotNull(n);
			Assert.True(a.include(tw));
			Assert.False(n.include(tw));
		}

		[StrictFactAttribute]
		public void testNegateIsUnwrap()
		{
			TreeFilter a = PathFilter.create("a/b");
			TreeFilter n = NotTreeFilter.create(a);
			Assert.Same(a, n.negate());
		}

		[StrictFactAttribute]
		public void testShouldBeRecursive_ALL()
		{
			TreeFilter a = TreeFilter.ALL;
			TreeFilter n = NotTreeFilter.create(a);
			Assert.Equal(a.shouldBeRecursive(), n.shouldBeRecursive());
		}

		[StrictFactAttribute]
		public void testShouldBeRecursive_PathFilter()
		{
			TreeFilter a = PathFilter.create("a/b");
			Assert.True(a.shouldBeRecursive());
			TreeFilter n = NotTreeFilter.create(a);
			Assert.True(n.shouldBeRecursive());
		}

		[StrictFactAttribute]
		public void testCloneIsDeepClone()
		{
			TreeFilter a = new AlwaysCloneTreeFilter();
			Assert.NotSame(a, a.Clone());
			TreeFilter n = NotTreeFilter.create(a);
			Assert.NotSame(n, n.Clone());
		}

		[StrictFactAttribute]
		public void testCloneIsSparseWhenPossible()
		{
			TreeFilter a = TreeFilter.ALL;
			Assert.Same(a, a.Clone());
			TreeFilter n = NotTreeFilter.create(a);
			Assert.Same(n, n.Clone());
		}
	}
}