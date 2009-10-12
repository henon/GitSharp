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

        public static GitSharp.Core.Repository SelectRepository()
        {
            string gitdir = "";
            string envGitDir = System.Environment.GetEnvironmentVariable("GIT_DIR");
            GitSharp.Core.Repository repo;

            //Determine which git directory to use
            if (GitDirectory != null)    //Directory specified by --git-dir 
                gitdir = GitDirectory;
            else if (envGitDir != null)  //Directory specified by $GIT_DIR
                gitdir = envGitDir;
            else                         //Local Directory
                gitdir = Path.Combine(Directory.GetCurrentDirectory(), ".git");

			if (Directory.Exists(gitdir))
				throw new ArgumentException("Directory: "+gitdir+" already exists. Cannot create a new directory.", "GitRepository");
			
            DirectoryInfo di = Directory.CreateDirectory(gitdir);
            if (!Directory.Exists(gitdir))
                throw new ArgumentException("No valid directory is available.", "GitRepository");
			
            if (!GitRequiresRoot)
                repo = new GitSharp.Core.Repository(di);
            else
                repo = GitSharp.Core.SystemReader.getInstance().getRepositoryRoot(gitdir);

            return repo;
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
        public static String GitDirectory
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
        private static String _gitDirectory = null;

        /// <summary>
        /// If a repository is required, this property determines if the root repository is needed. Default is false.
        /// </summary>
        public static Boolean GitRequiresRoot
        {
            get
            {
                return _gitRequiresRoot;
            }

            set
            {
                _gitRequiresRoot = value;
            }
        }
        private static Boolean _gitRequiresRoot = false;

        public static void Init()
        {
            Repository.Init();
        }

        public static void Init(string path)
        {
            Repository.Init(path);
        }

        public static void Init(InitCommand command)
        {
            command.Execute();
        }

    }
}