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

        [Test]
        public void testWrap()
        {
            GitSharp.TreeWalk.TreeWalk tw = new GitSharp.TreeWalk.TreeWalk(db);
            TreeFilter a = TreeFilter.ALL;
            TreeFilter n = NotTreeFilter.create(a);
            Assert.IsNotNull(n);
            Assert.IsTrue(a.include(tw));
            Assert.IsFalse(n.include(tw));
        }

        [Test]
        public void testNegateIsUnwrap()
        {
            TreeFilter a = PathFilter.create("a/b");
            TreeFilter n = NotTreeFilter.create(a);
            Assert.AreSame(a, n.negate());
        }

        [Test]
        public void testShouldBeRecursive_ALL()
        {
            TreeFilter a = TreeFilter.ALL;
            TreeFilter n = NotTreeFilter.create(a);
            Assert.AreEqual(a.shouldBeRecursive(), n.shouldBeRecursive());
        }

        [Test]
        public void testShouldBeRecursive_PathFilter()
        {
            TreeFilter a = PathFilter.create("a/b");
            Assert.IsTrue(a.shouldBeRecursive());
            TreeFilter n = NotTreeFilter.create(a);
            Assert.IsTrue(n.shouldBeRecursive());
        }

        [Test]
        public void testCloneIsDeepClone()
        {
            TreeFilter a = new AlwaysCloneTreeFilter();
            Assert.AreNotSame(a, a.Clone());
            TreeFilter n = NotTreeFilter.create(a);
            Assert.AreNotSame(n, n.Clone());
        }

        [Test]
        public void testCloneIsSparseWhenPossible()
        {
            TreeFilter a = TreeFilter.ALL;
            Assert.AreSame(a, a.Clone());
            TreeFilter n = NotTreeFilter.create(a);
            Assert.AreSame(n, n.Clone());
        }
    }
}
