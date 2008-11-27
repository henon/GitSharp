/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using System.IO;

namespace Gitty.Lib
{
    [Complete]
    public abstract class PackedObjectLoader : ObjectLoader
    {
        protected PackFile pack;

        internal WindowCursor curs;

        internal long objectOffset;


        public PackedObjectLoader(WindowCursor c, PackFile pr, long dataOffset, long objectOffset)
        {
            this.curs = c;
            this.pack = pr;
            this.DataOffset = dataOffset;
            this.objectOffset = objectOffset;
        }

        /**
         * @return offset of object data within pack file
         */
        public long DataOffset { get; protected set; }

        public override byte[] Bytes
        {
            get
            {
                byte[] data = this.CachedBytes;
                byte[] copy = new byte[data.Length];
                Array.Copy(data, 0, copy, 0, data.Length);
                return data;
            }
        }
        /**
         * Copy raw object representation from storage to provided output stream.
         * <p>
         * Copied data doesn't include object header. User must provide temporary
         * buffer used during copying by underlying I/O layer.
         * </p>
         *
         * @param out
         *            output stream when data is copied. No buffering is guaranteed.
         * @param buf
         *            temporary buffer used during copying. Recommended size is at
         *            least few kB.
         * @throws IOException
         *             when the object cannot be read.
         */
        public void CopyRawData(Stream o, byte[] buf)
        {
            pack.CopyRawData(this, o, buf);
        }

        /**
         * @return true if this loader is capable of fast raw-data copying basing on
         *         compressed data checksum; false if raw-data copying needs
         *         uncompressing and compressing data
         */
        public bool SupportsFastCopyRawData()
        {
            return pack.SupportsFastCopyRawData();
        }

        /**
         * @return id of delta base object for this object representation. null if
         *         object is not stored as delta.
         * @throws IOException
         *             when delta base cannot read.
         */
        public abstract ObjectId GetDeltaBase();

        protected ObjectType _objectType;
        public override ObjectType ObjectType
        {
            get
            {
                return _objectType;
            }
        }

        protected long _objectSize;
        public override long Size
        {
            get
            {
                return _objectSize;
            }
        }

    }
}
