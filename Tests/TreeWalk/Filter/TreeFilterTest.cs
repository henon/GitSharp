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
	public class TreeFilterTest : RepositoryTestCase
	{
		[StrictFactAttribute]
		public void testALL_IncludesAnything()
		{
			var tw = new GitSharp.TreeWalk.TreeWalk(db);
			Assert.True(TreeFilter.ALL.include(tw));
		}

		[StrictFactAttribute]
		public void testALL_ShouldNotBeRecursive()
		{
			Assert.False(TreeFilter.ALL.shouldBeRecursive());
		}

		[StrictFactAttribute]
		public void testALL_IdentityClone()
		{
			Assert.Same(TreeFilter.ALL, TreeFilter.ALL.Clone());
		}

		[StrictFactAttribute]
		public void testNotALL_IncludesNothing()
		{
			var tw = new GitSharp.TreeWalk.TreeWalk(db);
			Assert.False(TreeFilter.ALL.negate().include(tw));
		}

		[StrictFactAttribute]
		public void testANY_DIFF_IncludesSingleTreeCase()
		{
			var tw = new GitSharp.TreeWalk.TreeWalk(db);
			Assert.True(TreeFilter.ANY_DIFF.include(tw));
		}

		[StrictFactAttribute]
		public void testANY_DIFF_ShouldNotBeRecursive()
		{
			Assert.False(TreeFilter.ANY_DIFF.shouldBeRecursive());
		}

		[StrictFactAttribute]
		public void testANY_DIFF_IdentityClone()
		{
			Assert.Same(TreeFilter.ANY_DIFF, TreeFilter.ANY_DIFF.Clone());
		}
	}
}