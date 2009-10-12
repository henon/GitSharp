/*
 * Copyright (C) 2006, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using System.Linq;
using System.Text;
using System.IO;
using GitSharp.Core;
using GitSharp.Core.Exceptions;
using NDesk.Options;

// [henon] ported from org.spearce.jgit.pgm\src\org\spearce\jgit\pgm
namespace GitSharp.CLI
{
    /// <summary>
    /// Command line entry point.
    /// </summary>
    public class Program
    {
        private static CmdParserOptionSet options;

        /// <summary>
        /// Display the stack trace on exceptions
        /// </summary>
        private static bool showStackTrace;

        /// <summary>
        /// 
        /// </summary>
        private static List<String> arguments = new List<String>();

        /// <summary>
        /// Load the parser options and 
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            options = new CmdParserOptionSet()
            {
                { "complete", "display the complete commands", v => ShowComplete() },    

                { "help|h", "display this help text", v => ShowHelp() },
                { "incomplete", "display the incomplete commands", v => ShowIncomplete() },
                { "show-stack-trace", "display the C# stack trace on exceptions", v => showStackTrace=true },
                { "version", "", v => ShowVersion() },
                { "git-dir", "", v => NoOp()},
            };
            try
            {
#if ported
                //AwtAuthenticator.install();
                //HttpSupport.configureHttpProxy();
#endif
                execute(args);
            }
            catch (Die err)
            {
                Console.Error.WriteLine("fatal: " + err.Message);
                if (showStackTrace)
                    err.printStackTrace();
                Exit(128);
            }
            catch (Exception err)
            {
                if (!showStackTrace && err.InnerException != null
                        && err is TransportException)
                    Console.Error.WriteLine("fatal: " + err.InnerException.Message);

                if (err.GetType().Name.StartsWith("GitSharp.Exceptions."))
                {
                    Console.Error.WriteLine("fatal: " + err.Message);
                    if (showStackTrace)
                        err.printStackTrace();
                    Exit(128);
                }
                err.printStackTrace();
                Exit(1);
            }

        }

        /// <summary>
        /// Execute the command line
        /// </summary>
        /// <param name="argv"></param>
        private static void execute(string[] argv)
        {
            if (argv.Count() == 0)
            {
                ShowHelp();
            }
            else if (!argv[0].StartsWith("--") && !argv[0].StartsWith("-"))
            {

                CommandCatalog catalog = new CommandCatalog();
                CommandRef subcommand = catalog.Get(argv[0]);
                if (subcommand != null)
                {
                    TextBuiltin cmd = subcommand.Create();
                    List<String> args = argv.ToList();

                    if (cmd.RequiresRepository())
                    {
                        string gitdir = null;

                        try
                        {
                            for (int x = 0; x < args.Count; x++)
                            {
                                if (args[x].IndexOf("--git-dir=") > -1)
                                {
                                    if (args[x].Length > 10)
                                    {
                                        string str = args[x].Substring(11);
                                        if (!Git.Commands.GitRequiresRoot)
                                            gitdir = str;
                                        else
                                            gitdir = SystemReader.getInstance().getDirectoryRoot(str);
                                        args.RemoveAt(x);
                                        break;
                                        
                                    }
                                }
                            }
                            
                        }
                        catch (ArgumentException)
                        {
                            Git.Commands.OutputStream.WriteLine("error: can't find git directory");
                            Git.Commands.OutputStream.Flush();
                            Exit(1);
                        }

                        cmd.Init(Git.Commands.SelectRepository(), gitdir);
                    }
                    else
                    {
                        cmd.Init(null, null);
                    }

                    try
                    {
                        // Remove the subcommand from the command line
                        args.RemoveAt(0);
                        cmd.Execute(args.ToArray());
                    }
                    finally
                    {
                        if (Git.Commands.OutputStream != null)
                            Git.Commands.OutputStream.Flush();
                    }
                }
                else
                {
                    // List all available commands starting with argv[0] if the command
                    // specified does not exist.
                    // If no commands exist starting with argv[0], show the help screen.
                    if (!ShowCommandMatches(argv[0]))
                        ShowHelp();
                }
            }
            else
            {
                // If the first argument in the command line is an option (denoted by starting with - or --), 
                // no subcommand has been specified in the command line.
                try
                {
                    arguments = options.Parse(argv);
                }
                catch (OptionException err)
                {
                    if (arguments.Count > 0)
                    {
                        Console.Error.WriteLine("fatal: " + err.Message);
                        Exit(1);
                    }
                }
            }
            Exit(0);
        }

        /// <summary>
        /// Display the main offline help screen.
        /// </summary>
        private static void ShowHelp()
        {
            Console.Write("usage: git ");
            Console.Write(string.Join(" ", options.Select(o => "[--" + string.Join("|-", o.Names) + "]").ToArray()));
            Console.WriteLine("\nCOMMAND [ARGS]\n\nThe most commonly used git commands are:");
            options.WriteOptionDescriptions(Console.Error);
            Console.WriteLine();

            CommandCatalog catalog = new CommandCatalog();
            foreach (CommandRef c in catalog.Common())
            {
                Console.Write("      ");
                Console.Write(c.getName());
                for (int i = c.getName().Length + 8; i < 31; i++)
                    Console.Write(" ");
                Console.Write(c.getUsage());
                Console.WriteLine();
            }
            Console.Error.WriteLine();
            Console.Error.Write(@"See 'git help COMMAND' for more information on a specific command.");
        }

        /// <summary>
        /// Implementation of --version
        /// </summary>
        private static void ShowVersion()
        {
            var version_command = new GitSharp.CLI.Nonstandard.Version();
            version_command.Init(null, null);
            version_command.Execute(new string[0]);
        }

        /// <summary>
        /// Display the incomplete commands in GitSharp. Primarily for development use.
        /// </summary>
        private static void ShowIncomplete()
        {
            CommandCatalog catalog = new CommandCatalog();
            foreach (CommandRef c in catalog.Incomplete())
            {
                Console.Write("      ");
                Console.Write(c.getName());
                for (int i = c.getName().Length + 8; i < 31; i++)
                    Console.Write(" ");
                Console.Write(c.getUsage());
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Display the complete commands in GitSharp.
        /// </summary>
        private static void ShowComplete()
        {
            CommandCatalog catalog = new CommandCatalog();
            foreach (CommandRef c in catalog.Complete())
            {
                Console.Write("      ");
                Console.Write(c.getName());
                for (int i = c.getName().Length + 8; i < 31; i++)
                    Console.Write(" ");
                Console.Write(c.getUsage());
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Display the commands that start with the specified string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns>Returns true if matches exist, otherwise false.</returns>
        private static bool ShowCommandMatches(String s)
        {
            CommandCatalog catalog = new CommandCatalog();
            List<CommandRef> matches = catalog.StartsWith(s);
            if (matches.Count > 0)
            {
                foreach (CommandRef c in matches)
                {
                    Console.WriteLine("git: '"+s+"' is not a git command. See 'git --help'.");
                    Console.WriteLine();
                    Console.WriteLine("Did you mean this?");
                    Console.Write("      ");
                    Console.Write(c.getName());
                    for (int i = c.getName().Length + 8; i < 31; i++)
                        Console.Write(" ");
                    Console.Write(c.getUsage());
                    Console.WriteLine();
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private static DirectoryInfo findGitDir()
        {
            DirectoryInfo current = new DirectoryInfo(".");
            while (current != null)
            {
                DirectoryInfo gitDir = new DirectoryInfo(current + ".git");
                if (gitDir.Exists)
                    return gitDir;
                current = current.Parent;
            }
            return null;
        }

        /// <summary>
        /// Wait for Enter key if in DEBUG mode
        /// </summary>
        /// <param name="exit_code"></param>
        static public void Exit(int exit_code)
        {
#if DEBUG
            Console.WriteLine("\n\nrunning in DEBUG mode, press any key to exit.");
            Console.In.ReadLine();
#endif
            Environment.Exit(exit_code);
        }

        /// <summary>
        /// Placeholder for the --git-dir command-line option. It is handled before it reaches this method.
        /// </summary>
        static private void NoOp()
        {
        }

    }

}
