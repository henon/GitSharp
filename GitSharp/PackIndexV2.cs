/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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
using GitSharp.Util;
using GitSharp.Exceptions;

namespace GitSharp
{
    /** Support for the pack index v2 format. */
    public class PackIndexV2 : PackIndex
    {

        private const long IS_O64 = 1L << 31;

        private const int FANOUT = 256;

        private static readonly int[] NO_INTS = { };

        private static readonly byte[] NO_BYTES = { };

        private readonly long[] fanoutTable;

        /** 256 arrays of contiguous object names. */
        private int[][] names;

        /** 256 arrays of the 32 bit offset data, matching {@link #names}. */
        private byte[][] offset32;

        /** 256 arrays of the CRC-32 of objects, matching {@link #names}. */
        private byte[][] crc32;

        /** 64 bit offset table. */
        private byte[] offset64;

        public PackIndexV2(Stream fd)
        {
            byte[] fanoutRaw = new byte[4 * FANOUT];
            NB.ReadFully(fd, fanoutRaw, 0, fanoutRaw.Length);
            fanoutTable = new long[FANOUT];
            for (int k = 0; k < FANOUT; k++)
                fanoutTable[k] = NB.DecodeUInt32(fanoutRaw, k * 4);
            ObjectCount = fanoutTable[FANOUT - 1];

            names = new int[FANOUT][];
            offset32 = new byte[FANOUT][];
            crc32 = new byte[FANOUT][];

            // object name table. The size we can permit per fan-out bucket
            // is limited to Java's 2 GB per byte array limitation. That is
            // no more than 107,374,182 objects per fan-out.
            //
            for (int k = 0; k < FANOUT; k++)
            {
                long bucketCnt;
                if (k == 0)
                    bucketCnt = fanoutTable[k];
                else
                    bucketCnt = fanoutTable[k] - fanoutTable[k - 1];

                if (bucketCnt == 0)
                {
                    names[k] = NO_INTS;
                    offset32[k] = NO_BYTES;
                    crc32[k] = NO_BYTES;
                    continue;
                }

                long nameLen = bucketCnt * AnyObjectId.ObjectIdLength;
                if (nameLen > int.MaxValue)
                    throw new IOException("Index file is too large");

                int intNameLen = (int)nameLen;
                byte[] raw = new byte[intNameLen];
                int[] bin = new int[intNameLen >> 2];
                NB.ReadFully(fd, raw, 0, raw.Length);
                for (int i = 0; i < bin.Length; i++)
                    bin[i] = NB.DecodeInt32(raw, i << 2);

                names[k] = bin;
                offset32[k] = new byte[(int)(bucketCnt * 4)];
                crc32[k] = new byte[(int)(bucketCnt * 4)];
            }

            // CRC32 table.
            for (int k = 0; k < FANOUT; k++)
                NB.ReadFully(fd, crc32[k], 0, crc32[k].Length);

            // 32 bit offset table. Any entries with the most significant bit
            // set require a 64 bit offset entry in another table.
            //
            int o64cnt = 0;
            for (int k = 0; k < FANOUT; k++)
            {
                byte[] ofs = offset32[k];
                NB.ReadFully(fd, ofs, 0, ofs.Length);
                for (int p = 0; p < ofs.Length; p += 4)
                    if (ofs[p] < 0)
                        o64cnt++;
            }

            // 64 bit offset table. Most objects should not require an entry.
            //
            if (o64cnt > 0)
            {
                offset64 = new byte[o64cnt * 8];
                NB.ReadFully(fd, offset64, 0, offset64.Length);
            }
            else
            {
                offset64 = NO_BYTES;
            }
            _packChecksum = new byte[20];
            NB.ReadFully(fd, _packChecksum, 0, _packChecksum.Length);
        }

        public override IEnumerator<PackIndex.MutableEntry> GetEnumerator()
        {
            return new EntriesEnumeratorV2(this);
        }

        public override long ObjectCount{get;internal set;}

        public override long Offset64Count
        {
            get
            {
                return offset64.Length / 8;
            }
        }

        public override ObjectId GetObjectId(long nthPosition)
        {
            int levelOne = Array.BinarySearch(fanoutTable, nthPosition + 1);
            long lbase;
            if (levelOne >= 0)
            {
                // If we hit the bucket exactly the item is in the bucket, or
                // any bucket before it which has the same object count.
                //
                lbase = fanoutTable[levelOne];
                while (levelOne > 0 && lbase == fanoutTable[levelOne - 1])
                    levelOne--;
            }
            else
            {
                // The item is in the bucket we would insert it into.
                //
                levelOne = -(levelOne + 1);
            }

            lbase = levelOne > 0 ? fanoutTable[levelOne - 1] : 0;
            int p = (int)(nthPosition - lbase);
            int p4 = p << 2;
            return ObjectId.FromRaw(names[levelOne], p4 + p); // p * 5
        }

        public override long FindOffset(AnyObjectId objId)
        {
            int levelOne = objId.GetFirstByte();
            int levelTwo = BinarySearchLevelTwo(objId, levelOne);
            if (levelTwo == -1)
                return -1;
            long p = NB.DecodeUInt32(offset32[levelOne], levelTwo << 2);
            if ((p & IS_O64) != 0)
                return NB.DecodeUInt64(offset64, (8 * (int)(p & ~IS_O64)));
            return p;
        }

        public override long FindCRC32(AnyObjectId objId)
        {
            int levelOne = objId.GetFirstByte();
            int levelTwo = BinarySearchLevelTwo(objId, levelOne);
            if (levelTwo == -1)
                throw new MissingObjectException(objId.Copy(), ObjectType.Unknown);
            return NB.DecodeUInt32(crc32[levelOne], levelTwo << 2);
        }

        public override bool HasCRC32Support
        {
            get
            {
                return true;
            }
        }

        private int BinarySearchLevelTwo(AnyObjectId objId, int levelOne)
        {
            int[] data = names[levelOne];
            int high = (int)((uint)(offset32[levelOne].Length) >> 2);
            if (high == 0)
                return -1;
            int low = 0;
            do
            {
                int mid = (int)((uint)(low + high) >> 1);
                int mid4 = mid << 2;
                int cmp;

                cmp = objId.CompareTo(data, mid4 + mid); // mid * 5
                if (cmp < 0)
                    high = mid;
                else if (cmp == 0)
                {
                    return mid;
                }
                else
                    low = mid + 1;
            } while (low < high);
            return -1;
        }
        private class EntriesEnumeratorV2 : EntriesIterator
        {

            private int levelOne;

            private int levelTwo;

            private readonly PackIndexV2 _index;

            public EntriesEnumeratorV2(PackIndexV2 index)
            {
                _index = index;
            }

            public override bool MoveNext()
            {
                for (; levelOne < _index.names.Length; levelOne++)
                {
                    if (levelTwo < _index.names[levelOne].Length)
                    {
                        Current.FromRaw(_index.names[levelOne], levelTwo);
                        int arrayIdx = levelTwo / (AnyObjectId.ObjectIdLength / 4) * 4;
                        long offset = NB.DecodeUInt32(_index.offset32[levelOne], arrayIdx);
                        if ((offset & IS_O64) != 0)
                        {
                            arrayIdx = (8 * (int)(offset & ~IS_O64));
                            offset = NB.DecodeUInt64(_index.offset64, arrayIdx);
                        }
                        Current.Offset = offset;

                        levelTwo += AnyObjectId.ObjectIdLength / 4;
                        returnedNumber++;
                        return true;
                    }
                   levelTwo = 0;
                }
                return false;                
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
