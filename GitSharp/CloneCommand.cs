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

namespace Git
{
    public class CloneCommand
        : AbstractCommand
    {

        public CloneCommand() {
            Quiet=true;
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        /// <summary>
        /// The (possibly remote) repository to clone from.
        /// </summary>
        public string Repository { get; set; }

        /// <summary>
        /// The name of a new directory to clone into. The "humanish" part of the source repository is used if no directory is explicitly given 
        /// ("repo" for "/path/to/repo.git" and "foo" for "host.xz:foo/.git"). Cloning into an existing directory is only allowed if the directory is empty. 
        /// </summary>
        public string Directory { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When the repository to clone from is on a local machine, this flag bypasses normal "git aware" transport mechanism and clones the repository 
        /// by making a copy of HEAD and everything under objects and refs directories. The files under .git/objects/ directory are hardlinked to save 
        /// space when possible. This is now the default when the source repository is specified with /path/to/repo  syntax, so it essentially is a no-op 
        /// option. To force copying instead of hardlinking (which may be desirable if you are trying to make a back-up of your repository), but still avoid 
        /// the usual "git aware" transport mechanism, --no-hardlinks can be used. 
        /// </summary>
        public bool Local { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Optimize the cloning process from a repository on a local filesystem by copying files under .git/objects  directory. 
        /// </summary>
        public bool NoHardLinks { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When the repository to clone is on the local machine, instead of using hard links, automatically setup .git/objects/info/alternates to share the objects 
        /// with the source repository. The resulting repository starts out without any object of its own. 
        ///      
        /// NOTE: this is a possibly dangerous operation; do not use it unless you understand what it does. If you clone your repository using this option and then 
        /// delete branches (or use any other git command that makes any existing commit unreferenced) in the source repository, some objects may become 
        /// unreferenced (or dangling). These objects may be removed by normal git operations (such as git-commit) which automatically call git gc --auto. 
        /// (See git-gc(1).) If these objects are removed and were referenced by the cloned repository, then the cloned repository will become corrupt.
        /// </summary>
        public bool Shared { get; set; }           

        /// <summary>
        /// Not implemented
        /// 
        /// If the reference repository is on the local machine automatically setup .git/objects/info/alternates to obtain objects from the reference repository. Using 
        /// an already existing repository as an alternate will require fewer objects to be copied from the repository being cloned, reducing network and local storage costs.
        /// 
        /// NOTE: see NOTE to --shared option.
        /// </summary>
        public string ReferenceRepository { get; set; }

        /// <summary>
        /// Operate quietly. This flag is also passed to the `rsync' command when given.
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// Display the progressbar, even in case the standard output is not a terminal. 
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// No checkout of HEAD is performed after the clone is complete. 
        /// </summary>
        public bool NoCheckout { get; set; }

        /// <summary>
        /// Make a bare GIT repository. That is, instead of creating <directory> and placing the administrative files in <directory>/.git, make the <directory>  itself the $GIT_DIR. 
        /// This obviously implies the -n  because there is nowhere to check out the working tree. Also the branch heads at the remote are copied directly to corresponding local 
        /// branch heads, without mapping them to refs/remotes/origin/. When this option is used, neither remote-tracking branches nor the related configuration variables are created. 
        /// </summary>
        public bool Bare { get; set; }

        /// <summary>
        /// Set up a mirror of the remote repository. This implies --bare. 
        /// </summary>
        public bool Mirror { get; set; }

        /// <summary>
        /// Instead of using the remote name origin to keep track of the upstream repository, use <name>. 
        /// </summary>
        public string OriginName { get; set; }               

        /// <summary>
        /// Not implemented.
        /// 
        /// When given, and the repository to clone from is accessed via ssh, this specifies a non-default path for the command run on the other end. 
        /// </summary>
        public string UploadPack { get; set; }           

        /// <summary>
        /// Not implemented.
        /// 
        /// Specify the directory from which templates will be used; if unset the templates are taken from the installation defined default, typically /usr/share/git-core/templates. 
        /// </summary>
        public string TemplateDirectory { get; set; }

        /// <summary>
        /// Not implemented.
        /// 
        /// Create a shallow clone with a history truncated to the specified number of revisions. A shallow repository has a number of limitations (you cannot clone or fetch from it, 
        /// nor push from nor into it), but is adequate if you are only interested in the recent history of a large project with a long history, and would want to send in fixes as patches. 
        /// </summary>
        public int Depth { get; set; } 


        public override void Execute()
        {

        }
    }
}