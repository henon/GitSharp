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
    public class ShowBranch : TextBuiltin
    {
        private ShowBranchCommand cmd = new ShowBranchCommand();
        private static Boolean isHelp;

        public override void Run(string[] args)
        {
            options = new CmdParserOptionSet()
            {
               { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
               { "r|remotes", "Show the remote-tracking branches", v => cmd.Remotes = true },
               { "a|all", "Show both remote-tracking branches and local branches", v => cmd.All = true },
               { "current", "With this option, the command includes the current branch to the list of revs to be shown when it is not given on the command line", v => cmd.Current = true },
               { "topo-order", "By default, the branches and their commits are shown in         reverse chronological order", v => cmd.TopoOrder = true },
               { "date-order", "This option is similar to '--topo-order' in the sense that no parent comes before all of its children, but otherwise commits are ordered according to their commit date", v => cmd.DateOrder = true },
               { "sparse", "By default, the output omits merges that are reachable from only one tip being shown", v => cmd.Sparse = true },
               { "more=", "Usually the command stops output upon showing the commit that is the common ancestor of all the branches", v => cmd.More = v },
               { "list", "Synonym to `--more=-1`", v => cmd.List = true },
               { "merge-base", "Instead of showing the commit list, determine possible merge bases for the specified commits", v => cmd.MergeBase = true },
               { "independent=", "Among the <reference>s given, display only the ones that cannot be reached from any other <reference>", v => cmd.Independent = v },
               { "no-name", "Do not show naming strings for each commit", v => cmd.NoName = true },
               { "sha1-name", "Instead of naming the commits using the path to reach them from heads (e", v => cmd.Sha1Name = true },
               { "topics", "Shows only commits that are NOT on the first branch given", v => cmd.Topics = true },
               { "g|reflog=", "Shows <n> most recent ref-log entries for the given ref", v => cmd.Reflog = v },
               { "color", "Color the status sign (one of these: `*` `!` `+` `-`) of each commit corresponding to the branch it's in", v => cmd.Color = true },
               { "no-color", "Turn off colored output, even when the configuration file gives the default to color output", v => cmd.NoColor = true },
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
