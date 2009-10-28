/*
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
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
    [Command(complete = false, common = true, usage = "Update remote refs along with associated objects")]
    class Push : TextBuiltin
    {
        private static Boolean isHelp = false;

#if ported
        private static Boolean isVerbose = false;
        private static string repo = "";
        private static Boolean pushAllRefs = false;
        private static Boolean mirrorAllRefs = false;
        private static Boolean pushTags = false;
        private static Boolean isDryRun = false;
        private static Boolean isForced = false;
        private static Boolean useThinPack = false;
        private static string receivePack = "";
#endif

        override public void Run(String[] args)
        {
            options = new CmdParserOptionSet()
            {
                { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
#if ported
                
                { "v|verbose", "Be verbose", v=> {isVerbose = true;}},
                { "repo=", "{repository}", (string v) => repo = v},
                { "all", "Push all refs", v=> {pushAllRefs = true;}},
                { "mirror", "Mirror all refs", v=> {mirrorAllRefs = true;}},
                { "tags", "Push tags", v=> {pushTags = true;}},
                { "n|dry-run", "Dry run", v=>{isDryRun = true;}},
                { "f|force", "Force updates", v=> {isForced = true;}},
                { "thin", "Use thin pack", v=> {useThinPack = true;}},
                { "receive-pack=", "{Receive pack} program", (string v) => receivePack = v},
                { "exec=", "{Receive pack} program", (string v) => receivePack = v},
#endif
            };

            try
            {
                List<String> arguments = ParseOptions(args);
                if (arguments.Count > 0)
                {
                    //Add the file(s)
                    DoPush(arguments);
                }
                else if (args.Length <= 0)
                {
                    //Display the modified files for the existing repository
                    Console.WriteLine("Warning: You did not specify any refspecs to push, and the current remote");
                    Console.WriteLine("Warning: has not configured any push refspecs. The default action in this");
                    Console.WriteLine("Warning: case is to push all matching refspecs, that is, all branches");
                    Console.WriteLine("Warning: that exist both locally and remotely will be updated. This may");
                    Console.WriteLine("Warning: not necessarily be what you want to happen.");
                    Console.WriteLine("Warning:");
                    Console.WriteLine("Warning: You can specify what action you want to take in this case, and");
                    Console.WriteLine("Warning: avoid seeing this message again, by configuring 'push.default' to:");
                    Console.WriteLine("Warning:   'nothing'  : Do not push anything");
                    Console.WriteLine("Warning:   'matching' : Push all matching branches (default)");
                    Console.WriteLine("Warning:   'tracking' : Push the current branch to whatever it is tracking");
                    Console.WriteLine("Warning:   'current'  : Push the current branch");
                    
                    // Enter passphrase
                    Console.WriteLine("This command still needs to be implemented.");

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
                Console.WriteLine("usage: git push [options] [<repository> <refspec>...]");
                Console.WriteLine();
                options.WriteOptionDescriptions(Console.Out);
            }
        }

        private static void DoPush(List<String> filesAdded)
        {
            Console.WriteLine("This command still needs to be implemented.");
        }
    }
}
