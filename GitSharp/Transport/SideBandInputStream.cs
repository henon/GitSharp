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
using System.Text.RegularExpressions;
using GitSharp.Exceptions;
using GitSharp.Util;

namespace GitSharp.Transport
{

    public class SideBandInputStream : Stream
    {
        public const int CH_DATA = 1;
        public const int CH_PROGRESS = 2;
        public const int CH_ERROR = 3;

        private static readonly Regex P_UNBOUNDED = new Regex("^([\\w ]+): (\\d+)( |, done)?.*", RegexOptions.Singleline);
        private static readonly Regex P_BOUNDED = new Regex("^([\\w ]+):.*\\((\\d+)/(\\d+)\\).*", RegexOptions.Singleline);

        private readonly PacketLineIn pckIn;
        private readonly Stream ins;
        private readonly ProgressMonitor monitor;
        private string progressBuffer;
        private string currentTask;
        private int lastCnt;
        private bool eof;
        private int channel;
        private int available;

        public SideBandInputStream(PacketLineIn aPckIn, Stream aIns, ProgressMonitor aProgress)
        {
            pckIn = aPckIn;
            ins = aIns;
            monitor = aProgress;
            currentTask = string.Empty;
        	progressBuffer = string.Empty;
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override int ReadByte()
        {
            needDataPacket();
            if (eof)
                return -1;
            available--;
            return ins.ReadByte();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int r = 0;
            while (count > 0)
            {
                needDataPacket();
                if (eof)
                    break;
                int n = ins.Read(buffer, offset, Math.Min(count, available));
                if (n < 0)
                    break;
                r += n;
                offset += n;
                count -= n;
                available -= n;
            }
            return eof && r == 0 ? -1 : r;
        }

        private void needDataPacket()
        {
            if (eof || (channel == CH_DATA && available > 0))
                return;
            for (;;)
            {
                available = pckIn.ReadLength();
                if (available == 0)
                {
                    eof = true;
                    return;
                }

                channel = ins.ReadByte();
                available -= 5; // length header plus channel indicator
                if (available == 0)
                    continue;

                switch (channel)
                {
                    case CH_DATA:
                        return;
                    case CH_PROGRESS:
                        progress(ReadString(available));
                        continue;
                    case CH_ERROR:
                        eof = true;
                        throw new TransportException("remote: " + ReadString(available));
                    default:
                        throw new TransportException("Invalid channel " + channel);
                }
            }
        }

        private void progress(string pkt)
        {
            pkt = progressBuffer + pkt;
            for (;;)
            {
                int lf = pkt.IndexOf('\n');
                int cr = pkt.IndexOf('\r');
                int s;
                if (0 <= lf && 0 <= cr)
                    s = Math.Min(lf, cr);
                else if (0 <= lf)
                    s = lf;
                else if (0 <= cr)
                    s = cr;
                else
                    break;

                string msg = pkt.Slice(0, s);
                if (doProgressLine(msg))
                    pkt = pkt.Substring(s + 1);
                else
                    break;
            }
            progressBuffer = pkt;
        }

        private bool doProgressLine(string msg)
        {
            Match matcher = P_BOUNDED.Match(msg);
            if (matcher.Success)
            {
                string taskname = matcher.Groups[1].Value;
                if (!currentTask.Equals(taskname))
                {
                    currentTask = taskname;
                    lastCnt = 0;
                    int tot = int.Parse(matcher.Groups[3].Value);
                    monitor.BeginTask(currentTask, tot);
                }
                int cnt = int.Parse(matcher.Groups[2].Value);
                monitor.Update(cnt - lastCnt);
                lastCnt = cnt;
                return true;
            }

            matcher = P_UNBOUNDED.Match(msg);
            if (matcher.Success)
            {
                string taskname = matcher.Groups[1].Value;
                if (!currentTask.Equals(taskname))
                {
                    currentTask = taskname;
                    lastCnt = 0;
                    monitor.BeginTask(currentTask, ProgressMonitor.UNKNOWN);
                }
                int cnt = int.Parse(matcher.Groups[2].Value);
                monitor.Update(cnt - lastCnt);
                lastCnt = cnt;
                return true;
            }

            return false;
        }

        private string ReadString(int len)
        {
            var raw = new byte[len];
            NB.ReadFully(ins, raw, 0, len);
            return Constants.CHARSET.GetString(raw, 0, len);
        }
    }
}