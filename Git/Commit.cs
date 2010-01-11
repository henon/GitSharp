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
    [Command(complete = false, common = true, usage = "Record changes to the repository")]
    class Commit : TextBuiltin
    {
        private static Boolean isHelp = false;

#if ported
        private static Boolean isCommitAll = false;
        private static String reUseMessage = "";
        private static String reEditMessage = "";
        private static String cleanupOption = "default";
        private static String untrackedFileMode = "all";
        private static String message = "";
        private static String author = "";
        private static String logFile = "";
        private static String templateFile = "";
        private static Boolean isSignOff= false;
        private static Boolean isNoVerify = false;
        private static Boolean isAllowEmpty = false;
        private static Boolean isAmend = false;
        private static Boolean isForceEdit = false;
        private static Boolean isInclude = false;
        private static Boolean isCommitOnly = false;
        private static Boolean isInteractive = false;
        private static Boolean isVerbose = false;
        private static Boolean isQuiet = false;
        private static Boolean isDryRun = false;
#endif

        override public void Run(String[] args)
        {
            options = new CmdParserOptionSet()
            {
                { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
#if ported
                { "v|verbose", "Be verbose", v=>{isVerbose = true;}},
                { "q|quiet", "Be quiet", v=>{isQuiet = true;}},
                { "F|file=", "Read log from {file}", (string v) => logFile = v },
                { "author=", "Override {author} for commit", (string v) => author = v },
                { "m|message=", "Specify commit {message}", (string v) => message = v },
                { "c|reedit-message=", "Reuse and edit {message} from specified commit", (string v) => reEditMessage = v },
                { "C|reuse-message=", "Reuse {message} from specified commit", (string v) => reUseMessage = v },
                { "s|signoff", "Add Signed-off-by:", v=>{isSignOff = true;}},
                { "t|template=", "Use specified {template} file", (string v) => templateFile = v },
                { "e|edit", "Force edit of commit", v=>{isForceEdit = true;}},
                { "a|all", "Commit all changed files.", v=>{isCommitAll = true;}},
                { "i|include", "Add specified files to index for commit", v=>{isInclude = true;}},
                { "interactive", "Interactively add files", v=>{isInteractive = true;}},
                { "o|only", "Commit only specified files", v=>{isCommitOnly = true;}},
                { "n|no-verify", "Bypass pre-commit hook", v=>{isNoVerify = true;}},
                { "amend", "Amend previous commit", v=>{isAmend = true;}},
                { "u|untracked-files=", "Show untracked files, optional {MODE}s: all, normal, no.", (string v) => untrackedFileMode = v },
                { "allow-empty", "Ok to record an empty change", v=> {isAllowEmpty = true;}},
                { "cleanup=", "How to strip spaces and #comments from message. Options are: " +
                    "verbatim, whitespace, strip, and default.", (string v) => cleanupOption = v },
                { "dry-run", "Don't actually commit the files, just show if they exist.", v=>{isDryRun = true;}},


               // [Mr Happy] There are the options that should be compatible w/ the stub, placed for convenience only.
               //{ "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
               //{ "a|all", "Tell the command to automatically stage files that have been modified and deleted, but new files you have not told git about are not affected", v => cmd.All = true },
               //{ "C|reuse-message=", "Take an existing commit object, and reuse the log message and the authorship information (including the timestamp) when creating the commit", v => cmd.Reusemessage = v },
               //{ "c|reedit-message=", "Like '-C', but with '-c' the editor is invoked, so that the user can further edit the commit message", v => cmd.Reeditmessage = v },
               //{ "reset-author", "When used with -C/-c/--amend options, declare that the authorship of the resulting commit now belongs of the committer", v => cmd.Resetauthor = true },
               //{ "F|file=", "Take the commit message from the given file", v => cmd.File = v },
               //{ "author=", "Override the author name used in the commit", v => cmd.Author = v },
               //{ "m|message=", "Use the given <msg> as the commit message", v => cmd.Message = v },
               //{ "t|template=", "Use the contents of the given file as the initial version of the commit message", v => cmd.Template = v },
               //{ "s|signoff", "Add Signed-off-by line by the committer at the end of the commit log message", v => cmd.Signoff = true },
               //{ "n|no-verify", "This option bypasses the pre-commit and commit-msg hooks", v => cmd.Noverify = true },
               //{ "allow-empty", "Usually recording a commit that has the exact same tree as its sole parent commit is a mistake, and the command prevents you from making such a commit", v => cmd.Allowempty = true },
               //{ "cleanup=", "This option sets how the commit message is cleaned up", v => cmd.Cleanup = v },
               //{ "e|edit", "The message taken from file with `-F`, command line with `-m`, and from file with `-C` are usually used as the commit log message unmodified", v => cmd.Edit = true },
               //{ "amend", "Used to amend the tip of the current branch", v => cmd.Amend = true },
#endif
            };

            try
            {
                List<String> arguments = ParseOptions(args);
                if (arguments.Count > 0)
                {
                    //Execute the commit using the specified file pattern
                    DoCommit(arguments[0]);
                }
                else if (args.Length <= 0)
                {
                    //Display status if no changes are added to commit
                    //If changes have been made, commit them?
                    Console.WriteLine("These commands still need to be implemented.");
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
                Console.WriteLine("usage: git commit [options] [--] <filepattern>...");
                Console.WriteLine();
                options.WriteOptionDescriptions(Console.Out);
            }
        }

        public static void DoCommit(String filepattern)
        {
            Console.WriteLine("This command still needs to be implemented.");
        }
    }
}
