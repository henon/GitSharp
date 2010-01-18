/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Rolenun <rolenun@gmail.com>
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
using GitSharp.Commands;
using NDesk.Options;

namespace GitSharp.CLI
{
    [Command(complete = false, common = true, requiresRepository=true, usage = "Show the working tree status")]
    class Status : TextBuiltin
    {
        private StatusCommand cmd = new StatusCommand();

        private static Boolean isHelp = false;

#if ported
        private static Boolean isCommitAll = false;
        private static String reUseMessage = "";
        private static String reEditMessage = "";
        private static String cleanupOption = "default";
        private static String untrackedFileMode = "all";
        private static String message = "";
        private static String author = "";
        private static String logFile = "";
        private static String templateFile = "";
        private static Boolean isSignOff= false;
        private static Boolean isNoVerify = false;
        private static Boolean isAllowEmpty = false;
        private static Boolean isAmend = false;
        private static Boolean isForceEdit = false;
        private static Boolean isInclude = false;
        private static Boolean isCommitOnly = false;
        private static Boolean isInteractive = false;
        private static Boolean isVerbose = false;
        private static Boolean isQuiet = false;
        private static Boolean isDryRun = false;
#endif

        override public void Run(String[] args)
        {
           
            // The command takes the same options as git-commit. It shows what would be 
            // committed if the same options are given.
            options = new CmdParserOptionSet()
            {
                { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
#if ported
                { "q|quiet", "Be quiet", v=>{isQuiet = true;}},
                { "v|verbose", "Be verbose", v=>{isVerbose = true;}},
                { "F|file=", "Read log from {file}", (string v) => logFile = v },
                { "author=", "Override {author} for commit", (string v) => author = v },
                { "m|message=", "Specify commit {message}", (string v) => message = v },
                { "c|reedit-message=", "Reuse and edit {message} from specified commit", (string v) => reEditMessage = v },
                { "C|reuse-message=", "Reuse {message} from specified commit", (string v) => reUseMessage = v },
                { "s|signoff", "Add Signed-off-by:", v=>{isSignOff = true;}},
                { "t|template=", "Use specified {template} file", (string v) => templateFile = v },
                { "e|edit", "Force edit of commit", v=>{isForceEdit = true;}},
                { "a|all", "Commit all changed files.", v=>{isCommitAll = true;}},
                { "i|include", "Add specified files to index for commit", v=>{isInclude = true;}},
                { "interactive", "Interactively add files", v=>{isInteractive = true;}},
                { "o|only", "Commit only specified files", v=>{isCommitOnly = true;}},
                { "n|no-verify", "Bypass pre-commit hook", v=>{isNoVerify = true;}},
                { "amend", "Amend previous commit", v=>{isAmend = true;}},
                { "u|untracked-files=", "Show untracked files, optional {MODE}s: all, normal, no.", (string v) => untrackedFileMode = v },
                { "allow-empty", "Ok to record an empty change", v=> {isAllowEmpty = true;}},
                { "cleanup=", "How to strip spaces and #comments from message. Options are: " +
                    "verbatim, whitespace, strip, and default.", (string v) => cleanupOption = v },
                { "dry-run", "Don't actually commit the files, just show if they exist.", v=>{isDryRun = true;}},
#endif
            };

            try
            {
                List<String> arguments = ParseOptions(args);
                if (arguments.Count > 0)
                {
                    //Execute the status using the specified file pattern
                    //cmd.Source = arguments[0];
                    cmd.Execute();
                    Display();
                }
                else if (args.Length <= 0)
                {
                    //Display status if no changes are added to commit
                    //If changes have been made, commit them?
                    //Console.WriteLine("These commands still need to be implemented.");
                    cmd.Execute();
                    Display();
                }
                else
                {
                    OfflineHelp();
                }
            }
            catch (OptionException e)
            {
                OutputStream.WriteLine(e.Message);
            }
        }

        private void OfflineHelp()
        {
            if (!isHelp)
            {
                isHelp = true;
                cmd.OutputStream.WriteLine("usage: git status [options] [--] <filepattern>...");
                cmd.OutputStream.WriteLine();
                options.WriteOptionDescriptions(Console.Out);
            }
        }

        public void Display()
        {
            // The output below is used to display both where the file is being added and specifying the file.
            // Unit testing is still pending.
            /*OutputStream.WriteLine("# Staged Tests: StageType + status.Staged");
            OutputStream.WriteLine("# Staged Total: " + (stagedModified.Count + stagedRemoved.Count + stagedMissing.Count + stagedAdded.Count));
            OutputStream.WriteLine("# Test: Modified Object Count: " + stagedModified.Count);
            OutputStream.WriteLine("# Test: Removed Object Count: " + stagedRemoved.Count);
            OutputStream.WriteLine("# Test: Missing Object Count: " + stagedMissing.Count);
            OutputStream.WriteLine("# Test: Added Object Count: " + stagedAdded.Count);
            OutputStream.WriteLine("#");
            OutputStream.WriteLine("# Modified Tests: StageType w/o status.Staged");
            OutputStream.WriteLine("# Modified Total: " + (Modified.Count+Removed.Count+Missing.Count+Added.Count));
            OutputStream.WriteLine("# Test: Changed Object Count: " + Modified.Count);
            OutputStream.WriteLine("# Test: Removed Object Count: " + Removed.Count);
            OutputStream.WriteLine("# Test: Missing Object Count: " + Missing.Count);
            OutputStream.WriteLine("# Test: Added Object Count: " + Added.Count);
            OutputStream.WriteLine("#");
            OutputStream.WriteLine("# MergeConflict Tests: " + status.MergeConflict.Count);
            OutputStream.WriteLine("# Test: Object Count: " + status.MergeConflict.Count);
            OutputStream.WriteLine("#");
            OutputStream.WriteLine("# UnTracked Tests: status.Untracked");
            OutputStream.WriteLine("# Test: Untracked Object Count: " + status.Untracked.Count);
            OutputStream.WriteLine("# Test: Ignored Object Count: Pending");
            OutputStream.WriteLine("#");*/

            //Display the stages of all files
            doDisplayMergeConflict();
            OutputStream.WriteLine("# On branch " + cmd.Repository.CurrentBranch.Name);
            //OutputStream.WriteLine("# Your branch is ahead of 'xxx' by x commits."); //Todo
            OutputStream.WriteLine("#");

            doDisplayStaged();
            doDisplayUnstaged();
            doDisplayUntracked();

            if (cmd.Results.StagedList.Count <= 0)
            {
                OutputStream.WriteLine("no changes added to commit (use \"git add\" and/or \"git commit -a\")");
            }
            else if (cmd.IndexSize <= 0)
            {
                OutputStream.WriteLine("# On branch " + cmd.Repository.CurrentBranch.Name);
                OutputStream.WriteLine("#");
                OutputStream.WriteLine("# Initial commit");
                OutputStream.WriteLine("#");
                OutputStream.WriteLine("# nothing to commit (create/copy files and use \"git add\" to track)");
            }
            else
            {
                OutputStream.WriteLine("# nothing to commit (working directory clean)");
            }
            //Leave this in until completed.
            throw new NotImplementedException("The implementation is not yet complete. autocrlf support is not added.");
        }

        public void DoStatus(String filepattern)
        {

            OutputStream.WriteLine("This command still needs to be implemented.");
        }

        private void displayStatusList(Dictionary<string, int> statusList)
        {
            foreach (KeyValuePair<string, int> pair in statusList)
            {
                switch (pair.Value)
                {
                    case StatusType.Missing:
                        OutputStream.WriteLine("# missing: " + pair.Key);
                        break;
                    case StatusType.Removed:
                        OutputStream.WriteLine("# deleted: " + pair.Key);
                        break;
                    case StatusType.Modified:
                        OutputStream.WriteLine("# modified: " + pair.Key);
                        break;
                    case StatusType.Added:
                        OutputStream.WriteLine("# new file: " + pair.Key);
                        break;
                    case StatusType.MergeConflict:
                        OutputStream.WriteLine("# unmerged: " + pair.Key);
                        break;
                }

            }
        }

        private void doDisplayUnstaged()
        {
            if (cmd.Results.ModifiedList.Count > 0)
            {
                OutputStream.WriteLine("# Changed but not updated:");
                OutputStream.WriteLine("# (use \"git add (file)...\" to update what will be committed)");
                OutputStream.WriteLine("# (use \"git checkout -- (file)...\" to discard changes in working directory)");
                OutputStream.WriteLine("#");
                displayStatusList(cmd.Results.ModifiedList);
                OutputStream.WriteLine("#");
            }
        }

        private void doDisplayStaged()
        {
            if (cmd.Results.StagedList.Count > 0)
            {
                OutputStream.WriteLine("# Changes to be committed:");
                OutputStream.WriteLine("# (use \"git reset HEAD (file)...\" to unstage)");
                OutputStream.WriteLine("#");
                displayStatusList(cmd.Results.StagedList);
                OutputStream.WriteLine("#");
            }
        }

        private void doDisplayUntracked()
        {
            if (cmd.Results.UntrackedList.Count > 0)
            {
                OutputStream.WriteLine("# Untracked files:");
                OutputStream.WriteLine("# (use \"git add (file)...\" to include in what will be committed)");
                OutputStream.WriteLine("#");
                cmd.Results.UntrackedList.Sort();//.OrderBy(v => v.ToString());
                foreach (string hash in cmd.Results.UntrackedList)
                    OutputStream.WriteLine("# " + hash);
            }
        }

        private void doDisplayMergeConflict()
        {
            foreach (KeyValuePair<string, int> hash in cmd.Results.ModifiedList)
            {
                if (hash.Value == 5)
                    OutputStream.WriteLine(hash + ": needs merge");
            }
        }

    }
}
