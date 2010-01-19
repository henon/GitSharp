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
    public class LsFiles : TextBuiltin
    {
        private LsFilesCommand cmd = new LsFilesCommand();
        private static Boolean isHelp;

        public override void Run(string[] args)
        {
            options = new CmdParserOptionSet()
            {
               { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
               { "c|cached", "Show cached files in the output (default)", v => cmd.Cached = true },
               { "d|deleted", "Show deleted files in the output", v => cmd.Deleted = true },
               { "m|modified", "Show modified files in the output", v => cmd.Modified = true },
               { "o|others", "Show other (i", v => cmd.Others = true },
               { "i|ignored", "Show only ignored files in the output", v => cmd.Ignored = true },
               { "s|stage", "Show staged contents' object name, mode bits and stage number in the output", v => cmd.Stage = true },
               { "directory", "If a whole directory is classified as \"other\", show just its name (with a trailing slash) and not its whole contents", v => cmd.Directory = true },
               { "no-empty-directory", "Do not list empty directories", v => cmd.NoEmptyDirectory = true },
               { "u|unmerged", "Show unmerged files in the output (forces --stage)", v => cmd.Unmerged = true },
               { "k|killed", "Show files on the filesystem that need to be removed due to file/directory conflicts for checkout-index to succeed", v => cmd.Killed = true },
               { "z", "0 line termination on output", v => cmd.Z = true },
               { "x|exclude=", "Skips files matching pattern", v => cmd.Exclude = v },
               { "X|exclude-from=", "exclude patterns are read from <file>; 1 per line", v => cmd.ExcludeFrom = v },
               { "exclude-per-directory=", "read additional exclude patterns that apply only to the directory and its subdirectories in <file>", v => cmd.ExcludePerDirectory = v },
               { "exclude-standard", "Add the standard git exclusions:", v => cmd.ExcludeStandard = true },
               { "error-unmatch=", "If any <file> does not appear in the index, treat this as an error (return 1)", v => cmd.ErrorUnmatch = v },
               { "with-tree=", "When using --error-unmatch to expand the user supplied <file> (i", v => cmd.WithTree = v },
               { "t", "Identify the file status with the following tags (followed by a space) at the start of each line: H::cached M::unmerged R::removed/deleted C::modified/changed K::to be killed ?::other", v => cmd.T = true },
               { "v", "Similar to `-t`, but use lowercase letters for files that are marked as 'assume unchanged' (see linkgit:git-update-index[1])", v => cmd.V = true },
               { "full-name", "When run from a subdirectory, the command usually outputs paths relative to the current directory", v => cmd.FullName = true },
               { "abbrev=", "Instead of showing the full 40-byte hexadecimal object lines, show only a partial prefix", v => cmd.Abbrev = v },
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
