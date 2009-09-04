/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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

using GitSharp.TreeWalk.Filter;
using System;
using GitSharp.RevWalk;
using System.Collections.Generic;
using GitSharp.Exceptions;
namespace GitSharp.TreeWalk
{


    /**
     * Walks one or more {@link AbstractTreeIterator}s in parallel.
     * <p>
     * This class can perform n-way differences across as many trees as necessary.
     * <p>
     * Each tree added must have the same root as existing trees in the walk.
     * <p>
     * A TreeWalk instance can only be used once to generate results. Running a
     * second time requires creating a new TreeWalk instance, or invoking
     * {@link #reset()} and adding new trees before starting again. Resetting an
     * existing instance may be faster for some applications as some internal
     * buffers may be recycled.
     * <p>
     * TreeWalk instances are not thread-safe. Applications must either restrict
     * usage of a TreeWalk instance to a single thread, or implement their own
     * synchronization at a higher level.
     * <p>
     * Multiple simultaneous TreeWalk instances per {@link Repository} are
     * permitted, even from concurrent threads.
     */
    public class TreeWalk
    {
        /**
         * Open a tree walk and filter to exactly one path.
         * <p>
         * The returned tree walk is already positioned on the requested path, so
         * the caller should not need to invoke {@link #next()} unless they are
         * looking for a possible directory/file name conflict.
         * 
         * @param db
         *            repository to read tree object data from.
         * @param path
         *            single path to advance the tree walk instance into.
         * @param trees
         *            one or more trees to walk through, all with the same root.
         * @return a new tree walk configured for exactly this one path; null if no
         *         path was found in any of the trees.
         * @throws IOException
         *             reading a pack file or loose object failed.
         * @throws CorruptObjectException
         *             an tree object could not be read as its data stream did not
         *             appear to be a tree, or could not be inflated.
         * @throws IncorrectObjectTypeException
         *             an object we expected to be a tree was not a tree.
         * @throws MissingObjectException
         *             a tree object was not found.
         */
        public static TreeWalk forPath(Repository db, string path, params AnyObjectId[] trees)
        {
            TreeWalk r = new TreeWalk(db);
            r.setFilter(PathFilterGroup.createFromStrings(new HashSet<string>() { path }));
            r.setRecursive(r.getFilter().shouldBeRecursive());
            r.reset(trees);
            return r.next() ? r : null;
        }

        /**
         * Open a tree walk and filter to exactly one path.
         * <p>
         * The returned tree walk is already positioned on the requested path, so
         * the caller should not need to invoke {@link #next()} unless they are
         * looking for a possible directory/file name conflict.
         * 
         * @param db
         *            repository to read tree object data from.
         * @param path
         *            single path to advance the tree walk instance into.
         * @param tree
         *            the single tree to walk through.
         * @return a new tree walk configured for exactly this one path; null if no
         *         path was found in any of the trees.
         * @throws IOException
         *             reading a pack file or loose object failed.
         * @throws CorruptObjectException
         *             an tree object could not be read as its data stream did not
         *             appear to be a tree, or could not be inflated.
         * @throws IncorrectObjectTypeException
         *             an object we expected to be a tree was not a tree.
         * @throws MissingObjectException
         *             a tree object was not found.
         */
        public static TreeWalk forPath(Repository db, string path, RevTree tree)
        {
            return forPath(db, path, new ObjectId[] { tree });
        }

        private Repository db;

        private MutableObjectId idBuffer = new MutableObjectId();

        private WindowCursor curs = new WindowCursor();

        private TreeFilter filter;

        public AbstractTreeIterator[] trees;

        private bool recursive;

        private bool postOrderTraversal;

        private int depth;

        private bool advance;

        private bool postChildren;

        public AbstractTreeIterator currentHead;

        /**
         * Create a new tree walker for a given repository.
         * 
         * @param repo
         *            the repository the walker will obtain data from.
         */
        public TreeWalk(Repository repo)
        {
            db = repo;
            filter = TreeFilter.ALL;
            trees = new AbstractTreeIterator[] { new EmptyTreeIterator() };
        }

        /**
         * Get the repository this tree walker is reading from.
         * 
         * @return the repository configured when the walker was created.
         */
        public Repository getRepository()
        {
            return db;
        }

        /**
         * Get the currently configured filter.
         * 
         * @return the current filter. Never null as a filter is always needed.
         */
        public TreeFilter getFilter()
        {
            return filter;
        }

        /**
         * Set the tree entry filter for this walker.
         * <p>
         * Multiple filters may be combined by constructing an arbitrary tree of
         * <code>AndTreeFilter</code> or <code>OrTreeFilter</code> instances to
         * describe the bool expression required by the application. Custom
         * filter implementations may also be constructed by applications.
         * <p>
         * Note that filters are not thread-safe and may not be shared by concurrent
         * TreeWalk instances. Every TreeWalk must be supplied its own unique
         * filter, unless the filter implementation specifically states it is (and
         * always will be) thread-safe. Callers may use {@link TreeFilter#Clone()}
         * to create a unique filter tree for this TreeWalk instance.
         * 
         * @param newFilter
         *            the new filter. If null the special {@link TreeFilter#ALL}
         *            filter will be used instead, as it matches every entry.
         * @see org.spearce.jgit.treewalk.filter.AndTreeFilter
         * @see org.spearce.jgit.treewalk.filter.OrTreeFilter
         */
        public void setFilter(TreeFilter newFilter)
        {
            filter = newFilter != null ? newFilter : TreeFilter.ALL;
        }

        /**
         * Is this walker automatically entering into subtrees?
         * <p>
         * If the walker is recursive then the caller will not see a subtree node
         * and instead will only receive file nodes in all relevant subtrees.
         * 
         * @return true if automatically entering subtrees is enabled.
         */
        public bool isRecursive()
        {
            return recursive;
        }

        /**
         * Set the walker to enter (or not enter) subtrees automatically.
         * <p>
         * If recursive mode is enabled the walker will hide subtree nodes from the
         * calling application and will produce only file level nodes. If a tree
         * (directory) is deleted then all of the file level nodes will appear to be
         * deleted, recursively, through as many levels as necessary to account for
         * all entries.
         * 
         * @param b
         *            true to skip subtree nodes and only obtain files nodes.
         */
        public void setRecursive(bool b)
        {
            recursive = b;
        }

        /**
         * Does this walker return a tree entry after it exits the subtree?
         * <p>
         * If post order traversal is enabled then the walker will return a subtree
         * after it has returned the last entry within that subtree. This may cause
         * a subtree to be seen by the application twice if {@link #isRecursive()}
         * is false, as the application will see it once, call
         * {@link #enterSubtree()}, and then see it again as it leaves the subtree.
         * <p>
         * If an application does not enable {@link #isRecursive()} and it does not
         * call {@link #enterSubtree()} then the tree is returned only once as none
         * of the children were processed.
         *
         * @return true if subtrees are returned after entries within the subtree.
         */
        public bool isPostOrderTraversal()
        {
            return postOrderTraversal;
        }

        /**
         * Set the walker to return trees after their children.
         *
         * @param b
         *            true to get trees after their children.
         * @see #isPostOrderTraversal()
         */
        public void setPostOrderTraversal(bool b)
        {
            postOrderTraversal = b;
        }

        /** Reset this walker so new tree iterators can be added to it. */
        public void reset()
        {
            trees = new AbstractTreeIterator[0];
            advance = false;
            depth = 0;
        }

        /**
         * Reset this walker to run over a single existing tree.
         *
         * @param id
         *            the tree we need to parse. The walker will execute over this
         *            single tree if the reset is successful.
         * @throws MissingObjectException
         *             the given tree object does not exist in this repository.
         * @throws IncorrectObjectTypeException
         *             the given object id does not denote a tree, but instead names
         *             some other non-tree type of object. Note that commits are not
         *             trees, even if they are sometimes called a "tree-ish".
         * @throws CorruptObjectException
         *             the object claimed to be a tree, but its contents did not
         *             appear to be a tree. The repository may have data corruption.
         * @throws IOException
         *             a loose object or pack file could not be read.
         */
        public void reset(AnyObjectId id)
        {
            if (trees.Length == 1)
            {
                AbstractTreeIterator o = trees[0];
                while (o.parent != null)
                    o = o.parent;
                if (o is CanonicalTreeParser)
                {
                    o.matches = null;
                    o.matchShift = 0;
                    ((CanonicalTreeParser)o).reset(db, id, curs);
                    trees[0] = o;
                }
                else
                {
                    trees[0] = parserFor(id);
                }
            }
            else
            {
                trees = new AbstractTreeIterator[] { parserFor(id) };
            }

            advance = false;
            depth = 0;
        }

        /**
         * Reset this walker to run over a set of existing trees.
         * 
         * @param ids
         *            the trees we need to parse. The walker will execute over this
         *            many parallel trees if the reset is successful.
         * @throws MissingObjectException
         *             the given tree object does not exist in this repository.
         * @throws IncorrectObjectTypeException
         *             the given object id does not denote a tree, but instead names
         *             some other non-tree type of object. Note that commits are not
         *             trees, even if they are sometimes called a "tree-ish".
         * @throws CorruptObjectException
         *             the object claimed to be a tree, but its contents did not
         *             appear to be a tree. The repository may have data corruption.
         * @throws IOException
         *             a loose object or pack file could not be read.
         */
        public void reset(AnyObjectId[] ids)
        {
            int oldLen = trees.Length;
            int newLen = ids.Length;
            AbstractTreeIterator[] r = newLen == oldLen ? trees : new AbstractTreeIterator[newLen];
            for (int i = 0; i < newLen; i++)
            {
                AbstractTreeIterator o;

                if (i < oldLen)
                {
                    o = trees[i];
                    while (o.parent != null)
                        o = o.parent;
                    if (o is CanonicalTreeParser && o.pathOffset == 0)
                    {
                        o.matches = null;
                        o.matchShift = 0;
                        ((CanonicalTreeParser)o).reset(db, ids[i], curs);
                        r[i] = o;
                        continue;
                    }
                }

                o = parserFor(ids[i]);
                r[i] = o;
            }

            trees = r;
            advance = false;
            depth = 0;
        }

        /**
         * Add an already existing tree object for walking.
         * <p>
         * The position of this tree is returned to the caller, in case the caller
         * has lost track of the order they added the trees into the walker.
         * <p>
         * The tree must have the same root as existing trees in the walk.
         * 
         * @param id
         *            identity of the tree object the caller wants walked.
         * @return position of this tree within the walker.
         * @throws MissingObjectException
         *             the given tree object does not exist in this repository.
         * @throws IncorrectObjectTypeException
         *             the given object id does not denote a tree, but instead names
         *             some other non-tree type of object. Note that commits are not
         *             trees, even if they are sometimes called a "tree-ish".
         * @throws CorruptObjectException
         *             the object claimed to be a tree, but its contents did not
         *             appear to be a tree. The repository may have data corruption.
         * @throws IOException
         *             a loose object or pack file could not be read.
         */
        public int addTree(ObjectId id)
        {
            return addTree(parserFor(id));
        }

        /**
         * Add an already created tree iterator for walking.
         * <p>
         * The position of this tree is returned to the caller, in case the caller
         * has lost track of the order they added the trees into the walker.
         * <p>
         * The tree which the iterator operates on must have the same root as
         * existing trees in the walk.
         * 
         * @param p
         *            an iterator to walk over. The iterator should be new, with no
         *            parent, and should still be positioned before the first entry.
         *            The tree which the iterator operates on must have the same root
         *            as other trees in the walk.
         *
         * @return position of this tree within the walker.
         * @throws CorruptObjectException
         *             the iterator was unable to obtain its first entry, due to
         *             possible data corruption within the backing data store.
         */
        public int addTree(AbstractTreeIterator p)
        {
            int n = trees.Length;
            AbstractTreeIterator[] newTrees = new AbstractTreeIterator[n + 1];

            Array.Copy(trees, 0, newTrees, 0, n);
            newTrees[n] = p;
            p.matches = null;
            p.matchShift = 0;

            trees = newTrees;
            return n;
        }

        /**
         * Get the number of trees known to this walker.
         * 
         * @return the total number of trees this walker is iterating over.
         */
        public int getTreeCount()
        {
            return trees.Length;
        }

        /**
         * Advance this walker to the next relevant entry.
         * 
         * @return true if there is an entry available; false if all entries have
         *         been walked and the walk of this set of tree iterators is over.
         * @throws MissingObjectException
         *             {@link #isRecursive()} was enabled, a subtree was found, but
         *             the subtree object does not exist in this repository. The
         *             repository may be missing objects.
         * @throws IncorrectObjectTypeException
         *             {@link #isRecursive()} was enabled, a subtree was found, and
         *             the subtree id does not denote a tree, but instead names some
         *             other non-tree type of object. The repository may have data
         *             corruption.
         * @throws CorruptObjectException
         *             the contents of a tree did not appear to be a tree. The
         *             repository may have data corruption.
         * @throws IOException
         *             a loose object or pack file could not be read.
         */
        public bool next()
        {
            try
            {
                if (advance)
                {
                    advance = false;
                    postChildren = false;
                    popEntriesEqual();
                }

                for (; ; )
                {
                    AbstractTreeIterator t = min();
                    if (t.eof())
                    {
                        if (depth > 0)
                        {
                            exitSubtree();
                            if (postOrderTraversal)
                            {
                                advance = true;
                                postChildren = true;
                                return true;
                            }
                            popEntriesEqual();
                            continue;
                        }
                        return false;
                    }

                    currentHead = t;
                    if (!filter.include(this))
                    {
                        skipEntriesEqual();
                        continue;
                    }

                    if (recursive && FileMode.Tree.Equals(t.mode))
                    {
                        enterSubtree();
                        continue;
                    }

                    advance = true;
                    return true;
                }
            }
            catch (StopWalkException)
            {
                foreach (AbstractTreeIterator t in trees)
                    t.stopWalk();
                return false;
            }
        }

        /**
         * Obtain the tree iterator for the current entry.
         * <p>
         * Entering into (or exiting out of) a subtree causes the current tree
         * iterator instance to be changed for the nth tree. This allows the tree
         * iterators to manage only one list of items, with the diving handled by
         * recursive trees.
         *
         * @param <T>
         *            type of the tree iterator expected by the caller.
         * @param nth
         *            tree to obtain the current iterator of.
         * @param clazz
         *            type of the tree iterator expected by the caller.
         * @return r the current iterator of the requested type; null if the tree
         *         has no entry to match the current path.
         */
        public T getTree<T>(int nth, Type clazz) // [henon] was Class<T> clazz
            where T : AbstractTreeIterator
        {
            AbstractTreeIterator t = trees[nth];
            return t.matches == currentHead ? (T)t : null;
        }

        /**
         * Obtain the raw {@link FileMode} bits for the current entry.
         * <p>
         * Every added tree supplies mode bits, even if the tree does not contain
         * the current entry. In the latter case {@link FileMode#MISSING}'s mode
         * bits (0) are returned.
         * 
         * @param nth
         *            tree to obtain the mode bits from.
         * @return mode bits for the current entry of the nth tree.
         * @see FileMode#FromBits(int)
         */
        public int getRawMode(int nth)
        {
            AbstractTreeIterator t = trees[nth];
            return t.matches == currentHead ? t.mode : 0;
        }

        /**
         * Obtain the {@link FileMode} for the current entry.
         * <p>
         * Every added tree supplies a mode, even if the tree does not contain the
         * current entry. In the latter case {@link FileMode#MISSING} is returned.
         * 
         * @param nth
         *            tree to obtain the mode from.
         * @return mode for the current entry of the nth tree.
         */
        public FileMode getFileMode(int nth)
        {
            return FileMode.FromBits(getRawMode(nth));
        }

        /**
         * Obtain the ObjectId for the current entry.
         * <p>
         * Using this method to compare ObjectId values between trees of this walker
         * is very inefficient. Applications should try to use
         * {@link #idEqual(int, int)} or {@link #getObjectId(MutableObjectId, int)}
         * whenever possible.
         * <p>
         * Every tree supplies an object id, even if the tree does not contain the
         * current entry. In the latter case {@link ObjectId#zeroId()} is returned.
         * 
         * @param nth
         *            tree to obtain the object identifier from.
         * @return object identifier for the current tree entry.
         * @see #getObjectId(MutableObjectId, int)
         * @see #idEqual(int, int)
         */
        public ObjectId getObjectId(int nth)
        {
            AbstractTreeIterator t = trees[nth];
            return t.matches == currentHead ? t.getEntryObjectId() : ObjectId.ZeroId;
        }

        /**
         * Obtain the ObjectId for the current entry.
         * <p>
         * Every tree supplies an object id, even if the tree does not contain the
         * current entry. In the latter case {@link ObjectId#zeroId()} is supplied.
         * <p>
         * Applications should try to use {@link #idEqual(int, int)} when possible
         * as it avoids conversion overheads.
         *
         * @param out
         *            buffer to copy the object id into.
         * @param nth
         *            tree to obtain the object identifier from.
         * @see #idEqual(int, int)
         */
        public void getObjectId(MutableObjectId @out, int nth)
        {
            AbstractTreeIterator t = trees[nth];
            if (t.matches == currentHead)
                t.getEntryObjectId(@out);
            else
                @out.Clear();
        }

        /**
         * Compare two tree's current ObjectId values for equality.
         * 
         * @param nthA
         *            first tree to compare the object id from.
         * @param nthB
         *            second tree to compare the object id from.
         * @return result of
         *         <code>getObjectId(nthA).Equals(getObjectId(nthB))</code>.
         * @see #getObjectId(int)
         */
        public bool idEqual(int nthA, int nthB)
        {
            AbstractTreeIterator ch = currentHead;
            AbstractTreeIterator a = trees[nthA];
            AbstractTreeIterator b = trees[nthB];
            if (a.matches == ch && b.matches == ch)
                return a.idEqual(b);
            if (a.matches != ch && b.matches != ch)
            {
                // If neither tree matches the current path node then neither
                // tree has this entry. In such case the ObjectId is zero(),
                // and zero() is always equal to zero().
                //
                return true;
            }
            return false;
        }

        /**
         * Get the current entry's name within its parent tree.
         * <p>
         * This method is not very efficient and is primarily meant for debugging
         * and  output generation. Applications should try to avoid calling it,
         * and if invoked do so only once per interesting entry, where the name is
         * absolutely required for correct function.
         *
         * @return name of the current entry within the parent tree (or directory).
         *         The name never includes a '/'.
         */
        public string getNameString()
        {
            AbstractTreeIterator t = currentHead;
            int off = t.pathOffset;
            int end = t.pathLen;
            return RawParseUtils.decode(Constants.CHARSET, t.path, off, end);
        }

        /**
         * Get the current entry's complete path.
         * <p>
         * This method is not very efficient and is primarily meant for debugging
         * and  output generation. Applications should try to avoid calling it,
         * and if invoked do so only once per interesting entry, where the name is
         * absolutely required for correct function.
         * 
         * @return complete path of the current entry, from the root of the
         *         repository. If the current entry is in a subtree there will be at
         *         least one '/' in the returned string.
         */
        public string getPathString()
        {
            return pathOf(currentHead);
        }

        /**
         * Get the current entry's complete path as a UTF-8 byte array.
         *
         * @return complete path of the current entry, from the root of the
         *         repository. If the current entry is in a subtree there will be at
         *         least one '/' in the returned string.
         */
        public byte[] getRawPath()
        {
            AbstractTreeIterator t = currentHead;
            int n = t.pathLen;
            byte[] r = new byte[n];
            Array.Copy(t.path, 0, r, 0, n);
            return r;
        }

        /**
         * Test if the supplied path matches the current entry's path.
         * <p>
         * This method tests that the supplied path is exactly equal to the current
         * entry, or is one of its parent directories. It is faster to use this
         * method then to use {@link #getPathString()} to first create a string
         * object, then test <code>startsWith</code> or some other type of string
         * match function.
         * 
         * @param p
         *            path buffer to test. Callers should ensure the path does not
         *            end with '/' prior to invocation.
         * @param pLen
         *            number of bytes from <code>buf</code> to test.
         * @return < 0 if p is before the current path; 0 if p matches the current
         *         path; 1 if the current path is past p and p will never match
         *         again on this tree walk.
         */
        public int isPathPrefix(byte[] p, int pLen)
        {
            AbstractTreeIterator t = currentHead;
            byte[] c = t.path;
            int cLen = t.pathLen;
            int ci;

            for (ci = 0; ci < cLen && ci < pLen; ci++)
            {
                int c_value = (c[ci] & 0xff) - (p[ci] & 0xff);
                if (c_value != 0)
                    return c_value;
            }

            if (ci < cLen)
            {
                // Ran out of pattern but we still had current data.
                // If c[ci] == '/' then pattern matches the subtree.
                // Otherwise we cannot be certain so we return -1.
                //
                return c[ci] == '/' ? 0 : -1;
            }

            if (ci < pLen)
            {
                // Ran out of current, but we still have pattern data.
                // If p[ci] == '/' then pattern matches this subtree,
                // otherwise we cannot be certain so we return -1.
                //
                return p[ci] == '/' ? 0 : -1;
            }

            // Both strings are identical.
            //
            return 0;
        }

        /**
         * Test if the supplied path matches (being suffix of) the current entry's
         * path.
         * <p>
         * This method tests that the supplied path is exactly equal to the current
         * entry, or is relative to one of entry's parent directories. It is faster
         * to use this method then to use {@link #getPathString()} to first create
         * a String object, then test <code>endsWith</code> or some other type of
         * string match function.
         *
         * @param p
         *            path buffer to test.
         * @param pLen
         *            number of bytes from <code>buf</code> to test.
         * @return true if p is suffix of the current path;
         *         false if otherwise
         */
        public bool isPathSuffix(byte[] p, int pLen)
        {
            AbstractTreeIterator t = currentHead;
            byte[] c = t.path;
            int cLen = t.pathLen;
            int ci;

            for (ci = 1; ci < cLen && ci < pLen; ci++)
            {
                if (c[cLen - ci] != p[pLen - ci])
                    return false;
            }

            return true;
        }

        /**
         * Get the current subtree depth of this walker.
         *
         * @return the current subtree depth of this walker.
         */
        public int getDepth()
        {
            return depth;
        }

        /**
         * Is the current entry a subtree?
         * <p>
         * This method is faster then testing the raw mode bits of all trees to see
         * if any of them are a subtree. If at least one is a subtree then this
         * method will return true.
         * 
         * @return true if {@link #enterSubtree()} will work on the current node.
         */
        public bool isSubtree()
        {
            return FileMode.Tree.Equals(currentHead.mode);
        }

        /**
         * Is the current entry a subtree returned after its children?
         *
         * @return true if the current node is a tree that has been returned after
         *         its children were already processed.
         * @see #isPostOrderTraversal()
         */
        public bool isPostChildren()
        {
            return postChildren && isSubtree();
        }

        /**
         * Enter into the current subtree.
         * <p>
         * If the current entry is a subtree this method arranges for its children
         * to be returned before the next sibling following the subtree is returned.
         * 
         * @throws MissingObjectException
         *             a subtree was found, but the subtree object does not exist in
         *             this repository. The repository may be missing objects.
         * @throws IncorrectObjectTypeException
         *             a subtree was found, and the subtree id does not denote a
         *             tree, but instead names some other non-tree type of object.
         *             The repository may have data corruption.
         * @throws CorruptObjectException
         *             the contents of a tree did not appear to be a tree. The
         *             repository may have data corruption.
         * @throws IOException
         *             a loose object or pack file could not be read.
         */
        public void enterSubtree()
        {
            AbstractTreeIterator ch = currentHead;
            AbstractTreeIterator[] tmp = new AbstractTreeIterator[trees.Length];
            for (int i = 0; i < trees.Length; i++)
            {
                AbstractTreeIterator t = trees[i];
                AbstractTreeIterator n;
                if (t.matches == ch && !t.eof() && FileMode.Tree.Equals(t.mode))
                    n = t.createSubtreeIterator(db, idBuffer, curs);
                else
                    n = t.createEmptyTreeIterator();
                tmp[i] = n;
            }
            depth++;
            advance = false;
            Array.Copy(tmp, 0, trees, 0, trees.Length);
        }

        public virtual AbstractTreeIterator min()
        {
            int i = 0;
            AbstractTreeIterator minRef = trees[i];
            while (minRef.eof() && ++i < trees.Length)
                minRef = trees[i];
            if (minRef.eof())
                return minRef;

            minRef.matches = minRef;
            while (++i < trees.Length)
            {
                AbstractTreeIterator t = trees[i];
                if (t.eof())
                    continue;
                int cmp = t.pathCompare(minRef);
                if (cmp < 0)
                {
                    t.matches = t;
                    minRef = t;
                }
                else if (cmp == 0)
                {
                    t.matches = minRef;
                }
            }

            return minRef;
        }

        public virtual void popEntriesEqual()
        {
            AbstractTreeIterator ch = currentHead;
            for (int i = 0; i < trees.Length; i++)
            {
                AbstractTreeIterator t = trees[i];
                if (t.matches == ch)
                {
                    t.next(1);
                    t.matches = null;
                }
            }
        }

        public virtual void skipEntriesEqual()
        {
            AbstractTreeIterator ch = currentHead;
            for (int i = 0; i < trees.Length; i++)
            {
                AbstractTreeIterator t = trees[i];
                if (t.matches == ch)
                {
                    t.skip();
                    t.matches = null;
                }
            }
        }

        private void exitSubtree()
        {
            depth--;
            for (int i = 0; i < trees.Length; i++)
                trees[i] = trees[i].parent;

            AbstractTreeIterator minRef = null;
            foreach (AbstractTreeIterator t in trees)
            {
                if (t.matches != t)
                    continue;
                if (minRef == null || t.pathCompare(minRef) < 0)
                    minRef = t;
            }
            currentHead = minRef;
        }

        private CanonicalTreeParser parserFor(AnyObjectId id)
        {
            CanonicalTreeParser p = new CanonicalTreeParser();
            p.reset(db, id, curs);
            return p;
        }

        public static string pathOf(AbstractTreeIterator t)
        {
            return RawParseUtils.decode(Constants.CHARSET, t.path, 0, t.pathLen);
        }

    }
}