/*
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
using GitSharp.Util;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
using GitSharp.Exceptions;
using System.Runtime.CompilerServices;

namespace GitSharp
{

    public class UnpackedObjectCache
    {
        private static int CACHE_SZ = 1024;

        private static WeakReference<Entry> DEAD;

        private static int hash(long position)
        {
            return (int)((uint)(((int)position) << 22) >> 22);
        }

        private static int maxByteCount;

        private static Slot[] cache;

        private static Slot lruHead;

        private static Slot lruTail;

        private static int openByteCount;

        static UnpackedObjectCache()
        {
            DEAD = new WeakReference<Entry>(null);
            maxByteCount = new WindowCacheConfig().getDeltaBaseCacheLimit();

            cache = new Slot[CACHE_SZ];
            for (int i = 0; i < CACHE_SZ; i++)
                cache[i] = new Slot();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void reconfigure(WindowCacheConfig cfg)
        {
            int dbLimit = cfg.getDeltaBaseCacheLimit();
            if (maxByteCount != dbLimit)
            {
                maxByteCount = dbLimit;
                releaseMemory();
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Entry get(PackFile pack, long position)
        {
            Slot e = cache[hash(position)];
            if (e.provider == pack && e.position == position)
            {
                Entry buf = e.data.get();
                if (buf != null)
                {
                    moveToHead(e);
                    return buf;
                }
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void store(PackFile pack, long position,
                 byte[] data, int objectType)
        {
            if (data.Length > maxByteCount)
                return; // Too large to cache.

            Slot e = cache[hash(position)];
            clearEntry(e);

            openByteCount += data.Length;
            releaseMemory();

            e.provider = pack;
            e.position = position;
            e.sz = data.Length;
            e.data = new WeakReference<Entry>(new Entry(data, objectType));
            moveToHead(e);
        }

        private static void releaseMemory()
        {
            while (openByteCount > maxByteCount && lruTail != null)
            {
                Slot currOldest = lruTail;
                Slot nextOldest = currOldest.lruPrev;

                clearEntry(currOldest);
                currOldest.lruPrev = null;
                currOldest.lruNext = null;

                if (nextOldest == null)
                    lruHead = null;
                else
                    nextOldest.lruNext = null;
                lruTail = nextOldest;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void purge(PackFile file)
        {
            foreach (Slot e in cache)
            {
                if (e.provider == file)
                {
                    clearEntry(e);
                    unlink(e);
                }
            }
        }

        private static void moveToHead(Slot e)
        {
            unlink(e);
            e.lruPrev = null;
            e.lruNext = lruHead;
            if (lruHead != null)
                lruHead.lruPrev = e;
            else
                lruTail = e;
            lruHead = e;
        }

        private static void unlink(Slot e)
        {
            Slot prev = e.lruPrev;
            Slot next = e.lruNext;
            if (prev != null)
                prev.lruNext = next;
            if (next != null)
                next.lruPrev = prev;
        }

        private static void clearEntry(Slot e)
        {
            openByteCount -= e.sz;
            e.provider = null;
            e.data = DEAD;
            e.sz = 0;
        }


        public class Entry
        {
            public byte[] data;

            public int type;

            public Entry(byte[] aData, int aType)
            {
                data = aData;
                type = aType;
            }
        }

        private class Slot
        {
            public Slot lruPrev;

            public Slot lruNext;

            public PackFile provider;

            public long position;

            public int sz;

            public WeakReference<Entry> data = DEAD;
        }
    }
}
