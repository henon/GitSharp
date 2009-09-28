/*
 * Copyright (C) 2008, Google Inc.
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
using System.Text;

namespace GitSharp.Core.Transport
{

    public class SideBandProgressMonitor : ProgressMonitor
    {
        private readonly StreamWriter writer;
        private bool output;
        private DateTime taskBeganAt;
        private DateTime lastOutput;
        private string msg;
        private int lastWorked;
        private int totalWork;

        public SideBandProgressMonitor(PacketLineOut pckOut)
        {
            int bufsz = SideBandOutputStream.SMALL_BUF - SideBandOutputStream.HDR_SIZE;
            writer = new StreamWriter(new BufferedStream(new SideBandOutputStream(SideBandOutputStream.CH_PROGRESS, pckOut), bufsz), Constants.CHARSET);
        }

        public override void Start(int totalTasks)
        {
            taskBeganAt = DateTime.Now;
            lastOutput = taskBeganAt;
        }

        public override void BeginTask(string title, int total)
        {
            EndTask();
            msg = title;
            lastWorked = 0;
            totalWork = total;
        }

        public override void Update(int completed)
        {
            if (msg == null)
                return;

            int cmp = lastWorked + completed;
            DateTime now = DateTime.Now;
            if (!output && (now - taskBeganAt).TotalMilliseconds < 500)
                return;
            if (totalWork < 0)
            {
                if ((now - lastOutput).TotalMilliseconds >= 500)
                {
                    display(cmp, null);
                    lastOutput = now;
                }
            }
            else
            {
                if ((cmp * 100 / totalWork) != (lastWorked * 100) / totalWork || (now - lastOutput).TotalMilliseconds >= 500)
                {
                    display(cmp, null);
                    lastOutput = now;
                }
            }
            lastWorked = cmp;
            output = true;
        }

        private void display(int cmp, string eol)
        {
            StringBuilder m = new StringBuilder();
            m.Append(msg);
            m.Append(": ");

            if (totalWork < 0)
            {
                m.Append(cmp);
            }
            else
            {
                int pcnt = (cmp*100/totalWork);
                if (pcnt < 100)
                    m.Append(' ');
                if (pcnt < 10)
                    m.Append(' ');
                m.Append(pcnt);
                m.Append("% (");
                m.Append(cmp);
                m.Append("/");
                m.Append(totalWork);
                m.Append(")");
            }
            if (eol != null)
                m.Append(eol);
            else
            {
                m.Append("   \r");
            }
            writer.Write(m.ToString());
            writer.Flush();
        }

        public override bool IsCancelled
        {
            get { return false; }
        }

        public override void EndTask()
        {
            if (output)
            {
                if (totalWork < 0)
                    display(lastWorked, ", done\n");
                else
                    display(totalWork, "\n");
            }
            output = false;
            msg = null;
        }
    }

}