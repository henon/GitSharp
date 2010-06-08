/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Rolenun
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
using GitSharp.Commands;
using NDesk.Options;


namespace GitSharp.CLI
{

    [Command(complete=false, common=true, requiresRepository=true, usage = "Get and set repository or global options")]
    public class Config : TextBuiltin
    {
        private ConfigCommand cmd = new ConfigCommand();

        private static Boolean isHelp = false;

        public override void Run(string[] args)
        {
			
            options = new CmdParserOptionSet()
            {
                { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
/*                { "replace-all", "Replaces all lines matching the key (and optionally the value_regex).", v => cmd.ReplaceAll = true},
                { "add", "Adds a new line to the option without altering any existing values.", v => cmd.Add = false},
                { "get", "Get the value for a given key", v => cmd.Get = true},
                { "get-all", "Like get, but can handle multiple values for the key.", v=> cmd.GetAll = true},
                { "get-regexp", "Like --get-all, but interprets the name as a regular expression", v => cmd.GetRegExp = true},
                { "global", "Use the per-user config file instead of the default.", v => cmd.Global = true},
                { "system", "Use the system-wide config file instead of the default.", v => cmd.System = true},
                { "f|file", "Use the given config file instead of the one specified by GIT_CONFIG", (string v) => cmd.File = v},
                { "remove-section", "Remove the given section from the configuration file", v => cmd.RemoveSection = true},
                { "rename-section", "Rename the given section to a new name", v => cmd.RenameSection = true},
                { "unset", "Remove the line matching the key from config file", v => cmd.UnSet = true},
                { "unset-all", "Remove all lines matching the key from config file", v => cmd.UnSetAll = true},*/
                { "l|list", "List all variables set in config file", v => cmd.List = true},
/*                { "bool", "Ensure that the output is true or false", v => cmd.Bool = true },
                { "int", "Ensure that the output is a simple decimal number", v => cmd.Int = true },
                { "bool-or-int", "Ensure that the output matches the format of either --bool or --int, as described above", v => cmd.BoolOrInt = true },
                { "z|null", "Always end values with null character instead of newlines", v => cmd.Null = true },
                { "get-colorbool", "Find the color setting for {name} and output as true or false", v => cmd.GetColorBool = true },
                { "get-color", "Find the color configured for {name}", v => cmd.GetColor = true },
                { "e|edit", "Opens an editor to modify the specified config file as --global, --system, or repository (default)", v => cmd.Edit = true },*/
            };

            try
            {
                List<String> arguments = ParseOptions(args);
                if (arguments.Count > 0)
                {
                	cmd.Arg1 = arguments[0];
                	
                	if (arguments.Count > 1)
						cmd.Arg2 = arguments[1];
                	else
                		cmd.Arg2 = "";
	               	
                	if (arguments.Count > 2)
	                	cmd.Arg3 = arguments[2];
                	else
                		cmd.Arg3 = "";

                    cmd.Execute();
                }
                else if (cmd.List)
                {
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
                cmd.OutputStream.WriteLine("usage: git config [file-option] [options]");
                cmd.OutputStream.WriteLine();
                options.WriteOptionDescriptions(Console.Out);
                cmd.OutputStream.WriteLine();
            }
        }
    }
}