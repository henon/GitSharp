/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
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

using System;
using System.Collections.Generic;
using System.IO;
using GitSharp.Exceptions;
using GitSharp.RevWalk.Filter;
using GitSharp.TreeWalk.Filter;
using GitSharp.Util;

namespace GitSharp.RevWalk
{
	/**
	 * Walks a commit graph and produces the matching commits in order.
	 * <p>
	 * A RevWalk instance can only be used once to generate results. Running a
	 * second time requires creating a new RevWalk instance, or invoking
	 * {@link #reset()} before starting again. Resetting an existing instance may be
	 * faster for some applications as commit body parsing can be avoided on the
	 * later invocations.
	 * <p>
	 * RevWalk instances are not thread-safe. Applications must either restrict
	 * usage of a RevWalk instance to a single thread, or implement their own
	 * synchronization at a higher level.
	 * <p>
	 * Multiple simultaneous RevWalk instances per {@link Repository} are permitted,
	 * even from concurrent threads. Equality of {@link RevCommit}s from two
	 * different RevWalk instances is never true, even if their {@link ObjectId}s
	 * are equal (and thus they describe the same commit).
	 * <p>
	 * The offered iterator is over the list of RevCommits described by the
	 * configuration of this instance. Applications should restrict themselves to
	 * using either the provided Iterator or {@link #next()}, but never use both on
	 * the same RevWalk at the same time. The Iterator may buffer RevCommits, while
	 * {@link #next()} does not.
	 */
	public class RevWalk : IEnumerable<RevCommit>, IDisposable
	{
		/**
		 * Set on objects whose important header data has been loaded.
		 * <p>
		 * For a RevCommit this indicates we have pulled apart the tree and parent
		 * references from the raw bytes available in the repository and translated
		 * those to our own local RevTree and RevCommit instances. The raw buffer is
		 * also available for message and other header filtering.
		 * <p>
		 * For a RevTag this indicates we have pulled part the tag references to
		 * find out who the tag refers to, and what that object's type is.
		 */
		public static int PARSED = 1 << 0;

		/**
		 * Set on RevCommit instances added to our {@link #pending} queue.
		 * <p>
		 * We use this flag to avoid adding the same commit instance twice to our
		 * queue, especially if we reached it by more than one path.
		 */
		public static int SEEN = 1 << 1;

		/**
		 * Set on RevCommit instances the caller does not want output.
		 * <p>
		 * We flag commits as uninteresting if the caller does not want commits
		 * reachable from a commit given to {@link #markUninteresting(RevCommit)}.
		 * This flag is always carried into the commit's parents and is a key part
		 * of the "rev-list B --not A" feature; A is marked UNINTERESTING.
		 */
		public static int UNINTERESTING = 1 << 2;

		/**
		 * Set on a RevCommit that can collapse out of the history.
		 * <p>
		 * If the {@link #treeFilter} concluded that this commit matches his
		 * parents' for all of the paths that the filter is interested in then we
		 * mark the commit REWRITE. Later we can rewrite the parents of a REWRITE
		 * child to remove chains of REWRITE commits before we produce the child to
		 * the application.
		 * 
		 * @see RewriteGenerator
		 */
		public static int REWRITE = 1 << 3;

		/**
		 * Temporary mark for use within generators or filters.
		 * <p>
		 * This mark is only for local use within a single scope. If someone sets
		 * the mark they must unset it before any other code can see the mark.
		 */
		public static int TEMP_MARK = 1 << 4;

		/**
		 * Temporary mark for use within {@link TopoSortGenerator}.
		 * <p>
		 * This mark indicates the commit could not produce when it wanted to, as at
		 * least one child was behind it. Commits with this flag are delayed until
		 * all children have been output first.
		 */
		public static int TOPO_DELAY = 1 << 5;

		/** Number of flag bits we keep internal for our own use. See above flags. */
		public static int RESERVED_FLAGS = 6;

		private static readonly int APP_FLAGS = -1 & ~((1 << RESERVED_FLAGS) - 1);

		public Repository db;

		public WindowCursor curs;

		public MutableObjectId idBuffer;

		private ObjectIdSubclassMap<RevObject> objects;

		private int freeFlags = APP_FLAGS;

		private int delayFreeFlags;

		internal int carryFlags = UNINTERESTING;

		private List<RevCommit> roots;

		public AbstractRevQueue queue;

		public Generator pending;

		private HashSet<RevSort.Strategy> sorting;

		private RevFilter filter;

		private TreeFilter treeFilter;

		/**
		 * Create a new revision walker for a given repository.
		 * 
		 * @param repo
		 *            the repository the walker will obtain data from.
		 */
		public RevWalk(Repository repo)
		{
			db = repo;
			curs = new WindowCursor();
			idBuffer = new MutableObjectId();
			objects = new ObjectIdSubclassMap<RevObject>();
			roots = new List<RevCommit>();
			queue = new DateRevQueue();
			pending = new StartGenerator(this);
			sorting = new HashSet<RevSort.Strategy>() { RevSort.NONE };
			filter = RevFilter.ALL;
			treeFilter = TreeFilter.ALL;
		}

		/**
		 * Get the repository this walker loads objects from.
		 * 
		 * @return the repository this walker was created to Read.
		 */
		public Repository getRepository()
		{
			return db;
		}

		/**
		 * Mark a commit to start graph traversal from.
		 * <p>
		 * Callers are encouraged to use {@link #parseCommit(AnyObjectId)} to obtain
		 * the commit reference, rather than {@link #lookupCommit(AnyObjectId)}, as
		 * this method requires the commit to be parsed before it can be added as a
		 * root for the traversal.
		 * <p>
		 * The method will automatically parse an unparsed commit, but error
		 * handling may be more difficult for the application to explain why a
		 * RevCommit is not actually a commit. The object pool of this walker would
		 * also be 'poisoned' by the non-commit RevCommit.
		 * 
		 * @param c
		 *            the commit to start traversing from. The commit passed must be
		 *            from this same revision walker.
		 * @throws MissingObjectException
		 *             the commit supplied is not available from the object
		 *             database. This usually indicates the supplied commit is
		 *             invalid, but the reference was constructed during an earlier
		 *             invocation to {@link #lookupCommit(AnyObjectId)}.
		 * @throws IncorrectObjectTypeException
		 *             the object was not parsed yet and it was discovered during
		 *             parsing that it is not actually a commit. This usually
		 *             indicates the caller supplied a non-commit SHA-1 to
		 *             {@link #lookupCommit(AnyObjectId)}.
		 * @
		 *             a pack file or loose object could not be Read.
		 */
		public virtual void markStart(RevCommit c)
		{
			if ((c.flags & SEEN) != 0)
				return;
			if ((c.flags & PARSED) == 0)
				c.parse(this);
			c.flags |= SEEN;
			roots.Add(c);
			queue.add(c);
		}

		/**
		 * Mark commits to start graph traversal from.
		 * 
		 * @param list
		 *            commits to start traversing from. The commits passed must be
		 *            from this same revision walker.
		 * @throws MissingObjectException
		 *             one of the commits supplied is not available from the object
		 *             database. This usually indicates the supplied commit is
		 *             invalid, but the reference was constructed during an earlier
		 *             invocation to {@link #lookupCommit(AnyObjectId)}.
		 * @throws IncorrectObjectTypeException
		 *             the object was not parsed yet and it was discovered during
		 *             parsing that it is not actually a commit. This usually
		 *             indicates the caller supplied a non-commit SHA-1 to
		 *             {@link #lookupCommit(AnyObjectId)}.
		 * @
		 *             a pack file or loose object could not be Read.
		 */
		public virtual void markStart(IEnumerable<RevCommit> list)
		{
			foreach (RevCommit c in list)
				markStart(c);
		}

		/**
		 * Mark a commit to not produce in the output.
		 * <p>
		 * Uninteresting commits denote not just themselves but also their entire
		 * ancestry chain, back until the merge base of an uninteresting commit and
		 * an otherwise interesting commit.
		 * <p>
		 * Callers are encouraged to use {@link #parseCommit(AnyObjectId)} to obtain
		 * the commit reference, rather than {@link #lookupCommit(AnyObjectId)}, as
		 * this method requires the commit to be parsed before it can be added as a
		 * root for the traversal.
		 * <p>
		 * The method will automatically parse an unparsed commit, but error
		 * handling may be more difficult for the application to explain why a
		 * RevCommit is not actually a commit. The object pool of this walker would
		 * also be 'poisoned' by the non-commit RevCommit.
		 * 
		 * @param c
		 *            the commit to start traversing from. The commit passed must be
		 *            from this same revision walker.
		 * @throws MissingObjectException
		 *             the commit supplied is not available from the object
		 *             database. This usually indicates the supplied commit is
		 *             invalid, but the reference was constructed during an earlier
		 *             invocation to {@link #lookupCommit(AnyObjectId)}.
		 * @throws IncorrectObjectTypeException
		 *             the object was not parsed yet and it was discovered during
		 *             parsing that it is not actually a commit. This usually
		 *             indicates the caller supplied a non-commit SHA-1 to
		 *             {@link #lookupCommit(AnyObjectId)}.
		 * @
		 *             a pack file or loose object could not be Read.
		 */
		public virtual void markUninteresting(RevCommit c)
		{
			c.flags |= UNINTERESTING;
			carryFlagsImpl(c);
			markStart(c);
		}

		/**
		 * Determine if a commit is reachable from another commit.
		 * <p>
		 * A commit <code>base</code> is an ancestor of <code>tip</code> if we
		 * can find a path of commits that leads from <code>tip</code> and ends at
		 * <code>base</code>.
		 * <p>
		 * This utility function resets the walker, inserts the two supplied
		 * commits, and then executes a walk until an answer can be obtained.
		 * Currently allocated RevFlags that have been added to RevCommit instances
		 * will be retained through the reset.
		 * 
		 * @param base
		 *            commit the caller thinks is reachable from <code>tip</code>.
		 * @param tip
		 *            commit to start iteration from, and which is most likely a
		 *            descendant (child) of <code>base</code>.
		 * @return true if there is a path directly from <code>tip</code> to
		 *         <code>base</code> (and thus <code>base</code> is fully merged
		 *         into <code>tip</code>); false otherwise.
		 * @throws MissingObjectException
		 *             one or or more of the next commit's parents are not available
		 *             from the object database, but were thought to be candidates
		 *             for traversal. This usually indicates a broken link.
		 * @throws IncorrectObjectTypeException
		 *             one or or more of the next commit's parents are not actually
		 *             commit objects.
		 * @
		 *             a pack file or loose object could not be Read.
		 */
		public virtual bool isMergedInto(RevCommit @base, RevCommit tip)
		{
			RevFilter oldRF = filter;
			TreeFilter oldTF = treeFilter;
			try
			{
				finishDelayedFreeFlags();
				reset(~freeFlags & APP_FLAGS);
				filter = RevFilter.MERGE_BASE;
				treeFilter = TreeFilter.ALL;
				markStart(tip);
				markStart(@base);
				return (next() == @base);
			}
			finally
			{
				filter = oldRF;
				treeFilter = oldTF;
			}
		}

		/**
		 * Pop the next most recent commit.
		 * 
		 * @return next most recent commit; null if traversal is over.
		 * @throws MissingObjectException
		 *             one or or more of the next commit's parents are not available
		 *             from the object database, but were thought to be candidates
		 *             for traversal. This usually indicates a broken link.
		 * @throws IncorrectObjectTypeException
		 *             one or or more of the next commit's parents are not actually
		 *             commit objects.
		 * @
		 *             a pack file or loose object could not be Read.
		 */
		public virtual RevCommit next()
		{
			return pending.next();
		}

		/**
		 * Obtain the sort types applied to the commits returned.
		 * 
		 * @return the sorting strategies employed. At least one strategy is always
		 *         used, but that strategy may be {@link RevSort#NONE}.
		 */
		public virtual HashSet<RevSort.Strategy> getRevSort()
		{
			return new HashSet<RevSort.Strategy>(sorting);
		}

		/**
		 * Check whether the provided sorting strategy is enabled.
		 *
		 * @param sort
		 *            a sorting strategy to look for.
		 * @return true if this strategy is enabled, false otherwise
		 */
		public virtual bool hasRevSort(RevSort.Strategy sort)
		{
			return sorting.Contains(sort);
		}

		/**
		 * Select a single sorting strategy for the returned commits.
		 * <p>
		 * Disables all sorting strategies, then enables only the single strategy
		 * supplied by the caller.
		 * 
		 * @param s
		 *            a sorting strategy to enable.
		 */
		public virtual void sort(RevSort.Strategy s)
		{
			assertNotStarted();
			sorting.Clear();
			sorting.Add(s);
		}

		/**
		 * Add or remove a sorting strategy for the returned commits.
		 * <p>
		 * Multiple strategies can be applied at once, in which case some strategies
		 * may take precedence over others. As an example, {@link RevSort#TOPO} must
		 * take precedence over {@link RevSort#COMMIT_TIME_DESC}, otherwise it
		 * cannot enforce its ordering.
		 * 
		 * @param s
		 *            a sorting strategy to enable or disable.
		 * @param use
		 *            true if this strategy should be used, false if it should be
		 *            removed.
		 */
		public virtual void sort(RevSort.Strategy s, bool use)
		{
			assertNotStarted();
			if (use)
				sorting.Add(s);
			else
				sorting.Remove(s);

			if (sorting.Count > 1)
				sorting.Remove(RevSort.NONE);
			else if (sorting.Count == 0)
				sorting.Add(RevSort.NONE);
		}

		/**
		 * Get the currently configured commit filter.
		 * 
		 * @return the current filter. Never null as a filter is always needed.
		 */
		public virtual RevFilter getRevFilter()
		{
			return filter;
		}

		/**
		 * Set the commit filter for this walker.
		 * <p>
		 * Multiple filters may be combined by constructing an arbitrary tree of
		 * <code>AndRevFilter</code> or <code>OrRevFilter</code> instances to
		 * describe the bool expression required by the application. Custom
		 * filter implementations may also be constructed by applications.
		 * <p>
		 * Note that filters are not thread-safe and may not be shared by concurrent
		 * RevWalk instances. Every RevWalk must be supplied its own unique filter,
		 * unless the filter implementation specifically states it is (and always
		 * will be) thread-safe. Callers may use {@link RevFilter#Clone()} to Create
		 * a unique filter tree for this RevWalk instance.
		 * 
		 * @param newFilter
		 *            the new filter. If null the special {@link RevFilter#ALL}
		 *            filter will be used instead, as it matches every commit.
		 * @see org.spearce.jgit.revwalk.filter.AndRevFilter
		 * @see org.spearce.jgit.revwalk.filter.OrRevFilter
		 */
		public virtual void setRevFilter(RevFilter newFilter)
		{
			assertNotStarted();
			filter = newFilter != null ? newFilter : RevFilter.ALL;
		}

		/**
		 * Get the tree filter used to simplify commits by modified paths.
		 * 
		 * @return the current filter. Never null as a filter is always needed. If
		 *         no filter is being applied {@link TreeFilter#ALL} is returned.
		 */
		public virtual TreeFilter getTreeFilter()
		{
			return treeFilter;
		}

		/**
		 * Set the tree filter used to simplify commits by modified paths.
		 * <p>
		 * If null or {@link TreeFilter#ALL} the path limiter is removed. Commits
		 * will not be simplified.
		 * <p>
		 * If non-null and not {@link TreeFilter#ALL} then the tree filter will be
		 * installed and commits will have their ancestry simplified to hide commits
		 * that do not contain tree entries matched by the filter.
		 * <p>
		 * Usually callers should be inserting a filter graph including
		 * {@link TreeFilter#ANY_DIFF} along with one or more
		 * {@link org.spearce.jgit.treewalk.filter.PathFilter} instances.
		 * 
		 * @param newFilter
		 *            new filter. If null the special {@link TreeFilter#ALL} filter
		 *            will be used instead, as it matches everything.
		 * @see org.spearce.jgit.treewalk.filter.PathFilter
		 */
		public virtual void setTreeFilter(TreeFilter newFilter)
		{
			assertNotStarted();
			treeFilter = newFilter != null ? newFilter : TreeFilter.ALL;
		}

		/**
		 * Locate a reference to a blob without loading it.
		 * <p>
		 * The blob may or may not exist in the repository. It is impossible to tell
		 * from this method's return value.
		 *
		 * @param id
		 *            name of the blob object.
		 * @return reference to the blob object. Never null.
		 */
		public virtual RevBlob lookupBlob(AnyObjectId id)
		{
			RevBlob c = (RevBlob)objects.get(id);
			if (c == null)
			{
				c = new RevBlob(id);
				objects.add(c);
			}
			return c;
		}

		/**
		 * Locate a reference to a tree without loading it.
		 * <p>
		 * The tree may or may not exist in the repository. It is impossible to tell
		 * from this method's return value.
		 * 
		 * @param id
		 *            name of the tree object.
		 * @return reference to the tree object. Never null.
		 */
		public virtual RevTree lookupTree(AnyObjectId id)
		{
			RevTree c = (RevTree)objects.get(id);
			if (c == null)
			{
				c = new RevTree(id);
				objects.add(c);
			}
			return c;
		}

		/**
		 * Locate a reference to a commit without loading it.
		 * <p>
		 * The commit may or may not exist in the repository. It is impossible to
		 * tell from this method's return value.
		 * 
		 * @param id
		 *            name of the commit object.
		 * @return reference to the commit object. Never null.
		 */
		public virtual RevCommit lookupCommit(AnyObjectId id)
		{
			RevCommit c = (RevCommit)objects.get(id);
			if (c == null)
			{
				c = createCommit(id);
				objects.add(c);
			}
			return c;
		}

		/**
		 * Locate a reference to any object without loading it.
		 * <p>
		 * The object may or may not exist in the repository. It is impossible to
		 * tell from this method's return value.
		 * 
		 * @param id
		 *            name of the object.
		 * @param type
		 *            type of the object. Must be a valid Git object type.
		 * @return reference to the object. Never null.
		 */
		public virtual RevObject lookupAny(AnyObjectId id, int type)
		{
			RevObject r = objects.get(id);
			if (r == null)
			{
				switch (type)
				{
					case Constants.OBJ_COMMIT:
						r = createCommit(id);
						break;
					case Constants.OBJ_TREE:
						r = new RevTree(id);
						break;
					case Constants.OBJ_BLOB:
						r = new RevBlob(id);
						break;
					case Constants.OBJ_TAG:
						r = new RevTag(id);
						break;
					default:
						throw new ArgumentException("invalid git type: " + type);
				}
				objects.add(r);
			}
			return r;
		}

		/**
		 * Locate a reference to a commit and immediately parse its content.
		 * <p>
		 * Unlike {@link #lookupCommit(AnyObjectId)} this method only returns
		 * successfully if the commit object exists, is verified to be a commit, and
		 * was parsed without error.
		 * 
		 * @param id
		 *            name of the commit object.
		 * @return reference to the commit object. Never null.
		 * @throws MissingObjectException
		 *             the supplied commit does not exist.
		 * @throws IncorrectObjectTypeException
		 *             the supplied id is not a commit or an annotated tag.
		 * @
		 *             a pack file or loose object could not be Read.
		 */
		public virtual RevCommit parseCommit(AnyObjectId id)
		{
			RevObject c = parseAny(id);
			while (c is RevTag)
			{
				c = ((RevTag)c).getObject();
				parse(c);
			}
			if (!(c is RevCommit))
				throw new IncorrectObjectTypeException(id.ToObjectId(), Constants.TYPE_COMMIT);
			return (RevCommit)c;
		}

		/**
		 * Locate a reference to a tree.
		 * <p>
		 * This method only returns successfully if the tree object exists, is
		 * verified to be a tree, and was parsed without error.
		 *
		 * @param id
		 *            name of the tree object, or a commit or annotated tag that may
		 *            reference a tree.
		 * @return reference to the tree object. Never null.
		 * @throws MissingObjectException
		 *             the supplied tree does not exist.
		 * @throws IncorrectObjectTypeException
		 *             the supplied id is not a tree, a commit or an annotated tag.
		 * @
		 *             a pack file or loose object could not be Read.
		 */
		public virtual RevTree parseTree(AnyObjectId id)
		{
			RevObject c = parseAny(id);
			while (c is RevTag)
			{
				c = ((RevTag)c).getObject();
				parse(c);
			}

			RevTree t;
			if (c is RevCommit)
				t = ((RevCommit)c).Tree;
			else if (!(c is RevTree))
				throw new IncorrectObjectTypeException(id.ToObjectId(),
						Constants.TYPE_TREE);
			else
				t = (RevTree)c;

			if ((t.flags & PARSED) != 0)
				return t;
			ObjectLoader ldr = db.openObject(curs, t);
			if (ldr == null)
				throw new MissingObjectException(t, Constants.TYPE_TREE);
			if (ldr.Type != Constants.OBJ_TREE)
				throw new IncorrectObjectTypeException(t, Constants.TYPE_TREE);
			t.flags |= PARSED;
			return t;
		}

		/**
		 * Locate a reference to any object and immediately parse its content.
		 * <p>
		 * This method only returns successfully if the object exists and was parsed
		 * without error. Parsing an object can be expensive as the type must be
		 * determined. For blobs this may mean the blob content was unpacked
		 * unnecessarily, and thrown away.
		 * 
		 * @param id
		 *            name of the object.
		 * @return reference to the object. Never null.
		 * @throws MissingObjectException
		 *             the supplied does not exist.
		 * @
		 *             a pack file or loose object could not be Read.
		 */
		public virtual RevObject parseAny(AnyObjectId id)
		{
			RevObject r = objects.get(id);
			if (r == null)
			{
				ObjectLoader ldr = db.openObject(curs, id);
				if (ldr == null)
					throw new MissingObjectException(id.ToObjectId(), "unknown");
				byte[] data = ldr.CachedBytes;
				int type = ldr.Type;
				switch (type)
				{
					case Constants.OBJ_COMMIT:
						{
							RevCommit c = createCommit(id);
							c.parseCanonical(this, data);
							r = c;
							break;
						}
					case Constants.OBJ_TREE:
						{
							r = new RevTree(id);
							r.flags |= PARSED;
							break;
						}
					case Constants.OBJ_BLOB:
						{
							r = new RevBlob(id);
							r.flags |= PARSED;
							break;
						}
					case Constants.OBJ_TAG:
						{
							RevTag t = new RevTag(id);
							t.parseCanonical(this, data);
							r = t;
							break;
						}
					default:
						throw new ArgumentException("Bad object type: " + type);
				}
				objects.add(r);
			}
			else if ((r.flags & PARSED) == 0)
				r.parse(this);
			return r;
		}

		/**
		 * Ensure the object's content has been parsed.
		 * <p>
		 * This method only returns successfully if the object exists and was parsed
		 * without error.
		 * 
		 * @param obj
		 *            the object the caller needs to be parsed.
		 * @throws MissingObjectException
		 *             the supplied does not exist.
		 * @
		 *             a pack file or loose object could not be Read.
		 */
		public virtual void parse(RevObject obj)
		{
			if ((obj.flags & PARSED) != 0)
				return;
			obj.parse(this);
		}

		/**
		 * Create a new flag for application use during walking.
		 * <p>
		 * Applications are only assured to be able to Create 24 unique flags on any
		 * given revision walker instance. Any flags beyond 24 are offered only if
		 * the implementation has extra free space within its internal storage.
		 * 
		 * @param name
		 *            description of the flag, primarily useful for debugging.
		 * @return newly constructed flag instance.
		 * @throws ArgumentException
		 *             too many flags have been reserved on this revision walker.
		 */
		public virtual RevFlag newFlag(string name)
		{
			int m = allocFlag();
			return new RevFlag(this, name, m);
		}

		public int allocFlag()
		{
			if (freeFlags == 0)
				throw new ArgumentException(32 - RESERVED_FLAGS + " flags already created.");
			int m = freeFlags.LowestOneBit();
			freeFlags &= ~m;
			return m;
		}

		/**
		 * Automatically carry a flag from a child commit to its parents.
		 * <p>
		 * A carried flag is copied from the child commit onto its parents when the
		 * child commit is popped from the lowest level of walk's internal graph.
		 * 
		 * @param flag
		 *            the flag to carry onto parents, if set on a descendant.
		 */
		public virtual void carry(RevFlag flag)
		{
			if ((freeFlags & flag.Mask) != 0)
				throw new ArgumentException(flag.Name + " is disposed.");
			if (flag.Walker != this)
				throw new ArgumentException(flag.Name + " not from this.");
			carryFlags |= flag.Mask;
		}

		/**
		 * Automatically carry flags from a child commit to its parents.
		 * <p>
		 * A carried flag is copied from the child commit onto its parents when the
		 * child commit is popped from the lowest level of walk's internal graph.
		 * 
		 * @param set
		 *            the flags to carry onto parents, if set on a descendant.
		 */
		public virtual void carry(IEnumerable<RevFlag> set)
		{
			foreach (RevFlag flag in set)
				carry(flag);
		}

		/**
		 * Allow a flag to be recycled for a different use.
		 * <p>
		 * Recycled flags always come back as a different Java object instance when
		 * assigned again by {@link #newFlag(string)}.
		 * <p>
		 * If the flag was previously being carried, the carrying request is
		 * removed. Disposing of a carried flag while a traversal is in progress has
		 * an undefined behavior.
		 * 
		 * @param flag
		 *            the to recycle.
		 */
		public virtual void disposeFlag(RevFlag flag)
		{
			freeFlag(flag.Mask);
		}

		internal virtual void freeFlag(int mask)
		{
			if (isNotStarted())
			{
				freeFlags |= mask;
				carryFlags &= ~mask;
			}
			else
			{
				delayFreeFlags |= mask;
			}
		}

		private void finishDelayedFreeFlags()
		{
			if (delayFreeFlags != 0)
			{
				freeFlags |= delayFreeFlags;
				carryFlags &= ~delayFreeFlags;
				delayFreeFlags = 0;
			}
		}

		/**
		 * Resets internal state and allows this instance to be used again.
		 * <p>
		 * Unlike {@link #dispose()} previously acquired RevObject (and RevCommit)
		 * instances are not invalidated. RevFlag instances are not invalidated, but
		 * are removed from all RevObjects.
		 */
		public virtual void reset()
		{
			reset(0);
		}

		/**
		 * Resets internal state and allows this instance to be used again.
		 * <p>
		 * Unlike {@link #dispose()} previously acquired RevObject (and RevCommit)
		 * instances are not invalidated. RevFlag instances are not invalidated, but
		 * are removed from all RevObjects.
		 * 
		 * @param retainFlags
		 *            application flags that should <b>not</b> be cleared from
		 *            existing commit objects.
		 */
		public virtual void resetRetain(RevFlagSet retainFlags)
		{
			reset(retainFlags.Mask);
		}

		/**
		 * Resets internal state and allows this instance to be used again.
		 * <p>
		 * Unlike {@link #dispose()} previously acquired RevObject (and RevCommit)
		 * instances are not invalidated. RevFlag instances are not invalidated, but
		 * are removed from all RevObjects.
		 * 
		 * @param retainFlags
		 *            application flags that should <b>not</b> be cleared from
		 *            existing commit objects.
		 */
		public virtual void resetRetain(params RevFlag[] retainFlags)
		{
			int mask = 0;
			foreach (RevFlag flag in retainFlags)
				mask |= flag.Mask;
			reset(mask);
		}

		/**
		 * Resets internal state and allows this instance to be used again.
		 * <p>
		 * Unlike {@link #dispose()} previously acquired RevObject (and RevCommit)
		 * instances are not invalidated. RevFlag instances are not invalidated, but
		 * are removed from all RevObjects.
		 * 
		 * @param retainFlags
		 *            application flags that should <b>not</b> be cleared from
		 *            existing commit objects.
		 */
		internal virtual void reset(int retainFlags)
		{
			finishDelayedFreeFlags();
			retainFlags |= PARSED;
			int clearFlags = ~retainFlags;

			var q = new FIFORevQueue();
			foreach (RevCommit c in roots)
			{
				if ((c.flags & clearFlags) == 0) continue;
				c.flags &= retainFlags;
				c.reset();
				q.add(c);
			}

			while (true)
			{
				RevCommit c = q.next();
				if (c == null) break;
				if (c.Parents == null) continue;

				foreach (RevCommit p in c.Parents)
				{
					if ((p.flags & clearFlags) == 0) continue;
					p.flags &= retainFlags;
					p.reset();
					q.add(p);
				}
			}

			curs.Release();
			roots.Clear();
			queue = new DateRevQueue();
			pending = new StartGenerator(this);
		}

		/**
		 * Dispose all internal state and invalidate all RevObject instances.
		 * <p>
		 * All RevObject (and thus RevCommit, etc.) instances previously acquired
		 * from this RevWalk are invalidated by a dispose call. Applications must
		 * not retain or use RevObject instances obtained prior to the dispose call.
		 * All RevFlag instances are also invalidated, and must not be reused.
		 */
		public virtual void dispose()
		{
			freeFlags = APP_FLAGS;
			delayFreeFlags = 0;
			carryFlags = UNINTERESTING;
			objects.clear();
			curs.Release();
			roots.Clear();
			queue = new DateRevQueue();
			pending = new StartGenerator(this);
		}

		/**
		 * Returns an Iterator over the commits of this walker.
		 * <p>
		 * The returned iterator is only useful for one walk. If this RevWalk gets
		 * reset a new iterator must be obtained to walk over the new results.
		 * <p>
		 * Applications must not use both the Iterator and the {@link #next()} API
		 * at the same time. Pick one API and use that for the entire walk.
		 * <p>
		 * If a checked exception is thrown during the walk (see {@link #next()})
		 * it is rethrown from the Iterator as a {@link RevWalkException}.
		 * 
		 * @return an iterator over this walker's commits.
		 * @see RevWalkException
		 */

		public Iterator<RevCommit> iterator()
		{
			return new Iterator<RevCommit>(this);

		}

		public class Iterator<T> : IEnumerator<T>
			where T : RevCommit
		{
			private T first;
			private T next;
			private RevWalk revwalk;

			public Iterator(RevWalk revwalk)
			{
				this.revwalk = revwalk;
				try
				{
					first = next = (T)revwalk.next();
					//} catch (MissingObjectException e) {
					//    throw new RevWalkException(e);
					//} catch (IncorrectObjectTypeException e) {
					//    throw new RevWalkException(e);
				}
				catch (IOException e)
				{
					throw new RevWalkException(e);
				}
			}

			public T Current
			{
				get
				{
					return next;
				}
			}

			object System.Collections.IEnumerator.Current
			{
				get { return next; }
			}

			//public bool hasNext() {
			//    return this.next != null;
			//}

			public bool MoveNext()
			{
				try
				{
					T r = next;
					next = (T)revwalk.next();
					return this.next != null;
				}
				catch (MissingObjectException e)
				{
					throw new RevWalkException(e);
				}
				catch (IncorrectObjectTypeException e)
				{
					throw new RevWalkException(e);
				}
				catch (IOException e)
				{
					throw new RevWalkException(e);
				}
			}

			public void Reset()
			{
				this.next = first;
			}

			#region IDisposable Members

			public void Dispose()
			{
				first = null;
				next = null;
				revwalk = null;
			}

			#endregion


		}



		/** Throws an exception if we have started producing output. */
		internal void assertNotStarted()
		{
			if (isNotStarted())
				return;
			throw new InvalidOperationException("Output has already been started.");
		}

		private bool isNotStarted()
		{
			return pending is StartGenerator;
		}

		/**
		 * Construct a new unparsed commit for the given object.
		 * 
		 * @param id
		 *            the object this walker requires a commit reference for.
		 * @return a new unparsed reference for the object.
		 */
		internal virtual RevCommit createCommit(AnyObjectId id)
		{
			return new RevCommit(id);
		}

		internal virtual void carryFlagsImpl(RevCommit c)
		{
			int carry = c.flags & carryFlags;
			if (carry != 0)
				RevCommit.carryFlags(c, carry);
		}

		#region IEnumerable<RevCommit> Members


		public IEnumerator<RevCommit> GetEnumerator()
		{
			return iterator();
		}


		#endregion

		#region IEnumerable Members


		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return iterator();
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			dispose(); // [henon] refactor later
		}


		#endregion
	}
}
