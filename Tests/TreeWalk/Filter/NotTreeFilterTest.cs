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

using GitSharp.TreeWalk;
using GitSharp.TreeWalk.Filter;
namespace GitSharp.Tests.TreeWalk
{
    using NUnit.Framework;
    [TestFixture]
    public class NotTreeFilterTest : RepositoryTestCase
    {
#if false
	public void testWrap() throws Exception {
		final TreeWalk tw = new TreeWalk(db);
		final TreeFilter a = TreeFilter.ALL;
		final TreeFilter n = NotTreeFilter.create(a);
		assertNotNull(n);
		assertTrue(a.include(tw));
		assertFalse(n.include(tw));
	}

	public void testNegateIsUnwrap() throws Exception {
		final TreeFilter a = PathFilter.create("a/b");
		final TreeFilter n = NotTreeFilter.create(a);
		assertSame(a, n.negate());
	}

	public void testShouldBeRecursive_ALL() throws Exception {
		final TreeFilter a = TreeFilter.ALL;
		final TreeFilter n = NotTreeFilter.create(a);
		assertEquals(a.shouldBeRecursive(), n.shouldBeRecursive());
	}

	public void testShouldBeRecursive_PathFilter() throws Exception {
		final TreeFilter a = PathFilter.create("a/b");
		assertTrue(a.shouldBeRecursive());
		final TreeFilter n = NotTreeFilter.create(a);
		assertTrue(n.shouldBeRecursive());
	}

	public void testCloneIsDeepClone() throws Exception {
		final TreeFilter a = new AlwaysCloneTreeFilter();
		assertNotSame(a, a.clone());
		final TreeFilter n = NotTreeFilter.create(a);
		assertNotSame(n, n.clone());
	}

	public void testCloneIsSparseWhenPossible() throws Exception {
		final TreeFilter a = TreeFilter.ALL;
		assertSame(a, a.clone());
		final TreeFilter n = NotTreeFilter.create(a);
		assertSame(n, n.clone());
	}
#endif
    }
}
