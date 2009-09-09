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
using System.Collections;
using GitSharp.Util;

namespace GitSharp
{
    /// <summary>
    /// Caches slices of a <see cref="PackFile" /> in memory for faster read access.
    /// <para>
    /// The WindowCache serves as a Java based "buffer cache", loading segments of a
    /// <see cref="PackFile" /> into the JVM heap prior to use. As JGit often wants to do reads of
    /// only tiny slices of a file, the WindowCache tries to smooth out these tiny
    /// reads into larger block-sized IO operations.
    /// </para>
    /// </summary>
    internal class WindowCache : OffsetCache<ByteWindow, WindowCache.WindowRef>
    {
        private static volatile WindowCache _cache;

        private readonly int _maxFiles;
        private readonly int _maxBytes;
        private readonly bool _memoryMap;
        private readonly int _windowSizeShift;
        private readonly int _windowSize;
        private readonly AtomicValue<int> _openFiles;
        private readonly AtomicValue<int> _openBytes;

        static WindowCache()
        {
            reconfigure(new WindowCacheConfig());
        }

        private static int Bits(int newSize)
        {
            if (newSize < 4096)
            {
                throw new ArgumentException("Invalid window size");
            }

            if (newSize.BitCount() != 1)
            {
                throw new ArgumentException("Window size must be power of 2");
            }

            return newSize.NumberOfTrailingZeros();
        }

        /// <summary>
        /// Modify the configuration of the window cache.
        /// <para>
        /// The new configuration is applied immediately. If the new limits are
        /// smaller than what what is currently cached, older entries will be purged
        /// as soon as possible to allow the cache to meet the new limit.
        /// </summary>
        /// <param name="packedGitLimit">
        /// Maximum number of bytes to hold within this instance.
        /// </param>
        /// <param name="packedGitWindowSize">
        /// Number of bytes per window within the cache.
        /// </param>
        /// <param name="packedGitMMAP">
        /// True to enable use of mmap when creating windows.
        /// </param>
        /// <param name="deltaBaseCacheLimit">
        /// Number of bytes to hold in the delta base cache.
        /// </param>
        [Obsolete("Use WindowCache.reconfigure(WindowCacheConfig) instead.")]
        public static void reconfigure(int packedGitLimit, int packedGitWindowSize, bool packedGitMMAP, int deltaBaseCacheLimit)
        {
            var c = new WindowCacheConfig
                        {
                            PackedGitLimit = packedGitLimit,
                            PackedGitWindowSize = packedGitWindowSize,
                            PackedGitMMAP = packedGitMMAP,
                            DeltaBaseCacheLimit = deltaBaseCacheLimit
                        };
            reconfigure(c);
        }

        /// <summary>
        /// Modify the configuration of the window cache.
        /// <para>
        /// The new configuration is applied immediately. If the new limits are
        /// smaller than what what is currently cached, older entries will be purged
        /// as soon as possible to allow the cache to meet the new limit.
        /// </param>
        /// </summary>
        /// <param name="cfg">
        /// The new window cache configuration.
        /// </param>
        public static void reconfigure(WindowCacheConfig cfg)
        {
            var newCache = new WindowCache(cfg);
            WindowCache oldCache = _cache;

            if (oldCache != null)
            {
                oldCache.removeAll();
            }

            _cache = newCache;

            UnpackedObjectCache.Reconfigure(cfg);
        }

        internal static WindowCache Instance
        {
            get { return _cache; }
        }

        public static ByteWindow get(PackFile pack, long offset)
        {
            WindowCache c = _cache;
            ByteWindow r = c.getOrLoad(pack, c.ToStart(offset));
            if (c != _cache)
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

        public static void Purge(PackFile pack)
        {
            _cache.removeAll(pack);
        }

        private WindowCache(WindowCacheConfig cfg)
            : base(TableSize(cfg), LockCount(cfg))
        {
            _maxFiles = cfg.PackedGitOpenFiles;
            _maxBytes = cfg.PackedGitLimit;
            _memoryMap = cfg.PackedGitMMAP;
            _windowSizeShift = Bits(cfg.PackedGitWindowSize);
            _windowSize = 1 << _windowSizeShift;

            _openFiles = new AtomicValue<int>(0);
            _openBytes = new AtomicValue<int>(0);

            if (_maxFiles < 1)
            {
                throw new ArgumentException("Open files must be >= 1");
            }

            if (_maxBytes < _windowSize)
            {
                throw new ArgumentException("Window size must be < limit");
            }
        }

        public int getOpenFiles()
        {
            return _openFiles.get();
        }

        public int getOpenBytes()
        {
            return _openBytes.get();
        }

        internal override int hash(int packHash, long off)
        {
            return packHash + (int)((ulong)off >> _windowSizeShift);
        }

        internal override ByteWindow load(PackFile pack, long offset)
        {
            if (pack.beginWindowCache())
            {
                int c = _openFiles.get();
                _openFiles.compareAndSet(c, c+1);
            }
            try
            {
                if (_memoryMap)
                {
                    return pack.MemoryMappedByteWindow(offset, _windowSize);
                }

                return pack.Read(offset, _windowSize);
            }
            catch (Exception)
            {
                Close(pack);
                throw;
            }
        }

        internal override WindowRef createRef(PackFile p, long o, ByteWindow v)
        {
            var @ref = new WindowRef(p, o, v, queue);
            int c = _openBytes.get();
            _openBytes.compareAndSet(c, c + @ref.Size);
            return @ref;
        }

        internal override void clear(WindowRef @ref)
        {
            int c = _openBytes.get();
            _openBytes.compareAndSet(c, c - @ref.Size);
            Close(@ref.pack);
        }

        private void Close(PackFile pack)
        {
            if (!pack.endWindowCache()) return;
            int c = _openFiles.get();
            _openFiles.compareAndSet(c, c - 1);
        }

        internal override bool isFull()
        {
            return _maxFiles < _openFiles.get() || _maxBytes < _openBytes.get();
        }

        private long ToStart(long offset)
        {
            return (long)((ulong)offset >> _windowSizeShift) << _windowSizeShift;
        }

        private static int TableSize(WindowCacheConfig cfg)
        {
            int wsz = cfg.PackedGitWindowSize;
            int limit = cfg.PackedGitLimit;
            
            if (wsz <= 0)
            {
                throw new ArgumentException("Invalid window size");
            }

            if (limit < wsz)
            {
                throw new ArgumentException("Window size must be < limit");
            }

            return 5 * (limit / wsz) / 2;
        }

        private static int LockCount(WindowCacheConfig cfg)
        {
            return Math.Max(cfg.PackedGitOpenFiles, 32);
        }

        #region Nested Types

        internal class WindowRef : Ref<ByteWindow>
        {
            public WindowRef(PackFile pack, long position, ByteWindow v, Queue queue)
                : base(pack, position, v, queue)
            {
                Size = v.Size;
            }

            public int Size { get; private set; }
        }

        #endregion
    }
}