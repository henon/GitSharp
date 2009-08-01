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


    /**
     * Replaces a RevCommit's parents until not colored with REWRITE.
     * <p>
     * Before a RevCommit is returned to the caller its parents are updated to
     * create a dense DAG. Instead of reporting the actual parents as recorded when
     * the commit was created the returned commit will reflect the next closest
     * commit that matched the revision walker's filters.
     * <p>
     * This generator is the second phase of a path limited revision walk and
     * assumes it is receiving RevCommits from {@link RewriteTreeFilter},
     * after they have been fully buffered by {@link AbstractRevQueue}. The full
     * buffering is necessary to allow the simple loop used within our own
     * {@link #rewrite(RevCommit)} to pull completely through a strand of
     * {@link RevWalk#REWRITE} colored commits and come up with a simplification
     * that makes the DAG dense. Not fully buffering the commits first would cause
     * this loop to abort early, due to commits not being parsed and colored
     * correctly.
     * 
     * @see RewriteTreeFilter
     */
    public class RewriteGenerator : Generator
    {
        private static int REWRITE = RevWalk.REWRITE;

        /** For {@link #cleanup(RevCommit[])} to remove duplicate parents. */
        private static int DUPLICATE = RevWalk.TEMP_MARK;

        private Generator source;

        public RewriteGenerator(Generator s)
        {
            source = s;
        }

        public override void shareFreeList(BlockRevQueue q)
        {
            source.shareFreeList(q);
        }

        public override int outputType()
        {
            return source.outputType() & ~NEEDS_REWRITE;
        }

        public override RevCommit next()
        {
            for (; ; )
            {
                RevCommit c = source.next();
                if (c == null)
                    return null;

                bool rewrote = false;
                RevCommit[] pList = c.parents;
                int nParents = pList.Length;
                for (int i = 0; i < nParents; i++)
                {
                    RevCommit oldp = pList[i];
                    RevCommit newp = rewrite(oldp);
                    if (oldp != newp)
                    {
                        pList[i] = newp;
                        rewrote = true;
                    }
                }
                if (rewrote)
                    c.parents = cleanup(pList);

                return c;
            }
        }

        private RevCommit rewrite(RevCommit p)
        {
            for (; ; )
            {
                RevCommit[] pList = p.parents;
                if (pList.Length > 1)
                {
                    // This parent is a merge, so keep it.
                    //
                    return p;
                }

                if ((p.flags & RevWalk.UNINTERESTING) != 0)
                {
                    // Retain uninteresting parents. They show where the
                    // DAG was cut off because it wasn't interesting.
                    //
                    return p;
                }

                if ((p.flags & REWRITE) == 0)
                {
                    // This parent was not eligible for rewriting. We
                    // need to keep it in the DAG.
                    //
                    return p;
                }

                if (pList.Length == 0)
                {
                    // We can't go back any further, other than to
                    // just delete the parent entirely.
                    //
                    return null;
                }

                p = pList[0];
            }
        }

        private RevCommit[] cleanup(RevCommit[] oldList)
        {
            // Remove any duplicate parents caused due to rewrites (e.g. a merge
            // with two sides that both simplified back into the merge base).
            // We also may have deleted a parent by marking it null.
            //
            int newCnt = 0;
            for (int o = 0; o < oldList.Length; o++)
            {
                RevCommit p = oldList[o];
                if (p == null)
                    continue;
                if ((p.flags & DUPLICATE) != 0)
                {
                    oldList[o] = null;
                    continue;
                }
                p.flags |= DUPLICATE;
                newCnt++;
            }

            if (newCnt == oldList.Length)
            {
                foreach (RevCommit p in oldList)
                    p.flags &= ~DUPLICATE;
                return oldList;
            }

            RevCommit[] newList = new RevCommit[newCnt];
            newCnt = 0;
            foreach (RevCommit p in oldList)
            {
                if (p != null)
                {
                    newList[newCnt++] = p;
                    p.flags &= ~DUPLICATE;
                }
            }

            return newList;
        }
    }
}
