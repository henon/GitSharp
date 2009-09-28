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
using GitSharp.Commands;
using System.Diagnostics;

namespace GitSharp
{
    /// <summary>
    /// Represents a git repository
    /// </summary>
    public class Repository
    {
        #region Constructors


        internal GitSharp.Core.Repository _repo;

        internal Repository(GitSharp.Core.Repository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Initializes the Repository object.
        /// </summary>
        /// <param name="path">Path to the local git repository.</param>
        public Repository(string path)
            : this(new GitSharp.Core.Repository(new System.IO.DirectoryInfo(path)))
        {
        }


        #endregion

        #region Public properties


        /// <summary>
        /// Returns the path to the repository database (i.e. the .git directory).
        /// </summary>
        public string Directory
        {
            get
            {
                Debug.Assert(_repo != null, "Repository not initialized correctly.");
                return _repo.Directory.FullName;
            }
        }

        /// <summary>
        /// Returns true if this repository is a bare repository. Bare repositories don't have a working directory and thus do not support some operations.
        /// </summary>
        public bool IsBare
        {
            get
            {
                Debug.Assert(_repo != null, "Repository not initialized correctly.");
                return _repo.Config.getBoolean("core", "bare", false);
            }
        }

        /// <summary>
        /// Returns the path to the working directory (i.e. the parent of the .git directory of a non-bare repo). Returns null if it is a bare repository.
        /// </summary>
        public string WorkingDirectory
        {
            get
            {
                Debug.Assert(_repo != null, "Repository not initialized correctly.");
                if (IsBare)
                    return null;
                return _repo.WorkingDirectory.FullName;
            }
        }

        #endregion

        /// <summary>
        /// Checks if the directory given by the path is a valid git repository.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Returns true if the given path is a valid git repository, false otherwise.</returns>
        public static bool IsValid(string path)
        {
            var git = Path.Combine(path, ".git");
            if (!DirExists(path))
                return false;
            if (!DirExists(Path.Combine(path, "branches")) && !DirExists(Path.Combine(git, "branches")))
                return false;
            if (!DirExists(Path.Combine(path, "objects")) && !DirExists(Path.Combine(git, "objects")))
                return false;
            if (!DirExists(Path.Combine(path, "refs")) && !DirExists(Path.Combine(git, "refs")))
                return false;
            if (!DirExists(Path.Combine(path, "remote")) && !DirExists(Path.Combine(git, "remote")))
                return false;
            if (!FileExists(Path.Combine(path, "config")) && !FileExists(Path.Combine(git, "config")))
                return false;
            if (!FileExists(Path.Combine(path, "HEAD")) && !FileExists(Path.Combine(git, "HEAD")))
                return false;
            try
            {
                // let's see if it loads without throwing an exception
                new Repository(path);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private static bool DirExists(string path)
        {
            return new DirectoryInfo(path).Exists;
        }

        private static bool FileExists(string path)
        {
            return new FileInfo(path).Exists;
        }


        #region Repository initialization (git init)


        /// <summary>
        /// Initializes a non-bare git repository in the current directory if GIT_DIR is not set.
        /// </summary>
        /// <returns>The initialized repository</returns>
        public static Repository Init()
        {
            return Init(null, false);
        }

        /// <summary>
        /// Initializes a non-bare git repository in the location path points to.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>The initialized repository</returns>
        public static Repository Init(string path)
        {
            return Init(path, false);
        }

        /// <summary>
        /// Initializes a directory in the current location.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bare"></param>
        /// <returns></returns>
        public static Repository Init(string path, bool bare)
        {
            var cmd = new Init() { Bare = bare, Path = path };
            cmd.Execute();
            return cmd.InitializedRepository;
        }


        #endregion
    }
}