/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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
using System.Threading;
using System.IO;

namespace Gitty.Lib
{
    public class WindowedFile
    {
        public WindowedFile(FileInfo packFile)
        {

        }
        public ThreadStart OnOpen { get; set; }

        internal void Close()
        {
            throw new NotImplementedException();
        }

        internal void ReadCompressed(long position, byte[] dstbuf, WindowCursor curs)
        {
            throw new NotImplementedException();
        }

        internal void CopyToStream(long dataOffset, byte[] buf, int cnt, Stream stream, WindowCursor curs)
        {
            throw new NotImplementedException();
        }

        internal void ReadFully(long position, byte[] intbuf, WindowCursor curs)
        {
            throw new NotImplementedException();
        }

        internal int Read(long objectOffset, byte[] buf, int p, int toRead, WindowCursor curs)
        {
            throw new NotImplementedException();
        }

        internal int Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

		internal int Read(long position, byte[] sig, WindowCursor curs)
		{
			throw new NotImplementedException();
		}

		internal string Name
		{
			get { throw new NotImplementedException(); }
		}
	}
}
