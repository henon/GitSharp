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
using Gitty.Core.Util;

namespace Gitty.Core
{
    [Complete]
    public class PackIndexV1 : PackIndex
    {
        private const int IDX_HDR_LEN = 256 * 4;

        private readonly long[] idxHeader;

        private byte[][] idxdata;

        public PackIndexV1(Stream fd, byte[] hdr)
        {
            var fanoutTable = new byte[IDX_HDR_LEN];
            Array.Copy(hdr, 0, fanoutTable, 0, hdr.Length);
            NB.ReadFully(fd, fanoutTable, hdr.Length, IDX_HDR_LEN - hdr.Length);

            idxHeader = new long[256]; // really unsigned 32-bit...
            for (int k = 0; k < idxHeader.Length; k++)
                idxHeader[k] = NB.DecodeUInt32(fanoutTable, k * 4);
            idxdata = new byte[idxHeader.Length][];
            for (int k = 0; k < idxHeader.Length; k++)
            {
                int n;
                if (k == 0)
                    n = (int)(idxHeader[k]);
                else
                    n = (int)(idxHeader[k] - idxHeader[k - 1]);

                if (n <= 0) continue;

                idxdata[k] = new byte[n * (AnyObjectId.Constants.ObjectIdLength + 4)];
                NB.ReadFully(fd, idxdata[k], 0, idxdata[k].Length);
            }
            ObjectCount = idxHeader[255];
        }

        public override IEnumerator<PackIndex.MutableEntry> GetEnumerator()
        {
            return new IndexV1Enumerator(this);
        }

        public override long ObjectCount { get; protected set; }

        public override long Offset64Count
        {
            get
            {
                long n64 = 0;
                foreach (MutableEntry e in this)
                {
                    if (e.Offset >= int.MaxValue)
                        n64++;
                }
                return n64;
            }
        }

        public override ObjectId GetObjectId(long nthPosition)
        {
            int levelOne = Array.BinarySearch(idxHeader, nthPosition + 1);
            long lbase;
            if (levelOne >= 0)
            {
                // If we hit the bucket exactly the item is in the bucket, or
                // any bucket before it which has the same object count.
                //
                lbase = idxHeader[levelOne];
                while (levelOne > 0 && lbase == idxHeader[levelOne - 1])
                    levelOne--;
            }
            else
            {
                // The item is in the bucket we would insert it into.
                //
                levelOne = -(levelOne + 1);
            }

            lbase = levelOne > 0 ? idxHeader[levelOne - 1] : 0;
            int p = (int)(nthPosition - lbase);
            int dataIdx = ((4 + AnyObjectId.Constants.ObjectIdLength) * p) + 4;
            return ObjectId.FromRaw(idxdata[levelOne], dataIdx);
        }

        public override long FindOffset(AnyObjectId objId)
        {
            int levelOne = objId.GetFirstByte();
            byte[] data = idxdata[levelOne];
            if (data == null)
                return -1;
            int high = data.Length / (4 + AnyObjectId.Constants.ObjectIdLength);
            int low = 0;
            do
            {
                int mid = (low + high) / 2;
                int pos = ((4 + AnyObjectId.Constants.ObjectIdLength) * mid) + 4;
                int cmp = objId.CompareTo(data, pos);
                if (cmp < 0)
                    high = mid;
                else if (cmp == 0)
                {
                    int b0 = data[pos - 4] & 0xff;
                    int b1 = data[pos - 3] & 0xff;
                    int b2 = data[pos - 2] & 0xff;
                    int b3 = data[pos - 1] & 0xff;
                    return (((long)b0) << 24) | (b1 << 16) | (b2 << 8) | (b3);
                }
                else
                    low = mid + 1;
            } while (low < high);
            return -1;
        }

        public override long FindCRC32(AnyObjectId objId)
        {
            throw new NotSupportedException();
        }

        public override bool HasCRC32Support()
        {
            return false;
        }

        private class IndexV1Enumerator : EntriesIterator
        {
            private int levelOne;

            private int levelTwo;

            private PackIndexV1 _index;

            public IndexV1Enumerator(PackIndexV1 index)
            {
                _index = index;
            }

            public override bool MoveNext()
            {

                for (; levelOne < _index.idxdata.Length; levelOne++)
                {
                    if (_index.idxdata[levelOne] == null)
                        continue;

                    if (levelTwo < _index.idxdata[levelOne].Length)
                    {
                        long offset = NB.DecodeUInt32(_index.idxdata[levelOne], levelTwo);
                        Current.Offset = offset;
                        Current.FromRaw(_index.idxdata[levelOne], levelTwo + 4);
                        levelTwo += AnyObjectId.Constants.ObjectIdLength + 4;
                        returnedNumber++;
                    }
                    else
                    {
                        levelTwo = 0;
                    }
                }

                throw new InvalidOperationException();
            }


            public override void Reset()
            {
                returnedNumber = 0;
                levelOne = 0;
                levelTwo = 0;
                Current = new MutableEntry();
            }
        }
    }
}
