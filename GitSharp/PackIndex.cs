/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GitSharp.Util;

namespace GitSharp
{

    /**
     * Access path to locate objects by {@link ObjectId} in a {@link PackFile}.
     * <p>
     * Indexes are strictly redundant information in that we can rebuild all of the
     * data held in the index file from the on disk representation of the pack file
     * itself, but it is faster to access for random requests because data is stored
     * by ObjectId.
     * </p>
     */
    public abstract class PackIndex : IEnumerable<PackIndex.MutableEntry>
    {



        /** Footer checksum applied on the bottom of the pack file. */
        internal byte[] _packChecksum;

        public byte[] packChecksum
        {
            get
            {
                return _packChecksum;
            }
        }

        private static bool IsTOC(byte[] h)
        {
            byte[] toc = PackIndexWriter.TOC;
            for (int i = 0; i < toc.Length; i++)
                if (h[i] != toc[i])
                    return false;
            return true;
        }

        /**
         * Determine if an object is contained within the pack file.
         * 
         * @param id
         *            the object to look for. Must not be null.
         * @return true if the object is listed in this index; false otherwise.
         */
        public bool HasObject(AnyObjectId id)
        {
            return FindOffset(id) != -1;
        }

        /**
         * Provide iterator that gives access to index entries. Note, that iterator
         * returns reference to mutable object, the same reference in each call -
         * for performance reason. If client needs immutable objects, it must copy
         * returned object on its own.
         * <p>
         * Iterator returns objects in SHA-1 lexicographical order.
         * </p>
         * 
         * @return iterator over pack index entries
         */

        #region IEnumerable<MutableEntry> Members

        public abstract IEnumerator<PackIndex.MutableEntry> GetEnumerator();

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion


        /**
	 * Obtain the total number of objects described by this index.
	 * 
	 * @return number of objects in this index, and likewise in the associated
	 *         pack that this index was generated from.
	 */
        public abstract long ObjectCount { get; internal set; }

        /**
         * Obtain the total number of objects needing 64 bit offsets.
         *
         * @return number of objects in this index using a 64 bit offset; that is an
         *         object positioned after the 2 GB position within the file.
         */
        public abstract long Offset64Count { get; }

        /**
         * Get ObjectId for the n-th object entry returned by {@link #iterator()}.
         * <p>
         * This method is a constant-time replacement for the following loop:
         *
         * <pre>
         * Iterator&lt;MutableEntry&gt; eItr = index.iterator();
         * int curPosition = 0;
         * while (eItr.hasNext() &amp;&amp; curPosition++ &lt; nthPosition)
         * 	eItr.next();
         * ObjectId result = eItr.next().ToObjectId();
         * </pre>
         *
         * @param nthPosition
         *            position within the traversal of {@link #iterator()} that the
         *            caller needs the object for. The first returned
         *            {@link MutableEntry} is 0, the second is 1, etc.
         * @return the ObjectId for the corresponding entry.
         */
        public abstract ObjectId GetObjectId(long nthPosition);

        /**
         * Get ObjectId for the n-th object entry returned by {@link #iterator()}.
         * <p>
         * This method is a constant-time replacement for the following loop:
         *
         * <pre>
         * Iterator&lt;MutableEntry&gt; eItr = index.iterator();
         * int curPosition = 0;
         * while (eItr.hasNext() &amp;&amp; curPosition++ &lt; nthPosition)
         * 	eItr.next();
         * ObjectId result = eItr.next().ToObjectId();
         * </pre>
         *
         * @param nthPosition
         *            unsigned 32 bit position within the traversal of
         *            {@link #iterator()} that the caller needs the object for. The
         *            first returned {@link MutableEntry} is 0, the second is 1,
         *            etc. Positions past 2**31-1 are negative, but still valid.
         * @return the ObjectId for the corresponding entry.
         */
        public ObjectId GetObjectId(int nthPosition)
        {
            if (nthPosition >= 0)
                return GetObjectId((long)nthPosition);
            int u31 = nthPosition.UnsignedRightShift(1);
            int one = nthPosition & 1;
            return GetObjectId((((long)u31) << 1) | (uint)one);
        }

        /**
         * Locate the file offset position for the requested object.
         * 
         * @param objId
         *            name of the object to locate within the pack.
         * @return offset of the object's header and compressed content; -1 if the
         *         object does not exist in this index and is thus not stored in the
         *         associated pack.
         */
        public abstract long FindOffset(AnyObjectId objId);

        /**
         * Retrieve stored CRC32 checksum of the requested object raw-data
         * (including header).
         *
         * @param objId
         *            id of object to look for
         * @return CRC32 checksum of specified object (at 32 less significant bits)
         * @throws MissingObjectException
         *             when requested ObjectId was not found in this index
         * @throws InvalidOperationException
         *             when this index doesn't support CRC32 checksum
         */
        public abstract long FindCRC32(AnyObjectId objId);


        /**
         * Check whether this index supports (has) CRC32 checksums for objects.
         *
         * @return true if CRC32 is stored, false otherwise
         */
        public abstract bool HasCRC32Support { get; }

        public class MutableEntry
        {
            private readonly Func<MutableObjectId, MutableObjectId> _idBufferBuilder;
            private MutableObjectId _idBuffer;

            public MutableObjectId idBuffer
            {
                get
                {
                    if (_idBuffer == null)
                    {
                        _idBuffer = _idBufferBuilder(new MutableObjectId());
                    }
                    return _idBuffer;
                }
            }

            public MutableEntry(Func<MutableObjectId, MutableObjectId> idBufferBuilder)
            {
                _idBufferBuilder = idBufferBuilder;
            }

            public MutableEntry(MutableObjectId idBuffer)
            {
                _idBuffer = idBuffer;
            }
            /**
             * Returns offset for this index object entry
             * 
             * @return offset of this object in a pack file
             */
            public long Offset { get; set; }

            /** @return hex string describing the object id of this entry. */
            public String Name
            {
                get
                {
                    return idBuffer.Name;
                }
            }

            public ObjectId ToObjectId()
            {
                return idBuffer.ToObjectId();
            }

            /**
             * Returns mutable copy of this mutable entry.
             * 
             * @return copy of this mutable entry
             */

            public MutableEntry CloneEntry()
            {
                var r = new MutableEntry(new MutableObjectId(idBuffer.ToObjectId()));
                r.Offset = Offset;

                return r;
            }

            public override string ToString()
            {
                return idBuffer.ToString();
            }
        }

        internal abstract class EntriesIterator : IEnumerator<MutableEntry>
        {
            private MutableEntry _current;
            private readonly PackIndex _packIndex;

            protected EntriesIterator(PackIndex packIndex)
            {
                _packIndex = packIndex;
            }

            protected long ReturnedNumber;

            protected abstract MutableObjectId IdBufferBuilder(MutableObjectId idBuffer);

            private MutableEntry InitEntry()
            {
                return new MutableEntry(IdBufferBuilder);
            }

            public bool hasNext()
            {
                return ReturnedNumber < _packIndex.ObjectCount;
            }

            protected abstract MutableEntry InnerNext(MutableEntry entry);

            public MutableEntry next()
            {
                _current = InnerNext(InitEntry());
                return _current;
            }

            public bool MoveNext()
            {
                if (!hasNext())
                {
                    return false;
                }

                next();
                return true;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public MutableEntry Current
            {
                get { return _current; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
            }
        }

        /// <summary>
        /// Open an existing pack <code>.idx</code> file for reading..
        /// <p>
        /// The format of the file will be automatically detected and a proper access
        /// implementation for that format will be constructed and returned to the
        /// caller. The file may or may not be held open by the returned instance.
        /// </p>
        /// </summary>
        /// <param name="idxFile">existing pack .idx to read.</param>
        /// <returns></returns>
        public static PackIndex Open(FileInfo idxFile)
        {
            try
            {
                using (FileStream fs = idxFile.OpenRead())
                {
                    byte[] hdr = new byte[8];
                    NB.ReadFully(fs, hdr, 0, hdr.Length);

                    if (IsTOC(hdr))
                    {
                        int v = NB.DecodeInt32(hdr, 4);
                        switch (v)
                        {
                            case 2:
                                return new PackIndexV2(fs);
                            default:
                                throw new IOException("Unsupported pack index version " + v);
                        }
                    }
                    return new PackIndexV1(fs, hdr);
                }
            }
            catch (IOException)
            {
                throw new IOException("Unable to read pack index: " + idxFile.FullName);
            }
        }
    }
}
