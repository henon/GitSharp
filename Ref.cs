/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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

namespace Gitty.Core
{
    [Complete]
    public class Ref
    {
        public sealed class Storage
        {
            public static readonly Storage New = new Storage(true, false);
            public static readonly Storage Loose = new Storage(true, false);
            public static readonly Storage Packed = new Storage(false, true);
            public static readonly Storage LoosePacked = new Storage(true, true);
            public static readonly Storage Network = new Storage(false, false);

            public bool IsLoose { get; private set; }
            public bool IsPacked { get; private set; }

            private Storage(bool loose, bool packed)
            {
                this.IsLoose = loose;
                this.IsPacked = packed;
            }

            static Storage()
            {
                Loose = new Storage(true, false);
            }
        }

        public Ref(Storage storage, string origName, string refName, ObjectId id)
            : this(storage, origName, refName, id, null, false)
        {
        }

        public Ref(Storage storage, string refName, ObjectId id)
            : this(storage, refName, refName, id, null, false)
        {
        }

        public Ref(Storage storage, string refName, ObjectId id, ObjectId peeledObjectId, bool peeled)
            : this(storage, refName, refName, id, peeledObjectId, peeled)
        {
        }

        public Ref(Storage storage, string origName, string refName, ObjectId id, ObjectId peeledObjectId, bool peeled)
        {
            this.StorageFormat = storage;
            this.OriginalName = origName;
            this.Name = refName;
            this.ObjectId = id;
            this.PeeledObjectId = peeledObjectId;
            this.Peeled = peeled;
            this.Detached = this.OriginalName != this.Name;
        }

        public string Name { get; private set; }
        public string OriginalName { get; private set; }
        public Storage StorageFormat { get; private set; }
        public ObjectId ObjectId { get; private set; }
        public ObjectId PeeledObjectId { get; private set; }
        public bool Peeled { get; private set; }

        public bool Detached { get; private set; }

        public override string ToString()
        {
            return "Ref[" + (OriginalName == Name ? "" : "(" + OriginalName + ")") + Name + "=" + this.ObjectId.ToString() + "]";
        }
    }
}
