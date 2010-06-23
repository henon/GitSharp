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
    public class Filterbranch : TextBuiltin
    {
        private FilterBranchCommand cmd = new FilterBranchCommand();
        private static Boolean isHelp;

        public override void Run(string[] args)
        {
            options = new CmdParserOptionSet()
            {
               { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
               { "env-filter=", "This filter may be used if you only need to modify the environment in which the commit will be performed", v => cmd.EnvFilter = v },
               { "tree-filter=", "This is the filter for rewriting the tree and its contents", v => cmd.TreeFilter = v },
               { "index-filter=", "This is the filter for rewriting the index", v => cmd.IndexFilter = v },
               { "parent-filter=", "This is the filter for rewriting the commit's parent list", v => cmd.ParentFilter = v },
               { "msg-filter=", "This is the filter for rewriting the commit messages", v => cmd.MsgFilter = v },
               { "commit-filter=", "This is the filter for performing the commit", v => cmd.CommitFilter = v },
               { "tag-name-filter=", "This is the filter for rewriting tag names", v => cmd.TagNameFilter = v },
               { "subdirectory-filter=", "Only look at the history which touches the given subdirectory", v => cmd.SubdirectoryFilter = v },
               { "remap-to-ancestor", "Rewrite refs to the nearest rewritten ancestor instead of ignoring them", v => cmd.RemapToAncestor = true },
               { "prune-empty", "Some kind of filters will generate empty commits, that left the tree untouched", v => cmd.PruneEmpty = true },
               { "original=", "Use this option to set the namespace where the original commits will be stored", v => cmd.Original = v },
               { "d=", "Use this option to set the path to the temporary directory used for rewriting", v => cmd.D = v },
               { "f|force", "'git-filter-branch' refuses to start with an existing temporary directory or when there are already refs starting with 'refs/original/', unless forced", v => cmd.Force = true },
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
