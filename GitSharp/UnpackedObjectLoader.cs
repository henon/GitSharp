/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using GitSharp.Exceptions;
using System.IO;
using System.IO.Compression;
using GitSharp.Util;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace GitSharp
{
    /**
     * Loose object loader. This class loads an object not stored in a pack.
     */
    public class UnpackedObjectLoader : ObjectLoader
    {
        private int objectType;

        private int objectSize;

        private byte[] bytes;

        /**
         * Construct an ObjectLoader to read from the file.
         *
         * @param path
         *            location of the loose object to read.
         * @param id
         *            expected identity of the object being loaded, if known.
         * @throws FileNotFoundException
         *             the loose object file does not exist.
         * @
         *             the loose object file exists, but is corrupt.
         */
        public UnpackedObjectLoader(FileInfo path, AnyObjectId id)
            :
            this(readCompressed(path), id)
        { }

        private static byte[] readCompressed(FileInfo path)
        {
            var @in = new FileStream(path.FullName, System.IO.FileMode.Open, FileAccess.Read);
            try
            {
                byte[] compressed = new byte[(int)@in.Length];
                NB.ReadFully(@in, compressed, 0, compressed.Length);
                return compressed;
            }
            finally
            {
                @in.Close();
            }
        }

        /**
         * Construct an ObjectLoader from a loose object's compressed form.
         *
         * @param compressed
         *            entire content of the loose object file.
         * @ 
         *             The compressed data supplied does not match the format for a
         *             valid loose object.
         */
        public UnpackedObjectLoader(byte[] compressed)
            : this(compressed, null)
        {
        }

        private UnpackedObjectLoader(byte[] compressed, AnyObjectId id)
        {
            // Try to determine if this is a legacy format loose object or
            // a new style loose object. The legacy format was completely
            // compressed with zlib so the first byte must be 0x78 (15-bit
            // window size, deflated) and the first 16 bit word must be
            // evenly divisible by 31. Otherwise its a new style loose
            // object.
            //
            Inflater inflater = InflaterCache.Instance.get();
            try
            {
                int fb = compressed[0] & 0xff;
                if (fb == 0x78 && (((fb << 8) | compressed[1] & 0xff) % 31) == 0)
                {
                    inflater.SetInput(compressed);
                    byte[] hdr = new byte[64];
                    int avail = 0;
                    while (!inflater.IsFinished && avail < hdr.Length)
                        try
                        {
                            avail += inflater.Inflate(hdr, avail, hdr.Length
                                    - avail);
                        }
                        catch (IOException dfe)
                        {
                            CorruptObjectException coe;
                            coe = new CorruptObjectException(id, "bad stream", dfe);
                            //inflater.end();
                            throw coe;
                        }
                    if (avail < 5)
                        throw new CorruptObjectException(id, "no header");

                    MutableInteger p = new MutableInteger();
                    objectType = Constants.decodeTypeString(id, hdr, (byte)' ', p);
                    objectSize = RawParseUtils.parseBase10(hdr, p.value, p);
                    if (objectSize < 0)
                        throw new CorruptObjectException(id, "negative size");
                    if (hdr[p.value++] != 0)
                        throw new CorruptObjectException(id, "garbage after size");
                    bytes = new byte[objectSize];
                    if (p.value < avail)
                        Array.Copy(hdr, p.value, bytes, 0, avail - p.value);
                    decompress(id, inflater, avail - p.value);
                }
                else
                {
                    int p = 0;
                    int c = compressed[p++] & 0xff;
                    int typeCode = (c >> 4) & 7;
                    int size = c & 15;
                    int shift = 4;
                    while ((c & 0x80) != 0)
                    {
                        c = compressed[p++] & 0xff;
                        size += (c & 0x7f) << shift;
                        shift += 7;
                    }

                    switch (typeCode)
                    {
                        case Constants.OBJ_COMMIT:
                        case Constants.OBJ_TREE:
                        case Constants.OBJ_BLOB:
                        case Constants.OBJ_TAG:
                            objectType = typeCode;
                            break;
                        default:
                            throw new CorruptObjectException(id, "invalid type");
                    }

                    objectSize = size;
                    bytes = new byte[objectSize];
                    inflater.SetInput(compressed, p, compressed.Length - p);
                    decompress(id, inflater, 0);
                }
            }
            finally
            {
                InflaterCache.Instance.release(inflater);
            }
        }

        private void decompress(AnyObjectId id, Inflater inf, int p)
        {
            try
            {
                while (!inf.IsFinished)
                    p += inf.Inflate(bytes, p, objectSize - p);
            }
            catch (IOException dfe)
            {
                CorruptObjectException coe;
                coe = new CorruptObjectException(id, "bad stream", dfe);
                throw coe;
            }
            if (p != objectSize)
                throw new CorruptObjectException(id, "incorrect length");
        }


        public override int getType()
        {
            return objectType;
        }


        public override long getSize()
        {
            return objectSize;
        }


        public override byte[] getCachedBytes()
        {
            return bytes;
        }

        public override int getRawType()
        {
            return objectType;
        }


        public override long getRawSize()
        {
            return objectSize;
        }
    }
}
