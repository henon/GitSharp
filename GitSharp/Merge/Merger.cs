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

using System.IO;
using GitSharp.Exceptions;
using GitSharp.RevWalk;
using GitSharp.RevWalk.Filter;
using GitSharp.TreeWalk;

namespace GitSharp.Merge
{
    /**
     * Instance of a specific {@link MergeStrategy} for a single {@link Repository}.
     */
    public abstract class Merger
    {
        /** The repository this merger operates on. */
	    protected readonly Repository Db;

	    /** A RevWalk for computing merge bases, or listing incoming commits. */
        protected readonly RevWalk.RevWalk Walk;

	    private ObjectWriter _writer;

	    /** The original objects supplied in the merge; this can be any tree-ish. */
	    protected RevObject[] SourceObjects;

	    /** If {@link #sourceObjects}[i] is a commit, this is the commit. */
	    protected RevCommit[] SourceCommits;

	    /** The trees matching every entry in {@link #sourceObjects}. */
	    protected RevTree[] SourceTrees;

        /**
	     * Create a new merge instance for a repository.
	     *
	     * @param local
	     *            the repository this merger will Read and write data on.
	     */
	    protected Merger(Repository local) 
        {
		    Db = local;
            Walk = new RevWalk.RevWalk(Db);
	    }

        /**
	     * @return the repository this merger operates on.
	     */
	    public Repository GetRepository() 
        {
		    return Db;
	    }

	    /**
	     * @return an object writer to Create objects in {@link #getRepository()}.
	     */
	    public ObjectWriter GetObjectWriter() 
        {
		    if (_writer == null)
			    _writer = new ObjectWriter(GetRepository());
		    return _writer;
	    }

	    /**
	     * Merge together two or more tree-ish objects.
	     * <p>
	     * Any tree-ish may be supplied as inputs. Commits and/or tags pointing at
	     * trees or commits may be passed as input objects.
	     *
	     * @param tips
	     *            source trees to be combined together. The merge base is not
	     *            included in this set.
	     * @return true if the merge was completed without conflicts; false if the
	     *         merge strategy cannot handle this merge or there were conflicts
	     *         preventing it from automatically resolving all paths.
	     * @throws IncorrectObjectTypeException
	     *             one of the input objects is not a commit, but the strategy
	     *             requires it to be a commit.
	     * @throws IOException
	     *             one or more sources could not be Read, or outputs could not
	     *             be written to the Repository.
	     */
	    public virtual bool Merge(AnyObjectId[] tips)
        {
		    SourceObjects = new RevObject[tips.Length];
		    for (int i = 0; i < tips.Length; i++)
			    SourceObjects[i] = Walk.parseAny(tips[i]);

		    SourceCommits = new RevCommit[SourceObjects.Length];
		    for (int i = 0; i < SourceObjects.Length; i++) 
            {
			    try 
                {
				    SourceCommits[i] = Walk.parseCommit(SourceObjects[i]);
			    } 
                catch (IncorrectObjectTypeException) 
                {
				    SourceCommits[i] = null;
			    }
		    }

		    SourceTrees = new RevTree[SourceObjects.Length];
		    for (int i = 0; i < SourceObjects.Length; i++)
			    SourceTrees[i] = Walk.parseTree(SourceObjects[i]);

		    return MergeImpl();
	    }

	    /**
	     * Create an iterator to walk the merge base of two commits.
	     *
	     * @param aIdx
	     *            index of the first commit in {@link #sourceObjects}.
	     * @param bIdx
	     *            index of the second commit in {@link #sourceObjects}.
	     * @return the new iterator
	     * @throws IncorrectObjectTypeException
	     *             one of the input objects is not a commit.
	     * @throws IOException
	     *             objects are missing or multiple merge bases were found.
	     */
	    protected AbstractTreeIterator MergeBase(int aIdx, int bIdx)
        {
		    if (SourceCommits[aIdx] == null)
			    throw new IncorrectObjectTypeException(SourceObjects[aIdx],
					    Constants.TYPE_COMMIT);
		    if (SourceCommits[bIdx] == null)
			    throw new IncorrectObjectTypeException(SourceObjects[bIdx],
					    Constants.TYPE_COMMIT);

		    Walk.reset();
		    Walk.setRevFilter(RevFilter.MERGE_BASE);
		    Walk.markStart(SourceCommits[aIdx]);
		    Walk.markStart(SourceCommits[bIdx]);
		    RevCommit base1 = Walk.next();
		    if (base1 == null)
			    return new EmptyTreeIterator();
		    RevCommit base2 = Walk.next();
		    if (base2 != null) {
			    throw new IOException("Multiple merge bases for:" + "\n  "
					    + SourceCommits[aIdx].Name + "\n  "
					    + SourceCommits[bIdx].Name + "found:" + "\n  "
					    + base1.Name + "\n  " + base2.Name);
		    }
		    return OpenTree(base1.Tree);
	    }

	    /**
	     * Open an iterator over a tree.
	     *
	     * @param treeId
	     *            the tree to scan; must be a tree (not a treeish).
	     * @return an iterator for the tree.
	     * @throws IncorrectObjectTypeException
	     *             the input object is not a tree.
	     * @throws IOException
	     *             the tree object is not found or cannot be Read.
	     */
	    protected AbstractTreeIterator OpenTree(AnyObjectId treeId)
        {
		    var windowCursor = new WindowCursor();
		    try 
            {
			    return new CanonicalTreeParser(null, Db, treeId, windowCursor);
		    } 
            finally 
            {
			    windowCursor.Release();
		    }
	    }

	    /**
	     * Execute the merge.
	     * <p>
	     * This method is called from {@link #merge(AnyObjectId[])} After the
	     * {@link #sourceObjects}, {@link #sourceCommits} and {@link #sourceTrees}
	     * have been populated.
	     *
	     * @return true if the merge was completed without conflicts; false if the
	     *         merge strategy cannot handle this merge or there were conflicts
	     *         preventing it from automatically resolving all paths.
	     * @throws IncorrectObjectTypeException
	     *             one of the input objects is not a commit, but the strategy
	     *             requires it to be a commit.
	     * @throws IOException
	     *             one or more sources could not be Read, or outputs could not
	     *             be written to the Repository.
	     */
	    protected abstract bool MergeImpl();

	    /**
	     * @return resulting tree, if {@link #merge(AnyObjectId[])} returned true.
	     */
	    public abstract ObjectId GetResultTreeId();
    }
}