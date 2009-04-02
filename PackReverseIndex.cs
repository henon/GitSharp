/*
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
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

namespace Gitty.Core
{

    /**
 * <p>
 * Reverse index for forward pack index. Provides operations based on offset
 * instead of object id. Such offset-based reverse lookups are performed in
 * O(log n) time.
 * </p>
 *
 * @see PackIndex
 * @see PackFile
 */

    public class PackReverseIndex
    {
        /** Index we were created from, and that has our ObjectId data. */
        private PackIndex index;

        /**
         * (offset31, truly) Offsets accommodating in 31 bits.
         */
        private int[] offsets32;

        /**
         * Offsets not accommodating in 31 bits.
         */
        private long[] offsets64;

        /** Position of the corresponding {@link #offsets32} in {@link #index}. */
        private int[] nth32;

        /** Position of the corresponding {@link #offsets64} in {@link #index}. */
        private int[] nth64;

        /**
         * Create reverse index from straight/forward pack index, by indexing all
         * its entries.
         *
         * @param packIndex
         *            forward index - entries to (reverse) index.
         */
        public PackReverseIndex(PackIndex packIndex)
        {
            index = packIndex;

            long cnt = index.ObjectCount;
            long n64 = index.Offset64Count;
            long n32 = cnt - n64;
            if (n32 > int.MaxValue || n64 > int.MaxValue || cnt > 0xffffffffL)
                throw new ArgumentException("Huge indexes are not supported by gitty, yet");

            offsets32 = new int[(int)n32];
            offsets64 = new long[(int)n64];
            nth32 = new int[offsets32.Length];
            nth64 = new int[offsets64.Length];

            int i32 = 0;
            int i64 = 0;
            foreach (PackIndex.MutableEntry me in index)
            {
                long o = me.Offset;
                if (o < int.MaxValue)
                    offsets32[i32++] = (int)o;
                else
                    offsets64[i64++] = o;
            }


            Array.Sort(offsets32);
            Array.Sort(offsets64);

            int nth = 0;
            foreach (PackIndex.MutableEntry me in index)
            {
                long o = me.Offset;
                if (o < int.MaxValue)
                    nth32[Array.BinarySearch(offsets32, (int)o)] = nth++;
                else
                    nth64[Array.BinarySearch(offsets64, o)] = nth++;
            }
        }

        /**
         * Search for object id with the specified start offset in this pack
         * (reverse) index.
         *
         * @param offset
         *            start offset of object to find.
         * @return object id for this offset, or null if no object was found.
         */
        public ObjectId FindObject(long offset)
        {
            if (offset <= int.MaxValue)
            {
                int i32 = Array.BinarySearch(offsets32, (int)offset);
                if (i32 < 0)
                    return null;
                return index.GetObjectId(nth32[i32]);
            }

            int i64 = Array.BinarySearch(offsets64, offset);
            if (i64 < 0)
                return null;
            return index.GetObjectId(nth64[i64]);
        }

        /**
         * Search for the next offset to the specified offset in this pack (reverse)
         * index.
         *
         * @param offset
         *            start offset of previous object (must be valid-existing
         *            offset).
         * @param maxOffset
         *            maximum offset in a pack (returned when there is no next
         *            offset).
         * @return offset of the next object in a pack or maxOffset if provided
         *         offset was the last one.
         * @throws CorruptObjectException
         *             when there is no object with the provided offset.
         */
        public long FindNextOffset(long offset, long maxOffset)
        {
            if (offset <= int.MaxValue)
            {
                int i32 = Array.BinarySearch(offsets32, (int)offset);
                if (i32 < 0)
                    throw new CorruptObjectException("Can't find object in (reverse) pack index for the specified offset " + offset);

                if (i32 + 1 == offsets32.Length)
                {
                    if (offsets64.Length > 0)
                        return offsets64[0];
                    return maxOffset;
                }
                return offsets32[i32 + 1];
            }

            int i64 = Array.BinarySearch(offsets64, offset);
            if (i64 < 0)
                throw new CorruptObjectException(
                    "Can't find object in (reverse) pack index for the specified offset "
                    + offset);

            if (i64 + 1 == offsets64.Length)
                return maxOffset;
            return offsets64[i64 + 1];
        }
    }
}
