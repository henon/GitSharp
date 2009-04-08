/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
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
using System.IO;
using System.Security.Cryptography;

namespace Gitty.Core
{
    [Complete]
    public abstract class ObjectLoader
    {
        private ObjectId _id;
        public ObjectId Id
        {
            get
            {
                if (_id == null)
                {
                    var sha = new SHA1CryptoServiceProvider();

                    using (var writer = new StreamWriter(new MemoryStream()))
                    {
                        writer.Write(this.ObjectType.ToString().ToLower());
                        writer.Write((byte)' ');
                        writer.Write(this.Size.ToString());
                        writer.Write((byte)0);
                        writer.Write(this.CachedBytes);
                        writer.BaseStream.Seek(0, SeekOrigin.Begin);
                        _id = ObjectId.FromRaw(sha.ComputeHash(writer.BaseStream));
                    }
                }
                return _id;
            }
            set
            {
                if (_id != null)
                    throw new InvalidOperationException("Id already set.");
                _id = value;
            }
        }

        // I'm not entirely sure how Java's protected works, but it seems
        // this method was callable from a class that didn't inherit from this one,
        // so instead I've marked this as internal for now, though I think
        // public would probably be better - NR
        internal bool HasComputedId
        {
            get
            {
                return _id != null;
            }
        }

        public virtual ObjectType ObjectType { get; protected set; }
        public virtual long Size { get; protected set; }
        public abstract byte[] Bytes { get; }
        public abstract byte[] CachedBytes { get; }
        public abstract ObjectType RawType { get; }
        public abstract long RawSize { get; }

    }
}
