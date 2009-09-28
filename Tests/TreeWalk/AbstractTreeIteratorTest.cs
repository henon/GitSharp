/*
 * Copyright (C) 2009, Tor Arne Vestbø <torarnv@gmail.com>
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

using GitSharp.Core;
using GitSharp.Core.TreeWalk;
using NUnit.Framework;

namespace GitSharp.Tests.TreeWalk
{
    [TestFixture]
    public class AbstractTreeIteratorTest
    {
        public class FakeTreeIterator : WorkingTreeIterator
        {
            public FakeTreeIterator(string path, FileMode fileMode)
                : base(path)
            {
                Mode = fileMode.Bits;
                PathLen -= 1; // Get rid of extra '/'
            }

            public override AbstractTreeIterator createSubtreeIterator(Core.Repository repo)
            {
                return null;
            }
        }

		[Test]
        public void testPathCompare()
        {
            Assert.IsTrue(new FakeTreeIterator("a", FileMode.RegularFile).pathCompare(new FakeTreeIterator("a", FileMode.Tree)) < 0);
            Assert.IsTrue(new FakeTreeIterator("a", FileMode.Tree).pathCompare(new FakeTreeIterator("a", FileMode.RegularFile)) > 0);
            Assert.IsTrue(new FakeTreeIterator("a", FileMode.RegularFile).pathCompare(new FakeTreeIterator("a", FileMode.RegularFile)) == 0);
            Assert.IsTrue(new FakeTreeIterator("a", FileMode.Tree).pathCompare(new FakeTreeIterator("a", FileMode.Tree)) == 0);
        }
    }
}
