/*
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
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace GitSharp
{
    /** Active handle to a ByteWindow. */
    public class WindowCursor
    {
        /** Temporary buffer large enough for at least one raw object id. */
        internal byte[] tempId = new byte[Constants.OBJECT_ID_LENGTH];

        private Inflater inf;

        private ByteWindow window;


        /**
         * Copy bytes from the window to a caller supplied buffer.
         * 
         * @param pack
         *            the file the desired window is stored within.
         * @param position
         *            position within the file to read from.
         * @param dstbuf
         *            destination buffer to copy into.
         * @param dstoff
         *            offset within <code>dstbuf</code> to start copying into.
         * @param cnt
         *            number of bytes to copy. This value may exceed the number of
         *            bytes remaining in the window starting at offset
         *            <code>pos</code>.
         * @return number of bytes actually copied; this may be less than
         *         <code>cnt</code> if <code>cnt</code> exceeded the number of
         *         bytes available.
         * @
         *             this cursor does not match the provider or id and the proper
         *             window could not be acquired through the provider's cache.
         */
        public int copy(PackFile pack, long position, byte[] dstbuf, int dstoff, int cnt)
        {
            long Length = pack.Length;
            int need = cnt;
            while (need > 0 && position < Length)
            {
                pin(pack, position);
                int r = window.copy(position, dstbuf, dstoff, need);
                position += r;
                dstoff += r;
                need -= r;
            }
            return cnt - need;
        }


        /**
         * Pump bytes into the supplied inflater as input.
         * 
         * @param pack
         *            the file the desired window is stored within.
         * @param position
         *            position within the file to read from.
         * @param dstbuf
         *            destination buffer the inflater should output decompressed
         *            data to.
         * @param dstoff
         *            current offset within <code>dstbuf</code> to inflate into.
         * @return updated <code>dstoff</code> based on the number of bytes
         *         successfully inflated into <code>dstbuf</code>.
         * @
         *             this cursor does not match the provider or id and the proper
         *             window could not be acquired through the provider's cache.
         * @throws DataFormatException
         *             the inflater encountered an invalid chunk of data. Data
         *             stream corruption is likely.
         */
        public int inflate(PackFile pack, long position, byte[] dstbuf, int dstoff)
        {
            if (inf == null)
                inf = InflaterCache.Instance.get();
            else
                inf.Reset();
            for (; ; )
            {
                pin(pack, position);
                dstoff = window.inflate(position, dstbuf, dstoff, inf);
                if (inf.IsFinished)
                    return dstoff;
                position = window.end;
            }
        }

        public void inflateVerify(PackFile pack, long position)
        {
            if (inf == null)
                inf = InflaterCache.Instance.get();
            else
                inf.Reset();
            for (; ; )
            {
                pin(pack, position);
                window.inflateVerify(position, inf);
                if (inf.IsFinished)
                    return;
                position = window.end;
            }
        }

        private void pin(PackFile pack, long position)
        {
            ByteWindow w = window;
            if (w == null || !w.contains(pack, position))
            {
                // If memory is low, we may need what is in our window field to
                // be cleaned up by the GC during the get for the next window.
                // So we always clear it, even though we are just going to set
                // it again.
                //
                window = null;
                window = WindowCache.get(pack, position);
            }
        }

        /** Release the current window cursor. */
        public void release()
        {
            window = null;
            try
            {
                InflaterCache.Instance.release(inf);
            }
            finally
            {
                inf = null;
            }
        }

        /**
         * @param curs cursor to release; may be null.
         * @return always null.
         */
        public static WindowCursor release(WindowCursor curs)
        {
            if (curs != null)
                curs.release();
            return null;
        }
    }
}
