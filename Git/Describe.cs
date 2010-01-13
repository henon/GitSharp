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
    public class Describe : TextBuiltin
    {
        private DescribeCommand cmd = new DescribeCommand();
        private static Boolean isHelp;

        public override void Run(string[] args)
        {

            options = new CmdParserOptionSet()
            {
               { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
               { "all", "Instead of using only the annotated tags, use any ref found in `", v => cmd.All = true },
               { "tags", "Instead of using only the annotated tags, use any tag found in `", v => cmd.Tags = true },
               { "contains", "Instead of finding the tag that predates the commit, find the tag that comes after the commit, and thus contains it", v => cmd.Contains = true },
               { "abbrev=", "Instead of using the default 7 hexadecimal digits as the abbreviated object name, use <n> digits, or as many digits as needed to form a unique object name", v => cmd.Abbrev = v },
               { "candidates=", "Instead of considering only the 10 most recent tags as candidates to describe the input committish consider up to <n> candidates", v => cmd.Candidates = v },
               { "exact-match", "Only output exact matches (a tag directly references the supplied commit)", v => cmd.ExactMatch = true },
               { "debug", "Verbosely display information about the searching strategy being employed to standard error", v => cmd.Debug = true },
               { "long", "Always output the long format (the tag, the number of commits and the abbreviated commit name) even when it matches a tag", v => cmd.Long = true },
               { "match=", "Only consider tags matching the given pattern (can be used to avoid leaking private tags made from the repository)", v => cmd.Match = v },
               { "always", "Show uniquely abbreviated commit object as fallback", v => cmd.Always = true },
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
