/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
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
using System.IO;
using GitSharp.Exceptions;
using GitSharp.Util;

namespace GitSharp.Transport
{

    public class PacketLineIn
    {
        public enum AckNackResult
        {
            NAK,
            ACK,
            ACK_CONTINUE
        }

        private readonly Stream ins;
        private readonly byte[] lenbuffer;

        public PacketLineIn(Stream i)
        {
            ins = i;
            lenbuffer = new byte[4];
        }

        public Stream sideband(IProgressMonitor pm)
        {
            return new SideBandInputStream(this, ins, pm);
        }

        public AckNackResult readACK(MutableObjectId returnedId)
        {
            string line = ReadString();
            if (line == string.Empty)
                throw new PackProtocolException("Expected ACK/NAK, found EOF");
            if ("NAK".Equals(line))
                return AckNackResult.NAK;
            if (line.StartsWith("ACK "))
            {
                returnedId.FromString(line.Slice(4, 44));
                if (line.IndexOf("continue", 44) != -1)
                {
                    return AckNackResult.ACK_CONTINUE;
                }
                return AckNackResult.ACK;
            }
            throw new PackProtocolException("Expected ACK/NAK, got: " + line);
        }

        public string ReadString()
        {
            int len = ReadLength();
            if (len == 0)
                return string.Empty;

            len -= 4; // length header (4 bytes)
            
            if (len <= 0)
                return string.Empty;

            byte[] raw = new byte[len];
            NB.ReadFully(ins, raw, 0, len);
            if (raw[len - 1] == '\n')
                len--;
            return RawParseUtils.decode(Constants.CHARSET, raw, 0, len);
        }

        public string ReadStringRaw()
        {
            int len = ReadLength();
            if (len == 0)
                return string.Empty;

            len -= 4; // length header (4 bytes)
            if (len == 0)
                return string.Empty;

            byte[] raw = new byte[len];
            NB.ReadFully(ins, raw, 0, len);
            return RawParseUtils.decode(Constants.CHARSET, raw, 0, len);
        }

        public int ReadLength()
        {
            NB.ReadFully(ins, lenbuffer, 0, 4);
            try
            {
                int len = RawParseUtils.parseHexInt16(lenbuffer, 0);
                if (len != 0 && len < 4)
                    throw new IndexOutOfRangeException();
                return len;
            }
            catch (IndexOutOfRangeException)
            {
                throw new IOException("Invalid packet line header: " + (char) lenbuffer[0] +
                                                    (char) lenbuffer[1] + (char) lenbuffer[2] + (char) lenbuffer[3]);
            }
        }
    }


}