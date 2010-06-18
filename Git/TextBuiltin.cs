/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Rolenun <rolenun@gmail.com>
 * Copyright (C) 2010, Andy <andymancooper@gmail.com>
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
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
using System.IO;
using GitSharp.Core;
using NDesk.Options;
using System.Diagnostics;

namespace GitSharp.CLI
{

	/// <summary>
	/// Abstract command which can be invoked from the command line.
	/// 
	/// Commands are configured with a single "current" repository and then the
	/// execute(String[]) method is invoked with the arguments that appear
	/// after the subcommand.
	/// 
	/// Command constructors should perform as little work as possible as they may be
	/// invoked very early during process loading, and the command may not execute
	/// even though it was constructed.
	/// </summary>
	public abstract class TextBuiltin
	{
		/// <summary>
		/// Name of the command in use
		/// </summary>
		private String commandName;

		/// <summary>
		/// Website address of the command help file
		/// </summary>
		private String commandHelp;

		/// <summary>
		/// Specifies if the command requires a repository to execute
		/// </summary>
		private bool requiresRepository = false;

		/// <summary>
		/// Specifies if the command requires an upward search for the git repository
		/// </summary>
		private bool requiresRecursive = false;

		/// <summary>
		/// RevWalk used during command line parsing, if it was required.
		/// </summary>
		protected GitSharp.Core.RevWalk.RevWalk argWalk;

		/// <summary>
		/// Contains the remaining arguments after the options listed in the command line.
		/// </summary>
		public List<String> arguments = new List<String>();

		/// <summary>
		/// Custom OptionSet to allow special option handling rules such as --option dir
		/// </summary>
		public CmdParserOptionSet options;

		/// <summary>
		/// If the command is using a pager, this represents that child process.
		/// </summary>
		private Process _pager;

		/// <summary>
		/// Used by CommandCatalog and CommandRef to set the command name during initial creation.
		/// </summary>
		/// <param name="name">The command name.</param>
		public void setCommandName(String name)
		{
			commandName = name;
		}

		/// <summary>
		/// Used by CommandRef to get the command name during initial creation.
		/// </summary>
		public string getCommandName()
		{
			return commandName;
		}

		/// <summary>
		/// Used by CommandCatalog and CommandRef to set the website address of the command help during initial creation.
		/// </summary>
		/// <param name="cmdHelp">The website address of the command help.</param>
		internal void setCommandHelp(String cmdHelp)
		{
			commandHelp = cmdHelp;
		}

		/// <summary>
		/// Used by CommandRef to get the command help website during initial creation.
		/// </summary>
		public string getCommandHelp()
		{
			return commandHelp;
		}

		/// <summary>
		/// Determine if the command requires a repository
		/// </summary>
		public bool RequiresRepository
		{
			get
			{
				return requiresRepository;
			}

			internal set
			{
				requiresRepository = value;
			}
		}

		/// <summary>
		/// Specifies if the command requires an upward search for the git repository
		/// </summary>
		public bool RequiresRecursive
		{
			get
			{
				return requiresRecursive;
			}

			set
			{
				requiresRecursive = value;
			}
		}

		/// <summary>
		/// Initializes a command for use including the repository and output support.
		/// </summary>
		/// <param name="repo">Specifies the repository to use.</param>
		/// <param name="path"></param>
		public void Init(GitSharp.Repository repo, string path)
		{
			try
			{
				//Initialize the output stream for all console-based messages.
				Git.DefaultOutputStream = new StreamWriter(Console.OpenStandardOutput());
				Console.SetOut(Git.DefaultOutputStream);
			}
			catch (IOException)
			{
				throw die("cannot create output stream");
			}

			// Initialize the repository in use.
			if (repo != null)
			{
				GitRepository = repo;
				GitDirectory = (repo as Core.Repository).Directory.FullName;
			}
			else
			{
				GitRepository = null;
				GitDirectory = path;
			}
		}

		/// <summary>
		/// Starts a child process to paginate the command output.
		/// </summary>
		protected void SetupPager()
		{
			try
			{
				_pager = Platform.Instance.GetTextPager(GitRepository.Config["core.pager"]);
				if (_pager == null)
					return;
				_pager.Start();
				Git.DefaultOutputStream = _pager.StandardInput;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Failed to create pager");
				Console.Error.WriteLine(ex.ToString());
				_pager = null;
			}
		}

		/// <summary>
		/// Parses the command line and runs the corresponding subcommand 
		/// </summary>
		/// <param name="args">Specifies the command line arguments passed after the command name.</param>
		public void Execute(String[] args)
		{
			Run(args);
			if (_pager != null)
			{
				Git.DefaultOutputStream = null;
				_pager.StandardInput.Close();
				_pager.WaitForExit();
			}
		}

		/// <summary>
		/// Parses the options for all subcommands and executes the corresponding code for each option.
		/// </summary>
		/// <param name="args">Specifies the string from the options to the end of the command line.</param>
		/// <returns>Returns the arguments remaining after the options on the command line. Often, these are directories or paths.</returns>
		public List<String> ParseOptions(string[] args)
		{
			List<String> remainingArguments = new List<string>();
			try
			{
				options.Parse(args, out remainingArguments);
			}
			catch (OptionException err)
			{
				throw die("fatal: " + err.Message);
			}
			return remainingArguments;
		}

		/// <summary>
		/// Parses the options for all subcommands and executes the corresponding code for each option. This function should only be used if supporting -- &amp;filepatterns>
		/// </summary>
		/// <param name="args">Specifies the string from the options to the end of the command line.</param>
		/// <param name="filePaths">Returns a list of files following the -- argument</param>
		/// <param name="remainingArguments">Returns the arguments remaining after the options on the command line. Often, these are directories or paths.</param>
		/// <returns>Returns the arguments remaining after the options on the command line. Often, these are directories or paths.</returns>
		public void ParseOptions(string[] args, out List<String> filePaths, out List<String> remainingArguments)
		{
			try
			{
				options.Parse(args, out filePaths, out remainingArguments);
			}
			catch (OptionException err)
			{
				throw die("fatal: " + err.Message);
			}
		}

		/// <summary>
		/// Returns the current command for lightweight referencing.
		/// </summary>
		/// <returns>Returns this command.</returns>
		public Command GetCommand()
		{
			Type type = Type.GetType(this.ToString());
			object[] attributes = type.GetCustomAttributes(true);
			foreach (object attribute in attributes)
			{
				Command com = attribute as Command;
				if (com != null)
					return com;
			}
			return null;
		}

		/// <summary>
		/// Provides an abstract layer to perform the action of this command.
		/// This method should only be invoked by the  Execute(String[] args) method.
		/// </summary>
		public abstract void Run(String[] args);

		/// <summary>
		/// Opens the default webbrowser to display the command specific help.
		/// </summary>
		public void OnlineHelp()
		{
			if (commandHelp.Length > 0)
			{
				Console.WriteLine("Launching default browser to display HTML ...");
				System.Diagnostics.Process.Start(commandHelp);
			}
			else
			{
				Console.WriteLine("There is no online help available for this command.");
			}
		}

		ObjectId Resolve(string s)
		{
			Core.Repository repo = Git.DefaultRepository;
			ObjectId r = repo.Resolve(s);
			if (r == null)
				throw die("Not a revision: " + s);
			return r;
		}

		/// <summary>
		/// Generic method used to return an exception during fatal conditions. 
		/// </summary>
		/// <param name="why">Specifies the textual explanation of why the exception was thrown.</param>
		/// <returns>Returns a runtime exception the caller is expected to throw.</returns>
		protected static Die die(String why)
		{
			return new Die(why);
		}

		public StreamWriter OutputStream
		{
			get
			{
				return Git.DefaultOutputStream;
			}

			set
			{
				Git.DefaultOutputStream = value;
			}
		}

		public GitSharp.Repository GitRepository
		{
			get
			{
				return Git.DefaultRepository;
			}
			set
			{
				Git.DefaultRepository = value;
			}
		}

		public string GitDirectory { get; set; }
	}
}
