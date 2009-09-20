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
using System.Text;
using NDesk.Options;

namespace GitSharp.CLI
{
    [Command(complete = false, common = true, usage = "Checkout a branch or paths to the working tree")]
    class Checkout : TextBuiltin
    {

#if ported
        Boolean isQuiet = false;
        Boolean isForced = false;
        Boolean isTracked = false;
        Boolean isNoTrack = false;
        Boolean isMerging = false;
        Boolean isOurs = false;
        Boolean isTheirs = false;
        Boolean isConflict = false;
        private static string branchName = "";
#endif

        override public void Run(string[] args)
        {
            options = new CmdParserOptionSet()
            {
                { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
#if ported
                { "q|quiet", "Suppress feedback messages", v=> {isQuiet = true;}},
                { "f|force", "Force checkout and ignore unmerged changes", v=>{isForced = true;}},
                { "ours", "For unmerged paths, checkout stage #2 from the index", v=>{isOurs = true;}},
                { "theirs", "For unmerged paths, checkout stage #3 from the index", v=>{isTheirs = true;}},
                { "b|branch=", "Create a new {branch}",(string v) => branchName = v },
                { "t|track", "Set the upstream configuration", v=>{isTracked = true;}},
                { "no-track", "Do not set the upstream configuration", v=>{isNoTrack = true;}},
                { "l", "Create the new branch's reflog", v=>RefLog()},
                { "m|merge", "Perform a three-way merge between the current branch, your working tree contents " +
                    "and the new branch", v=>{isMerging = true;}},
                { "conflict","Same as merge above, but changes how the conflicting hunks are presented", isConflict = true},
                { "p|patch", "Creates a diff and applies it in reverse order to the working tree", v=>Patch()}
#endif
            };

            try
            {
                List<String> arguments = ParseOptions(args);
                if (arguments.Count > 0)
                {
                    //Checkout the new repository
                    DoCheckout(arguments[0]);
                }
                else if (args.Length <= 0)
                {
                    //Display the modified files for the existing repository
                    DoViewChanges();
                }
                else
                {
                    OfflineHelp();
                }
            } catch (OptionException e) {
                Console.WriteLine(e.Message);
            }
        }

        private static void OfflineHelp()
        {
            Console.WriteLine("usage:");
            Console.WriteLine("       git checkout [-q] [-f] [-m] [<branch>]");
            Console.WriteLine("       git checkout [-q] [-f] [-m] [-b <new_branch>] [<start_point>]");
            Console.WriteLine("       git checkout [-f|--ours|--theirs|-m|--conflict=<style>] [<tree-ish>] [--] <paths>...");
            Console.WriteLine("       git checkout --patch [<tree-ish>] [--] [<paths>...]");
            Console.WriteLine("\nThe available options for this command are:\n");
            Console.WriteLine();
            options.WriteOptionDescriptions(Console.Out);
            Console.WriteLine();
        }

        private static void RefLog()
        {
        }

        private static void Patch(String treeish)
        {
        }

        private static void DoCheckout(String repository)
        {
            Console.WriteLine("This command still needs to be implemented.");
        }

        private static void DoViewChanges()
        {
            Console.WriteLine("This command still needs to be implemented.");
        }

    }
}
