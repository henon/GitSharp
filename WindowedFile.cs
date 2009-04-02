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

namespace Gitty.Core
{
    public class WindowedFile
    {
	FileInfo fPath;
	FileStream fs;

        public WindowedFile(FileInfo packFile)
        {
	    fPath = packFile;
	    Length = -1;
        }

        public ThreadStart OnOpen { get; set; }

	public string Name {
	    get {
		return fPath.FullName;
	    }
	}
	
        public long Length { get; internal set; } 

        internal void Close()
        {
	    WindowCache.Purge (this);
	    Length = -1;
        }

        internal void ReadCompressed(long position, byte[] dstbuf, WindowCursor curs)
	{
	    Inflater inf = InflaterCache.GetInflater ();
	    try {
		if (curs.Inflate (this, position, dstbuf, 0, inf) != dstbuf.Length)
		    throw new IOException ("Short compressed stream at " + position);
	    } finally {
		InflaterCache.Release (inf);
	    }
        }

        internal void CopyToStream (long position, byte[] buf, int cnt, Stream outStream, WindowCursor curs)
        {
	    while (cnt > 0){
		int toRead = (int) Math.Min (cnt, buf.Length);
		int read = Read (position, buf, 0, toRead, curs);
		if (read != toRead)
		    throw new IOException ("End of File");
		position += read;
		cnt -= read;
		outStream.Write (buf, 0, read);
	    }
        }

        internal void ReadFully(long position, byte[] dstbuf, WindowCursor curs)
        {
	    if (Read (position, dstbuf, 0, dstbuf.Length, curs) != dstbuf.Length)
		throw new IOException("EOF");
        }

        internal int Read (long position, byte[] dstbuf, int dstoff, int cnt, WindowCursor curs)
        {
	    return curs.Copy (this, position, dstbuf, dstoff, cnt);
        }


	internal int Read (long position, byte[] dstbuf, WindowCursor curs)
	{
	    return Read (position, dstbuf, 0, dstbuf.Length, curs);
	}

	internal void CacheOpen ()
	{
	    fs = fPath.OpenRead ();
	    Length = fPath.Length;
	    try {
		OnOpen ();
	    } catch {
		CacheClose ();
		throw;
	    }
	}

	void CacheClose ()
	{
	    try {
		fs.Close ();
	    } catch {}
	    fs = null;
	    Length = -1;
	}
	    
	internal void LoadWindow (WindowCursor curs, int windowId, long pos, int windowSize)
	{
	    byte [] b = new byte [windowSize];

	    fs.Position = pos;
	    fs.Read (b, 0, b.Length);
	    curs.Window = new ByteArrayWindow (this, pos, windowId, b);
	    curs.Handle = b;
	}
	
	public override string ToString ()
	{
	    return "WindowedFile[" + Name + "]";
	}
    }
}
