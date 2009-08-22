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




using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.TreeWalk;
using GitSharp.Exceptions;
using GitSharp.Util;

namespace GitSharp.DirectoryCache
{

    /**
     * Iterate a {@link DirCache} as part of a <code>TreeWalk</code>.
     * <p>
     * This is an iterator to adapt a loaded <code>DirCache</code> instance (such as
     * read from an existing <code>.git/index</code> file) to the tree structure
     * used by a <code>TreeWalk</code>, making it possible for applications to walk
     * over any combination of tree objects already in the object database, index
     * files, or working directories.
     *
     * @see org.spearce.jgit.treewalk.TreeWalk
     */
    public class DirCacheIterator : AbstractTreeIterator
    {
        /** The cache this iterator was created to walk. */
        public DirCache cache;

        /** The tree this iterator is walking. */
        private DirCacheTree tree;

        /** First position in this tree. */
        private int treeStart;

        /** Last position in this tree. */
        private int treeEnd;

        /** Special buffer to hold the ObjectId of {@link #currentSubtree}. */
        private byte[] subtreeId;

        /** Index of entry within {@link #cache}. */
        public int ptr;

        /** Next subtree to consider within {@link #tree}. */
        private int nextSubtreePos;

        /** The current file entry from {@link #cache}. */
        public DirCacheEntry currentEntry;

        /** The subtree containing {@link #currentEntry} if this is first entry. */
        public DirCacheTree currentSubtree;

        /**
         * Create a new iterator for an already loaded DirCache instance.
         * <p>
         * The iterator implementation may copy part of the cache's data during
         * construction, so the cache must be read in prior to creating the
         * iterator.
         *
         * @param dc
         *            the cache to walk. It must be already loaded into memory.
         */
        public DirCacheIterator(DirCache dc)
        {
            cache = dc;
            tree = dc.getCacheTree(true);
            treeStart = 0;
            treeEnd = tree.getEntrySpan();
            subtreeId = new byte[Constants.OBJECT_ID_LENGTH];
            if (!eof())
                parseEntry();
        }

        public DirCacheIterator(DirCacheIterator p, DirCacheTree dct)
            : base(p, p.path, p.pathLen + 1)
        {

            cache = p.cache;
            tree = dct;
            treeStart = p.ptr;
            treeEnd = treeStart + tree.getEntrySpan();
            subtreeId = p.subtreeId;
            ptr = p.ptr;
            parseEntry();
        }


        public override AbstractTreeIterator createSubtreeIterator(Repository repo)
        {
            if (currentSubtree == null)
                throw new IncorrectObjectTypeException(getEntryObjectId(), Constants.TYPE_TREE);
            return new DirCacheIterator(this, currentSubtree);
        }


        public override EmptyTreeIterator createEmptyTreeIterator()
        {
            byte[] n = new byte[Math.Max(pathLen + 1, DEFAULT_PATH_SIZE)];
            Array.Copy(path, 0, n, 0, pathLen);
            n[pathLen] = (byte)'/';
            return new EmptyTreeIterator(this, n, pathLen + 1);
        }


        public override byte[] idBuffer()
        {
            if (currentSubtree != null)
                return subtreeId;
            if (currentEntry != null)
                return currentEntry.idBuffer();
            return zeroid;
        }


        public override int idOffset()
        {
            if (currentSubtree != null)
                return 0;
            if (currentEntry != null)
                return currentEntry.idOffset();
            return 0;
        }


        public override bool first()
        {
            return ptr == treeStart;
        }


        public override bool eof()
        {
            return ptr == treeEnd;
        }


        public override void next(int delta)
        {
            while (--delta >= 0)
            {
                if (currentSubtree != null)
                    ptr += currentSubtree.getEntrySpan();
                else
                    ptr++;
                if (eof())
                    break;
                parseEntry();
            }
        }


        public override void back(int delta)
        {
            while (--delta >= 0)
            {
                if (currentSubtree != null)
                    nextSubtreePos--;
                ptr--;
                parseEntry();
                if (currentSubtree != null)
                    ptr -= currentSubtree.getEntrySpan() - 1;
            }
        }

        private void parseEntry()
        {
            currentEntry = cache.getEntry(ptr);
            byte[] cep = currentEntry.path;

            if (nextSubtreePos != tree.getChildCount())
            {
                DirCacheTree s = tree.getChild(nextSubtreePos);
                if (s.contains(cep, pathOffset, cep.Length))
                {
                    // The current position is the first file of this subtree.
                    // Use the subtree instead as the current position.
                    //
                    currentSubtree = s;
                    nextSubtreePos++;

                    if (s.isValid())
                        s.getObjectId().copyRawTo(subtreeId, 0);
                    else
                        subtreeId.Fill( (byte)0);
                    mode = FileMode.Tree.Bits;
                    path = cep;
                    pathLen = pathOffset + s.nameLength();
                    return;
                }
            }

            // The current position is a file/symlink/gitlink so we
            // do not have a subtree located here.
            //
            mode = currentEntry.getRawMode();
            path = cep;
            pathLen = cep.Length;
            currentSubtree = null;
        }

        /**
         * Get the DirCacheEntry for the current file.
         *
         * @return the current cache entry, if this iterator is positioned on a
         *         non-tree.
         */
        public DirCacheEntry getDirCacheEntry()
        {
            return currentSubtree == null ? currentEntry : null;
        }
    }

}
