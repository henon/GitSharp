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
    public class Difffiles : TextBuiltin
    {
        private DiffFilesCommand cmd = new DiffFilesCommand();
        private static Boolean isHelp;

        public override void Run(string[] args)
        {
            cmd.Quiet = false;
			
            options = new CmdParserOptionSet()
            {
               { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
               { "1", "Diff against the \"base\" version, \"our branch\" or \"their branch\" respectively", v => cmd.One = true },
               { "2", "Diff against the \"base\" version, \"our branch\" or \"their branch\" respectively", v => cmd.Two = true },
               { "3", "Diff against the \"base\" version, \"our branch\" or \"their branch\" respectively", v => cmd.Three = true },
               { "0", "Diff against the \"base\" version, \"our branch\" or \"their branch\" respectively", v => cmd.Zero = true },
               { "c|cc", "This compares stage 2 (our branch), stage 3 (their branch) and the working tree file and outputs a combined diff, similar to the way 'diff-tree' shows a merge commit with these flags", v => cmd.Cc = true },
               { "q", "Remain silent even on nonexistent files", v => cmd.Q = true },
               { "p|no-stat", "ifdef::git-format-patch[] Generate plain patches without any diffstats", v => cmd.NoStat = true },
               { "p", "ifndef::git-format-patch[] Generate patch (see section on generating patches)", v => cmd.P = true },
               { "u", "ifndef::git-format-patch[] Generate patch (see section on generating patches)", v => cmd.U = true },
               { "U|unified=", "Generate diffs with <n> lines of context instead of the usual three", v => cmd.Unified = v },
               { "raw", "ifndef::git-format-patch[] Generate the raw format", v => cmd.Raw = true },
               { "patch-with-raw", "ifndef::git-format-patch[] Synonym for `-p --raw`", v => cmd.PatchWithRaw = true },
               { "patience", "Generate a diff using the \"patience diff\" algorithm", v => cmd.Patience = true },
               { "stat=", "Generate a diffstat", v => cmd.Stat = v },
               { "numstat", "Similar to `--stat`, but shows number of added and deleted lines in decimal notation and pathname without abbreviation, to make it more machine friendly", v => cmd.Numstat = true },
               { "shortstat", "Output only the last line of the `--stat` format containing total number of modified files, as well as number of added and deleted lines", v => cmd.Shortstat = true },
               { "dirstat=", "Output the distribution of relative amount of changes (number of lines added or removed) for each sub-directory", v => cmd.Dirstat = v },
               { "dirstat-by-file=", "Same as `--dirstat`, but counts changed files instead of lines", v => cmd.DirstatByFile = v },
               { "summary", "Output a condensed summary of extended header information such as creations, renames and mode changes", v => cmd.Summary = true },
               { "patch-with-stat", "ifndef::git-format-patch[] Synonym for `-p --stat`", v => cmd.PatchWithStat = true },
               { "z", "ifdef::git-log[] Separate the commits with NULs instead of with new newlines", v => cmd.Z = true },
               { "name-only", "Show only names of changed files", v => cmd.NameOnly = true },
               { "name-status", "Show only names and status of changed files", v => cmd.NameStatus = true },
               { "submodule=", "Chose the output format for submodule differences", v => cmd.Submodule = v },
               { "color", "Show colored diff", v => cmd.Color = true },
               { "no-color", "Turn off colored diff, even when the configuration file gives the default to color output", v => cmd.NoColor = true },
               { "color-words=", "Show colored word diff, i", v => cmd.ColorWords = v },
               { "no-renames", "Turn off rename detection, even when the configuration file gives the default to do so", v => cmd.NoRenames = true },
               { "check", "ifndef::git-format-patch[] Warn if changes introduce trailing whitespace or an indent that uses a space before a tab", v => cmd.Check = true },
               { "full-index", "Instead of the first handful of characters, show the full pre- and post-image blob object names on the \"index\" line when generating patch format output", v => cmd.FullIndex = true },
               { "binary", "In addition to `--full-index`, output a binary diff that can be applied with `git-apply`", v => cmd.Binary = true },
               { "abbrev=", "Instead of showing the full 40-byte hexadecimal object name in diff-raw format output and diff-tree header lines, show only a partial prefix", v => cmd.Abbrev = v },
               { "B", "Break complete rewrite changes into pairs of delete and create", v => cmd.B = true },
               { "M", "Detect renames", v => cmd.M = true },
               { "C", "Detect copies as well as renames", v => cmd.C = true },
               { "diff-filter=", "ifndef::git-format-patch[] Select only files that are Added (`A`), Copied (`C`), Deleted (`D`), Modified (`M`), Renamed (`R`), have their type (i", v => cmd.DiffFilter = v },
               { "find-copies-harder", "For performance reasons, by default, `-C` option finds copies only if the original file of the copy was modified in the same changeset", v => cmd.FindCopiesHarder = true },
               { "l=", "The `-M` and `-C` options require O(n^2) processing time where n is the number of potential rename/copy targets", v => cmd.L = v },
               { "S=", "ifndef::git-format-patch[] Look for differences that introduce or remove an instance of <string>", v => cmd.S = v },
               { "pickaxe-all", "When `-S` finds a change, show all the changes in that changeset, not just the files that contain the change in <string>", v => cmd.PickaxeAll = true },
               { "pickaxe-regex=", "Make the <string> not a plain string but an extended POSIX regex to match", v => cmd.PickaxeRegex = v },
               { "O=", "Output the patch in the order specified in the <orderfile>, which has one shell glob pattern per line", v => cmd.O = v },
               { "R", "ifndef::git-format-patch[] Swap two inputs; that is, show differences from index or on-disk file to tree contents", v => cmd.R = true },
               { "relative=", "When run from a subdirectory of the project, it can be told to exclude changes outside the directory and show pathnames relative to it with this option", v => cmd.Relative = v },
               { "a|text", "Treat all files as text", v => cmd.Text = true },
               { "ignore-space-at-eol", "Ignore changes in whitespace at EOL", v => cmd.IgnoreSpaceAtEol = true },
               { "b|ignore-space-change", "Ignore changes in amount of whitespace", v => cmd.IgnoreSpaceChange = true },
               { "w|ignore-all-space", "Ignore whitespace when comparing lines", v => cmd.IgnoreAllSpace = true },
               { "inter-hunk-context=", "Show the context between diff hunks, up to the specified number of lines, thereby fusing hunks that are close to each other", v => cmd.InterHunkContext = v },
               { "exit-code", "ifndef::git-format-patch[] Make the program exit with codes similar to diff(1)", v => cmd.ExitCode = true },
               { "quiet", "Disable all output of the program", v => cmd.Quiet = true },
               { "ext-diff", "Allow an external diff helper to be executed", v => cmd.ExtDiff = true },
               { "no-ext-diff", "Disallow external diff drivers", v => cmd.NoExtDiff = true },
               { "ignore-submodules", "Ignore changes to submodules in the diff generation", v => cmd.IgnoreSubmodules = true },
               { "src-prefix=", "Show the given source prefix instead of \"a/\"", v => cmd.SrcPrefix = v },
               { "dst-prefix=", "Show the given destination prefix instead of \"b/\"", v => cmd.DstPrefix = v },
               { "no-prefix", "Do not show any source or destination prefix", v => cmd.NoPrefix = true },
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
