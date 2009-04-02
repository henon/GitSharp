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
using Gitty.Core.Exceptions;
using System.IO;

namespace Gitty.Core
{
    public class UnpackedObjectLoader : ObjectLoader 
    {
	long objectSize;
	byte [] bytes;
	ObjectType objectType;
	
        public UnpackedObjectLoader(Repository repo, ObjectId objectId)
		: this (ReadCompressed (repo, objectId), objectId)
        {
        }

	public UnpackedObjectLoader (byte [] compressed) : this (compressed, null)
	{
	}
	
	public UnpackedObjectLoader (byte [] compressed, ObjectId id)
	{
	    Id = id;
	    // Try to determine if this is a legacy format loose object or
	    // a new style loose object. The legacy format was completely
	    // compressed with zlib so the first byte must be 0x78 (15-bit
	    // window size, deflated) and the first 16 bit word must be
	    // evenly divisible by 31. Otherwise its a new style loose
	    // object.
	    //
	    Inflater inflater = InflaterCache.GetInflater ();
	    try {
		int fb = compressed [0] & 0xff;
		
		if (fb == 0x78 && (((fb << 8) | compressed [1] & 0xff) % 31) == 0) {
		    inflater.SetInput (compressed);
		    byte[] hdr = new byte [64];
		    int avail = 0;

		    while (!inflater.IsFinished && avail < hdr.Length){
			try {
			    avail += inflater.Inflate (hdr, avail, hdr.Length - avail);
			} catch (Exception inn) {
			    throw new CorruptObjectException(id, "bad stream", inn);
			}
		    }
		    
		    if (avail < 5)
			throw new CorruptObjectException(id, "no header");

		    int p = 0;
		    objectType = Codec.DecodeTypeString (id, hdr, (byte) ' ', ref p);
		    objectSize = RawParseUtils.ParseBase10 (hdr, ref p);
		    if (objectSize < 0)
			throw new CorruptObjectException(id, "negative size");
		    if (hdr [p++] != 0)
			throw new CorruptObjectException(id, "garbage after size");
		    bytes = new byte [objectSize];
		    if (p < avail)
			Array.Copy (hdr, p, bytes, 0, avail - p);
		    Decompress (id, inflater, avail - p);
		} else {
		    int p = 0;
		    int c = compressed [p++] & 0xff;
		    ObjectType typeCode = (ObjectType) ((c >> 4) & 7);
		    int size = c & 15;
		    int shift = 4;
		    while ((c & 0x80) != 0) {
			c = compressed[p++] & 0xff;
			size += (c & 0x7f) << shift;
			shift += 7;
		    }
		    
		    switch (typeCode) {
		    case ObjectType.Commit:
		    case ObjectType.Tree:
		    case ObjectType.Blob:
		    case ObjectType.Tag:
			objectType = typeCode;
			break;
		    default:
			throw new CorruptObjectException(id, "invalid type");
		    }
		    
		    objectSize = size;
		    bytes = new byte[objectSize];
		    inflater.SetInput (compressed, p, compressed.Length - p);
		    Decompress (id, inflater, 0);
		}
	    } finally {
		InflaterCache.Release(inflater);
	    }
	}

	void Decompress (ObjectId id, Inflater inf, int p)
	{
	    try {
		while (!inf.IsFinished){
		    p += inf.Inflate (bytes, p, (int) objectSize - p);
		}
	    } catch (Exception e) {
		throw new CorruptObjectException (id, "bad stream", e);
	    }
	    if (p != objectSize)
		throw new CorruptObjectException (id, "Invalid Length");
	}
	    
	static byte [] ReadCompressed (Repository db, ObjectId id)
	{
	    byte [] compressed;
	    
	    using (FileStream objStream = db.ToFile (id).OpenRead ()){
		compressed = new byte [objStream.Length];
		objStream.Read (compressed, 0, (int) objStream.Length);
	    }
	    return compressed;
	}
	
        public override ObjectType ObjectType {
	    get { return objectType; }
	}
	
        public override long Size {
	    get  { return objectSize; }
	}

        public override byte[] Bytes {
	    get { return bytes; }
        }

        public override byte[] CachedBytes {
            get { return bytes; } 
        }

        public override ObjectType RawType {
            get { return objectType; }
        }

        public override long RawSize {
            get { return objectSize; }
        }
    }
}
