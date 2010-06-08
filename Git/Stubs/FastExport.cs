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
    public class Fastexport : TextBuiltin
    {
        private FastExportCommand cmd = new FastExportCommand();
        private static Boolean isHelp;

        public override void Run(string[] args)
        {
            options = new CmdParserOptionSet()
            {
               { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
               { "progress=", "Insert 'progress' statements every <n> objects, to be shown by 'git-fast-import' during import", v => cmd.Progress = v },
               { "signed-tags=", "Specify how to handle signed tags", v => cmd.SignedTags = v },
               { "tag-of-filtered-object=", "Specify how to handle tags whose tagged objectis filtered out", v => cmd.TagOfFilteredObject = v },
               { "M", "Perform move and/or copy detection, as described in the linkgit:git-diff[1] manual page, and use it to generate rename and copy commands in the output dump", v => cmd.M = true },
               { "C", "Perform move and/or copy detection, as described in the linkgit:git-diff[1] manual page, and use it to generate rename and copy commands in the output dump", v => cmd.C = true },
               { "export-marks=", "Dumps the internal marks table to <file> when complete", v => cmd.ExportMarks = v },
               { "import-marks=", "Before processing any input, load the marks specified in <file>", v => cmd.ImportMarks = v },
               { "fake-missing-tagger", "Some old repositories have tags without a tagger", v => cmd.FakeMissingTagger = true },
               { "no-data", "Skip output of blob objects and instead refer to blobs via their original SHA-1 hash", v => cmd.NoData = true },
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
