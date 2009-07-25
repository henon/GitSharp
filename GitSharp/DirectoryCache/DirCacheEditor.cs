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
using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;

namespace GitSharp.DirectoryCache
{
    /**
     * Updates a {@link DirCache} by supplying discrete edit commands.
     * <p>
     * An editor updates a DirCache by taking a list of {@link PathEdit} commands
     * and executing them against the entries of the destination cache to produce a
     * new cache. This edit style allows applications to insert a few commands and
     * then have the editor compute the proper entry indexes necessary to perform an
     * efficient in-order update of the index records. This can be easier to use
     * than {@link DirCacheBuilder}.
     * <p>
     *
     * @see DirCacheBuilder
     */
    public class DirCacheEditor : BaseDirCacheEditor
    {
        private static Comparison<PathEdit> EDIT_CMP = (o1, o2) =>
        {
            byte[] a = o1.path;
            byte[] b = o2.path;
            return DirCache.cmp(a, a.Length, b, b.Length);
        };

        private List<PathEdit> edits;

        /**
         * Construct a new editor.
         *
         * @param dc
         *            the cache this editor will eventually update.
         * @param ecnt
         *            estimated number of entries the editor will have upon
         *            completion. This sizes the initial entry table.
         */
        public DirCacheEditor(DirCache dc, int ecnt)
            : base(dc, ecnt)
        {
            edits = new List<PathEdit>();
        }

        /**
         * Append one edit command to the list of commands to be applied.
         * <p>
         * Edit commands may be added in any order chosen by the application. They
         * are automatically rearranged by the builder to provide the most efficient
         * update possible.
         *
         * @param edit
         *            another edit command.
         */
        public void add(PathEdit edit)
        {
            edits.Add(edit);
        }

        public override bool commit()
        {
            if (edits.Count == 0) // isEmpty()
            {
                // No changes? Don't rewrite the index.
                //
                cache.unlock();
                return true;
            }
            return base.commit();
        }

        public override void finish()
        {
            if (edits.Count > 0) // !edits.isEmpty()
            { 
                applyEdits();
                replace();
            }
        }

        private void applyEdits()
        {
            edits.Sort(EDIT_CMP);

            int maxIdx = cache.getEntryCount();
            int lastIdx = 0;
            foreach (PathEdit e in edits)
            {
                int eIdx = cache.findEntry(e.path, e.path.Length);
                bool missing = eIdx < 0;
                if (eIdx < 0)
                    eIdx = -(eIdx + 1);
                int cnt = Math.Min(eIdx, maxIdx) - lastIdx;
                if (cnt > 0)
                    fastKeep(lastIdx, cnt);
                lastIdx = missing ? eIdx : cache.nextEntry(eIdx);

                if (e is DeletePath)
                    continue;
                if (e is DeleteTree)
                {
                    lastIdx = cache.nextEntry(e.path, e.path.Length, eIdx);
                    continue;
                }

                DirCacheEntry ent;
                if (missing)
                    ent = new DirCacheEntry(e.path);
                else
                    ent = cache.getEntry(eIdx);
                e.apply(ent);
                fastAdd(ent);
            }

            int count = maxIdx - lastIdx;
            if (count > 0)
                fastKeep(lastIdx, count);
        }

        /**
         * Any index record update.
         * <p>
         * Applications should subclass and provide their own implementation for the
         * {@link #apply(DirCacheEntry)} method. The editor will invoke apply once
         * for each record in the index which matches the path name. If there are
         * multiple records (for example in stages 1, 2 and 3), the edit instance
         * will be called multiple times, once for each stage.
         */
        public abstract class PathEdit
        {
            public byte[] path;

            /**
             * Create a new update command by path name.
             *
             * @param entryPath
             *            path of the file within the repository.
             */
            public PathEdit(String entryPath)
            {
                path = Constants.encode(entryPath);
            }

            /**
             * Create a new update command for an existing entry instance.
             *
             * @param ent
             *            entry instance to match path of. Only the path of this
             *            entry is actually considered during command evaluation.
             */
            public PathEdit(DirCacheEntry ent)
            {
                path = ent.path;
            }

            /**
             * Apply the update to a single cache entry matching the path.
             * <p>
             * After apply is invoked the entry is added to the output table, and
             * will be included in the new index.
             *
             * @param ent
             *            the entry being processed. All fields are zeroed out if
             *            the path is a new path in the index.
             */
            public abstract void apply(DirCacheEntry ent);
        }

        /**
         * Deletes a single file entry from the index.
         * <p>
         * This deletion command removes only a single file at the given location,
         * but removes multiple stages (if present) for that path. To remove a
         * complete subtree use {@link DeleteTree} instead.
         *
         * @see DeleteTree
         */
        public class DeletePath : PathEdit
        {
            /**
             * Create a new deletion command by path name.
             *
             * @param entryPath
             *            path of the file within the repository.
             */
            public DeletePath(String entryPath)
                : base(entryPath)
            {
            }

            /**
             * Create a new deletion command for an existing entry instance.
             *
             * @param ent
             *            entry instance to remove. Only the path of this entry is
             *            actually considered during command evaluation.
             */
            public DeletePath(DirCacheEntry ent)
                : base(ent)
            {
            }

            public override void apply(DirCacheEntry ent)
            {
                throw new NotSupportedException("No apply in delete");
            }
        }

        /**
         * Recursively deletes all paths under a subtree.
         * <p>
         * This deletion command is more generic than {@link DeletePath} as it can
         * remove all records which appear recursively under the same subtree.
         * Multiple stages are removed (if present) for any deleted entry.
         * <p>
         * This command will not remove a single file entry. To remove a single file
         * use {@link DeletePath}.
         *
         * @see DeletePath
         */
        public class DeleteTree : PathEdit
        {
            /**
             * Create a new tree deletion command by path name.
             *
             * @param entryPath
             *            path of the subtree within the repository. If the path
             *            does not end with "/" a "/" is implicitly added to ensure
             *            only the subtree's contents are matched by the command.
             */
            public DeleteTree(String entryPath)
                : base(entryPath.EndsWith("/") ? entryPath : entryPath + "/")
            {
            }

            public override void apply(DirCacheEntry ent)
            {
                throw new NotSupportedException("No apply in delete");
            }
        }
    }

}
