/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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
using System.IO;
using GitSharp.Exceptions;
using GitSharp.Util;
using System.IO.Compression;
using Winterdom.IO.FileMap;
using System.Runtime.CompilerServices;

namespace GitSharp
{
    /**
 * A Git version 2 pack file representation. A pack file contains Git objects in
 * delta packed format yielding high compression of lots of object where some
 * objects are similar.
 */
    public class PackFile : IEnumerable<PackIndex.MutableEntry>
    {

        /** Sorts PackFiles to be most recently created to least recently created. */
        public static Comparison<PackFile> SORT = new Comparison<PackFile>( (a, b) =>  b.packLastModified - a.packLastModified);


        private FileInfo idxFile;

        private FileInfo packFile;

        internal int hash;

        private MemoryMappedFile fd_map;
        private FileStream fd;

        public long Length
        {
            get; private set;
        }

        private int activeWindows;

        private int activeCopyRawData;

        private int packLastModified;

        private volatile bool invalid;

        private byte[] packChecksum;

        private PackIndex loadedIdx;

        private PackReverseIndex reverseIdx;

        /**
         * Construct a reader for an existing, pre-indexed packfile.
         * 
         * @param idxFile
         *            path of the <code>.idx</code> file listing the contents.
         * @param packFile
         *            path of the <code>.pack</code> file holding the data.
         */
        public PackFile(FileInfo idxFile, FileInfo packFile)
        {
            this.idxFile = idxFile;
            this.packFile = packFile;
            this.packLastModified = (int)(packFile.LastAccessTime.Ticks >> 10); // [henon] why the heck right shift by 10 ?? ... seems to have to do with the SORT comparison
            // Multiply by 31 here so we can more directly combine with another
            // value in WindowCache.hash(), without doing the multiply there.
            //
            hash = this.GetHashCode() * 31;
            this.Length = long.MaxValue;
            //ReadPackHeader();
        }

        public PackFile(string idxFile, string packFile) : this(new FileInfo(idxFile), new FileInfo(packFile)) { }


        [MethodImpl(MethodImplOptions.Synchronized)]
        private PackIndex idx()
        {
            if (loadedIdx == null)
            {
                if (invalid)
                    throw new PackInvalidException(packFile.FullName);

                try
                {
                    PackIndex idx = PackIndex.Open(idxFile);

                    if (packChecksum == null)
                        packChecksum = idx.packChecksum;
                    else if (Enumerable.SequenceEqual(packChecksum, idx.packChecksum))
                        throw new PackMismatchException("Pack checksum mismatch");

                    loadedIdx = idx;
                }
                catch (IOException e)
                {
                    invalid = true;
                    throw e;
                }
            }
            return loadedIdx;

        }

        /** @return the File object which locates this pack on disk. */
        internal PackedObjectLoader ResolveBase(WindowCursor curs, long offset)
        {
            return reader(curs, offset);
        }


        // [henon]: was getPackFile()
        public FileInfo File { get { return packFile; } private set { packFile = value; } }


        /**
         * Determine if an object is contained within the pack file.
         * <p>
         * For performance reasons only the index file is searched; the main pack
         * content is ignored entirely.
         * </p>
         * 
         * @param id
         *            the object to look for. Must not be null.
         * @return true if the object is in this pack; false otherwise.
         */
        public bool HasObject(AnyObjectId id)
        {
            return idx().HasObject(id);
        }

        /**
         * Get an object from this pack.
         * 
         * @param curs
         *            temporary working space associated with the calling thread.
         * @param id
         *            the object to obtain from the pack. Must not be null.
         * @return the object loader for the requested object if it is contained in
         *         this pack; null if the object was not found.
         * @
         *             the pack file or the index could not be read.
         */
        public PackedObjectLoader Get(WindowCursor curs, AnyObjectId id)
        {
            if (id == null)
                return null;
            long offset = idx().FindOffset(id);
            return 0 < offset ? reader(curs, offset) : null;
        }

        /**
         * Close the resources utilized by this repository
         */
        public void Close()
        {
            UnpackedObjectCache.purge(this);
            WindowCache.purge(this);
            lock (this)
            {
                loadedIdx = null;
                reverseIdx = null;
            }
        }

        /**
          * Provide iterator over entries in associated pack index, that should also
          * exist in this pack file. Objects returned by such iterator are mutable
          * during iteration.
          * <p>
          * Iterator returns objects in SHA-1 lexicographical order.
          * </p>
          * 
          * @return iterator over entries of associated pack index
          * 
          * @see PackIndex#iterator()
          */
        public IEnumerator<PackIndex.MutableEntry> GetEnumerator()
        {
            return idx().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return idx().GetEnumerator();
        }

        /**
         * Obtain the total number of objects available in this pack. This method
         * relies on pack index, giving number of effectively available objects.
         * 
         * @return number of objects in index of this pack, likewise in this pack
	     * @
	     *             the index file cannot be loaded into memory.
	     */
        public long ObjectCount
        {
            get { return idx().ObjectCount; }
        }

        /**
         * Search for object id with the specified start offset in associated pack
         * (reverse) index.
         *
         * @param offset
         *            start offset of object to find
         * @return object id for this offset, or null if no object was found
         */
        public ObjectId FindObjectForOffset(long offset)
        {
            return getReverseIdx().FindObject(offset);
        }

        public UnpackedObjectCache.Entry readCache(long position)
        {
            return UnpackedObjectCache.get(this, position);
        }

        public void saveCache(long position, byte[] data, int type)
        {
            UnpackedObjectCache.store(this, position, data, type);
        }

        public byte[] decompress(long position, long totalSize, WindowCursor curs)
        {
            byte[] dstbuf = new byte[totalSize];
            if (curs.inflate(this, position, dstbuf, 0) != totalSize)
                throw new EndOfStreamException("Short compressed stream at " + position);
            return dstbuf;
        }

        internal void copyRawData(PackedObjectLoader loader, Stream @out, byte[] buf, WindowCursor curs)
        {
            long objectOffset = loader.objectOffset;
            long dataOffset = loader.getDataOffset();
            int cnt = (int)(findEndOffset(objectOffset) - dataOffset);

            if (idx().HasCRC32Support)
            {
                Crc32 crc = new Crc32();
                int headerCnt = (int)(dataOffset - objectOffset);
                while (headerCnt > 0)
                {
                    int toRead = Math.Min(headerCnt, buf.Length);
                    readFully(objectOffset, buf, 0, toRead, curs);
                    crc.Update(buf, 0, toRead);
                    headerCnt -= toRead;
                }
                var crcOut = new CheckedOutputStream(@out, crc);
                copyToStream(dataOffset, buf, cnt, crcOut, curs);
                long computed = crc.Value;
                ObjectId id = FindObjectForOffset(objectOffset);
                long expected = idx().FindCRC32(id);
                if (computed != expected)
                    throw new CorruptObjectException("object at " + dataOffset + " in " + File.FullName + " has bad zlib stream");
            }
            else
            {
                try
                {
                    curs.inflateVerify(this, dataOffset);
                }
                catch (Exception fe) // [henon] was DataFormatException
                {
                    throw new CorruptObjectException("object at " + dataOffset + " in " + File.FullName + " has bad zlib stream", fe);
                }
                copyToStream(dataOffset, buf, cnt, @out, curs);
            }
        }

        public bool SupportsFastCopyRawData
        {
            get { return idx().HasCRC32Support; }
        }

        internal bool IsInvalid
        {
            get
            {
                return invalid;
            }
        }

        private void readFully(long position, byte[] dstbuf, int dstoff, int cnt, WindowCursor curs)
        {
            if (curs.copy(this, position, dstbuf, dstoff, cnt) != cnt)
                throw new EndOfStreamException();
        }

        private void copyToStream(long position, byte[] buf, long cnt, Stream @out, WindowCursor curs)
        {
            while (cnt > 0)
            {
                int toRead = (int)Math.Min(cnt, buf.Length);
                readFully(position, buf, 0, toRead, curs);
                position += toRead;
                cnt -= toRead;
                @out.Write(buf, 0, toRead);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void beginCopyRawData()
        {
            if (++activeCopyRawData == 1 && activeWindows == 0)
                doOpen();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void endCopyRawData()
        {
            if (--activeCopyRawData == 0 && activeWindows == 0)
                doClose();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool beginWindowCache()
        {

            if (++activeWindows == 1)
            {
                if (activeCopyRawData == 0)
                    doOpen();
                return true;
            }
            return false;

        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool endWindowCache()
        {
            bool r = --activeWindows == 0;
            if (r && activeCopyRawData == 0)
                doClose();
            return r;
        }

        private void doOpen()
        {
            try
            {
                if (invalid)
                    throw new PackInvalidException(packFile.FullName);
                fd = new FileStream(packFile.FullName, System.IO.FileMode.Open, FileAccess.Read);
                Length = packFile.Length;
                onOpenPack();
            }
            catch (Exception re)
            {
                openFail();
                throw re;
            }
        }

        private void openFail()
        {
            activeWindows = 0;
            activeCopyRawData = 0;
            invalid = true;
            doClose();
        }

        private void doClose()
        {
            if (fd != null)
            {
                try
                {
                    fd.Close();
                }
                catch (IOException)
                {
                    // Ignore a close event. We had it open only for reading.
                    // There should not be errors related to network buffers
                    // not flushed, etc.
                }
                fd = null;
            }
        }

        internal ByteArrayWindow read(long pos, int size)
        {
            if (Length < pos + size)
                size = (int)(Length - pos);
            byte[] buf = new byte[size];
            NB.ReadFully(fd, pos, buf, 0, size);
            return new ByteArrayWindow(this, pos, buf);
        }

        internal ByteWindow mmap(long pos, int size)
        {
            if (Length < pos + size)
                size = (int)(Length - pos);
            Stream map;
            try
            {
                fd_map = MemoryMappedFile.Create(packFile.FullName, MapProtection.PageReadOnly);
                map = fd_map.MapView(MapAccess.FileMapRead, pos, size); // was: map = fd.map(MapMode.READ_ONLY, pos, size);
            }
            catch (IOException)
            {
                // The most likely reason this failed is the process has run out
                // of virtual memory. We need to discard quickly, and try to
                // force the GC to finalize and release any existing mappings.
                //
                GC.Collect();
                GC.WaitForPendingFinalizers();
                map = fd_map.MapView(MapAccess.FileMapRead, pos, size);
            }
            //if (map.hasArray())
            //    return new ByteArrayWindow(this, pos, map.array());
            return new ByteBufferWindow(this, pos, map);
        }

        // [henon] copied from dotgit:
        //private Stream GetPackStream(int packFileOffset, int Length, ref int viewOffset)
        //{
        //    int dwFileMapStart = (packFileOffset / (int)_systemInfo.dwAllocationGranularity) * (int)_systemInfo.dwAllocationGranularity;
        //    int dwMapViewSize = (packFileOffset % (int)_systemInfo.dwAllocationGranularity) + Length;
        //    int dwFileMapSize = packFileOffset + Length;
        //    viewOffset = packFileOffset - dwFileMapStart;
        //    return fd.MapView(MapAccess.FileMapRead, dwFileMapStart, dwMapViewSize);
        //}

        private void onOpenPack()
        {
            PackIndex idx = this.idx();
            byte[] buf = new byte[20];

            NB.ReadFully(fd, 0, buf, 0, 12);
            if (RawParseUtils.match(buf, 0, Constants.PACK_SIGNATURE) != 4)
                throw new IOException("Not a PACK file.");
            long vers = NB.decodeUInt32(buf, 4);
            long packCnt = NB.decodeUInt32(buf, 8);
            if (vers != 2 && vers != 3)
                throw new IOException("Unsupported pack version " + vers + ".");

            if (packCnt != idx.ObjectCount)
                throw new PackMismatchException("Pack object count mismatch:"
                        + " pack " + packCnt
                        + " index " + idx.ObjectCount
                        + ": " + File.FullName);

            NB.ReadFully(fd, Length - 20, buf, 0, 20);
            if (!Enumerable.SequenceEqual(buf, packChecksum))
                throw new PackMismatchException("Pack checksum mismatch:"
                        + " pack " + ObjectId.FromRaw(buf).ToString()
                        + " index " + ObjectId.FromRaw(idx.packChecksum).ToString()
                        + ": " + File.FullName);
        }


        //private void ReadPackHeader()
        //{
        //    var reader = new BinaryReader(_stream);

        //    var sig = reader.ReadBytes(Constants.PackSignature.Length);

        //    for (int k = 0; k < Constants.PackSignature.Length; k++)
        //        if (sig[k] != Constants.PackSignature[k])
        //            throw new IOException("Not a PACK file.");

        //    var vers = reader.ReadUInt32();
        //    if (vers != 2 && vers != 3)
        //        throw new IOException("Unsupported pack version " + vers + ".");

        //    long objectCnt = reader.ReadUInt32();
        //    if (Index.ObjectCount != objectCnt)
        //        throw new IOException("Pack index object count mismatch; expected " + objectCnt + " found " + Index.ObjectCount + ": " + _stream.Name);
        //}

        private PackedObjectLoader reader(WindowCursor curs, long objOffset)
        {
            long pos = objOffset;
            int p = 0;
            byte[] ib = curs.tempId; // reader.ReadBytes(ObjectId.ObjectIdLength);
            readFully(pos, ib, 0, 20, curs);
            int c = ib[p++] & 0xff;
            int typeCode = (c >> 4) & 7;
            long dataSize = c & 15;
            int shift = 4;
            while ((c & 0x80) != 0)
            {
                c = ib[p++] & 0xff;
                dataSize += (c & 0x7f) << shift;
                shift += 7;
            }
            pos += p;

            switch (typeCode)
            {
                case Constants.OBJ_COMMIT:
                case Constants.OBJ_TREE:
                case Constants.OBJ_BLOB:
                case Constants.OBJ_TAG:
                    return new WholePackedObjectLoader(this, pos, objOffset, typeCode, (int)dataSize);
                case Constants.OBJ_OFS_DELTA:
                    readFully(pos, ib, 0, 20, curs);
                    p = 0;
                    c = ib[p++] & 0xff;
                    long ofs = c & 127;
                    while ((c & 0x80) != 0)
                    {
                        ofs += 1;
                        c = ib[p++] & 0xff;
                        ofs <<= 7;
                        ofs += (c & 127);
                    }
                    return new DeltaOfsPackedObjectLoader(this, pos + p, objOffset, (int)dataSize, objOffset - ofs);
                case Constants.OBJ_REF_DELTA:
                    readFully(pos, ib, 0, 20, curs);
                    return new DeltaRefPackedObjectLoader(this, pos + ib.Length, objOffset, (int)dataSize, ObjectId.FromRaw(ib));

                default:
                    throw new IOException("Unknown object type " + typeCode + ".");
            }
        }

        private long findEndOffset(long startOffset)
        {
            long maxOffset = Length - 20;
            return getReverseIdx().FindNextOffset(startOffset, maxOffset);
        }

        private PackReverseIndex _reverseIndex;

        [MethodImpl(MethodImplOptions.Synchronized)]
        private PackReverseIndex getReverseIdx()
        {

            if (_reverseIndex == null)
                _reverseIndex = new PackReverseIndex(idx());
            return _reverseIndex;

        }



    }
}
