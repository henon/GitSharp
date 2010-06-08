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
    public class UpdateIndex : TextBuiltin
    {
        private UpdateIndexCommand cmd = new UpdateIndexCommand();
        private static Boolean isHelp;

        public override void Run(string[] args)
        {
            options = new CmdParserOptionSet()
            {
               { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
               { "add", "If a specified file isn't in the index already then it's added", v => cmd.Add = true },
               { "remove", "If a specified file is in the index but is missing then it's removed", v => cmd.Remove = true },
               { "refresh", "Looks at the current index and checks to see if merges or updates are needed by checking stat() information", v => cmd.Refresh = true },
               { "q", "Quiet", v => cmd.Q = true },
               { "ignore-submodules", "Do not try to update submodules", v => cmd.IgnoreSubmodules = true },
               { "unmerged", "If --refresh finds unmerged changes in the index, the default behavior is to error out", v => cmd.Unmerged = true },
               { "ignore-missing", "Ignores missing files during a --refresh", v => cmd.IgnoreMissing = true },
               { "cacheinfo=", "Directly insert the specified info into the index", v => cmd.Cacheinfo = v },
               { "index-info", "Read index information from stdin", v => cmd.IndexInfo = true },
               { "chmod=", "Set the execute permissions on the updated files", v => cmd.Chmod = v },
               { "assume-unchanged", "When these flags are specified, the object names recorded for the paths are not updated", v => cmd.AssumeUnchanged = true },
               { "no-assume-unchanged", "When these flags are specified, the object names recorded for the paths are not updated", v => cmd.NoAssumeUnchanged = true },
               { "really-refresh", "Like '--refresh', but checks stat information unconditionally, without regard to the \"assume unchanged\" setting", v => cmd.ReallyRefresh = true },
               { "g|again", "Runs 'git-update-index' itself on the paths whose index entries are different from those from the `HEAD` commit", v => cmd.Again = true },
               { "unresolve", "Restores the 'unmerged' or 'needs updating' state of a file during a merge if it was cleared by accident", v => cmd.Unresolve = true },
               { "info-only", "Do not create objects in the object database for all <file> arguments that follow this flag; just insert their object IDs into the index", v => cmd.InfoOnly = true },
               { "force-remove", "Remove the file from the index even when the working directory still has such a file", v => cmd.ForceRemove = true },
               { "replace", "By default, when a file `path` exists in the index, 'git-update-index' refuses an attempt to add `path/file`", v => cmd.Replace = true },
               { "stdin", "Instead of taking list of paths from the command line, read list of paths from the standard input", v => cmd.Stdin = true },
               { "verbose", "Report what is being added and removed from index", v => cmd.Verbose = true },
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
