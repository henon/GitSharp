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
    public class Repack : TextBuiltin
    {
        private RepackCommand cmd = new RepackCommand();
        private static Boolean isHelp;

        public override void Run(string[] args)
        {
            options = new CmdParserOptionSet()
            {
               { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
               { "a", "Instead of incrementally packing the unpacked objects, pack everything referenced into a single pack", v => cmd.a = true },
               { "A", "Same as `-a`, unless '-d' is used", v => cmd.A = true },
               { "d", "After packing, if the newly created packs make some existing packs redundant, remove the redundant packs", v => cmd.D = true },
               { "l", "Pass the `--local` option to 'git-pack-objects'", v => cmd.L = true },
               { "f", "Pass the `--no-reuse-object` option to `git-pack-objects`, see linkgit:git-pack-objects[1]", v => cmd.F = true },
               { "q", "Pass the `-q` option to 'git-pack-objects'", v => cmd.Q = true },
               { "n", "Do not update the server information with 'git-update-server-info'", v => cmd.N = true },
               { "window=", "These two options affect how the objects contained in the pack are stored using delta compression", v => cmd.Window = v },
               { "depth=", "These two options affect how the objects contained in the pack are stored using delta compression", v => cmd.Depth = v },
               { "window-memory=", "This option provides an additional limit on top of `--window`; the window size will dynamically scale down so as to not take up more than N bytes in memory", v => cmd.WindowMemory = v },
               { "max-pack-size=", "Maximum size of each output packfile, expressed in MiB", v => cmd.MaxPackSize = v },
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
