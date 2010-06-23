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
    [Command(complete = false, common = true, usage = "Show various types of objects")]
    class Show : TextBuiltin
    {

        private static Boolean isHelp = false;

#if ported
        private static string prettyFormat = "";
        private static string abbrevCommit = "";
        private static Boolean isOneLine = false;
        private static string encoding = "";
#endif

        override public void Run(String[] args)
        {

            options = new CmdParserOptionSet()
            {
                { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
#if ported
                { "pretty|format", "Pretty print the contents of the commit logs in a specified {format}", (string v) => prettyFormat = v},
                { "abbrev-commit", "Show only a commit object's partial name", (string v) => abbrevCommit = v},
                { "oneline", "Shorthand for --pretty=online and --abbrev-commit together", v=>{isOneLine = true;}},
                { "encoding", "Display the commit log message(s) using the specified {encoding}", (string v) => encoding = v},
#endif
            };

            try
            {
                List<String> arguments = ParseOptions(args);
                if (arguments.Count > 0)
                {
                    DoShow(arguments);
                }
                else if (args.Length <= 0)
                {
                    //Do show with preset arguments
                    DoShow(arguments);
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
                Console.WriteLine("usage: git show [options] <object>...");
               Console.WriteLine();
                options.WriteOptionDescriptions(Console.Out);
                Console.WriteLine();
            }
        }

        private void DoShow(List<String> args)
        {
            Console.WriteLine("This command still needs to be implemented.");
        }
    }
}
