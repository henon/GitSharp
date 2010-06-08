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
using GitSharp.Commands;
using NDesk.Options;
using GitSharp.CLI;

namespace GitSharp.CLI
{

    [Command(common=true, requiresRepository=true, usage = "")]
    public class Rebase : TextBuiltin
    {
        private RebaseCommand cmd = new RebaseCommand();
        private static Boolean isHelp;
        public override void Run(string[] args)
        {
            cmd.Quiet = false;
			
            options = new CmdParserOptionSet()
            {
               { "continue", "Restart the rebasing process after having resolved a merge conflict", v => cmd.Continue = true },
               { "abort", "Restore the original branch and abort the rebase operation", v => cmd.Abort = true },
               { "skip", "Restart the rebasing process by skipping the current patch", v => cmd.Skip = true },
               { "m|merge", "Use merging strategies to rebase", v => cmd.Merge = true },
               { "s|strategy=", "Use the given merge strategy", v => cmd.Strategy = v },
               { "q|quiet", "Be quiet", v => cmd.Quiet = true },
               { "v|verbose", "Be verbose", v => cmd.Verbose = true },
               { "stat", "Show a diffstat of what changed upstream since the last rebase", v => cmd.Stat = true },
               { "n|no-stat", "Do not show a diffstat as part of the rebase process", v => cmd.NoStat = true },
               { "no-verify", "This option bypasses the pre-rebase hook", v => cmd.NoVerify = true },
               { "C=", "Ensure at least <n> lines of surrounding context match before and after each change", v => cmd.Context = v },
               { "f|force-rebase", "Force the rebase even if the current branch is a descendant of the commit you are rebasing onto", v => cmd.Forcerebase = true },
               { "ignore-whitespace=", "These flag are passed to the 'git-apply' program (see linkgit:git-apply[1]) that applies the patch", v => cmd.IgnoreWhitespace = v },
               { "whitespace=", "These flag are passed to the 'git-apply' program (see linkgit:git-apply[1]) that applies the patch", v => cmd.Whitespace = v },
               { "committer-date-is-author-date", "These flags are passed to 'git-am' to easily change the dates of the rebased commits (see linkgit:git-am[1])", v => cmd.CommitterDateIsAuthorDate = true },
               { "ignore-date", "These flags are passed to 'git-am' to easily change the dates of the rebased commits (see linkgit:git-am[1])", v => cmd.IgnoreDate = true },
               { "i|interactive", "Make a list of the commits which are about to be rebased", v => cmd.Interactive = true },
               { "p|preserve-merges", "Instead of ignoring merges, try to recreate them", v => cmd.PreserveMerges = true },
               { "root=", "Rebase all commits reachable from <branch>, instead of limiting them with an <upstream>", v => cmd.Root = v },
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
                cmd.OutputStream.WriteLine("/*Usage*/");
                cmd.OutputStream.WriteLine();
                options.WriteOptionDescriptions(Console.Out);
                cmd.OutputStream.WriteLine();
            }
        }
    }
}
