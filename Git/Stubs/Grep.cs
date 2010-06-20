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
    public class Grep : TextBuiltin
    {
        private GrepCommand cmd = new GrepCommand();
        private static Boolean isHelp;

        public override void Run(string[] args)
        {
            options = new CmdParserOptionSet()
            {
               { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
               { "cached", "Instead of searching in the working tree files, check the blobs registered in the index file", v => cmd.Cached = true },
               { "a|text", "Process binary files as if they were text", v => cmd.Text = true },
               { "i|ignore-case", "Ignore case differences between the patterns and the files", v => cmd.IgnoreCase = true },
               { "I", "Don't match the pattern in binary files", v => cmd.I = true },
               { "max-depth=", "For each pathspec given on command line, descend at most <depth> levels of directories", v => cmd.MaxDepth = v },
               { "w|word-regexp", "Match the pattern only at word boundary (either begin at the beginning of a line, or preceded by a non-word character; end at the end of a line or followed by a non-word character)", v => cmd.WordRegexp = true },
               { "v|invert-match", "Select non-matching lines", v => cmd.InvertMatch = true },
               { "h|H", "By default, the command shows the filename for each match", v => cmd.H = true },
               { "full-name", "When run from a subdirectory, the command usually outputs paths relative to the current directory", v => cmd.FullName = true },
               { "E|extended-regexp", "Use POSIX extended/basic regexp for patterns", v => cmd.ExtendedRegexp = true },
               { "G|basic-regexp", "Use POSIX extended/basic regexp for patterns", v => cmd.BasicRegexp = true },
               { "F|fixed-strings", "Use fixed strings for patterns (don't interpret pattern as a regex)", v => cmd.FixedStrings = true },
               { "n", "Prefix the line number to matching lines", v => cmd.N = true },
               { "l|files-with-matches", "Instead of showing every matched line, show only the names of files that contain (or do not contain) matches", v => cmd.FilesWithMatches = true },
               { "name-only", "Instead of showing every matched line, show only the names of files that contain (or do not contain) matches", v => cmd.NameOnly = true },
               { "files-without-match", "Instead of showing every matched line, show only the names of files that contain (or do not contain) matches", v => cmd.FilesWithoutMatch = true },
               { "z|null", "Output \0 instead of the character that normally follows a file name", v => cmd.Null = true },
               { "c|count", "Instead of showing every matched line, show the number of lines that match", v => cmd.Count = true },
               { "color", "Show colored matches", v => cmd.Color = true },
               { "no-color", "Turn off match highlighting, even when the configuration file gives the default to color output", v => cmd.NoColor = true },
               // [Mr Happy] Original documentation says: -[ABC] <context>
               { "A=", "Show `context` trailing (`A` -- after), or leading (`B` -- before), or both (`C` -- context) lines, and place a line containing `--` between contiguous groups of matches", v => cmd.A = v },
               { "B=", "Show `context` trailing (`A` -- after), or leading (`B` -- before), or both (`C` -- context) lines, and place a line containing `--` between contiguous groups of matches", v => cmd.B = v },
               { "C=", "Show `context` trailing (`A` -- after), or leading (`B` -- before), or both (`C` -- context) lines, and place a line containing `--` between contiguous groups of matches", v => cmd.C = v },
               // [Mr Happy] Is more of an argument that an option.
               //{ "<num>=", "A shortcut for specifying -C<num>", v => cmd.< = v },
               { "p|show-function", "Show the preceding line that contains the function name of the match, unless the matching line is a function name itself", v => cmd.ShowFunction = true },
               { "f=", "Read patterns from <file>, one per line", v => cmd.F = v },
               { "e", "The next parameter is the pattern", v => cmd.E = true },
               { "and", "(", v => cmd.And = true },
               { "or", "(", v => cmd.Or = true },
               { "not", "(", v => cmd.Not = true },
               { "all-match", "When giving multiple pattern expressions combined with `--or`, this flag is specified to limit the match to files that have lines to match all of them", v => cmd.AllMatch = true },
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
