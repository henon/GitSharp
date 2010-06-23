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
    public class Apply : TextBuiltin
    {
        private ApplyCommand cmd = new ApplyCommand();
        private static Boolean isHelp;

        public override void Run(string[] args)
        {			
            options = new CmdParserOptionSet()
            {
               { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
               { "stat", "Instead of applying the patch, output diffstat for the input", v => cmd.Stat = true },
               { "numstat", "Similar to `--stat`, but shows the number of added and deleted lines in decimal notation and the pathname without abbreviation, to make it more machine friendly", v => cmd.Numstat = true },
               { "summary", "Instead of applying the patch, output a condensed summary of information obtained from git diff extended headers, such as creations, renames and mode changes", v => cmd.Summary = true },
               { "check", "Instead of applying the patch, see if the patch is applicable to the current working tree and/or the index file and detects errors", v => cmd.Check = true },
               { "index", "When `--check` is in effect, or when applying the patch (which is the default when none of the options that disables it is in effect), make sure the patch is applicable to what the current index file records", v => cmd.Index = true },
               { "cached", "Apply a patch without touching the working tree", v => cmd.Cached = true },
               { "build-fake-ancestor=", "Newer 'git-diff' output has embedded 'index information' for each blob to help identify the original version that the patch applies to", v => cmd.BuildFakeAncestor = v },
               { "R|reverse", "Apply the patch in reverse", v => cmd.Reverse = true },
               { "reject", "For atomicity, 'git-apply' by default fails the whole patch and does not touch the working tree when some of the hunks do not apply", v => cmd.Reject = true },
               { "z", "When `--numstat` has been given, do not munge pathnames, but use a NUL-terminated machine-readable format", v => cmd.Z = true },
               { "p=", "Remove <n> leading slashes from traditional diff paths", v => cmd.P = v },
               { "C=", "Ensure at least <n> lines of surrounding context match before and after each change", v => cmd.C = v },
               { "unidiff-zero", "By default, 'git-apply' expects that the patch being applied is a unified diff with at least one line of context", v => cmd.UnidiffZero = true },
               { "apply", "If you use any of the options marked \"Turns off 'apply'\" above, 'git-apply' reads and outputs the requested information without actually applying the patch", v => cmd.Apply = true },
               { "no-add", "When applying a patch, ignore additions made by the patch", v => cmd.NoAdd = true },
               { "allow-binary-replacement", "Historically we did not allow binary patch applied without an explicit permission from the user, and this flag was the way to do so", v => cmd.AllowBinaryReplacement = true },
               { "binary", "Historically we did not allow binary patch applied without an explicit permission from the user, and this flag was the way to do so", v => cmd.Binary = true },
               { "exclude=", "Don't apply changes to files matching the given path pattern", v => cmd.Exclude = v },
               { "include=", "Apply changes to files matching the given path pattern", v => cmd.Include = v },
               { "ignore-space-change", "When applying a patch, ignore changes in whitespace in context lines if necessary", v => cmd.IgnoreSpaceChange = true },
               { "ignore-whitespace", "When applying a patch, ignore changes in whitespace in context lines if necessary", v => cmd.IgnoreWhitespace = true },
               { "whitespace=", "When applying a patch, detect a new or modified line that has whitespace errors", v => cmd.Whitespace = v },
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
