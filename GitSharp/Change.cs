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
    /// <summary>
    /// Represents a change between two commits
    /// </summary>
    public class Change
    {

        /// <summary>
        /// The commit that the other commit is compared against
        /// </summary>
        public Commit ReferenceCommit
        {
            get;
            internal set;
        }

        /// <summary>
        /// The compared commit
        /// </summary>
        public Commit ComparedCommit
        {
            get;
            internal set;
        }

        /// <summary>
        /// The kind of change i.e. Added, Deleted, Modified, etc.
        /// </summary>
        public string ChangeName { get; internal set; }

        /// <summary>
        /// The changed object in the ReferenceCommit. It may be null in some cases i.e. for "Added"-Change
        /// </summary>
        public AbstractObject ReferenceObject { get; internal set; }

        /// <summary>
        /// The changed object in the ComparedCommit. It may be null in some cases i.e. for "Removed"-Change
        /// </summary>
        public AbstractObject ComparedObject { get; internal set; }

        /// <summary>
        /// Always returns an object, no matter what kind of change. Except for "Removed"-Change it always returns the ComparedCommit's version of the object.
        /// </summary>
        public AbstractObject ChangedObject
        {
            get
            {
                if (ComparedObject != null)
                    return ComparedObject;
                else
                    return ReferenceObject;
            }
        }

        /// <summary>
        /// The filepath of the ChangedObject
        /// </summary>
        public string Path { get; internal set; }

        /// <summary>
        /// The filename of the ChangedObject
        /// </summary>
        public string Name { get; internal set; }
    }

}
