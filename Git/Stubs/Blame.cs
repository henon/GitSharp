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
    public class Blame : TextBuiltin
    {
        private BlameCommand cmd = new BlameCommand();
        private static Boolean isHelp;

        public override void Run(string[] args)
        {
		
            options = new CmdParserOptionSet()
            {
               { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
               { "c", "Use the same output mode as linkgit:git-annotate[1] (Default: off)", v => cmd.c = true },
               { "score-debug", "Include debugging information related to the movement of lines between files (see `-C`) and lines moved within a file (see `-M`)", v => cmd.ScoreDebug = true },
               { "f|show-name", "Show the filename in the original commit", v => cmd.ShowName = true },
               { "n|show-number", "Show the line number in the original commit (Default: off)", v => cmd.ShowNumber = true },
               { "s", "Suppress the author name and timestamp from the output", v => cmd.s = true },
               { "w", "Ignore whitespace when comparing the parent's version and the child's to find where the lines came from", v => cmd.W = true },
               { "b", "Show blank SHA-1 for boundary commits", v => cmd.B = true },
               { "root", "Do not treat root commits as boundaries", v => cmd.Root = true },
               { "show-stats", "Include additional statistics at the end of blame output", v => cmd.ShowStats = true },
               { "L=", "Annotate only the given line range", v => cmd.L = v },
               { "l", "Show long rev (Default: off)", v => cmd.l = true },
               { "t", "Show raw timestamp (Default: off)", v => cmd.T = true },
               { "S=", "Use revisions from revs-file instead of calling linkgit:git-rev-list[1]", v => cmd.S = v },
               { "reverse", "Walk history forward instead of backward", v => cmd.Reverse = true },
               { "p|porcelain", "Show in a format designed for machine consumption", v => cmd.Porcelain = true },
               { "incremental", "Show the result incrementally in a format designed for machine consumption", v => cmd.Incremental = true },
               { "encoding=", "Specifies the encoding used to output author names and commit summaries", v => cmd.Encoding = v },
               { "contents=", "When <rev> is not specified, the command annotates the changes starting backwards from the working tree copy", v => cmd.Contents = v },
               { "date=", "The value is one of the following alternatives: {relative,local,default,iso,rfc,short}", v => cmd.Date = v },
               { "M=", "Detect moving lines in the file as well", v => cmd.M = v },
               { "C=", "In addition to `-M`, detect lines copied from other files that were modified in the same commit", v => cmd.C = v },
               { "h|help", "Show help message", v => cmd.Help = true },
            };

            try
            {
                List<String> Arguments = ParseOptions(args);
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
