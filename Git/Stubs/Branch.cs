/*
 * Copyright (C) 2010, Dominique van de Vorle <dvdvorle@gmail.com>
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
using NDesk.Options;
using GitSharp.Commands;

namespace GitSharp.CLI
{

    [Command(common=true, requiresRepository=true, usage = "")]
    public class Branch : TextBuiltin
    {
        private BranchCommand cmd = new BranchCommand();
        private static Boolean isHelp;

        public override void Run(string[] args)
        {		
            options = new CmdParserOptionSet()
            {
               { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
               { "d", "Delete a branch", v => cmd.d = true },
               { "D", "Delete a branch irrespective of its merged status", v => cmd.D = true },
               { "l", "Create the branch's reflog", v => cmd.L = true },
               { "f|force", "Reset <branchname> to <startpoint> if <branchname> exists already", v => cmd.Force = true },
               { "m", "Move/rename a branch and the corresponding reflog", v => cmd.m = true },
               { "M", "Move/rename a branch even if the new branch name already exists", v => cmd.M = true },
               { "color", "Color branches to highlight current, local, and remote branches", v => cmd.Color = true },
               { "no-color", "Turn off branch colors, even when the configuration file gives the default to color output", v => cmd.NoColor = true },
               { "r", "List or delete (if used with -d) the remote-tracking branches", v => cmd.R = true },
               { "a", "List both remote-tracking branches and local branches", v => cmd.A = true },
               { "v|verbose", "Show sha1 and commit subject line for each head, along with relationship to upstream branch (if any)", v => cmd.Verbose = true },
               { "abbrev=", "Alter the sha1's minimum display length in the output listing", v => cmd.Abbrev = v },
               { "no-abbrev", "Display the full sha1s in the output listing rather than abbreviating them", v => cmd.NoAbbrev = true },
               { "t|track", "When creating a new branch, set up configuration to mark the start-point branch as \"upstream\" from the new branch", v => cmd.Track = true },
               { "no-track", "Do not set up \"upstream\" configuration, even if the branch", v => cmd.NoTrack = true },
               { "contains=", "Only list branches which contain the specified commit", v => cmd.Contains = v },
               { "merged=", "Only list branches whose tips are reachable from the specified commit (HEAD if not specified)", v => cmd.Merged = v },
               { "no-merged=", "Only list branches whose tips are not reachable from the specified commit (HEAD if not specified)", v => cmd.NoMerged = v },
             };

            try
            {
                List<String> arguments = ParseOptions(args);
                if (arguments.Count > 0)
                {
                    cmd.Arguments = arguments;
                    cmd.Execute();
                }
                else
                {
                    OfflineHelp();
                }
            }
            catch (Exception e)            
            {
                cmd.OutputStream.WriteLine(e.Message);
            }
        }

        private void OfflineHelp()
        {
            if (!isHelp)
            {
                isHelp = true;
                cmd.OutputStream.WriteLine("Here should be the usage...");
                cmd.OutputStream.WriteLine();
                options.WriteOptionDescriptions(Console.Out);
                cmd.OutputStream.WriteLine();
            }
        }
    }
}
