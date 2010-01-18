/*
 * Copyright (C) 2010, Rolenun <rolenun@gmail.com>
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
using System.Linq;
using System.Text;
using GitSharp.Core;

namespace GitSharp.Commands
{
    #region Status Command
    public class StatusCommand : AbstractCommand
    {
    	private StatusResults results = new StatusResults();

        public StatusCommand()
        {
            AnyDifferences = false;
        }

        public Boolean AnyDifferences { get; set; }
        public int IndexSize { get; private set; }
        public StatusResults Results 
        {
        	get { return results; } 
        	private set { results = value; }
        }

        public override void Execute()
        {
            RepositoryStatus status = new RepositoryStatus(Repository);
            
            IgnoreRules rules;

            //Read ignore file list and remove from the untracked list
            try
            {
                rules = new IgnoreRules(Path.Combine(Repository.WorkingDirectory, ".gitignore"));
            }
            catch (FileNotFoundException) 
            {
                //.gitignore file does not exist for a newly initialized repository.
                string[] lines = {};
                rules = new IgnoreRules(lines);
            } 

            foreach (string hash in status.Untracked)
            {
                string path = Path.Combine(Repository.WorkingDirectory, hash);
                if (!rules.IgnoreFile(Repository.WorkingDirectory, path) && !rules.IgnoreDir(Repository.WorkingDirectory, path))
                {
                    results.UntrackedList.Add(hash);
                }
            }
            
            if (status.AnyDifferences || results.UntrackedList.Count > 0)
            {
                // Files use the following states: removed, missing, added, and modified.
                // If a file has been staged, it is also added to the RepositoryStatus.Staged HashSet.
                //
                // The StatusState known as "Untracked" is determined by what is *not* staged or modified.
                // It is then intersected with the .gitignore list to determine what should be listed as untracked.
                // Using intersections will accurately display the "bucket" each file was added to.

                // Note: In standard git, they use cached references so the following scenario is possible. 
                //    1) Filename = a.txt; StatusState=staged; StatusType=added
                //    2) Filename = a.txt; StatusState=modified; StatusType=added
                // Notice that the same filename exists in two separate status's because it points to a reference
                // Todo: This test has failed so far with this command.

                HashSet<string> stagedRemoved = new HashSet<string>(status.Staged);
                stagedRemoved.IntersectWith(status.Removed);
                HashSet<string> stagedMissing = new HashSet<string>(status.Staged);
                stagedMissing.IntersectWith(status.Missing);
                HashSet<string> stagedAdded = new HashSet<string>(status.Staged);
                stagedAdded.IntersectWith(status.Added);
                HashSet<string> stagedModified = new HashSet<string>(status.Staged);
                stagedModified.IntersectWith(status.Modified);
                stagedModified.ExceptWith(status.MergeConflict);

                HashSet<string> Removed = new HashSet<string>(status.Removed);
                Removed.ExceptWith(status.Staged);
                HashSet<string> Missing = new HashSet<string>(status.Missing);
                Missing.ExceptWith(status.Staged);
                HashSet<string> Added = new HashSet<string>(status.Added);
                Added.ExceptWith(status.Staged);
                HashSet<string> Modified = new HashSet<string>(status.Modified);
                Modified.ExceptWith(status.Staged);

                DoStagedList(status);
                DoModifiedList(status);
            }
            else
                AnyDifferences = true;

            IndexSize = status.IndexSize;
            //Leave this in until completed. 
            //throw new NotImplementedException("The implementation is not yet complete. autocrlf support is not added.");
        }

        private void DoModifiedList(RepositoryStatus status)
        {
            //Create a single list to sort and display the modified (non-staged) files by filename.
            //Sorting in this manner causes additional speed overhead. Performance enhancements should be considered.
            HashSet<string> hset = null;

            if (status.MergeConflict.Count > 0)
            {
                hset = new HashSet<string>(status.MergeConflict);
                foreach (string hash in hset)
                {
                    results.ModifiedList.Add(hash, StatusType.MergeConflict);
                    status.Modified.Remove(hash);
                    status.Staged.Remove(hash);
                }
            }

            if (status.Missing.Count > 0)
            {
                hset = new HashSet<string>(status.Missing);
                hset.ExceptWith(status.Staged);
                foreach (string hash in hset)
                    results.ModifiedList.Add(hash, StatusType.Missing);
            }

            if (status.Removed.Count > 0)
            {
                hset = new HashSet<string>(status.Removed);
                hset.ExceptWith(status.Staged);
                foreach (string hash in hset)
                    results.ModifiedList.Add(hash, StatusType.Removed);
            }

            if (status.Modified.Count > 0)
            {
                hset = new HashSet<string>(status.Modified);
                hset.ExceptWith(status.Staged);
                foreach (string hash in hset)
                    results.ModifiedList.Add(hash, StatusType.Modified);
            }

            if (status.Added.Count > 0)
            {
                hset = new HashSet<string>(status.Added);
                hset.ExceptWith(status.Staged);
                foreach (string hash in hset)
                    results.ModifiedList.Add(hash, StatusType.Added);
            }

            results.ModifiedList.OrderBy(v => v.Key);
        }

        private void DoStagedList(RepositoryStatus status)
        {
            //Create a single list to sort and display the staged files by filename.
            //Sorting in this manner causes additional speed overhead so should be considered optional.
            //With all the additional testing currently added, please keep in mind it will run twice as fast
            //once the tests are removed.

            HashSet<string> hset = null;

            if (status.MergeConflict.Count > 0)
            {
                hset = new HashSet<string>(status.MergeConflict);
                foreach (string hash in hset)
                {
                    //Merge conflicts are only displayed in the modified non-staged area.
                    status.Modified.Remove(hash);
                    status.Staged.Remove(hash);
                }
            }
            if (status.Missing.Count > 0)
            {
                hset = new HashSet<string>(status.Staged);
                hset.IntersectWith(status.Missing);
                foreach (string hash in hset)
                    results.StagedList.Add(hash, StatusType.Missing);
            }

            if (status.Removed.Count > 0)
            {
                hset = new HashSet<string>(status.Staged);
                hset.IntersectWith(status.Removed);
                foreach (string hash in hset)
                    results.StagedList.Add(hash, StatusType.Removed);
            }

            if (status.Modified.Count > 0)
            {
                hset = new HashSet<string>(status.Staged);
                hset.IntersectWith(status.Modified);
                foreach (string hash in hset)
                    results.StagedList.Add(hash, StatusType.Modified);
            }

            if (status.Added.Count > 0)
            {
                hset = new HashSet<string>(status.Staged);
                hset.IntersectWith(status.Added);
                foreach (string hash in hset)
                    results.StagedList.Add(hash, StatusType.Added);
            }

            results.StagedList.OrderBy(v => v.Key);
        }
    }
    #endregion

    #region Status Results

    public class StatusResults
    {
        public List<string> UntrackedList = new List<string>();
        public Dictionary<string, int> StagedList = new Dictionary<string, int>();
        public Dictionary<string, int> ModifiedList = new Dictionary<string, int>();

        // Returns all available matches of a file and its status based on filename.
        public Dictionary<string,int> Search(string filepattern)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            foreach (string hash in UntrackedList)
            {
                if (hash.Contains(filepattern))
                {
                    result.Add(hash, StatusState.Untracked);
                }
            }
            foreach (string key in ModifiedList.Keys)
            {
                if (key.Contains(filepattern))
                {
                    result.Add(key, StatusState.Modified);
                }
            }
            foreach (string key in StagedList.Keys)
            {
                if (key.Contains(filepattern))
                {
                    result.Add(key, StatusState.Staged);
                }
            }

            return result;
        }
        
        public bool Contains(string filepattern, int state)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            if (state == StatusState.Untracked)
            {
                foreach (string hash in UntrackedList)
                {
                    if (hash.Contains(filepattern))
                    {
                        return true;
                    }
                }
            }

            if (state == StatusState.Modified)
            {
                foreach (string key in ModifiedList.Keys)
                {
                    if (key.Contains(filepattern))
                    {
                        result.Add(key, StatusState.Modified);
                    }
                }
            }

            if (state == StatusState.Staged)
            {
                foreach (string key in StagedList.Keys)
                {
                    if (key.Contains(filepattern))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

    }

    #endregion

    #region Status Types
    
    public struct StatusState
    {
        public const int Staged = 1;
        public const int Modified = 2;
        public const int Untracked = 3;
    }

    public struct StatusType
    {
        public const int Added = 1;
        public const int Modified = 2;
        public const int Removed = 3;
        public const int Missing = 4;
        public const int MergeConflict = 5;
    }

    #endregion

}