/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Rolenun <rolenun@gmail.com>
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

namespace GitSharp.CLI
{
    [Command(complete = false, common = true, usage = "Remove files from the working tree and from the index")]
    class Rm : TextBuiltin
    {

        private static Boolean isHelp = false;

#if ported
        private static Boolean isDryRun = false;
        private static Boolean isForced = false;
        private static Boolean isQuiet = false;
        private static Boolean isCached = false;
        private static Boolean allowRecursiveRemoval = false;
        private static Boolean ignoreUnmatch = false;
#endif

        override public void Run(String[] args)
        {

            options = new CmdParserOptionSet()
            {
                { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
#if ported
                { "n|dry-run", "Dry run", v=> {isDryRun = true;}},
                { "f|force", "Override the up-to-date check", v=>{isForced = true;}},
                { "q|quiet", "Be quiet", v=>{isQuiet = true;}},
                { "cached", "Only remove from the index", v=>{isCached = true;}},
                { "r", "Allow recursive removal", v=>{allowRecursiveRemoval = true;}},
                { "ignore-match", "Exit with a zero status even if nothing is matched", v=>{ignoreUnmatch = true;}},
#endif
            };

            try
            {
                List<String> arguments = ParseOptions(args);
                if (arguments.Count > 0)
                {
                    DoRm(arguments);
                }
                else
                {
                    OfflineHelp();
                }
            } catch (OptionException e) {
                Console.WriteLine(e.Message);
            }
        }

        private void OfflineHelp()
        {
            if (!isHelp)
            {
                isHelp = true;
                Console.WriteLine("usage: git rm [options] [--] <file>...");
               Console.WriteLine();
                options.WriteOptionDescriptions(Console.Out);
                Console.WriteLine();
            }
        }

        private void DoRm(List<String> args)
        {
            Console.WriteLine("This command still needs to be implemented.");
        }

    }
}
