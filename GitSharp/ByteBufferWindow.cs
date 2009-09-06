/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
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
using ICSharpCode.SharpZipLib.Zip.Compression;
using System.IO;

namespace GitSharp
{

    /**
     * A window for accessing git packs using a {@link ByteBuffer} for storage.
     *
     * @see ByteWindow
     */
    internal class ByteBufferWindow : ByteWindow
    {
        private Stream _stream;

        public ByteBufferWindow(PackFile pack, long o, Stream b)
            : base(pack, o, b.Length)
        {
            _stream = b;
        }


        internal override int copy(int p, byte[] b, int o, int n)
        {
            _stream.Position=(p);
            n = (int)Math.Min(_stream.Length - p, n);
            _stream.Read(b, o, n);
            return n;
        }


        internal override int Inflate(int pos, byte[] b, int o, Inflater inf)
        {
            byte[] tmp = new byte[512];
            var s = _stream;
            s.Position=pos;
            while ((s.Length-s.Position) > 0 && !inf.IsFinished)
            {
                if (inf.IsNeedingInput)
                {
                    int n = (int)Math.Min((s.Length - s.Position), tmp.Length);
                    s.Read(tmp, 0, n);
                    inf.SetInput(tmp, 0, n);
                }
                o += inf.Inflate(b, o, b.Length - o);
            }
            while (!inf.IsFinished && !inf.IsNeedingInput)
                o += inf.Inflate(b, o, b.Length - o);
            return o;
        }


        internal override void inflateVerify(int pos, Inflater inf)
        {
            byte[] tmp = new byte[512];
            var s = _stream;
            s.Position=(pos);
            while ((s.Length - s.Position) > 0 && !inf.IsFinished)
            {
                if (inf.IsNeedingInput)
                {
                    int n = (int)Math.Min((s.Length - s.Position), tmp.Length);
                    s.Read(tmp, 0, n);
                    inf.SetInput(tmp, 0, n);
                }
                inf.Inflate(verifyGarbageBuffer, 0, verifyGarbageBuffer.Length);
            }
            while (!inf.IsFinished && !inf.IsNeedingInput)
                inf.Inflate(verifyGarbageBuffer, 0, verifyGarbageBuffer.Length);
        }
    }
}
