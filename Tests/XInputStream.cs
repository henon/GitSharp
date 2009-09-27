/*
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
using System.IO;
using GitSharp.Util;

namespace GitSharp.Tests.Util
{
	internal class XInputStream : IDisposable
	{
		private readonly byte[] _intbuf = new byte[8];
		private FileStream _filestream;

		internal XInputStream(FileStream s)
		{
			_filestream = s;
		}

		internal long Length
		{
			get { return _filestream.Length; }
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (_filestream != null)
			{
				_filestream.Close();
				_filestream = null;
			}
		}

		#endregion

		internal byte[] ReadFully(int len)
		{
			var b = new byte[len];
			_filestream.Read(b, 0, len);
			return b;
		}

		internal void ReadFully(byte[] b, int o, int len)
		{
			int r;
			while (len > 0 && (r = _filestream.Read(b, o, len)) > 0)
			{
				o += r;
				len -= r;
			}
			if (len > 0)
			{
				throw new EndOfStreamException();
			}
		}

		internal int ReadUInt8()
		{
			int r = _filestream.ReadByte();
			if (r < 0)
			{
				throw new EndOfStreamException();
			}
			return r;
		}

		internal long ReadUInt32()
		{
			ReadFully(_intbuf, 0, 4);
			return NB.decodeUInt32(_intbuf, 0);
		}

		internal void Close()
		{
			_filestream.Close();
		}
	}
}