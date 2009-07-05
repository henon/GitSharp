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

using GitSharp.TreeWalk;
using GitSharp.Exceptions;
namespace GitSharp.RevWalk
{



    /**
     * Specialized subclass of RevWalk to include trees, blobs and tags.
     * <p>
     * Unlike RevWalk this subclass is able to remember starting roots that include
     * annotated tags, or arbitrary trees or blobs. Once commit generation is
     * complete and all commits have been popped by the application, individual
     * annotated tag, tree and blob objects can be popped through the additional
     * method {@link #nextObject()}.
     * <p>
     * Tree and blob objects reachable from interesting commits are automatically
     * scheduled for inclusion in the results of {@link #nextObject()}, returning
     * each object exactly once. Objects are sorted and returned according to the
     * the commits that reference them and the order they appear within a tree.
     * Ordering can be affected by changing the {@link RevSort} used to order the
     * commits that are returned first.
     */
    public class ObjectWalk : RevWalk
    {

        /**
         * Indicates a non-RevCommit is in {@link #pendingObjects}.
         * <p>
         * We can safely reuse {@link RevWalk#REWRITE} here for the same value as it
         * is only set on RevCommit and {@link #pendingObjects} never has RevCommit
         * instances inserted into it.
         */
        private static int IN_PENDING = RevWalk.REWRITE;

        private CanonicalTreeParser treeWalk;

        private BlockObjQueue pendingObjects;

        private RevTree currentTree;

        private bool fromTreeWalk;

        private RevTree nextSubtree;

        /**
         * Create a new revision and object walker for a given repository.
         * 
         * @param repo
         *            the repository the walker will obtain data from.
         */
        public ObjectWalk(Repository repo)
            : base(repo)
        {
            pendingObjects = new BlockObjQueue();
            treeWalk = new CanonicalTreeParser();
        }

        /**
         * Mark an object or commit to start graph traversal from.
         * <p>
         * Callers are encouraged to use {@link RevWalk#parseAny(AnyObjectId)}
         * instead of {@link RevWalk#lookupAny(AnyObjectId, int)}, as this method
         * requires the object to be parsed before it can be added as a root for the
         * traversal.
         * <p>
         * The method will automatically parse an unparsed object, but error
         * handling may be more difficult for the application to explain why a
         * RevObject is not actually valid. The object pool of this walker would
         * also be 'poisoned' by the invalid RevObject.
         * <p>
         * This method will automatically call {@link RevWalk#markStart(RevCommit)}
         * if passed RevCommit instance, or a RevTag that directly (or indirectly)
         * references a RevCommit.
         * 
         * @param o
         *            the object to start traversing from. The object passed must be
         *            from this same revision walker.
         * @throws MissingObjectException
         *             the object supplied is not available from the object
         *             database. This usually indicates the supplied object is
         *             invalid, but the reference was constructed during an earlier
         *             invocation to {@link RevWalk#lookupAny(AnyObjectId, int)}.
         * @throws IncorrectObjectTypeException
         *             the object was not parsed yet and it was discovered during
         *             parsing that it is not actually the type of the instance
         *             passed in. This usually indicates the caller used the wrong
         *             type in a {@link RevWalk#lookupAny(AnyObjectId, int)} call.
         * @
         *             a pack file or loose object could not be read.
         */
        public void markStart(RevObject o)
        {
            while (o is RevTag)
            {
                addObject(o);
                o = ((RevTag)o).getObject();
                parse(o);
            }

            if (o is RevCommit)
                base.markStart((RevCommit)o);
            else
                addObject(o);
        }

        /**
         * Mark an object to not produce in the output.
         * <p>
         * Uninteresting objects denote not just themselves but also their entire
         * reachable chain, back until the merge base of an uninteresting commit and
         * an otherwise interesting commit.
         * <p>
         * Callers are encouraged to use {@link RevWalk#parseAny(AnyObjectId)}
         * instead of {@link RevWalk#lookupAny(AnyObjectId, int)}, as this method
         * requires the object to be parsed before it can be added as a root for the
         * traversal.
         * <p>
         * The method will automatically parse an unparsed object, but error
         * handling may be more difficult for the application to explain why a
         * RevObject is not actually valid. The object pool of this walker would
         * also be 'poisoned' by the invalid RevObject.
         * <p>
         * This method will automatically call {@link RevWalk#markStart(RevCommit)}
         * if passed RevCommit instance, or a RevTag that directly (or indirectly)
         * references a RevCommit.
         * 
         * @param o
         *            the object to start traversing from. The object passed must be
         * @throws MissingObjectException
         *             the object supplied is not available from the object
         *             database. This usually indicates the supplied object is
         *             invalid, but the reference was constructed during an earlier
         *             invocation to {@link RevWalk#lookupAny(AnyObjectId, int)}.
         * @throws IncorrectObjectTypeException
         *             the object was not parsed yet and it was discovered during
         *             parsing that it is not actually the type of the instance
         *             passed in. This usually indicates the caller used the wrong
         *             type in a {@link RevWalk#lookupAny(AnyObjectId, int)} call.
         * @
         *             a pack file or loose object could not be read.
         */
        public void markUninteresting(RevObject o)
        {
            while (o is RevTag)
            {
                o.flags |= UNINTERESTING;
                if (hasRevSort(RevSort.BOUNDARY))
                    addObject(o);
                o = ((RevTag)o).getObject();
                parse(o);
            }

            if (o is RevCommit)
                base.markUninteresting((RevCommit)o);
            else if (o is RevTree)
                markTreeUninteresting((RevTree)o);
            else
                o.flags |= UNINTERESTING;

            if (o.getType() != Constants.OBJ_COMMIT && hasRevSort(RevSort.BOUNDARY))
            {
                addObject(o);
            }
        }

        public override RevCommit next()
        {
            for (; ; )
            {
                RevCommit r = base.next();
                if (r == null)
                    return null;
                if ((r.flags & UNINTERESTING) != 0)
                {
                    markTreeUninteresting(r.getTree());
                    if (hasRevSort(RevSort.BOUNDARY))
                    {
                        pendingObjects.add(r.getTree());
                        return r;
                    }
                    continue;
                }
                pendingObjects.add(r.getTree());
                return r;
            }
        }

        /**
         * Pop the next most recent object.
         * 
         * @return next most recent object; null if traversal is over.
         * @throws MissingObjectException
         *             one or or more of the next objects are not available from the
         *             object database, but were thought to be candidates for
         *             traversal. This usually indicates a broken link.
         * @throws IncorrectObjectTypeException
         *             one or or more of the objects in a tree do not match the type
         *             indicated.
         * @
         *             a pack file or loose object could not be read.
         */
        public RevObject nextObject()
        {
            fromTreeWalk = false;

            if (nextSubtree != null)
            {
                treeWalk = treeWalk.createSubtreeIterator0(db, nextSubtree, curs);
                nextSubtree = null;
            }

            while (!treeWalk.eof())
            {
                FileMode mode = treeWalk.getEntryFileMode();
                int sType = (int)mode.ObjectType;

                switch (sType)
                {
                    case Constants.OBJ_BLOB:
                        {
                            treeWalk.getEntryObjectId(idBuffer);
                            RevBlob o = lookupBlob(idBuffer);
                            if ((o.flags & SEEN) != 0)
                                break;
                            o.flags |= SEEN;
                            if (shouldSkipObject(o))
                                break;
                            fromTreeWalk = true;
                            return o;
                        }
                    case Constants.OBJ_TREE:
                        {
                            treeWalk.getEntryObjectId(idBuffer);
                            RevTree o = lookupTree(idBuffer);
                            if ((o.flags & SEEN) != 0)
                                break;
                            o.flags |= SEEN;
                            if (shouldSkipObject(o))
                                break;
                            nextSubtree = o;
                            fromTreeWalk = true;
                            return o;
                        }
                    default:
                        if (FileMode.GitLink.Equals(mode.Bits))
                            break;
                        treeWalk.getEntryObjectId(idBuffer);
                        throw new CorruptObjectException("Invalid mode " + mode
                                + " for " + idBuffer.ToString() + " "
                                + treeWalk.getEntryPathString() + " in " + currentTree
                                + ".");
                }

                treeWalk = treeWalk.next();
            }

            for (; ; )
            {
                RevObject o = pendingObjects.next();
                if (o == null)
                    return null;
                if ((o.flags & SEEN) != 0)
                    continue;
                o.flags |= SEEN;
                if (shouldSkipObject(o))
                    continue;
                if (o is RevTree)
                {
                    currentTree = (RevTree)o;
                    treeWalk = treeWalk.resetRoot(db, currentTree, curs);
                }
                return o;
            }
        }

        private bool shouldSkipObject(RevObject o)
        {
            return (o.flags & UNINTERESTING) != 0 && !hasRevSort(RevSort.BOUNDARY);
        }

        /**
         * Verify all interesting objects are available, and reachable.
         * <p>
         * Callers should populate starting points and ending points with
         * {@link #markStart(RevObject)} and {@link #markUninteresting(RevObject)}
         * and then use this method to verify all objects between those two points
         * exist in the repository and are readable.
         * <p>
         * This method returns successfully if everything is connected; it throws an
         * exception if there is a connectivity problem. The exception message
         * provides some detail about the connectivity failure.
         * 
         * @throws MissingObjectException
         *             one or or more of the next objects are not available from the
         *             object database, but were thought to be candidates for
         *             traversal. This usually indicates a broken link.
         * @throws IncorrectObjectTypeException
         *             one or or more of the objects in a tree do not match the type
         *             indicated.
         * @
         *             a pack file or loose object could not be read.
         */
        public void checkConnectivity()
        {
            for (; ; )
            {
                RevCommit c = next();
                if (c == null)
                    break;
            }
            for (; ; )
            {
                RevObject o = nextObject();
                if (o == null)
                    break;
                if (o is RevBlob && !db.HasObject(o))
                    throw new MissingObjectException(o, Constants.TYPE_BLOB);
            }
        }

        /**
         * Get the current object's complete path.
         * <p>
         * This method is not very efficient and is primarily meant for debugging
         * and  output generation. Applications should try to avoid calling it,
         * and if invoked do so only once per interesting entry, where the name is
         * absolutely required for correct function.
         * 
         * @return complete path of the current entry, from the root of the
         *         repository. If the current entry is in a subtree there will be at
         *         least one '/' in the returned string. Null if the current entry
         *         has no path, such as for annotated tags or root level trees.
         */
        public string getPathString()
        {
            return fromTreeWalk ? treeWalk.getEntryPathString() : null;
        }

        public override void dispose()
        {
            base.dispose();
            pendingObjects = new BlockObjQueue();
            nextSubtree = null;
            currentTree = null;
        }

        internal override void reset(int retainFlags)
        {
            base.reset(retainFlags);
            pendingObjects = new BlockObjQueue();
            nextSubtree = null;
        }

        private void addObject(RevObject o)
        {
            if ((o.flags & IN_PENDING) == 0)
            {
                o.flags |= IN_PENDING;
                pendingObjects.add(o);
            }
        }

        private void markTreeUninteresting(RevTree tree)
        {
            if ((tree.flags & UNINTERESTING) != 0)
                return;
            tree.flags |= UNINTERESTING;

            treeWalk = treeWalk.resetRoot(db, tree, curs);
            while (!treeWalk.eof())
            {
                FileMode mode = treeWalk.getEntryFileMode();
                int sType = (int)mode.ObjectType;

                switch (sType)
                {
                    case Constants.OBJ_BLOB:
                        {
                            treeWalk.getEntryObjectId(idBuffer);
                            lookupBlob(idBuffer).flags |= UNINTERESTING;
                            break;
                        }
                    case Constants.OBJ_TREE:
                        {
                            treeWalk.getEntryObjectId(idBuffer);
                            RevTree t = lookupTree(idBuffer);
                            if ((t.flags & UNINTERESTING) == 0)
                            {
                                t.flags |= UNINTERESTING;
                                treeWalk = treeWalk.createSubtreeIterator0(db, t, curs);
                                continue;
                            }
                            break;
                        }
                    default:
                        if (FileMode.GitLink.Equals(mode.Bits))
                            break;
                        treeWalk.getEntryObjectId(idBuffer);
                        throw new CorruptObjectException("Invalid mode " + mode
                                + " for " + idBuffer.ToString() + " "
                                + treeWalk.getEntryPathString() + " in " + tree + ".");
                }

                treeWalk = treeWalk.next();
            }
        }
    }
}
