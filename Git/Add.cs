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
using GitSharp.Commands;

namespace GitSharp.CLI
{
    [Command(complete = false, common = true, requiresRepository = true, usage = "Add file contents to the index")]
    class Add : TextBuiltin
    {
        private AddCommand cmd = new AddCommand();

        private static Boolean isHelp = false;

#if ported
        private static Boolean isDryRun = false;
        private static Boolean isVerbose = false;
        private static Boolean isForced = false;
        private static Boolean isInteractive = false;
        private static Boolean isUpdateKnown = false;
        private static Boolean isUpdateAll = false;
        private static Boolean isIntentToAdd = false;
        private static Boolean isRefreshOnly = false;
        private static Boolean isIgnoreErrors = false;
#endif

        override public void Run(String[] args)
        {
            options = new CmdParserOptionSet()
            {
                { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
#if ported
                { "n|dry-run", "Don't actually add the files, just show if they exist.", v=>{isDryRun = true;}},
                { "v|verbose", "Be verbose.", v=> {isVerbose = true;}},
                { "f|force", "Allow adding otherwise ignored files.", v=> {isForced = true;}},
                { "i|interactive", "Interactive picking.", v=>{isInteractive = true;}},
                { "p|patch", "Interactive patching.", v=>DoPatch()},
                { "e|edit", "Open the diff vs. the index in an editor and let the user edit it.", v=>DoEdit()},
                { "u|update", "Update tracked files.", v=> {isUpdateKnown = true;}},
                { "A|all", "Add all files, noticing removal of tracked files.", v=>{isUpdateAll = true;}},
                { "N|intent-to-add", "Record only the fact the path will be added later.", v=>{isIntentToAdd = true;}},
                { "refresh", "Don't add the files, only refresh the index.", v=> {isRefreshOnly = true;}},
                { "ignore-errors", "Just skip files which cannot be added because of errors.", v=>{isIgnoreErrors = true;}},
#endif
            };

            try
            {
                List<String> arguments = ParseOptions(args);
                if (arguments.Count > 0)
                {
                    //Add the file(s)
                    //DoAdd(arguments);
                    try
                    {
                        cmd.Arguments = arguments;
                        cmd.Execute();
                    }
                    catch (ArgumentException e)
                    {
                        Console.WriteLine("Path does not exist: " + e.ParamName);
                        Console.WriteLine("Adding path(s) has been aborted.");
                    }
                }
                else if (args.Length <= 0)
                {
                    //Display the modified files for the existing repository
                    Console.WriteLine("Nothing specified, nothing added.");
                    Console.WriteLine("Maybe you wanted to say 'git add .'?");
                }
                else
                {
                    OfflineHelp();
                }
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void OfflineHelp()
        {
            if (!isHelp)
            {
                isHelp = true;
                Console.WriteLine("usage: git add [options] [--] <filepattern>..."); 
                Console.WriteLine(); 
                options.WriteOptionDescriptions(Console.Out); 
            }
        }

        private static void DoAdd(List<String> filesAdded)
        {
            Console.WriteLine("This command still needs to be implemented.");
        }

        private static void DoEdit()
        {
            Console.WriteLine("This option still needs to be implemented.");
        }

        private static void DoPatch()
        {
            Console.WriteLine("This option still needs to be implemented.");
        }

    }
}
