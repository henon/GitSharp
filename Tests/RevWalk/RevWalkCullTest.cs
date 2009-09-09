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
    public class RevWalkCullTest : RevWalkTestCase
    {
        [Fact]
        public void testProperlyCullAllAncestors1()
        {
            // Credit goes to Junio C Hamano <gitster@pobox.com> for this
            // test case in git-core (t/t6009-rev-list-parent.sh)
            //
            // We induce a clock skew so two is dated before one.
            //
            RevCommit a = commit();
            RevCommit b = commit(-2400, a);
            RevCommit c = commit(b);
            RevCommit d = commit(c);

            markStart(a);
            markUninteresting(d);
            Assert.Null(rw.next());
        }

        [Fact]
        public void testProperlyCullAllAncestors2()
        {
            // Despite clock skew on c1 being very old it should not
            // produce, neither should a or b, or any part of that chain.
            //
            RevCommit a = commit();
            RevCommit b = commit(a);
            RevCommit c1 = commit(-5, b);
            RevCommit c2 = commit(10, b);
            RevCommit d = commit(c1, c2);

            markStart(d);
            markUninteresting(c1);
            assertCommit(d, rw.next());
            assertCommit(c2, rw.next());
            Assert.Null(rw.next());
        }

        [Fact]
        public void testProperlyCullAllAncestors_LongHistory()
        {
            RevCommit a = commit();
            RevCommit b = commit(a);
            for (int i = 0; i < 24; i++)
            {
                b = commit(b);
                if ((i & 2) == 0)
                    markUninteresting(b);
            }
            RevCommit c = commit(b);

            markStart(c);
            markUninteresting(b);
            assertCommit(c, rw.next());
            Assert.Null(rw.next());

            // We should have aborted before we got back so far that "a"
            // would be parsed. Thus, its parents shouldn't be allocated.
            //
            Assert.Null(a.Parents);
        }
    }
}