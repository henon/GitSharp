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
    public class RevParse : TextBuiltin
    {
        private RevParseCommand cmd = new RevParseCommand();
        private static Boolean isHelp;

        public override void Run(string[] args)
        {
            cmd.Quiet = false;
			
            options = new CmdParserOptionSet()
            {
               { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
               { "parseopt", "Use 'git-rev-parse' in option parsing mode (see PARSEOPT section below)", v => cmd.Parseopt = true },
               { "keep-dashdash", "Only meaningful in `--parseopt` mode", v => cmd.KeepDashdash = true },
               { "stop-at-non-option", "Only meaningful in `--parseopt` mode", v => cmd.StopAtNonOption = true },
               { "sq-quote", "Use 'git-rev-parse' in shell quoting mode (see SQ-QUOTE section below)", v => cmd.SqQuote = true },
               { "revs-only", "Do not output flags and parameters not meant for 'git-rev-list' command", v => cmd.RevsOnly = true },
               { "no-revs", "Do not output flags and parameters meant for 'git-rev-list' command", v => cmd.NoRevs = true },
               { "flags", "Do not output non-flag parameters", v => cmd.Flags = true },
               { "no-flags", "Do not output flag parameters", v => cmd.NoFlags = true },
               { "default=", "If there is no parameter given by the user, use `<arg>` instead", v => cmd.Default = v },
               { "verify", "The parameter given must be usable as a single, valid object name", v => cmd.Verify = true },
               { "q|quiet", "Only meaningful in `--verify` mode", v => cmd.Quiet = true },
               { "sq", "Usually the output is made one line per flag and parameter", v => cmd.Sq = true },
               { "not", "When showing object names, prefix them with '{caret}' and strip '{caret}' prefix from the object names that already have one", v => cmd.Not = true },
               { "symbolic", "Usually the object names are output in SHA1 form (with possible '{caret}' prefix); this option makes them output in a form as close to the original input as possible", v => cmd.Symbolic = true },
               { "symbolic-full-name", "This is similar to --symbolic, but it omits input that are not refs (i", v => cmd.SymbolicFullName = true },
               { "abbrev-ref=", "A non-ambiguous short name of the objects name", v => cmd.AbbrevRef = v },
               { "all", "Show all refs found in `$GIT_DIR/refs`", v => cmd.All = true },
               { "branches", "Show branch refs found in `$GIT_DIR/refs/heads`", v => cmd.Branches = true },
               { "tags", "Show tag refs found in `$GIT_DIR/refs/tags`", v => cmd.Tags = true },
               { "remotes", "Show tag refs found in `$GIT_DIR/refs/remotes`", v => cmd.Remotes = true },
               { "show-prefix", "When the command is invoked from a subdirectory, show the path of the current directory relative to the top-level directory", v => cmd.ShowPrefix = true },
               { "show-cdup", "When the command is invoked from a subdirectory, show the path of the top-level directory relative to the current directory (typically a sequence of \"", v => cmd.ShowCdup = true },
               { "git-dir", "Show `$GIT_DIR` if defined else show the path to the", v => cmd.GitDir = true },
               { "is-inside-git-dir", "When the current working directory is below the repository directory print \"true\", otherwise \"false\"", v => cmd.IsInsideGitDir = true },
               { "is-inside-work-tree", "When the current working directory is inside the work tree of the repository print \"true\", otherwise \"false\"", v => cmd.IsInsideWorkTree = true },
               { "is-bare-repository", "When the repository is bare print \"true\", otherwise \"false\"", v => cmd.IsBareRepository = true },
               // [Mr Happy] Don't know how CmdParseOptionSet handles this. Might rename a property of the RevParseCommand-class.
               //{ "short", "Instead of outputting the full SHA1 values of object names try to abbreviate them to a shorter unique name", v => cmd.Short = true },
               { "short=", "Instead of outputting the full SHA1 values of object names try to abbreviate them to a shorter unique name", v => cmd.Short = v },
               { "since=", "Parse the date string, and output the corresponding --max-age= parameter for 'git-rev-list'", v => cmd.Since = v },
               { "after=", "Parse the date string, and output the corresponding --max-age= parameter for 'git-rev-list'", v => cmd.After = v },
               { "until=", "Parse the date string, and output the corresponding --min-age= parameter for 'git-rev-list'", v => cmd.Until = v },
               { "before=", "Parse the date string, and output the corresponding --min-age= parameter for 'git-rev-list'", v => cmd.Before = v },
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
