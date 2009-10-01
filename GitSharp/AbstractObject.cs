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
using ObjectId = GitSharp.Core.ObjectId;

namespace Git
{
    public abstract class AbstractObject
    {
        protected Repository _repo;
        protected ObjectId _id; // <--- the git object is lazy loaded. only a _id is required until properties are accessed.

        internal AbstractObject(Repository repo, ObjectId id)
        {
             _repo = repo;
             _id = id;
        }

        internal AbstractObject(Repository repo, string name)
        {
            _repo = repo;
            _id = _repo._internal_repo.Resolve(name);
        }

        /// <summary>
        /// The object's SHA1 hash.
        /// </summary>
        public string Hash
        {
            get
            {
                return _id.ToString();
            }
        }

        /// <summary>
        /// the object's abbreviated SHA1 hash
        /// </summary>
        public string ShortHash
        {
            get
            {
                return _id.Abbreviate(_repo._internal_repo).ToString();
            }
        }

        public bool IsBlob
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsCommit
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsTag
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsTree
        {
            get { throw new NotImplementedException(); }
        }

#if implemented
        public Diff Diff(AbstractObject other) { }

        public ?? Grep(?? pattern) { }

        public Byte[] Content { get; }

        public long Size { get; }
#endif

    }
}
