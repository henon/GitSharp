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
