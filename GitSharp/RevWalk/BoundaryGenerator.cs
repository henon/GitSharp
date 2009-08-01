/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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


    public class BoundaryGenerator : Generator
    {
        public static int UNINTERESTING = RevWalk.UNINTERESTING;

        public Generator g;

        public BoundaryGenerator(RevWalk w, Generator s)
        {
            g = new InitialGenerator(w, s, this);
        }

        public override int outputType()
        {
            return g.outputType() | HAS_UNINTERESTING;
        }

        public override void shareFreeList(BlockRevQueue q)
        {
            g.shareFreeList(q);
        }

        public override RevCommit next()
        {
            return g.next();
        }

        private class InitialGenerator : Generator
        {
            private static int PARSED = RevWalk.PARSED;

            private static int DUPLICATE = RevWalk.TEMP_MARK;

            private RevWalk walk;

            private FIFORevQueue held;

            private Generator source;
            private BoundaryGenerator parent;

            public InitialGenerator(RevWalk w, Generator s, BoundaryGenerator parent) // [henon] parent needed because we cannot access outer instances in C#
            {
                walk = w;
                held = new FIFORevQueue();
                source = s;
                source.shareFreeList(held);
                this.parent = parent;
            }

            public override int outputType()
            {
                return source.outputType();
            }

            public override void shareFreeList(BlockRevQueue q)
            {
                q.shareFreeList(held);
            }

            public override RevCommit next()
            {
                RevCommit c = source.next();
                if (c != null)
                {
                    foreach (RevCommit p in c.parents)
                        if ((p.flags & UNINTERESTING) != 0)
                            held.add(p);
                    return c;
                }

                FIFORevQueue boundary = new FIFORevQueue();
                boundary.shareFreeList(held);
                for (; ; )
                {
                    c = held.next();
                    if (c == null)
                        break;
                    if ((c.flags & DUPLICATE) != 0)
                        continue;
                    if ((c.flags & PARSED) == 0)
                        c.parse(walk);
                    c.flags |= DUPLICATE;
                    boundary.add(c);
                }
                boundary.removeFlag(DUPLICATE);
                parent.g = boundary;
                return boundary.next();
            }
        }
    }
}
