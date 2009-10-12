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
using GitSharp.Core;

namespace Git
{
    /// <summary>
    /// Abstract base class of all git commands. It provides basic infrastructure
    /// </summary>
    public abstract class AbstractCommand : IGitCommand
    {
        /// <summary>
        /// Abbreviates a ref-name, used in internal output
        /// </summary>
        /// <param name="dst">long ref</param>
        /// <param name="abbreviateRemote">abbreviate as remote</param>
        /// <returns></returns>
        protected string AbbreviateRef(String dst, bool abbreviateRemote)
        {
            if (dst.StartsWith(Constants.R_HEADS))
                dst = dst.Substring(Constants.R_HEADS.Length);
            else if (dst.StartsWith(Constants.R_TAGS))
                dst = dst.Substring(Constants.R_TAGS.Length);
            else if (abbreviateRemote && dst.StartsWith(Constants.R_REMOTES))
                dst = dst.Substring(Constants.R_REMOTES.Length);
            return dst;
        }

        /// <summary>
        /// Returns the value of the process' environment variable GIT_DIR
        /// </summary>
        protected string GIT_DIR
        {
            get
            {
                return System.Environment.GetEnvironmentVariable("GIT_DIR");
            }
        }

        /// <summary>
        /// This command's output stream. If not explicitly set, the command writes to Git.OutputStream out.
        /// </summary>
        public StreamWriter OutputStream
        {
            get
            {
                if (_output == null)
                    return Git.Commands.OutputStream;
                return _output;
            }
            set
            {
                _output = value;
            }
        }
        StreamWriter _output = null;

        /// <summary>
        /// The root git repository. If not explicitly set, the command uses Git.GitRepository.
        /// </summary>
        public GitSharp.Core.Repository GitRepository
        {
            get
            {
                if (_gitRepository == null)
                    return Git.Commands.GitRepository;
                return _gitRepository;
            }
            set
            {
                _gitRepository = value;
            }
        }
        GitSharp.Core.Repository _gitRepository = null;

        /// <summary>
        /// The root git directory. If not explicitly set, the command uses Git.GitDirectory. Set using --git-dir.
        /// </summary>
        public String GitDirectory
        {
            get
            {
                if (_gitDirectory == null)
                    return Git.Commands.GitDirectory;
                return _gitDirectory;
            }
            set
            {
                _gitDirectory = value;
            }
        }
        String _gitDirectory = null;

        public Boolean GitRequiresRoot
        {
            get
            {
                if (_gitRequiresRoot == false)
                    return Git.Commands.GitRequiresRoot;
                return _gitRequiresRoot;
            }
            set
            {
                _gitRequiresRoot = value;
            }
        }
        Boolean _gitRequiresRoot = false;

        /// <summary>
        /// Execute the git command.
        /// </summary>
        public abstract void Execute();

    }
}