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

using ICSharpCode.SharpZipLib.Zip.Compression;

namespace GitSharp
{
    /// <summary>
    /// Active handle to a ByteWindow.
    /// </summary>
    public class WindowCursor
    {
        private Inflater _inf;
        private ByteWindow _window;

        public WindowCursor()
        {
            TempId = new byte[Constants.OBJECT_ID_LENGTH];
        }

        /// <summary>
        /// Temporary buffer large enough for at least one raw object id.
        /// </summary>
        internal byte[] TempId { get; private set; }

        /// <summary>
        /// Copy bytes from the window to a caller supplied buffer.
        /// </summary>
        /// <param name="pack">The file the desired window is stored within.</param>
        /// <param name="position">Position within the file to read from.</param>
        /// <param name="dstbuf">Destination buffer to copy into.</param>
        /// <param name="dstoff">Offset within <see cref="dstbuf"/> to start copying into.</param>
        /// <param name="cnt">
        /// The number of bytes to copy. This value may exceed the number of
        /// bytes remaining in the window starting at offset <see cref="position"/>.
        /// </param>
        /// <returns>
        /// number of bytes actually copied; this may be less than
        /// <see cref="cnt"/> if <see cref="cnt"/> exceeded the number of
        /// bytes available.
        /// </returns>
        /// <remarks>
        /// This cursor does not match the provider or id and the proper 
        /// window could not be acquired through the provider's cache.
        /// </remarks>
        public int copy(PackFile pack, long position, byte[] dstbuf, int dstoff, int cnt)
        {
            long Length = pack.Length;
            int need = cnt;
            while (need > 0 && position < Length)
            {
                Pin(pack, position);
                int r = _window.copy(position, dstbuf, dstoff, need);
                position += r;
                dstoff += r;
                need -= r;
            }
            return cnt - need;
        }

        /// <summary>
        /// Pump bytes into the supplied inflater as input.
        /// </summary>
        /// <param name="pack">The file the desired window is stored within.</param>
        /// <param name="position">Position within the file to read from.</param>
        /// <param name="dstbuf">
        /// Destination buffer the inflater should output decompressed
        /// data to.
        /// </param>
        /// <param name="dstoff">Current offset within <see cref="dstbuf"/> to inflate into.</param>
        /// <returns>
        /// Updated <see cref=dstoff"/> based on the number of bytes
        /// successfully inflated into <see cref=dstbuff"/>.
        /// </returns>
        /// <remarks>
        /// this cursor does not match the provider or id and the proper
        /// window could not be acquired through the provider's cache.
        /// </remarks>
        public int inflate(PackFile pack, long position, byte[] dstbuf, int dstoff)
        {
            if (_inf == null)
            {
                _inf = InflaterCache.Instance.get();
            }
            else
            {
                _inf.Reset();
            }

            while (true)
            {
                Pin(pack, position);
                dstoff = _window.inflate(position, dstbuf, dstoff, _inf);
                if (_inf.IsFinished)
                {
                    return dstoff;
                }
                position = _window.end;
            }
        }

        public void inflateVerify(PackFile pack, long position)
        {
            if (_inf == null)
            {
                _inf = InflaterCache.Instance.get();
            }
            else
            {
                _inf.Reset();
            }

            while (true)
            {
                Pin(pack, position);
                _window.inflateVerify(position, _inf);
                if (_inf.IsFinished)
                {
                    return;
                }
                position = _window.end;
            }
        }

        private void Pin(PackFile pack, long position)
        {
            ByteWindow w = _window;
            if (w == null || !w.contains(pack, position))
            {
                // If memory is low, we may need what is in our window field to
                // be cleaned up by the GC during the get for the next window.
                // So we always clear it, even though we are just going to set
                // it again.
                //
                _window = null;
                _window = WindowCache.get(pack, position);
            }
        }

        /// <summary>
        /// Release the current window cursor.
        /// </summary>
        public void release()
        {
            _window = null;
            try
            {
                InflaterCache.Instance.release(_inf);
            }
            finally
            {
                _inf = null;
            }
        }

        /// <summary>
        /// Release the window cursor.
        /// </summary>
        /// <param name="cursor">cursor to release; may be null.
        /// </param>
        /// <returns>always null</returns>
        public static WindowCursor release(WindowCursor cursor)
        {
            if (cursor != null)
            {
                cursor.release();
            }

            return null;
        }
    }
}
