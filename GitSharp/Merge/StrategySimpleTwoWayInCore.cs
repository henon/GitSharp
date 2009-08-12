/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Dan Rigby <dan@danrigby.com>
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

using System;
using GitSharp.DirectoryCache;
using GitSharp.Exceptions;
using GitSharp.TreeWalk;

namespace GitSharp.Merge
{
    /**
     * Merges two commits together in-memory, ignoring any working directory.
     * <p>
     * The strategy chooses a path from one of the two input trees if the path is
     * unchanged in the other relative to their common merge base tree. This is a
     * trivial 3-way merge (at the file path level only).
     * <p>
     * Modifications of the same file path (content and/or file mode) by both input
     * trees will cause a merge conflict, as this strategy does not attempt to merge
     * file contents.
     */
    public class StrategySimpleTwoWayInCore : ThreeWayMergeStrategy
    {
        /** Create a new instance of the strategy. */
	    public StrategySimpleTwoWayInCore() 
        {
		    //
	    }

	    public override String getName() 
        {
		    return "simple-two-way-in-core";
	    }

	    public override Merger newMerger(Repository db) 
        {
		    return new InCoreMerger(db);
	    }

	    private class InCoreMerger : ThreeWayMerger 
        {
		    private const int T_BASE = 0;

		    private const int T_OURS = 1;

		    private const int T_THEIRS = 2;

		    private readonly NameConflictTreeWalk tw;

		    private readonly DirCache cache;

		    private DirCacheBuilder builder;

		    private ObjectId resultTree;

		    public InCoreMerger(Repository local) : base(local)
            {
			    tw = new NameConflictTreeWalk(db);
			    cache = DirCache.newInCore();
		    }

		    protected override bool mergeImpl()
            {
			    tw.reset();
			    tw.addTree(mergeBase());
			    tw.addTree(sourceTrees[0]);
			    tw.addTree(sourceTrees[1]);

			    bool hasConflict = false;
			    builder = cache.builder();
			    while (tw.next()) 
                {
				    int modeO = tw.getRawMode(T_OURS);
				    int modeT = tw.getRawMode(T_THEIRS);
				    if (modeO == modeT && tw.idEqual(T_OURS, T_THEIRS)) 
                    {
					    add(T_OURS, DirCacheEntry.STAGE_0);
					    continue;
				    }

				    int modeB = tw.getRawMode(T_BASE);
				    if (modeB == modeO && tw.idEqual(T_BASE, T_OURS))
					    add(T_THEIRS, DirCacheEntry.STAGE_0);
				    else if (modeB == modeT && tw.idEqual(T_BASE, T_THEIRS))
					    add(T_OURS, DirCacheEntry.STAGE_0);
				    else if (tw.isSubtree()) 
                    {
					    if (nonTree(modeB)) 
                        {
						    add(T_BASE, DirCacheEntry.STAGE_1);
						    hasConflict = true;
					    }
					    if (nonTree(modeO)) 
                        {
						    add(T_OURS, DirCacheEntry.STAGE_2);
						    hasConflict = true;
					    }
					    if (nonTree(modeT)) 
                        {
						    add(T_THEIRS, DirCacheEntry.STAGE_3);
						    hasConflict = true;
					    }
					    tw.enterSubtree();
				    } 
                    else 
                    {
					    add(T_BASE, DirCacheEntry.STAGE_1);
					    add(T_OURS, DirCacheEntry.STAGE_2);
					    add(T_THEIRS, DirCacheEntry.STAGE_3);
					    hasConflict = true;
				    }
			    }
			    builder.finish();
			    builder = null;

			    if (hasConflict)
				    return false;
			    try {
				    resultTree = cache.writeTree(getObjectWriter());
				    return true;
			    } catch (UnmergedPathException upe) {
				    resultTree = null;
				    return false;
			    }
		    }

		    private static bool nonTree(int mode) {
			    return mode != 0 && !FileMode.Tree.Equals(mode);
		    }

		    private void add(int tree, int stage)
            {
			    AbstractTreeIterator i = getTree(tree);
			    if (i != null) 
                {
				    if (FileMode.Tree.Equals(tw.getRawMode(tree))) 
                    {
					    builder.addTree(tw.getRawPath(), stage, db, tw
							    .getObjectId(tree));
				    } 
                    else 
                    {
					    DirCacheEntry e;

					    e = new DirCacheEntry(tw.getRawPath(), stage);
					    e.setObjectIdFromRaw(i.idBuffer(), i.idOffset());
					    e.setFileMode(tw.getFileMode(tree));
					    builder.add(e);
				    }
			    }
		    }

		    private AbstractTreeIterator getTree(int tree) 
            {
                return tw.getTree<AbstractTreeIterator>(tree, typeof(AbstractTreeIterator));
		    }

		    public override ObjectId getResultTreeId() {
			    return resultTree;
		    }
	    }
    }
}