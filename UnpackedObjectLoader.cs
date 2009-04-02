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
using Gitty.Core.Exceptions;
using System.IO;
using System.IO.Compression;
using Gitty.Core.Util;

namespace Gitty.Core
{
    public class UnpackedObjectLoader : ObjectLoader
    {
        

        public UnpackedObjectLoader(Repository repo, ObjectId objectId)
            : this(ReadCompressed(repo, objectId), objectId)
        {
        }

        public UnpackedObjectLoader(byte[] compressed)
            : this(compressed, null)
        {
        }

        public UnpackedObjectLoader(byte[] compressed, ObjectId id)
        {
            Id = id;
            // Try to determine if this is a legacy format loose object or
            // a new style loose object. The legacy format was completely
            // compressed with zlib so the first byte must be 0x78 (15-bit
            // window size, deflated) and the first 16 bit word must be
            // evenly divisible by 31. Otherwise its a new style loose
            // object.
            //
            var stream = new MemoryStream(compressed);
            var deflate = new DeflateStream(stream, CompressionMode.Decompress);
            using(deflate)
            {
                int fb = stream.ReadByte() & 0xff;

                if (fb == 0x78 && (((fb << 8) | stream.ReadByte() & 0xff) % 31) == 0)
                {
                    var header = new byte[64];
                    int avail = 0;
                    int bytesIn = -1;
                    while (bytesIn != 0 && avail < header.Length)
                    {
                        try
                        {
                            bytesIn = deflate.Read(header, avail, header.Length - avail);
                            avail += bytesIn;
                        }
                        catch (Exception inn)
                        {
                            throw new CorruptObjectException(id, "bad stream", inn);
                        }
                    }

                    if (avail < 5)
                        throw new CorruptObjectException(id, "no header");

                    int p = 0;
                    this.ObjectType = Codec.DecodeTypeString(id, header, (byte)' ', ref p);
                    this.Size = RawParseUtils.ParseBase10(header, ref p);

                    if (this.Size < 0)
                        throw new CorruptObjectException(id, "negative size");

                    if (header[p++] != 0)
                        throw new CorruptObjectException(id, "garbage after size");

                    _bytes = new byte[this.Size];

                    if (p < avail)
                        Array.Copy(header, p, _bytes, 0, avail - p);

                    Decompress(id, deflate, avail - p);
                }
                else
                {
                    throw new NotSupportedException("Compression type not supported");
                    //int p = 0;
                    //int c = compressed[p++] & 0xff;
                    //ObjectType typeCode = (ObjectType)((c >> 4) & 7);
                    //int size = c & 15;
                    //int shift = 4;
                    //while ((c & 0x80) != 0)
                    //{
                    //    c = compressed[p++] & 0xff;
                    //    size += (c & 0x7f) << shift;
                    //    shift += 7;
                    //}

                    //switch (typeCode)
                    //{
                    //    case ObjectType.Commit:
                    //    case ObjectType.Tree:
                    //    case ObjectType.Blob:
                    //    case ObjectType.Tag:
                    //        objectType = typeCode;
                    //        break;
                    //    default:
                    //        throw new CorruptObjectException(id, "invalid type");
                    //}

                    //this.Size = size;
                    //bytes = new byte[this.Size];
                    //inflater.SetInput(compressed, p, compressed.Length - p);
                    //Decompress(id, inflater, 0);
                }
            }            
        }

        private void Decompress(ObjectId id, DeflateStream inf, int p)
        {
            try
            {
                var bytesRead = -1;

                while (bytesRead != 0)
                {
                    bytesRead = inf.Read(_bytes, p, (int)this.Size - p);
                    p += bytesRead;
                }
            }
            catch (Exception e)
            {
                throw new CorruptObjectException(id, "bad stream", e);
            }
            if (p != this.Size)
                throw new CorruptObjectException(id, "Invalid Length");
        }

        static byte[] ReadCompressed(Repository db, ObjectId id)
        {
            byte[] compressed;

            using (FileStream objStream = db.ToFile(id).OpenRead())
            {
                compressed = new byte[objStream.Length];
                objStream.Read(compressed, 0, (int)objStream.Length);
            }
            return compressed;
        }

        private byte[] _bytes;
        public override byte[] Bytes
        {
            get { return _bytes; }
        }

        public override byte[] CachedBytes
        {
            get { return _bytes; }
        }

        public override ObjectType RawType
        {
            get { return this.ObjectType; }
        }

        public override long RawSize
        {
            get { return this.Size; }
        }
    }
}
