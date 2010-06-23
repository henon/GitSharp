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
    [Command(complete = false, common = true, usage = "Download objects and refs from another repository")]
    class Fetch : TextBuiltin
    {
        private static Boolean isHelp = false;

#if ported
        private static Boolean isQuiet = false;
        private static Boolean isVerbose = false;
        private static Boolean isAppend = false;
        private static String uploadPack = "";
        private static Boolean isForced = false;
        private static Boolean isTags = false;
        private static Boolean isNoTags = false;
        private static Boolean isKeep = false;
        private static Boolean isUpdateHeadOk = false;
        private static Int32 depth = 0;
#endif 

        override public void Run(String[] args)
        {
            options = new CmdParserOptionSet()
            {
                { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},

#if ported
                { "q|quiet", "Be quiet", v=>{isQuiet = true;}},
                { "v|verbose", "Be verbose", v=>{isVerbose = true;}},
                { "append", "Append to .git/FETCH_HEAD instead of overwriting", v=>{isAppend = true;}},
                { "upload-pack=", "{Path} to upload pack on remote end", (string v) => uploadPack = v },
                { "force", "Force overwrite of local branch", v=> {isForced = true;}},
                { "tags", "Fetch all tags and associated objects", v=>{isTags = true;} },
                { "no-tags", "Disable tags from being fetched and stored locally", v=>{isNoTags = true;}},
                { "k|keep", "Keep download pack", v=>{isKeep = true;}},
                { "u|update-head-ok", "Allow updating of HEAD ref", v=>{isUpdateHeadOk = true;}},
                { "depth=", "Deepen the history of a shallow repository created by git clone", (int v) => depth = v },
#endif
            };

            try
            {
                List<String> arguments = ParseOptions(args);
                if (arguments.Count > 0)
                {
                    DoFetch(arguments);
                }
                else if (args.Length <= 0)
                {
                    // DoFetch with preset arguments
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

        private void OfflineHelp()
        {
            if (!isHelp)
            {
                isHelp = true;
                Console.WriteLine("usage: git fetch [options] [<repository> <refspec>...]");
                Console.WriteLine();
                options.WriteOptionDescriptions(Console.Out);
            }
        }

        private void DoFetch(List<String> args)
        {
            Console.WriteLine("This command still needs to be implemented.");
        }
    }
}
