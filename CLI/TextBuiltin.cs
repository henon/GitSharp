/*
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
using System.IO;
using System.Text;
using GitSharp;
using GitSharp.RevWalk;
using NDesk.Options;

namespace GitSharp.CLI
{
/**
 * Abstract command which can be invoked from the command line.
 * <p>
 * Commands are configured with a single "current" repository and then the
 * {@link #execute(String[])} method is invoked with the arguments that appear
 * on the command line after the command name.
 * <p>
 * Command constructors should perform as little work as possible as they may be
 * invoked very early during process loading, and the command may not execute
 * even though it was constructed.
 */

public abstract class TextBuiltin 
{
	private String commandName;
    private String commandHelp;

	//@Option(name = "--help", usage = "display this help text", aliases = { "-h" })
	//private bool help;

	/** Stream to output to, typically this is standard output. */
	protected StreamWriter streamOut;

	/** Git repository the command was invoked within. */
	protected Repository db;

	/** Directory supplied via --git-dir command line option. */
    protected String gitdir;

	/** RevWalk used during command line parsing, if it was required. */
    protected GitSharp.RevWalk.RevWalk argWalk;

    public List<String> arguments = new List<String>();
    public static CmdParserOptionSet options;

	public void setCommandName(String name) {
		commandName = name;
	}

    public string getCommandName()
    {
        return commandName;
    }

    internal void setCommandHelp(String cmdHelp)
    {
        commandHelp = cmdHelp;
    }

    public string getCommandHelp()
    {
        return commandHelp;
    }
	/** @return true if {@link #db}/{@link #getRepository()} is required. */
	public virtual bool RequiresRepository() {
		return false;
	}

	public void Init(Repository repo, String gd) {
		try {

#if ported
			String outputEncoding = repo != null ? repo.Config.getString("i18n", null, "logOutputEncoding") : null;

			if (outputEncoding != null)
            {
                streamOut = new StreamWriter(Console.OpenStandardOutput(), outputEncoding);
                Console.SetOut(streamOut);
            }
			else
            {
#endif

            streamOut = new StreamWriter(Console.OpenStandardOutput());
            Console.SetOut(streamOut);
		} catch (IOException) {
			throw die("cannot create output stream");
		}

		if (repo != null) {
			db = repo;
			gitdir = repo.Directory.FullName;
		} else {
			db = null;
			gitdir = gd;
		}
	}

	/**
	 * Parse arguments and run this command.
	 *
	 * @param args
	 *            command line arguments passed after the command name.
	 * @throws Exception
	 *             an error occurred while processing the command. The main
	 *             framework will catch the exception and print a message on
	 *             standard error.
	 */
	public void Execute(String[] args) {
		Run(args);
	}

    public List<String> ParseOptions(string[] args)
    {
        try
        {
            arguments = options.Parse(args);
        }
        catch (OptionException err)
        {
            throw die("fatal: " + err.Message);
        }

        return arguments;
    }

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

	/**
	 * Perform the actions of this command.
	 * <p>
	 * This method should only be invoked by {@link #execute(String[])}.
	 *
	 * @throws Exception
	 *             an error occurred while processing the command. The main
	 *             framework will catch the exception and print a message on
	 *             standard error.
	 */
	public abstract void Run(String[] args);

    /// <summary>
    /// Opens the default webbrowser to display the command specific help.
    /// </summary>
    /// <param name="url">Specifies the web address to navigate to.</param>
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

	/**
	 * @return the repository this command accesses.
	 */
	public Repository GetRepository() {
		return db;
	}

	ObjectId Resolve(String s) {
		ObjectId r = db.Resolve(s);
		if (r == null)
			throw die("Not a revision: " + s);
		return r;
	}

	/**
	 * @param why
	 *            textual explanation
	 * @return a runtime exception the caller is expected to throw
	 */
	protected static Die die(String why) {
		return new Die(why);
	}

	String AbbreviateRef(String dst, bool abbreviateRemote) {
        if (dst.StartsWith(Constants.R_HEADS))
			dst = dst.Substring(Constants.R_HEADS.Length);
        else if (dst.StartsWith(Constants.R_TAGS))
            dst = dst.Substring(Constants.R_TAGS.Length);
        else if (abbreviateRemote && dst.StartsWith(Constants.R_REMOTES))
            dst = dst.Substring(Constants.R_REMOTES.Length);
		return dst;
	}
}
}