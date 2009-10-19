/*
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
using System.Reflection;

namespace Git
{
    public static class Commands
    {
        /// <summary>
        /// Get or set the output stream that all git commands are writing to. Per default this returns a StreamWriter wrapping the standard output stream.
        /// </summary>
        public static StreamWriter OutputStream
        {
            get
            {
                if (_output == null)
                {
                    _output = new StreamWriter(Console.OpenStandardOutput());
                    Console.SetOut(_output);
                }
                return _output;
            }
            set
            {
                _output = value;
            }
        }
        private static StreamWriter _output;

        /// <summary>
        /// Performs upward recursive lookup to return git directory. Usage of GIT_DIR and --git-dir apply.
        /// </summary>
        /// <param name="useRoot"></param>
        /// <returns></returns>
        public static DirectoryInfo FindGitDirectory(DirectoryInfo rootDirectory, bool recursive, bool isBare)
        {
            DirectoryInfo directory = null;
            DirectoryInfo gitDir = null;
            string envGitDir = System.Environment.GetEnvironmentVariable("GIT_DIR");
            
            //Determine which git directory to use
            if (rootDirectory != null)         	//Directory specified by --git-dir 
                directory = rootDirectory;
            else if (envGitDir != null) 		//Directory specified by $GIT_DIR
            	directory = new DirectoryInfo(envGitDir);
            else                        		//Current Directory
            {
                DirectoryInfo current = new DirectoryInfo(Directory.GetCurrentDirectory());

                if (recursive)
                {
                    //Check for non-bare repositories
                    if (!isBare)
                    {
                    	while (current != null)
                    	{
                        	gitDir = new DirectoryInfo(Path.Combine(current.FullName, ".git"));
                        	if (gitDir.Exists)
                            	return current.Parent;

                        	current = current.Parent;
                    	}
                    }
                    else
                    {
                    	//Check for bare repositories
                    	while (current != null)
                    	{
                    	    gitDir = new DirectoryInfo(current.FullName);
                     	   	if (gitDir.FullName.EndsWith(".git") && gitDir.Exists)
                     	       return current;

                        	current = current.Parent;
                    	}
                    }
                }
            }
            
            if (!directory.FullName.EndsWith(".git"))
            {
            	if (!isBare)
            		directory = new DirectoryInfo(Path.Combine(directory.FullName,".git"));
            	else
            		directory = new DirectoryInfo(directory.FullName+".git");
            }

            
            return directory;
        }

        /// <summary>
        /// Get or set the root git repository. By default, this returns the git repository the command is initialized in. Overriden by using --git-dir or $GITDIR respectively.
        /// </summary>
        public static GitSharp.Core.Repository GitRepository
        {
            get
            {
                return _gitRepository;
            }
            set
            {
                _gitRepository = value;
            }
        }
        private static GitSharp.Core.Repository _gitRepository = null;

        /// <summary>
        /// Get or set the git directory. Per default, this returns the root git directory the command is initialized in.
        /// </summary>
        public static DirectoryInfo GitDirectory
        {
            get
            {
                return _gitDirectory;
            }
            set
            {
                _gitDirectory = value;
            }
        }
        private static DirectoryInfo _gitDirectory = null;

        #region CloneCommand
        public static void Clone(string fromUrl, string toPath, bool isQuiet)
        {
            CloneCommand cmd = new CloneCommand();
            if (cmd != null)
            {
        		cmd.Path = fromUrl;
        		cmd.Directory = toPath;
        		cmd.Quiet = isQuiet;
        		cmd.Execute();
            }
        }
        
        public static void Clone(CloneCommand command)
        {
        	command.Execute();
        }
        #endregion
        
        #region InitCommand
        public static void Init()
        {
            Repository.Init();
        }

        public static void Init(bool bare)
        {
        	Repository.Init(bare);
        }
        
        public static void Init(InitCommand command)
        {
            command.Execute();
        }
        
        #endregion
        

    }
}