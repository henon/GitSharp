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

using GitSharp.Core.TreeWalk;
using GitSharp.Core.TreeWalk.Filter;
using NUnit.Framework;

namespace GitSharp.Tests.TreeWalk.Filter
{
    [TestFixture]
    public class TreeFilterTest : RepositoryTestCase
    {

        [Test]
        public void testALL_IncludesAnything()
        {
            GitSharp.Core.TreeWalk.TreeWalk tw = new GitSharp.Core.TreeWalk.TreeWalk(db);
            Assert.IsTrue(TreeFilter.ALL.include(tw));
        }

        [Test]
        public void testALL_ShouldNotBeRecursive()
        {
            Assert.IsFalse(TreeFilter.ALL.shouldBeRecursive());
        }

        [Test]
        public void testALL_IdentityClone()
        {
            Assert.AreSame(TreeFilter.ALL, TreeFilter.ALL.Clone());
        }

        [Test]
        public void testNotALL_IncludesNothing()
        {
            GitSharp.Core.TreeWalk.TreeWalk tw = new GitSharp.Core.TreeWalk.TreeWalk(db);
            Assert.IsFalse(TreeFilter.ALL.negate().include(tw));
        }

        [Test]
        public void testANY_DIFF_IncludesSingleTreeCase()
        {
            GitSharp.Core.TreeWalk.TreeWalk tw = new GitSharp.Core.TreeWalk.TreeWalk(db);
            Assert.IsTrue(TreeFilter.ANY_DIFF.include(tw));
        }

        [Test]
        public void testANY_DIFF_ShouldNotBeRecursive()
        {
            Assert.IsFalse(TreeFilter.ANY_DIFF.shouldBeRecursive());
        }

        [Test]
        public void testANY_DIFF_IdentityClone()
        {
            Assert.AreSame(TreeFilter.ANY_DIFF, TreeFilter.ANY_DIFF.Clone());
        }

    }
}