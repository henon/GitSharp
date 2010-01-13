/*
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

namespace GitSharp
{
    /// <summary>
    /// A summary of changes made to the working directory of a repository with respect to its index.
    /// </summary>
    public class RepositoryStatus
    {
        private GitSharp.Core.IndexDiff _diff;

        public RepositoryStatus(Repository repo)
        {
            _diff = new GitSharp.Core.IndexDiff(repo._internal_repo);
            AnyDifferences = _diff.Diff();
        }

        internal RepositoryStatus(GitSharp.Core.IndexDiff diff)
        {
            _diff = diff;
            AnyDifferences = _diff.Diff();
        }

        public bool AnyDifferences { get; private set; }

        /// <summary>
        /// List of files added to the index, which are not in the current commit
        /// </summary>
        public HashSet<string> Added { get { return _diff.Added; } }

        /// <summary>
        /// List of files added to the index, which are already in the current commit with different content
        /// </summary>
        public HashSet<string> Staged { get { return _diff.Changed; } }

        /// <summary>
        /// List of files removed from the index but are existent in the current commit
        /// </summary>
        public HashSet<string> Removed { get { return _diff.Removed; } }

        /// <summary>
        /// List of files existent in the index but are missing in the working directory
        /// </summary>
        public HashSet<string> Missing { get { return _diff.Missing; } }

        /// <summary>
        /// List of files with unstaged modifications. A file may be modified and staged at the same time if it has been modified after adding.
        /// </summary>
        public HashSet<string> Modified { get { return _diff.Modified; } }

         /// <summary>
         /// List of files existing in the working directory but are neither tracked in the index nor in the current commit.
         /// </summary>
        public HashSet<string> Untracked { get { return _diff.Untracked; } }

        /// <summary>
        /// List of files with staged modifications that conflict.
        /// </summary>
        public HashSet<string> MergeConflict { get { return _diff.MergeConflict; } }

        /// <summary>
        /// Returns the number of files checked into the git repository
        /// </summary>
        public int IndexSize { get { return _diff.IndexSize; } } 
    }
}
