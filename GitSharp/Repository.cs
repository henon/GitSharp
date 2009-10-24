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
using System.Diagnostics;

using CoreRepository = GitSharp.Core.Repository;
using System.Text.RegularExpressions;

namespace Git
{
    /// <summary>
    /// Represents a git repository
    /// </summary>
    public class Repository
    {
        #region Constructors


        internal CoreRepository _internal_repo;

        internal Repository(CoreRepository repo)
        {
            _internal_repo = repo;
        }

        /// <summary>
        /// Initializes the Repository object.
        /// </summary>
        /// <param name="path">Path to the local git repository.</param>
        public Repository(string path)
            : this(GitSharp.Core.Repository.Open(path))
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
                Debug.Assert(_internal_repo != null, "Repository not initialized correctly.");
                return _internal_repo.Directory.FullName;
            }
        }

        /// <summary>
        /// Gets or sets Head which is a symbolic reference to the active branch. Note that setting head 
        /// does not automatically check out that branch into the repositories working directory. 
        /// </summary>
        public Branch Head
        {
            get
            {
                Debug.Assert(_internal_repo != null, "Repository not initialized correctly.");
                return new Branch(this, "HEAD");
            }
            set
            {
                // Todo: what should we do with null?
                if (Head.Name != value.Name)
                {
                    if (Branches.ContainsKey(value.Name))
                    {
                        var updateRef = _internal_repo.UpdateRef("HEAD");
                        updateRef.NewObjectId = value.Target._id;
                        updateRef.IsForceUpdate = true;
                        updateRef.Update();
                        _internal_repo.WriteSymref(GitSharp.Core.Constants.HEAD, value.Name);
                    }
                    else
                        throw new ArgumentException("Trying to set HEAD to non existent branch: " + value.Name);
                }

            }
        }

        public Index Index
        {
            get
            {
                return new Index(this); // <--- this is just a wrapper around the internal repo's GitIndex instance so need not cache it here
            }
        }

        /// <summary>
        /// Returns true if this repository is a bare repository. Bare repositories don't have a working directory and thus do not support some operations.
        /// </summary>
        public bool IsBare
        {
            get
            {
                Debug.Assert(_internal_repo != null, "Repository not initialized correctly.");
                return _internal_repo.Config.getBoolean("core", "bare", false);
            }
        }

        /// <summary>
        /// Returns the path to the working directory (i.e. the parent of the .git directory of a non-bare repo). Returns null if it is a bare repository.
        /// </summary>
        public string WorkingDirectory
        {
            get
            {
                Debug.Assert(_internal_repo != null, "Repository not initialized correctly.");
                if (IsBare)
                    return null;
                return _internal_repo.WorkingDirectory.FullName;
            }
        }

        #endregion

        /// <summary>
        /// Checks if the directory given by the path is a valid non-bare git repository. The given path may either point to 
        /// the repository or the repository's inner .git directory.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Returns true if the given path is a valid git repository, false otherwise.</returns>
        public static bool IsValid(string path)
        {
            return IsValid(path, false);
        }

        /// <summary>
        /// Checks if the directory given by the path is a valid git repository.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bare"></param>
        /// <returns>Returns true if the given path is a valid git repository, false otherwise.</returns>
        public static bool IsValid(string gitdir, bool bare)
        {
            if (!bare)
            {
                if (!bare && !Regex.IsMatch(gitdir, "\\.git[/\\\\]?$"))
                    gitdir = Path.Combine(gitdir, ".git");
                if (!DirExists(gitdir))
                    return false;
                if (!DirExists(Path.Combine(gitdir, "objects")))
                    return false;
                if (!DirExists(Path.Combine(gitdir, "objects/info")))
                    return false;
                if (!DirExists(Path.Combine(gitdir, "objects/pack")))
                    return false;
                if (!DirExists(Path.Combine(gitdir, "refs")))
                    return false;
                if (!DirExists(Path.Combine(gitdir, "refs/heads")))
                    return false;
                if (!DirExists(Path.Combine(gitdir, "refs/tags")))
                    return false;
                if (!FileExists(Path.Combine(gitdir, "config")))
                    return false;
                if (!FileExists(Path.Combine(gitdir, "HEAD")))
                    return false;
                //Set the root directory (the parent of the .git directory)
                //  for load testing
                //gitdir = gitdir.Substring(0,gitdir.Length-4);
            }
            else
            {
                //In progress
                throw new NotImplementedException();
                //if (!DirExists(Path.Combine(path, "description")) && !DirExists(Path.Combine(git, "description")))
                //    return false;
                //if (!DirExists(Path.Combine(path, "hooks")) && !DirExists(Path.Combine(git, "hooks")))
                //    return false;
                //if (!DirExists(Path.Combine(path, "info")) && !DirExists(Path.Combine(git, "info")))
                //    return false;
                //if (!DirExists(Path.Combine(path, "packed_refs")) && !DirExists(Path.Combine(git, "packed_refs")))
                //    return false;
            }

            try
            {
                // let's see if it loads without throwing an exception
                new Repository(gitdir);
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

        public IDictionary<string, Ref> Refs
        {
            get
            {
                var internal_refs = _internal_repo.getAllRefs();
                var dict = new Dictionary<string, Ref>(internal_refs.Count);
                foreach (var pair in internal_refs)
                    dict[pair.Key] = new Ref(this, pair.Value);
                return dict;
            }
        }

        public IDictionary<string, Tag> Tags
        {
            get
            {
                var internal_tags = _internal_repo.getTags();
                var dict = new Dictionary<string, Tag>(internal_tags.Count);
                foreach (var pair in internal_tags)
                    dict[pair.Key] = new Tag(this, pair.Value);
                return dict;
            }
        }

        public IDictionary<string, Branch> Branches
        {
            get
            {
                var internal_refs = _internal_repo._refDb.GetBranches();
                var dict = new Dictionary<string, Branch>(internal_refs.Count);
                foreach (var pair in internal_refs)
                    dict[pair.Key] = new Branch(this, pair.Value);
                return dict;
            }
        }

        public Branch CurrentBranch
        {
            get
            {
                return new Branch(this, _internal_repo.getBranch());
            }
        }

        public IDictionary<string, Branch> RemoteBranches
        {
            get
            {
                var internal_refs = _internal_repo._refDb.GetRemotes();
                var dict = new Dictionary<string, Branch>(internal_refs.Count);
                foreach (var pair in internal_refs)
                {
                    var branch = new Branch(this, pair.Value);
                    branch.IsRemote = true;
                    dict[pair.Key] = branch;
                }
                return dict;
            }
        }

        public Config Config
        {
            get
            {
                return new Config(this);
            }
        }

        public override string ToString()
        {
            return "Repository[" + Directory + "]";
        }

        #region Repository initialization (git init)


        /// <summary>
        /// Initializes a non-bare repository. Use GitDirectory to specify location.
        /// </summary>
        /// <returns>The initialized repository</returns>
        public static Repository Init(string path)
        {
            return Init(path, false);
        }

        /// <summary>
        /// Initializes a repository. Use GitDirectory to specify the location. Default is the current directory.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bare"></param>
        /// <returns></returns>
        public static Repository Init(string path, bool bare)
        {
            var cmd = new InitCommand() { Path=path, Bare = bare };
            return Init(cmd);
        }

        /// <summary>
        /// Initializes a repository in the current location using the provided git command's options.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bare"></param>
        /// <returns></returns>
        public static Repository Init(InitCommand cmd)
        {
            cmd.Execute();
            return cmd.Repository;
        }

        #endregion

    }
}