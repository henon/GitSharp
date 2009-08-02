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


    /** Sorts commits in topological order. */
    public class TopoSortGenerator : Generator
    {
        private static int TOPO_DELAY = RevWalk.TOPO_DELAY;

        private FIFORevQueue pending;

        private int _outputType;

        /**
         * Create a new sorter and completely spin the generator.
         * <p>
         * When the constructor completes the supplied generator will have no
         * commits remaining, as all of the commits will be held inside of this
         * generator's internal buffer.
         * 
         * @param s
         *            generator to pull all commits out of, and into this buffer.
         * @throws MissingObjectException
         * @throws IncorrectObjectTypeException
         * @
         */
        public TopoSortGenerator(Generator s)
        {
            pending = new FIFORevQueue();
            _outputType = s.outputType() | SORT_TOPO;
            s.shareFreeList(pending);
            for (; ; )
            {
                RevCommit c = s.next();
                if (c == null)
                    break;
                foreach (RevCommit p in c.parents)
                    p.inDegree++;
                pending.add(c);
            }
        }

        public override int outputType()
        {
            return _outputType;
        }

        public override void shareFreeList(BlockRevQueue q)
        {
            q.shareFreeList(pending);
        }

        public override RevCommit next()
        {
            for (; ; )
            {
                RevCommit c = pending.next();
                if (c == null)
                    return null;

                if (c.inDegree > 0)
                {
                    // At least one of our children is missing. We delay
                    // production until all of our children are output.
                    //
                    c.flags |= TOPO_DELAY;
                    continue;
                }

                // All of our children have already produced,
                // so it is OK for us to produce now as well.
                //
                foreach (RevCommit p in c.parents)
                {
                    if (--p.inDegree == 0 && (p.flags & TOPO_DELAY) != 0)
                    {
                        // This parent tried to come before us, but we are
                        // his last child. unpop the parent so it goes right
                        // behind this child.
                        //
                        p.flags &= ~TOPO_DELAY;
                        pending.unpop(p);
                    }
                }
                return c;
            }
        }
    }
}
