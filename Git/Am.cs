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
    public class Am : TextBuiltin
    {
        private AmCommand cmd = new AmCommand();
        private static Boolean isHelp;

        public override void Run(string[] args)
        {
            cmd.Quiet = false;
			
            options = new CmdParserOptionSet()
            {
               { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
               { "s|signoff", "Add a `Signed-off-by:` line to the commit message, using the committer identity of yourself", v => cmd.Signoff = true },
               { "k|keep", "Pass `-k` flag to 'git-mailinfo' (see linkgit:git-mailinfo[1])", v => cmd.Keep = true },
               { "c|scissors", "Remove everything in body before a scissors line (see linkgit:git-mailinfo[1])", v => cmd.Scissors = true },
               { "no-scissors", "Ignore scissors lines (see linkgit:git-mailinfo[1])", v => cmd.Noscissors = true },
               { "q|quiet", "Be quiet", v => cmd.Quiet = true },
               { "u|utf8", "Pass `-u` flag to 'git-mailinfo' (see linkgit:git-mailinfo[1])", v => cmd.Utf8 = true },
               { "no-utf8", "Pass `-n` flag to 'git-mailinfo' (see linkgit:git-mailinfo[1])", v => cmd.Noutf8 = true },
               { "3|3way", "When the patch does not apply cleanly, fall back on 3-way merge if the patch records the identity of blobs it is supposed to apply to and we have those blobs available locally", v => cmd.Threeway = true },
               { "ignore-date", "These flags are passed to the 'git-apply' (see linkgit:git-apply[1]) program that applies the patch", v => cmd.Ignoredate = true },
               { "ignore-space-change", "These flags are passed to the 'git-apply' (see linkgit:git-apply[1]) program that applies the patch", v => cmd.Ignorespacechange = true },
               { "ignore-whitespace=", "These flags are passed to the 'git-apply' (see linkgit:git-apply[1]) program that applies the patch", v => cmd.Ignorewhitespace = v },
               { "w|whitespace=", "These flags are passed to the 'git-apply' (see linkgit:git-apply[1]) program that applies the patch", v => cmd.Whitespace = v },
               { "p|directory=", "These flags are passed to the 'git-apply' (see linkgit:git-apply[1]) program that applies the patch", v => cmd.Directory = v },
               { "reject", "These flags are passed to the 'git-apply' (see linkgit:git-apply[1]) program that applies the patch", v => cmd.Reject = true },
               { "i|interactive", "Run interactively", v => cmd.Interactive = true },
               { "committer-date-is-author-date", "By default the command records the date from the e-mail message as the commit author date, and uses the time of commit creation as the committer date", v => cmd.Committerdateisauthordate = true },
               { "ignore-date", "By default the command records the date from the e-mail message as the commit author date, and uses the time of commit creation as the committer date", v => cmd.Ignoredate = true },
               { "skip", "Skip the current patch", v => cmd.Skip = true },
               { "r|resolved", "After a patch failure (e", v => cmd.Resolved = true },
               { "resolvemsg=", "When a patch failure occurs, <msg> will be printed to the screen before exiting", v => cmd.Resolvemsg = v },
               { "abort", "Restore the original branch and abort the patching operation", v => cmd.Abort = true },
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
