/*
 * Copyright (C) 2009, Google Inc.
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

namespace GitSharp.RevWalk
{


    /**
     * Filters out commits marked {@link RevWalk#UNINTERESTING}.
     * <p>
     * This generator is only in front of another generator that has fully buffered
     * commits, such that we are called only after the {@link PendingGenerator} has
     * exhausted its input queue and given up. It skips over any uninteresting
     * commits that may have leaked out of the PendingGenerator due to clock skew
     * being detected in the commit objects.
     */
    class FixUninterestingGenerator : Generator
    {
        private Generator pending;

        FixUninterestingGenerator(Generator g)
        {
            pending = g;
        }

        public override int outputType()
        {
            return pending.outputType();
        }

        public override RevCommit next()
        {
            for (; ; )
            {
                RevCommit c = pending.next();
                if (c == null)
                    return null;
                if ((c.flags & RevWalk.UNINTERESTING) == 0)
                    return c;
            }
        }
    }
}