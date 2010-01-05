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

namespace GitSharp
{
    public class StatusCommand : AbstractCommand
    {

        public StatusCommand()
        {
        }

        public override void Execute()
        {
            RepositoryStatus status = new RepositoryStatus(Repository);
            OutputStream.WriteLine("# On branch ..."); //Todo: Insert branch detection here.
            //OutputStream.WriteLine("# Your branch is ahead of 'xxx' by x commits."); //Todo
            OutputStream.WriteLine("#");
            if (status.AnyDifferences)
            {
                // Files use the following states: removed, missing, added, and modified.
                // If a file has been staged, it is also added to the RepositoryStatus.Staged HashSet.
                //
                // The remaining StatusType known as "Untracked" is determined by what is *not* staged or modified.
                // It is then intersected with the .gitignore list to determine what should be listed as untracked.
                // Using intersections will accurately display the "bucket" each file was added to.

                // Note: In standard git, they use cached references so the following scenario is possible. 
                //    1) Filename = a.txt; StatusType=staged; FileState=added
                //    2) Filename = a.txt; StatusType=modified; FileState=added
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

                HashSet<string> Removed = new HashSet<string>(status.Removed);
                Removed.ExceptWith(status.Staged);
                HashSet<string> Missing = new HashSet<string>(status.Missing);
                Missing.ExceptWith(status.Staged);
                HashSet<string> Added = new HashSet<string>(status.Added);
                Added.ExceptWith(status.Staged);
                HashSet<string> Modified = new HashSet<string>(status.Modified);
                Modified.ExceptWith(status.Staged);

                // The output below is used to display both where the file is being added and specifying the file.
                // Unit testing is still pending.
                OutputStream.WriteLine("# Staged Tests: StageType + status.Staged");
                OutputStream.WriteLine("# Staged Total: " + status.Staged.Count);
                OutputStream.WriteLine("# Test:     Modified Object Count: " + stagedModified.Count);
                OutputStream.WriteLine("# Test:      Removed Object Count: " + stagedRemoved.Count);
                OutputStream.WriteLine("# Test:      Missing Object Count: " + stagedMissing.Count);
                OutputStream.WriteLine("# Test:        Added Object Count: " + stagedAdded.Count);
                OutputStream.WriteLine("#");
                OutputStream.WriteLine("# Modified Tests: StageType w/o status.Staged");
                OutputStream.WriteLine("# Modified Total: " + (status.Modified.Count - status.Staged.Count));
                OutputStream.WriteLine("# Test:      Changed Object Count: " + Modified.Count);
                OutputStream.WriteLine("# Test:      Removed Object Count: " + Removed.Count);
                OutputStream.WriteLine("# Test:      Missing Object Count: " + Missing.Count);
                OutputStream.WriteLine("# Test:        Added Object Count: " + Added.Count);
                OutputStream.WriteLine("#");
                OutputStream.WriteLine("# UnTracked Tests: status.Untracked");
                OutputStream.WriteLine("# Test:    Untracked Object Count: " + status.Untracked.Count);
                OutputStream.WriteLine("# Test:      Ignored Object Count: Pending");
                OutputStream.WriteLine("#");

                //Todo: merge conflict display

                //Display the three stages of all files
                doDisplayStaged(status);
                doDisplayUnstaged(status);
                doDisplayUntracked(status);
            }
            else
            {
                OutputStream.WriteLine("# nothing to commit (working directory clean");
            }
            //Leave this in until completed. The command returns inaccurate results due to IndexDiff.
            throw new NotImplementedException();
        }

        private Dictionary<string, int> GetModifiedList(RepositoryStatus status)
        {
            //Create a single list to sort and display the modified (non-staged) files by filename.
            //Sorting in this manner causes additional speed overhead so should be considered optional.
            //With all the additional testing currently added, please keep in mind it will run twice as fast
            //once the tests are removed.
            Dictionary<string, int> modifiedList = new Dictionary<string, int>();
            HashSet<string> hset = null;

            if (status.Missing.Count > 0)
            {
                hset = new HashSet<string>(status.Missing);
                hset.ExceptWith(status.Staged);
                foreach (string hash in hset)
                    modifiedList.Add(hash, 1);
            }

            if (status.Removed.Count > 0)
            {
                hset = new HashSet<string>(status.Removed);
                hset.ExceptWith(status.Staged);
                foreach (string hash in hset)
                    modifiedList.Add(hash, 2);
            }

            if (status.Modified.Count > 0)
            {
                hset = new HashSet<string>(status.Modified);
                hset.ExceptWith(status.Staged);
                foreach (string hash in hset)
                    modifiedList.Add(hash, 3);
            }

            if (status.Added.Count > 0)
            {
                hset = new HashSet<string>(status.Added);
                hset.ExceptWith(status.Staged);
                foreach (string hash in hset)
                    modifiedList.Add(hash, 4);
            }

            modifiedList.OrderBy(v => v.Key);
            return modifiedList;
        }

        private Dictionary<string, int> GetStagedList(RepositoryStatus status)
        {
            //Create a single list to sort and display the staged files by filename.
            //Sorting in this manner causes additional speed overhead so should be considered optional.
            //With all the additional testing currently added, please keep in mind it will run twice as fast
            //once the tests are removed.
            Dictionary<string, int> stagedList = new Dictionary<string, int>();
            HashSet<string> hset = null;

            if (status.Missing.Count > 0)
            {
                hset = new HashSet<string>(status.Staged);
                hset.IntersectWith(status.Missing);
                foreach (string hash in hset)
                    stagedList.Add(hash, 1);
            }

            if (status.Removed.Count > 0)
            {
                hset = new HashSet<string>(status.Staged);
                hset.IntersectWith(status.Removed);
                foreach (string hash in hset)
                    stagedList.Add(hash, 2);
            }

            if (status.Modified.Count > 0)
            {
                hset = new HashSet<string>(status.Staged);
                hset.IntersectWith(status.Modified);
                foreach (string hash in hset)
                    stagedList.Add(hash, 3);
            }

            if (status.Added.Count > 0)
            {
                hset = new HashSet<string>(status.Staged);
                hset.IntersectWith(status.Added);
                foreach (string hash in hset)
                    stagedList.Add(hash, 4);
            }

            stagedList.OrderBy(v => v.Key);
            return stagedList;
        }
        private void displayStatusList(Dictionary<string, int> statusList)
        {
            foreach (KeyValuePair<string, int> pair in statusList)
            {
                switch (pair.Value)
                {
                    case 1:
                        OutputStream.WriteLine("#       missing:    " + pair.Key);
                        break;
                    case 2:
                        OutputStream.WriteLine("#       deleted:    " + pair.Key);
                        break;
                    case 3:
                        OutputStream.WriteLine("#       modified:   " + pair.Key);
                        break;
                    case 4:
                        OutputStream.WriteLine("#       new file:   " + pair.Key);
                        break;
                }

            }
        }

        private void doDisplayUnstaged(RepositoryStatus status)
        {
            OutputStream.WriteLine("# Changed but not updated:");
            OutputStream.WriteLine("#   (use \"git add (file)...\" to update what will be committed)");
            OutputStream.WriteLine("#   (use \"git checkout -- (file)...\" to discard changes in working directory");
            OutputStream.WriteLine("#");
            Dictionary<string, int> statusList = GetModifiedList(status);
            displayStatusList(statusList);
            OutputStream.WriteLine("#");
        }

        private void doDisplayStaged(RepositoryStatus status)
        {
            OutputStream.WriteLine("# Changes to be committed:");
            OutputStream.WriteLine("#   (use \"git reset HEAD (file)...\" to unstage)");
            OutputStream.WriteLine("#");
            Dictionary<string, int> statusList = GetStagedList(status);
            displayStatusList(statusList);
            OutputStream.WriteLine("#");
        }

        private void doDisplayUntracked(RepositoryStatus status)
        {
            if (status.Untracked.Count > 0)
            {
                OutputStream.WriteLine("# Untracked files:");
                OutputStream.WriteLine("#   (use \"git add (file)...\" to include in what will be committed)");
                OutputStream.WriteLine("#");
                List<string> sortUntracked = status.Untracked.OrderBy(v => v.ToString()).ToList();

                //Read ignore file list and remove from the untracked list
                IgnoreRules rules = new IgnoreRules(Path.Combine(Repository.WorkingDirectory, ".gitignore"));
                foreach (string hash in sortUntracked)
                {
                    string path = Path.Combine(Repository.WorkingDirectory, hash);
                    if (!rules.IgnoreFile(Repository.WorkingDirectory, path) && !rules.IgnoreDir(Repository.WorkingDirectory, path))
                    {
                        OutputStream.WriteLine("#       " + hash);
                    }
                }
            }
        }
    }
}