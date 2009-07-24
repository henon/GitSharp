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

namespace GitSharp.DirectoryCache
{
    /**
     * Generic update/editing support for {@link DirCache}.
     * <p>
     * The different update strategies extend this class to provide their own unique
     * services to applications.
     */
    public abstract class BaseDirCacheEditor
    {
        /** The cache instance this editor updates during {@link #finish()}. */
        protected DirCache cache;

        /**
         * Entry table this builder will eventually replace into {@link #cache}.
         * <p>
         * Use {@link #fastAdd(DirCacheEntry)} or {@link #fastKeep(int, int)} to
         * make additions to this table. The table is automatically expanded if it
         * is too small for a new addition.
         * <p>
         * Typically the entries in here are sorted by their path names, just like
         * they are in the DirCache instance.
         */
        protected DirCacheEntry[] entries;

        /** Total number of valid entries in {@link #entries}. */
        protected int entryCnt;

        /**
         * Construct a new editor.
         *
         * @param dc
         *            the cache this editor will eventually update.
         * @param ecnt
         *            estimated number of entries the editor will have upon
         *            completion. This sizes the initial entry table.
         */
        protected BaseDirCacheEditor(DirCache dc, int ecnt)
        {
            cache = dc;
            entries = new DirCacheEntry[ecnt];
        }

        /**
         * @return the cache we will update on {@link #finish()}.
         */
        public DirCache getDirCache()
        {
            return cache;
        }

        /**
         * Append one entry into the resulting entry list.
         * <p>
         * The entry is placed at the end of the entry list. The caller is
         * responsible for making sure the  table is correctly sorted.
         * <p>
         * The {@link #entries} table is automatically expanded if there is
         * insufficient space for the new addition.
         *
         * @param newEntry
         *            the new entry to add.
         */
        protected void fastAdd(DirCacheEntry newEntry)
        {
            if (entries.length == entryCnt)
            {
                DirCacheEntry[] n = new DirCacheEntry[(entryCnt + 16) * 3 / 2];
                System.arraycopy(entries, 0, n, 0, entryCnt);
                entries = n;
            }
            entries[entryCnt++] = newEntry;
        }

        /**
         * Add a range of existing entries from the destination cache.
         * <p>
         * The entries are placed at the end of the entry list, preserving their
         * current order. The caller is responsible for making sure the  table
         * is correctly sorted.
         * <p>
         * This method copies from the destination cache, which has not yet been
         * updated with this editor's new table. So all offsets into the destination
         * cache are not affected by any updates that may be currently taking place
         * in this editor.
         * <p>
         * The {@link #entries} table is automatically expanded if there is
         * insufficient space for the new additions.
         *
         * @param pos
         *            first entry to copy from the destination cache.
         * @param cnt
         *            number of entries to copy.
         */
        protected void fastKeep(int pos, int cnt)
        {
            if (entryCnt + cnt > entries.length)
            {
                int m1 = (entryCnt + 16) * 3 / 2;
                int m2 = entryCnt + cnt;
                DirCacheEntry[] n = new DirCacheEntry[Math.max(m1, m2)];
                System.arraycopy(entries, 0, n, 0, entryCnt);
                entries = n;
            }

            cache.toArray(pos, entries, entryCnt, cnt);
            entryCnt += cnt;
        }

        /**
         * Finish this builder and update the destination {@link DirCache}.
         * <p>
         * When this method completes this builder instance is no longer usable by
         * the calling application. A new builder must be created to make additional
         * changes to the index entries.
         * <p>
         * After completion the DirCache returned by {@link #getDirCache()} will
         * contain all modifications.
         * <p>
         * <i>Note to implementors:</i> Make sure {@link #entries} is fully sorted
         * then invoke {@link #replace()} to update the DirCache with the new table.
         */
        public abstract void finish();

        /**
         * Update the DirCache with the contents of {@link #entries}.
         * <p>
         * This method should be invoked only during an implementation of
         * {@link #finish()}, and only after {@link #entries} is sorted.
         */
        protected void replace()
        {
            if (entryCnt < entries.length / 2)
            {
                DirCacheEntry[] n = new DirCacheEntry[entryCnt];
                System.arraycopy(entries, 0, n, 0, entryCnt);
                entries = n;
            }
            cache.replace(entries, entryCnt);
        }

        /**
         * Finish, write, commit this change, and release the index lock.
         * <p>
         * If this method fails (returns false) the lock is still released.
         * <p>
         * This is a utility method for applications as the finish-write-commit
         * pattern is very common after using a builder to update entries.
         *
         * @return true if the commit was successful and the file contains the new
         *         data; false if the commit failed and the file remains with the
         *         old data.
         * @throws IllegalStateException
         *             the lock is not held.
         * @throws IOException
         *             the output file could not be created. The caller no longer
         *             holds the lock.
         */
        public bool commit()
        {
            finish();
            cache.write();
            return cache.commit();
        }
    }

}
