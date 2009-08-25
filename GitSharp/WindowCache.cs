/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyrigth (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using System.Collections;

namespace GitSharp
{

    /**
     * Caches slices of a {@link PackFile} in memory for faster read access.
     * <p>
     * The WindowCache serves as a Java based "buffer cache", loading segments of a
     * PackFile into the JVM heap prior to use. As JGit often wants to do reads of
     * only tiny slices of a file, the WindowCache tries to smooth out these tiny
     * reads into larger block-sized IO operations.
     */
    internal class WindowCache : OffsetCache<ByteWindow, WindowCache.WindowRef>
    {
        private static int bits(int newSize)
        {
            if (newSize < 4096)
                throw new ArgumentException("Invalid window size");
            if (newSize.BitCount() != 1)
                throw new ArgumentException("Window size must be power of 2");
            return newSize.NumberOfTrailingZeros();
        }

        private static volatile WindowCache cache;

        static WindowCache()
        {
            reconfigure(new WindowCacheConfig());
        }

        /**
         * Modify the configuration of the window cache.
         * <p>
         * The new configuration is applied immediately. If the new limits are
         * smaller than what what is currently cached, older entries will be purged
         * as soon as possible to allow the cache to meet the new limit.
         * 
         * @param packedGitLimit
         *            maximum number of bytes to hold within this instance.
         * @param packedGitWindowSize
         *            number of bytes per window within the cache.
         * @param packedGitMMAP
         *            true to enable use of mmap when creating windows.
         * @param deltaBaseCacheLimit
         *            number of bytes to hold in the delta base cache.
         * @deprecated Use {@link WindowCacheConfig} instead.
         */
        public static void reconfigure(int packedGitLimit, int packedGitWindowSize, bool packedGitMMAP, int deltaBaseCacheLimit)
        {
            WindowCacheConfig c = new WindowCacheConfig();
            c.setPackedGitLimit(packedGitLimit);
            c.setPackedGitWindowSize(packedGitWindowSize);
            c.setPackedGitMMAP(packedGitMMAP);
            c.setDeltaBaseCacheLimit(deltaBaseCacheLimit);
            reconfigure(c);
        }

        /**
         * Modify the configuration of the window cache.
         * <p>
         * The new configuration is applied immediately. If the new limits are
         * smaller than what what is currently cached, older entries will be purged
         * as soon as possible to allow the cache to meet the new limit.
         *
         * @param cfg
         *            the new window cache configuration.
         * @throws ArgumentException
         *             the cache configuration contains one or more invalid
         *             settings, usually too low of a limit.
         */
        public static void reconfigure(WindowCacheConfig cfg)
        {
            WindowCache nc = new WindowCache(cfg);
            WindowCache oc = cache;
            if (oc != null)
                oc.removeAll();
            cache = nc;
            UnpackedObjectCache.reconfigure(cfg);
        }

        internal static WindowCache getInstance()
        {
            return cache;
        }

        public static ByteWindow get(PackFile pack, long offset)
        {
            WindowCache c = cache;
            ByteWindow r = c.getOrLoad(pack, c.toStart(offset));
            if (c != cache)
            {
                // The cache was reconfigured while we were using the old one
                // to load this window. The window is still valid, but our
                // cache may think its still live. Ensure the window is removed
                // from the old cache so resources can be released.
                //
                c.removeAll();
            }
            return r;
        }

        public static void purge(PackFile pack)
        {
            cache.removeAll(pack);
        }

        private int maxFiles;

        private int maxBytes;

        private bool mmap;

        private int windowSizeShift;

        private int windowSize;

        private AtomicValue<int> openFiles;

        private AtomicValue<int> openBytes;

        private WindowCache(WindowCacheConfig cfg)
            : base(tableSize(cfg), lockCount(cfg))
        {
            maxFiles = cfg.getPackedGitOpenFiles();
            maxBytes = cfg.getPackedGitLimit();
            mmap = cfg.isPackedGitMMAP();
            windowSizeShift = bits(cfg.getPackedGitWindowSize());
            windowSize = 1 << windowSizeShift;

            openFiles = new AtomicValue<int>(0);
            openBytes = new AtomicValue<int>(0);

            if (maxFiles < 1)
                throw new ArgumentException("Open files must be >= 1");
            if (maxBytes < windowSize)
                throw new ArgumentException("Window size must be < limit");
        }

        public int getOpenFiles()
        {
            return openFiles.get();
        }

        public int getOpenBytes()
        {
            return openBytes.get();
        }

        internal override int hash(int packHash, long off)
        {
            return packHash + (int)((ulong)off >> windowSizeShift);
        }


        internal override ByteWindow load(PackFile pack, long offset)
        {
            if (pack.beginWindowCache())
            {
                int c = openFiles.get();
                openFiles.compareAndSet(c, c+1);
            }
            try
            {
                if (mmap)
                    return pack.mmap(offset, windowSize);
                return pack.read(offset, windowSize);
            }
            catch (Exception e)
            {
                close(pack);
                throw e;
            }
        }


        internal override WindowRef createRef(PackFile p, long o, ByteWindow v)
        {
            WindowRef @ref = new WindowRef(p, o, v, queue);
            int c = openBytes.get();
            openBytes.compareAndSet(c, c + @ref.size);
            return @ref;
        }


        internal override void clear(WindowRef @ref)
        {
            int c = openBytes.get();
            openBytes.compareAndSet(c, c - @ref.size);
            close(@ref.pack);
        }

        private void close(PackFile pack)
        {
            if (pack.endWindowCache())
            {
                int c = openFiles.get();
                openFiles.compareAndSet(c, c - 1);
            }
        }


        internal override bool isFull()
        {
            return maxFiles < openFiles.get() || maxBytes < openBytes.get();
        }

        private long toStart(long offset)
        {
            return (long)((ulong)offset >> windowSizeShift) << windowSizeShift;
        }

        private static int tableSize(WindowCacheConfig cfg)
        {
            int wsz = cfg.getPackedGitWindowSize();
            int limit = cfg.getPackedGitLimit();
            if (wsz <= 0)
                throw new ArgumentException("Invalid window size");
            if (limit < wsz)
                throw new ArgumentException("Window size must be < limit");
            return 5 * (limit / wsz) / 2;
        }

        private static int lockCount(WindowCacheConfig cfg)
        {
            return Math.Max(cfg.getPackedGitOpenFiles(), 32);
        }

        internal class WindowRef : OffsetCache<ByteWindow, WindowCache.WindowRef>.Ref<ByteWindow>
        {
            internal int size;

            public WindowRef(PackFile pack, long position, ByteWindow v, Queue queue)
                : base(pack, position, v, queue)
            {
                size = v.size();
            }
        }
    }
}