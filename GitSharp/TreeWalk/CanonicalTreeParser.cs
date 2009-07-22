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

using GitSharp.Exceptions;
using System;

namespace GitSharp.TreeWalk
{


    /** Parses raw Git trees from the canonical semi-text/semi-binary format. */
    public class CanonicalTreeParser : AbstractTreeIterator
    {
        private static byte[] EMPTY = { };

        private byte[] raw;

        /** First offset within {@link #raw} of the current entry's data. */
        private int currPtr;

        /** Offset one past the current entry (first byte of next entry. */
        private int nextPtr;

        /** Create a new parser. */
        public CanonicalTreeParser()
        {
            raw = EMPTY;
        }

        /**
         * Create a new parser for a tree appearing in a subset of a repository.
         *
         * @param prefix
         *            position of this iterator in the repository tree. The value
         *            may be null or the empty array to indicate the prefix is the
         *            root of the repository. A trailing slash ('/') is
         *            automatically appended if the prefix does not end in '/'.
         * @param repo
         *            repository to load the tree data from.
         * @param treeId
         *            identity of the tree being parsed; used only in exception
         *            messages if data corruption is found.
         * @param curs
         *            a window cursor to use during data access from the repository.
         * @throws MissingObjectException
         *             the object supplied is not available from the repository.
         * @throws IncorrectObjectTypeException
         *             the object supplied as an argument is not actually a tree and
         *             cannot be parsed as though it were a tree.
         * @throws IOException
         *             a loose object or pack file could not be read.
         */
        public CanonicalTreeParser(byte[] prefix, Repository repo, AnyObjectId treeId, WindowCursor curs)
            : base(prefix)
        {

            reset(repo, treeId, curs);
        }

        private CanonicalTreeParser(CanonicalTreeParser p)
            : base(p)
        {

        }

        /**
         * Reset this parser to walk through the given tree data.
         * 
         * @param treeData
         *            the raw tree content.
         */
        public void reset(byte[] treeData)
        {
            raw = treeData;
            currPtr = 0;
            if (!eof())
                parseEntry();
        }

        /**
         * Reset this parser to walk through the given tree.
         * 
         * @param repo
         *            repository to load the tree data from.
         * @param id
         *            identity of the tree being parsed; used only in exception
         *            messages if data corruption is found.
         * @param curs
         *            window cursor to use during repository access.
         * @return the root level parser.
         * @throws MissingObjectException
         *             the object supplied is not available from the repository.
         * @throws IncorrectObjectTypeException
         *             the object supplied as an argument is not actually a tree and
         *             cannot be parsed as though it were a tree.
         * @throws IOException
         *             a loose object or pack file could not be read.
         */
        public CanonicalTreeParser resetRoot(Repository repo, AnyObjectId id, WindowCursor curs)
        {
            CanonicalTreeParser p = this;
            while (p.parent != null)
                p = (CanonicalTreeParser)p.parent;
            p.reset(repo, id, curs);
            return p;
        }

        /** @return this iterator, or its parent, if the tree is at eof. */
        public CanonicalTreeParser next()
        {
            CanonicalTreeParser p = this;
            for (; ; )
            {
                p.next(1);
                if (p.eof() && p.parent != null)
                {
                    // Parent was left pointing at the entry for us; advance
                    // the parent to the next entry, possibly unwinding many
                    // levels up the tree.
                    //
                    p = (CanonicalTreeParser)p.parent;
                    continue;
                }
                return p;
            }
        }

        /**
         * Reset this parser to walk through the given tree.
         *
         * @param repo
         *            repository to load the tree data from.
         * @param id
         *            identity of the tree being parsed; used only in exception
         *            messages if data corruption is found.
         * @param curs
         *            window cursor to use during repository access.
         * @throws MissingObjectException
         *             the object supplied is not available from the repository.
         * @throws IncorrectObjectTypeException
         *             the object supplied as an argument is not actually a tree and
         *             cannot be parsed as though it were a tree.
         * @throws IOException
         *             a loose object or pack file could not be read.
         */
        public void reset(Repository repo, AnyObjectId id, WindowCursor curs)
        {
            ObjectLoader ldr = repo.openObject(curs, id);
            if (ldr == null)
            {
                ObjectId me = id.ToObjectId();
                throw new MissingObjectException(me, Constants.TYPE_TREE);
            }
            byte[] subtreeData = ldr.getCachedBytes();
            if (ldr.getType() != Constants.OBJ_TREE)
            {
                ObjectId me = id.ToObjectId();
                throw new IncorrectObjectTypeException(me, Constants.TYPE_TREE);
            }
            reset(subtreeData);
        }

        public new CanonicalTreeParser createSubtreeIterator(Repository repo, MutableObjectId idBuffer, WindowCursor curs)
        {
            idBuffer.FromRaw(this.idBuffer(), this.idOffset());
            if (!FileMode.Tree.Equals(mode))
            {
                ObjectId me = idBuffer.ToObjectId();
                throw new IncorrectObjectTypeException(me, Constants.TYPE_TREE);
            }
            return createSubtreeIterator0(repo, idBuffer, curs);
        }

        /**
         * Back door to quickly create a subtree iterator for any subtree.
         * <p>
         * Don't use this unless you are ObjectWalk. The method is meant to be
         * called only once the current entry has been identified as a tree and its
         * identity has been converted into an ObjectId.
         *
         * @param repo
         *            repository to load the tree data from.
         * @param id
         *            ObjectId of the tree to open.
         * @param curs
         *            window cursor to use during repository access.
         * @return a new parser that walks over the current subtree.
         * @throws IOException
         *             a loose object or pack file could not be read.
         */
        public CanonicalTreeParser createSubtreeIterator0(Repository repo, AnyObjectId id, WindowCursor curs) // [henon] createSubtreeIterator0 <--- not a typo!
        {
            CanonicalTreeParser p = new CanonicalTreeParser(this);
            p.reset(repo, id, curs);
            return p;
        }

        public override AbstractTreeIterator createSubtreeIterator(Repository repo)
        {
            WindowCursor curs = new WindowCursor();
            try
            {
                return createSubtreeIterator(repo, new MutableObjectId(), curs);
            }
            finally
            {
                curs.release();
            }
        }

        public override byte[] idBuffer()
        {
            return raw;
        }

        public override int idOffset()
        {
            return nextPtr - Constants.OBJECT_ID_LENGTH;
        }

        public override bool first()
        {
            return currPtr == 0;
        }

        public override bool eof()
        {
            return currPtr == raw.Length;
        }

        public override void next(int delta)
        {
            if (delta == 1)
            {
                // Moving forward one is the most common case.
                //
                currPtr = nextPtr;
                if (!eof())
                    parseEntry();
                return;
            }

            // Fast skip over records, then parse the last one.
            //
            int end = raw.Length;
            int ptr = nextPtr;
            while (--delta > 0 && ptr != end)
            {
                while (raw[ptr] != 0)
                    ptr++;
                ptr += Constants.OBJECT_ID_LENGTH + 1;
            }
            if (delta != 0)
                throw new IndexOutOfRangeException(delta.ToString());
            currPtr = ptr;
            if (!eof())
                parseEntry();
        }

        public override void back(int delta)
        {
            int ptr = currPtr;
            while (--delta >= 0)
            {
                if (ptr == 0)
                    throw new IndexOutOfRangeException(delta.ToString());

                // Rewind back beyond the id and the null byte. Find the
                // last space, this _might_ be the split between the mode
                // and the path. Most paths in most trees do not contain a
                // space so this prunes our search more quickly.
                //
                ptr -= Constants.OBJECT_ID_LENGTH;
                while (raw[--ptr] != ' ')
                {
                    /* nothing */
                }
                if (--ptr < Constants.OBJECT_ID_LENGTH)
                {
                    if (delta != 0)
                        throw new IndexOutOfRangeException(delta.ToString());
                    ptr = 0;
                    break;
                }

                // Locate a position that matches "\0.{20}[0-7]" such that
                // the ptr will rest on the [0-7]. This must be the first
                // byte of the mode. This search works because the path in
                // the prior record must have a non-zero Length and must not
                // contain a null byte.
                //
                for (int n; ; ptr = n)
                {
                    n = ptr - 1;
                    byte b = raw[n];
                    if ((byte)'0' <= b && b <= (byte)'7')
                        continue;
                    if (raw[n - Constants.OBJECT_ID_LENGTH] != 0)
                        continue;
                    break;
                }
            }
            currPtr = ptr;
            parseEntry();
        }

        private void parseEntry()
        {
            int ptr = currPtr;
            byte c = raw[ptr++];
            int tmp = c - (byte)'0';
            for (; ; )
            {
                c = raw[ptr++];
                if (' ' == c)
                    break;
                tmp <<= 3;
                tmp += c - (byte)'0';
            }
            mode = tmp;

            tmp = pathOffset;
            for (; ; tmp++)
            {
                c = raw[ptr++];
                if (c == 0)
                    break;
                try
                {
                    path[tmp] = c;
                }
                catch (IndexOutOfRangeException)
                {
                    growPath(tmp);
                    path[tmp] = c;
                }
            }
            pathLen = tmp;
            nextPtr = ptr + Constants.OBJECT_ID_LENGTH;
        }
    }
}