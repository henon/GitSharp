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
namespace GitSharp.TreeWalk
{


    /**
     * Walks a Git tree (directory) in Git sort order.
     * <p>
     * A new iterator instance should be positioned on the first entry, or at eof.
     * Data for the first entry (if not at eof) should be available immediately.
     * <p>
     * Implementors must walk a tree in the Git sort order, which has the following
     * odd sorting:
     * <ol>
     * <li>A.c</li>
     * <li>A/c</li>
     * <li>A0c</li>
     * </ol>
     * <p>
     * In the second item, <code>A</code> is the name of a subtree and
     * <code>c</code> is a file within that subtree. The other two items are files
     * in the root level tree.
     * 
     * @see CanonicalTreeParser
     */
    public abstract class AbstractTreeIterator
    {
        /** Default size for the {@link #path} buffer. */
        public static int DEFAULT_PATH_SIZE = 128;

        /** A dummy object id buffer that matches the zero ObjectId. */
        public static byte[] zeroid = new byte[Constants.OBJECT_ID_LENGTH];

        /** Iterator for the parent tree; null if we are the root iterator. */
        public AbstractTreeIterator parent;

        /** The iterator this current entry is path equal to. */
        public AbstractTreeIterator matches;

        /**
         * Number of entries we moved forward to force a D/F conflict match.
         *
         * @see NameConflictTreeWalk
         */
        public int matchShift;

        /**
         * Mode bits for the current entry.
         * <p>
         * A numerical value from FileMode is usually faster for an iterator to
         * obtain from its data source so this is the preferred representation.
         * 
         * @see org.spearce.jgit.lib.FileMode
         */
        public int mode;

        /**
         * Path buffer for the current entry.
         * <p>
         * This buffer is pre-allocated at the start of walking and is shared from
         * parent iterators down into their subtree iterators. The sharing allows
         * the current entry to always be a full path from the root, while each
         * subtree only needs to populate the part that is under their control.
         */
        public byte[] path;

        /**
         * Position within {@link #path} this iterator starts writing at.
         * <p>
         * This is the first offset in {@link #path} that this iterator must
         * populate during {@link #next}. At the root level (when {@link #parent}
         * is null) this is 0. For a subtree iterator the index before this position
         * should have the value '/'.
         */
        public int pathOffset;

        /**
         * Total Length of the current entry's complete path from the root.
         * <p>
         * This is the number of bytes within {@link #path} that pertain to the
         * current entry. Values at this index through the end of the array are
         * garbage and may be randomly populated from prior entries.
         */
        public int pathLen;

        /** Create a new iterator with no parent. */
        public AbstractTreeIterator()
        {
            parent = null;
            path = new byte[DEFAULT_PATH_SIZE];
            pathOffset = 0;
        }

        /**
         * Create a new iterator with no parent and a prefix.
         * <p>
         * The prefix path supplied is inserted in front of all paths generated by
         * this iterator. It is intended to be used when an iterator is being
         * created for a subsection of an overall repository and needs to be
         * combined with other iterators that are created to run over the entire
         * repository namespace.
         *
         * @param prefix
         *            position of this iterator in the repository tree. The value
         *            may be null or the empty string to indicate the prefix is the
         *            root of the repository. A trailing slash ('/') is
         *            automatically appended if the prefix does not end in '/'.
         */
        public AbstractTreeIterator(string prefix)
        {
            parent = null;

            if (prefix != null && prefix.Length > 0)
            {
                byte[] b = Constants.CHARSET.GetBytes(prefix);
                pathLen = b.Length;
                path = new byte[Math.Max(DEFAULT_PATH_SIZE, pathLen + 1)];
                Array.Copy(b, 0, path, 0, pathLen);
                if (path[pathLen - 1] != (byte)'/')
                    path[pathLen++] = (byte)'/';
                pathOffset = pathLen;
            }
            else
            {
                path = new byte[DEFAULT_PATH_SIZE];
                pathOffset = 0;
            }
        }

        /**
         * Create a new iterator with no parent and a prefix.
         * <p>
         * The prefix path supplied is inserted in front of all paths generated by
         * this iterator. It is intended to be used when an iterator is being
         * created for a subsection of an overall repository and needs to be
         * combined with other iterators that are created to run over the entire
         * repository namespace.
         *
         * @param prefix
         *            position of this iterator in the repository tree. The value
         *            may be null or the empty array to indicate the prefix is the
         *            root of the repository. A trailing slash ('/') is
         *            automatically appended if the prefix does not end in '/'.
         */
        public AbstractTreeIterator(byte[] prefix)
        {
            parent = null;

            if (prefix != null && prefix.Length > 0)
            {
                pathLen = prefix.Length;
                path = new byte[Math.Max(DEFAULT_PATH_SIZE, pathLen + 1)];
                Array.Copy(prefix, 0, path, 0, pathLen);
                if (path[pathLen - 1] != (byte)'/')
                    path[pathLen++] = (byte)'/';
                pathOffset = pathLen;
            }
            else
            {
                path = new byte[DEFAULT_PATH_SIZE];
                pathOffset = 0;
            }
        }

        /**
         * Create an iterator for a subtree of an existing iterator.
         * 
         * @param p
         *            parent tree iterator.
         */
        public AbstractTreeIterator(AbstractTreeIterator p)
        {
            parent = p;
            path = p.path;
            pathOffset = p.pathLen + 1;
            try
            {
                path[pathOffset - 1] = (byte)'/';
            }
            catch (IndexOutOfRangeException e)
            {
                growPath(p.pathLen);
                path[pathOffset - 1] = (byte)'/';
            }
        }

        /**
         * Create an iterator for a subtree of an existing iterator.
         * <p>
         * The caller is responsible for setting up the path of the child iterator.
         *
         * @param p
         *            parent tree iterator.
         * @param childPath
         *            path array to be used by the child iterator. This path must
         *            contain the path from the top of the walk to the first child
         *            and must end with a '/'.
         * @param childPathOffset
         *            position within <code>childPath</code> where the child can
         *            insert its data. The value at
         *            <code>childPath[childPathOffset-1]</code> must be '/'.
         */
        public AbstractTreeIterator(AbstractTreeIterator p,
                 byte[] childPath, int childPathOffset)
        {
            parent = p;
            path = childPath;
            pathOffset = childPathOffset;
        }

        /**
         * Grow the path buffer larger.
         * 
         * @param len
         *            number of live bytes in the path buffer. This many bytes will
         *            be moved into the larger buffer.
         */
        public void growPath(int len)
        {
            byte[] o = path;
            byte[] n = new byte[o.Length << 1];
            Array.Copy(o, 0, n, 0, len);
            for (AbstractTreeIterator p = this; p != null && p.path == o; p = p.parent)
                p.path = n;
        }

        /**
         * Compare the path of this current entry to another iterator's entry.
         * 
         * @param p
         *            the other iterator to compare the path against.
         * @return -1 if this entry sorts first; 0 if the entries are equal; 1 if
         *         p's entry sorts first.
         */
        public int pathCompare(AbstractTreeIterator p)
        {
            return pathCompare(p, p.mode);
        }

        int pathCompare(AbstractTreeIterator p, int pMode)
        {
            byte[] a = path;
            byte[] b = p.path;
            int aLen = pathLen;
            int bLen = p.pathLen;
            int cPos;

            // Its common when we are a subtree for both parents to match;
            // when this happens everything in path[0..cPos] is known to
            // be equal and does not require evaluation again.
            //
            cPos = alreadyMatch(this, p);

            for (; cPos < aLen && cPos < bLen; cPos++)
            {
                int cmp = (a[cPos] & 0xff) - (b[cPos] & 0xff);
                if (cmp != 0)
                    return cmp;
            }

            if (cPos < aLen)
                return (a[cPos] & 0xff) - lastPathChar(pMode);
            if (cPos < bLen)
                return lastPathChar(mode) - (b[cPos] & 0xff);
            return lastPathChar(mode) - lastPathChar(pMode);
        }

        private static int alreadyMatch(AbstractTreeIterator a, AbstractTreeIterator b)
        {
            for (; ; )
            {
                AbstractTreeIterator ap = a.parent;
                AbstractTreeIterator bp = b.parent;
                if (ap == null || bp == null)
                    return 0;
                if (ap.matches == bp.matches)
                    return a.pathOffset;
                a = ap;
                b = bp;
            }
        }

        private static int lastPathChar(int mode)
        {
            return FileMode.Tree.Equals(mode) ? (byte)'/' : (byte)'\0';
        }

        /**
         * Check if the current entry of both iterators has the same id.
         * <p>
         * This method is faster than {@link #getEntryObjectId()} as it does not
         * require copying the bytes out of the buffers. A direct {@link #idBuffer}
         * compare operation is performed.
         * 
         * @param otherIterator
         *            the other iterator to test against.
         * @return true if both iterators have the same object id; false otherwise.
         */
        public virtual bool idEqual(AbstractTreeIterator otherIterator)
        {
            return ObjectId.Equals(idBuffer(), idOffset(), otherIterator.idBuffer(), otherIterator.idOffset());
        }

        /**
         * Get the object id of the current entry.
         * 
         * @return an object id for the current entry.
         */
        public virtual ObjectId getEntryObjectId()
        {
            return ObjectId.FromRaw(idBuffer(), idOffset());
        }

        /**
         * Obtain the ObjectId for the current entry.
         *
         * @param out
         *            buffer to copy the object id into.
         */
        public virtual void getEntryObjectId(MutableObjectId @out)
        {
            @out.FromRaw(idBuffer(), idOffset());
        }

        /** @return the file mode of the current entry. */
        public virtual FileMode getEntryFileMode()
        {
            return FileMode.FromBits(mode);
        }

        /** @return path of the current entry, as a string. */
        public string getEntryPathString()
        {
            return TreeWalk.pathOf(this);
        }

        /**
         * Get the byte array buffer object IDs must be copied out of.
         * <p>
         * The id buffer contains the bytes necessary to construct an ObjectId for
         * the current entry of this iterator. The buffer can be the same buffer for
         * all entries, or it can be a unique buffer per-entry. Implementations are
         * encouraged to expose their private buffer whenever possible to reduce
         * garbage generation and copying costs.
         * 
         * @return byte array the implementation stores object IDs within.
         * @see #getEntryObjectId()
         */
        public abstract byte[] idBuffer();

        /**
         * Get the position within {@link #idBuffer()} of this entry's ObjectId.
         * 
         * @return offset into the array returned by {@link #idBuffer()} where the
         *         ObjectId must be copied out of.
         */
        public abstract int idOffset();

        /**
         * Create a new iterator for the current entry's subtree.
         * <p>
         * The parent reference of the iterator must be <code>this</code>,
         * otherwise the caller would not be able to exit out of the subtree
         * iterator correctly and return to continue walking <code>this</code>.
         * 
         * @param repo
         *            repository to load the tree data from.
         * @return a new parser that walks over the current subtree.
         * @throws IncorrectObjectTypeException
         *             the current entry is not actually a tree and cannot be parsed
         *             as though it were a tree.
         * @throws IOException
         *             a loose object or pack file could not be read.
         */
        public abstract AbstractTreeIterator createSubtreeIterator(Repository repo);

        /**
         * Create a new iterator as though the current entry were a subtree.
         *
         * @return a new empty tree iterator.
         */
        public virtual EmptyTreeIterator createEmptyTreeIterator()
        {
            return new EmptyTreeIterator(this);
        }

        /**
         * Create a new iterator for the current entry's subtree.
         * <p>
         * The parent reference of the iterator must be <code>this</code>, otherwise
         * the caller would not be able to exit out of the subtree iterator
         * correctly and return to continue walking <code>this</code>.
         *
         * @param repo
         *            repository to load the tree data from.
         * @param idBuffer
         *            temporary ObjectId buffer for use by this method.
         * @param curs
         *            window cursor to use during repository access.
         * @return a new parser that walks over the current subtree.
         * @throws IncorrectObjectTypeException
         *             the current entry is not actually a tree and cannot be parsed
         *             as though it were a tree.
         * @throws IOException
         *             a loose object or pack file could not be read.
         */
        public virtual AbstractTreeIterator createSubtreeIterator(Repository repo, MutableObjectId idBuffer, WindowCursor curs)
        {
            return createSubtreeIterator(repo);
        }

        /**
         * Is this tree iterator positioned on its first entry?
         * <p>
         * An iterator is positioned on the first entry if <code>back(1)</code>
         * would be an invalid request as there is no entry before the current one.
         * <p>
         * An empty iterator (one with no entries) will be
         * <code>first() &amp;&amp; eof()</code>.
         *
         * @return true if the iterator is positioned on the first entry.
         */
        public abstract bool first();

        /**
         * Is this tree iterator at its EOF point (no more entries)?
         * <p>
         * An iterator is at EOF if there is no current entry.
         * 
         * @return true if we have walked all entries and have none left.
         */
        public abstract bool eof();

        /**
         * Move to next entry, populating this iterator with the entry data.
         * <p>
         * The delta indicates how many moves forward should occur. The most common
         * delta is 1 to move to the next entry.
         * <p>
         * Implementations must populate the following members:
         * <ul>
         * <li>{@link #mode}</li>
         * <li>{@link #path} (from {@link #pathOffset} to {@link #pathLen})</li>
         * <li>{@link #pathLen}</li>
         * </ul>
         * as well as any implementation dependent information necessary to
         * accurately return data from {@link #idBuffer()} and {@link #idOffset()}
         * when demanded.
         *
         * @param delta
         *            number of entries to move the iterator by. Must be a positive,
         *            non-zero integer.
         * @throws CorruptObjectException
         *             the tree is invalid.
         */
        public abstract void next(int delta);

        /**
         * Move to prior entry, populating this iterator with the entry data.
         * <p>
         * The delta indicates how many moves backward should occur.The most common
         * delta is 1 to move to the prior entry.
         * <p>
         * Implementations must populate the following members:
         * <ul>
         * <li>{@link #mode}</li>
         * <li>{@link #path} (from {@link #pathOffset} to {@link #pathLen})</li>
         * <li>{@link #pathLen}</li>
         * </ul>
         * as well as any implementation dependent information necessary to
         * accurately return data from {@link #idBuffer()} and {@link #idOffset()}
         * when demanded.
         * 
         * @param delta
         *            number of entries to move the iterator by. Must be a positive,
         *            non-zero integer.
         * @throws CorruptObjectException
         *             the tree is invalid.
         */
        public abstract void back(int delta);

        /**
         * Advance to the next tree entry, populating this iterator with its data.
         * <p>
         * This method behaves like <code>seek(1)</code> but is called by
         * {@link TreeWalk} only if a {@link TreeFilter} was used and ruled out the
         * current entry from the results. In such cases this tree iterator may
         * perform special behavior.
         *
         * @throws CorruptObjectException
         *             the tree is invalid.
         */
        public virtual void skip()
        {
            next(1);
        }

        /**
         * Indicates to the iterator that no more entries will be read.
         * <p>
         * This is only invoked by TreeWalk when the iteration is aborted early due
         * to a {@link org.spearce.jgit.errors.StopWalkException} being thrown from
         * within a TreeFilter.
         */
        public virtual void stopWalk()
        {
            // Do nothing by default.  Most iterators do not care.
        }
    }
}
