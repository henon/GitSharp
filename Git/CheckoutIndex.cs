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
    // [Mr Happy] Might be merged w/ regular checkout?
    [Command(common=true, requiresRepository=true, usage = "")]
    public class Checkoutindex : TextBuiltin
    {
        private CheckoutIndexCommand cmd = new CheckoutIndexCommand();
        private static Boolean isHelp;

        public override void Run(string[] args)
        {
            cmd.Quiet = false;
			
            options = new CmdParserOptionSet()
            {
               { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
               { "u|index", "update stat information for the checked out entries in the index file", v => cmd.Index = true },
               { "q|quiet", "be quiet if files exist or are not in the index", v => cmd.Quiet = true },
               { "f|force", "forces overwrite of existing files", v => cmd.Force = true },
               { "a|all", "checks out all files in the index", v => cmd.All = true },
               { "n|no-create", "Don't checkout new files, only refresh files already checked out", v => cmd.NoCreate = true },
               { "prefix=", "When creating files, prepend <string> (usually a directory including a trailing /)", v => cmd.Prefix = v },
               { "stage=", "Instead of checking out unmerged entries, copy out the files from named stage", v => cmd.Stage = v },
               { "temp", "Instead of copying the files to the working directory write the content to temporary files", v => cmd.Temp = true },
               { "stdin", "Instead of taking list of paths from the command line, read list of paths from the standard input", v => cmd.Stdin = true },
               { "z", "Only meaningful with `--stdin`; paths are separated with NUL character instead of LF", v => cmd.Z = true },
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
