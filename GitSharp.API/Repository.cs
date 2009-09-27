using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.API.Commands;

namespace GitSharp.API
{
    /// <summary>
    /// Represents a git repository
    /// </summary>
    public class Repository
    {
        internal GitSharp.Repository _repo;

        internal Repository(GitSharp.Repository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Initializes the Repository object.
        /// </summary>
        /// <param name="path">Path to the local git repository.</param>
        public Repository(string path) : this( new GitSharp.Repository(new System.IO.DirectoryInfo(path)))
        {
        }

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
        public static Repository Init(string path, bool bare) {
            var cmd = new Init() { Bare = bare, Path = path };
            cmd.Execute();
            return cmd.InitializedRepository;
        }
    }
}
