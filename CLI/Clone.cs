/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2008, Caytchen 
 * Copyright (C) 2008, Rolenun
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

    [Command(common=true, usage = "Clone a repository into a new directory")]
    public class Clone : TextBuiltin
    {
        private Git.CloneCommand cmd = new Git.CloneCommand();

        /*
         * private static Boolean isHelp = false;              //Complete        
        private static Boolean isQuiet = false;        
        private static Boolean isVerbose = false;        
        private static Boolean isNoCheckout = false;        //Complete        
        private static Boolean isCreateBareRepo = false;    //In progress        
        private static Boolean isCreateMirrorRepo = false;  //More info needed        
        private static Boolean isNoHardLinks = false;       //Unimplemented        
        private static Boolean isShared = false;            //Unimplemented        
        private static String templateRepo = "";            //More info needed        
        private static String referenceRepo = "";           //More info needed        
        private static String optionOrigin = "";            //Complete        
        private static String uploadPack = "";              //More info needed        
        private static Int32 depth = 0;                     //More info needed
        */
        private static Boolean isHelp = false;

        public override void Run(string[] args)
        {
            cmd.Quiet = false;
			this.RequiresRepository = true;
			
            options = new CmdParserOptionSet()
            {
                { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
                { "q|quiet", "Be quiet", v => cmd.Quiet = true},
                { "v|verbose", "Be verbose", v => cmd.Quiet = false},
                { "n|no-checkout", "Don't create a checkout", v => cmd.NoCheckout = true},
                { "bare", "Create a bare repository", v=> cmd.Bare = true},
                { "naked", "Create a bare repository", v => cmd.Bare = true},
                { "mirror", "Create a mirror repository (implies bare)", v => cmd.Mirror = true},
                { "l|local", "To clone from a local repository", v => {}}, // was: die("--local is the default behavior. This option is no-op.").  [henon] I think we should silently ignore that switch instead of exiting.
                { "no-hardlinks", "(No-op) Do not use hard links, always copy", v => die("--no-hardlinks is not supported")},
                { "s|shared", "(No-op) Setup as shared repository", v => die("--shared is not supported")},
                { "template=", "{Path} the template repository",(string v) => cmd.TemplateDirectory = v },
                { "reference=", "Reference {repo}sitory",(string v) => cmd.ReferenceRepository = v },
                { "o|origin=", "Use <{branch}> instead of 'origin' to track upstream",(string v) => cmd.OriginName = v },
                { "u|upload-pack=", "{Path} to git-upload-pack on the remote",(string v) => cmd.UploadPack = v },
                { "depth=", "Create a shallow clone of that {depth}",(int v) => cmd.Depth = v },
                { "git-dir", "Set the new directory to clone into", (string v) => cmd.GitDirectory = v },
            };

            try
            {
                List<String> arguments = ParseOptions(args);
                if (arguments.Count > 0)
                {
                    cmd.Source = arguments[0];
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
                cmd.OutputStream.WriteLine("usage: git clone [options] [--] <repo> [<dir>]");
                cmd.OutputStream.WriteLine();
                options.WriteOptionDescriptions(Console.Out);
                cmd.OutputStream.WriteLine();
            }
        }
    }
}