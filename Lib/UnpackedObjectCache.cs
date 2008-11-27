/*
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gitty.Util;

namespace Gitty.Lib
{
    [Complete]
    public class UnpackedObjectCache
    {

        private static int CACHE_SZ = 256;

        private static int MB = 1024 * 1024;

        private static WeakReference<Entry> Dead;

        private static int maxByteCount;

        private static Slot[] cache;

        private static Slot lruHead;

        private static Slot lruTail;

        private static int openByteCount;

        static UnpackedObjectCache()
        {

            Dead = new WeakReference<Entry>(null);
            maxByteCount = 10 * MB;

            cache = new Slot[CACHE_SZ];
            for (int i = 0; i < CACHE_SZ; i++)
                cache[i] = new Slot();

        }


        private static int Hash(WindowedFile pack, long position)
        {
            int h = pack.GetHashCode() + (int)position;
            h += h >> 16;
            h += h >> 8;
            return h % CACHE_SZ;
        }
        static void Reconfigure(int dbLimit)
        {
            lock (typeof(UnpackedObjectCache))
            {
                if (maxByteCount != dbLimit)
                {
                    maxByteCount = dbLimit;
                    ReleaseMemory();
                }
            }
        }
        public static Entry Get(WindowedFile pack, long position)
        {
            lock (typeof(UnpackedObjectCache))
            {
                Slot e = cache[Hash(pack, position)];
                if (e.provider == pack && e.position == position)
                {
                    Entry buf = e.data.Target;
                    if (buf != null)
                    {
                        MoveToHead(e);
                        return buf;
                    }
                }
                return null;
            }
        }

        public static void Store(WindowedFile pack, long position, byte[] data, ObjectType objectType)
        {
            lock (typeof(UnpackedObjectCache))
            {
                if (data.Length > maxByteCount)
                    return; // Too large to cache.

                Slot e = cache[Hash(pack, position)];
                ClearEntry(e);

                openByteCount += data.Length;
                ReleaseMemory();

                e.provider = pack;
                e.position = position;
                e.data = new WeakReference<Entry>(new Entry(data, objectType));
                MoveToHead(e);
            }
        }

        private static void ReleaseMemory()
        {
            while (openByteCount > maxByteCount && lruTail != null)
            {
                Slot currOldest = lruTail;
                Slot nextOldest = currOldest.lruPrev;

                ClearEntry(currOldest);
                currOldest.lruPrev = null;
                currOldest.lruNext = null;

                if (nextOldest == null)
                    lruHead = null;
                else
                    nextOldest.lruNext = null;
                lruTail = nextOldest;
            }
        }

        public static void Purge(WindowedFile file)
        {
            lock (typeof(UnpackedObjectCache))
            {
                foreach (Slot e in cache)
                {
                    if (e.provider == file)
                    {
                        ClearEntry(e);
                        Unlink(e);
                    }
                }
            }
        }

        private static void MoveToHead(Slot e)
        {
            Unlink(e);
            e.lruPrev = null;
            e.lruNext = lruHead;
            if (lruHead != null)
                lruHead.lruPrev = e;
            else
                lruTail = e;
            lruHead = e;
        }

        private static void Unlink(Slot e)
        {
            Slot prev = e.lruPrev;
            Slot next = e.lruNext;
            if (prev != null)
                prev.lruNext = next;
            if (next != null)
                next.lruPrev = prev;
        }

        private static void ClearEntry(Slot e)
        {
            Entry old = e.data.Target;
            if (old != null)
                openByteCount -= old.Data.Length;
            e.provider = null;
            e.data = Dead;
        }

        private UnpackedObjectCache()
        {
            throw new InvalidOperationException();
        }
        public class Entry
        {
            public byte[] Data { get; private set; }

            public ObjectType Type { get; private set; }


            public Entry(byte[] data, ObjectType type)
            {
                this.Data = data;
                this.Type = type;
            }
        }

        private class Slot
        {
            public Slot lruPrev;

            public Slot lruNext;

            public WindowedFile provider;

            public long position;

            public WeakReference<Entry> data = Dead;
        }
    }
}
