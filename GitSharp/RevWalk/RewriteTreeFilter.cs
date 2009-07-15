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

using GitSharp.RevWalk.Filter;
using GitSharp.TreeWalk.Filter;
using System;
namespace GitSharp.RevWalk
{



    /**
     * First phase of a path limited revision walk.
     * <p>
     * This filter is ANDed to evaluate after all other filters and ties the
     * configured {@link TreeFilter} into the revision walking process.
     * <p>
     * Each commit is differenced concurrently against all of its parents to look
     * for tree entries that are interesting to the TreeFilter. If none are found
     * the commit is colored with {@link RevWalk#REWRITE}, allowing a later pass
     * implemented by {@link RewriteGenerator} to remove those colored commits from
     * the DAG.
     * 
     * @see RewriteGenerator
     */
    public class RewriteTreeFilter : RevFilter
    {
        private static int PARSED = RevWalk.PARSED;

        private static int UNINTERESTING = RevWalk.UNINTERESTING;

        private static int REWRITE = RevWalk.REWRITE;

        private TreeWalk.TreeWalk pathFilter;

        public RewriteTreeFilter(RevWalk walker, TreeFilter t)
        {
            pathFilter = new TreeWalk.TreeWalk(walker.db);
            pathFilter.setFilter(t);
            pathFilter.setRecursive(t.shouldBeRecursive());
        }

        public override RevFilter Clone()
        {
            throw new InvalidOperationException();
        }

        public override bool include(RevWalk walker, RevCommit c)
        {
            // Reset the tree filter to scan this commit and parents.
            //
            RevCommit[] pList = c.parents;
            int nParents = pList.Length;
            TreeWalk.TreeWalk tw = pathFilter;
            ObjectId[] trees = new ObjectId[nParents + 1];
            for (int i = 0; i < nParents; i++)
            {
                RevCommit p = c.parents[i];
                if ((p.flags & PARSED) == 0)
                    p.parse(walker);
                trees[i] = p.getTree();
            }
            trees[nParents] = c.getTree();
            tw.reset(trees);

            if (nParents == 1)
            {
                // We have exactly one parent. This is a very common case.
                //
                int chgs = 0, adds = 0;
                while (tw.next())
                {
                    chgs++;
                    if (tw.getRawMode(0) == 0 && tw.getRawMode(1) != 0)
                        adds++;
                    else
                        break; // no point in looking at this further.
                }

                if (chgs == 0)
                {
                    // No changes, so our tree is effectively the same as
                    // our parent tree. We pass the buck to our parent.
                    //
                    c.flags |= REWRITE;
                    return false;
                }
                else
                {
                    // We have interesting items, but neither of the special
                    // cases denoted above.
                    //
                    return true;
                }
            }
            else if (nParents == 0)
            {
                // We have no parents to compare against. Consider us to be
                // REWRITE only if we have no paths matching our filter.
                //
                if (tw.next())
                    return true;
                c.flags |= REWRITE;
                return false;
            }

            // We are a merge commit. We can only be REWRITE if we are same
            // to _all_ parents. We may also be able to eliminate a parent if
            // it does not contribute changes to us. Such a parent may be an
            // uninteresting side branch.
            //
            int[] chgs_ = new int[nParents];
            int[] adds_ = new int[nParents];
            while (tw.next())
            {
                int myMode = tw.getRawMode(nParents);
                for (int i = 0; i < nParents; i++)
                {
                    int pMode = tw.getRawMode(i);
                    if (myMode == pMode && tw.idEqual(i, nParents))
                        continue;

                    chgs_[i]++;
                    if (pMode == 0 && myMode != 0)
                        adds_[i]++;
                }
            }

            bool same = false;
            bool diff = false;
            for (int i = 0; i < nParents; i++)
            {
                if (chgs_[i] == 0)
                {
                    // No changes, so our tree is effectively the same as
                    // this parent tree. We pass the buck to only this one
                    // parent commit.
                    //

                    RevCommit p = pList[i];
                    if ((p.flags & UNINTERESTING) != 0)
                    {
                        // This parent was marked as not interesting by the
                        // application. We should look for another parent
                        // that is interesting.
                        //
                        same = true;
                        continue;
                    }

                    c.flags |= REWRITE;
                    c.parents = new RevCommit[] { p };
                    return false;
                }

                if (chgs_[i] == adds_[i])
                {
                    // All of the differences from this parent were because we
                    // added files that they did not have. This parent is our
                    // "empty tree root" and thus their history is not relevant.
                    // Cut our grandparents to be an empty list.
                    //
                    pList[i].parents = RevCommit.NO_PARENTS;
                }

                // We have an interesting difference relative to this parent.
                //
                diff = true;
            }

            if (diff && !same)
            {
                // We did not abort above, so we are different in at least one
                // way from all of our parents. We have to take the blame for
                // that difference.
                //
                return true;
            }

            // We are the same as all of our parents. We must keep them
            // as they are and allow those parents to flow into pending
            // for further scanning.
            //
            c.flags |= REWRITE;
            return false;
        }
    }
}
