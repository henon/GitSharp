/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
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
using Gitty.Core.Util;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace Gitty.Core
{
    abstract class ByteWindow<T> : WeakReference<T> {
	readonly WindowedFile provider;

	readonly int id;

	readonly int size;

	int lastAccessed;

	readonly long start;

	readonly long end;

	/**
	 * Constructor for ByteWindow.
	 * 
	 * @param o
	 *            the WindowedFile providing data access
	 * @param pos
	 *            the position in the file the data comes from.
	 * @param d
	 *            an id provided by the WindowedFile. See
	 *            {@link WindowCache#get(WindowCursor, WindowedFile, long)}.
	 * @param ref
	 *            the object value required to perform data access.
	 * @param sz
	 *            the total number of bytes in this window.
	 */
	
	ByteWindow(WindowedFile o, long pos, int d, T refT, int sz) 
//        : base(refT, (ReferenceQueue<T>) WindowCache.clearedWindowQueue)
        : base (refT)
    {
        throw new NotImplementedException("the above constructor is not right because .net has no equivelent to SoftReferences");

		provider = o;
		size = sz;
		id = d;
		start = pos;
		end = start + size;
	}

	bool Contains(WindowedFile neededFile, long neededPos) 
    {
		return provider == neededFile && start <= neededPos && neededPos < end;
	}

	/**
	 * Copy bytes from the window to a caller supplied buffer.
	 * 
	 * @param ref
	 *            the object value required to perform data access.
	 * @param pos
	 *            offset within the file to start copying from.
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
	 */
	int Copy(T refT, long pos, byte[] dstbuf, int dstoff, int cnt) 
    {
		return Copy(refT, (int) (pos - start), dstbuf, dstoff, cnt);
	}

	/**
	 * Copy bytes from the window to a caller supplied buffer.
	 * 
	 * @param ref
	 *            the object value required to perform data access.
	 * @param pos
	 *            offset within the window to start copying from.
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
	 */
	public abstract int Copy(T refT, int pos, byte[] dstbuf, int dstoff, int cnt);

	/**
	 * Pump bytes into the supplied inflater as input.
	 * 
	 * @param ref
	 *            the object value required to perform data access.
	 * @param pos
	 *            offset within the window to start supplying input from.
	 * @param dstbuf
	 *            destination buffer the inflater should output decompressed
	 *            data to.
	 * @param dstoff
	 *            current offset within <code>dstbuf</code> to inflate into.
	 * @param inf
	 *            the inflater to feed input to. The caller is responsible for
	 *            initializing the inflater as multiple windows may need to
	 *            supply data to the same inflater to completely decompress
	 *            something.
	 * @return updated <code>dstoff</code> based on the number of bytes
	 *         successfully copied into <code>dstbuf</code> by
	 *         <code>inf</code>. If the inflater is not yet finished then
	 *         another window's data must still be supplied as input to finish
	 *         decompression.
	 * @throws DataFormatException
	 *             the inflater encountered an invalid chunk of data. Data
	 *             stream corruption is likely.
	 */
	int Inflate(T refT, long pos, byte[] dstbuf, int dstoff, Inflater inf)
    {
		return Inflate(refT, (int) (pos - start), dstbuf, dstoff, inf);
	}

	/**
	 * Pump bytes into the supplied inflater as input.
	 * 
	 * @param ref
	 *            the object value required to perform data access.
	 * @param pos
	 *            offset within the window to start supplying input from.
	 * @param dstbuf
	 *            destination buffer the inflater should output decompressed
	 *            data to.
	 * @param dstoff
	 *            current offset within <code>dstbuf</code> to inflate into.
	 * @param inf
	 *            the inflater to feed input to. The caller is responsible for
	 *            initializing the inflater as multiple windows may need to
	 *            supply data to the same inflater to completely decompress
	 *            something.
	 * @return updated <code>dstoff</code> based on the number of bytes
	 *         successfully copied into <code>dstbuf</code> by
	 *         <code>inf</code>. If the inflater is not yet finished then
	 *         another window's data must still be supplied as input to finish
	 *         decompression.
	 * @throws DataFormatException
	 *             the inflater encountered an invalid chunk of data. Data
	 *             stream corruption is likely.
	 */
	public abstract int Inflate(T refT, int pos, byte[] dstbuf, int dstoff, Inflater inf);
}
}
