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
    public class PackObjects : TextBuiltin
    {
        private PackObjectsCommand cmd = new PackObjectsCommand();
        private static Boolean isHelp;

        public override void Run(string[] args)
        {
            options = new CmdParserOptionSet()
            {
               { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
               { "stdout", "Write the pack contents (what would have been written to", v => cmd.Stdout = true },
               { "revs", "Read the revision arguments from the standard input, instead of individual object names", v => cmd.Revs = true },
               { "unpacked", "This implies `--revs`", v => cmd.Unpacked = true },
               { "all", "This implies `--revs`", v => cmd.All = true },
               { "include-tag", "Include unasked-for annotated tags if the object they reference was included in the resulting packfile", v => cmd.IncludeTag = true },
               { "window=", "These two options affect how the objects contained in the pack are stored using delta compression", v => cmd.Window = v },
               { "depth=", "These two options affect how the objects contained in the pack are stored using delta compression", v => cmd.Depth = v },
               { "window-memory=", "This option provides an additional limit on top of `--window`; the window size will dynamically scale down so as to not take up more than N bytes in memory", v => cmd.WindowMemory = v },
               { "max-pack-size=", "Maximum size of each output packfile, expressed in MiB", v => cmd.MaxPackSize = v },
               { "honor-pack-keep", "This flag causes an object already in a local pack that has a", v => cmd.HonorPackKeep = true },
               { "incremental", "This flag causes an object already in a pack ignored even if it appears in the standard input", v => cmd.Incremental = true },
               { "local", "This flag is similar to `--incremental`; instead of ignoring all packed objects, it only ignores objects that are packed and/or not in the local object store (i", v => cmd.Local = true },
               { "non-empty", "Only create a packed archive if it would contain at         least one object", v => cmd.NonEmpty = true },
               { "progress", "Progress status is reported on the standard error stream by default when it is attached to a terminal, unless -q is specified", v => cmd.Progress = true },
               { "all-progress", "When --stdout is specified then progress report is displayed during the object count and compression phases but inhibited during the write-out phase", v => cmd.AllProgress = true },
               { "all-progress-implied", "This is used to imply --all-progress whenever progress display is activated", v => cmd.AllProgressImplied = true },
               { "q", "This flag makes the command not to report its progress on the standard error stream", v => cmd.Q = true },
               { "no-reuse-delta", "When creating a packed archive in a repository that has existing packs, the command reuses existing deltas", v => cmd.NoReuseDelta = true },
               { "no-reuse-object", "This flag tells the command not to reuse existing object data at all, including non deltified object, forcing recompression of everything", v => cmd.NoReuseObject = true },
               { "compression=", "Specifies compression level for newly-compressed data in the generated pack", v => cmd.Compression = v },
               { "delta-base-offset", "A packed archive can express base object of a delta as either 20-byte object name or as an offset in the stream, but older version of git does not understand the latter", v => cmd.DeltaBaseOffset = true },
               { "threads=", "Specifies the number of threads to spawn when searching for best delta matches", v => cmd.Threads = v },
               { "index-version=", "This is intended to be used by the test suite only", v => cmd.IndexVersion = v },
               { "keep-true-parents", "With this option, parents that are hidden by grafts are packed nevertheless", v => cmd.KeepTrueParents = true },
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
