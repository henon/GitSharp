/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
 * Copyrigth (C) 2009, Henon <meinrad.recheis@gmail.com>
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using GitSharp.Exceptions;
using GitSharp.Util;
using Winterdom.IO.FileMap;

namespace GitSharp
{
	/// <summary>
	/// A Git version 2 pack file representation. A pack file contains Git objects in
	/// delta packed format yielding high compression of lots of object where some
	/// objects are similar.
	/// </summary>
	public class PackFile : IEnumerable<PackIndex.MutableEntry>
	{
		/// <summary>
		/// Sorts PackFiles to be most recently created to least recently created.
		/// </summary>
		internal static readonly Comparison<PackFile> PackFileSortComparison = (a, b) => b._packLastModified - a._packLastModified;
		private volatile bool _invalid;

		private readonly FileInfo _idxFile;
		private readonly int _packLastModified;
		private readonly int _hash;

		private MemoryMappedFile _fdMap;
		private FileStream _fd;
		private int _activeWindows;
		private int _activeCopyRawData;
		private PackReverseIndex _reverseIndex;
		private byte[] _packChecksum;
		private PackIndex _loadedIdx;

		/// <summary>
		/// Construct a Reader for an existing, pre-indexed packfile.
		/// </summary>
		/// <param name="idxFile">path of the <code>.idx</code> file listing the contents.</param>
		/// <param name="packFile">path of the <code>.pack</code> file holding the data.</param>
		public PackFile(FileInfo idxFile, FileInfo packFile)
		{
			_idxFile = idxFile;
			File = packFile;

			// [henon] why the heck right shift by 10 ?? ... seems to have to do with the SORT comparison
			_packLastModified = (int)(packFile.LastAccessTime.Ticks >> 10);

			// Multiply by 31 here so we can more directly combine with another
			// value in WindowCache.hash(), without doing the multiply there.
			//
			_hash = GetHashCode() * 31;

			Length = long.MaxValue;
			//ReadPackHeader();
		}

		public PackFile(string idxFile, string packFile)
			: this(new FileInfo(idxFile), new FileInfo(packFile))
		{
		}

		// [henon]: was getPackFile()
		public FileInfo File { get; private set; }

		public long Length { get; private set; }

		/// <summary>
		/// Obtain the total number of objects available in this pack. This method
		/// relies on pack index, giving number of effectively available objects.
		/// </summary>
		/// <remarks>
		/// Returns the number of objects in index of this pack, likewise in this pack.
		/// </remarks>
		public long ObjectCount
		{
			get { return LoadPackIndex().ObjectCount; }
		}

		public bool SupportsFastCopyRawData
		{
			get { return LoadPackIndex().HasCRC32Support; }
		}

		internal bool IsInvalid
		{
			get { return _invalid; }
		}

		internal int Hash
		{
			get { return _hash; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="curs"></param>
		/// <param name="offset"></param>
		/// <returns>
		/// The file object which locates this pack on disk.
		/// </returns>
		internal PackedObjectLoader ResolveBase(WindowCursor curs, long offset)
		{
			return Reader(curs, offset);
		}

		/// <summary>
		/// * Determine if an object is contained within the pack file.
		/// <para>
		/// For performance reasons only the index file is searched; the main pack
		/// content is ignored entirely.
		/// </para>
		/// </summary>
		/// <param name="id">The object to look for. Must not be null.</param>
		/// <returns>True if the object is in this pack; false otherwise.</returns>
		public bool HasObject(AnyObjectId id)
		{
			return LoadPackIndex().HasObject(id);
		}

		/// <summary>
		/// Get an object from this pack.
		/// </summary>
		/// <param name="curs">temporary working space associated with the calling thread.</param>
		/// <param name="id">the object to obtain from the pack. Must not be null.</param>
		/// <returns>
		/// The object loader for the requested object if it is contained in
		/// this pack; null if the object was not found.
		/// </returns>
		public PackedObjectLoader Get(WindowCursor curs, AnyObjectId id)
		{
			if (id == null) return null;
			long offset = LoadPackIndex().FindOffset(id);
			return 0 < offset ? Reader(curs, offset) : null;
		}

		/// <summary>
		/// Close the resources utilized by this repository.
		/// </summary>
		public void Close()
		{
			UnpackedObjectCache.purge(this);
			WindowCache.Purge(this);

			lock (this)
			{
				_loadedIdx = null;
			}
		}

		#region IEnumerable Implementation

		public IEnumerator<PackIndex.MutableEntry> GetEnumerator()
		{
			return LoadPackIndex().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return LoadPackIndex().GetEnumerator();
		}

		#endregion

		/// <summary>
		/// Search for object id with the specified start offset in associated pack
		/// (reverse) index.
		/// </summary>
		/// <param name="offset">start offset of object to find</param>
		/// <returns>
		/// Object id for this offset, or null if no object was found
		/// </returns>
		public ObjectId FindObjectForOffset(long offset)
		{
			return GetReverseIdx().FindObject(offset);
		}

		public UnpackedObjectCache.Entry readCache(long position)
		{
			return UnpackedObjectCache.get(this, position);
		}

		public void saveCache(long position, byte[] data, int type)
		{
			UnpackedObjectCache.store(this, position, data, type);
		}

		public byte[] decompress(long position, long totalSize, WindowCursor curs)
		{
			var dstbuf = new byte[totalSize];

			if (curs.Inflate(this, position, dstbuf, 0) != totalSize)
			{
				throw new EndOfStreamException("Short compressed stream at " + position);
			}

			return dstbuf;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public void beginCopyRawData()
		{
			if (++_activeCopyRawData == 1 && _activeWindows == 0)
			{
				DoOpen();
			}
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public void endCopyRawData()
		{
			if (--_activeCopyRawData == 0 && _activeWindows == 0)
			{
				DoClose();
			}
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public bool beginWindowCache()
		{
			if (++_activeWindows == 1)
			{
				if (_activeCopyRawData == 0)
				{
					DoOpen();
				}
				return true;
			}

			return false;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public bool endWindowCache()
		{
			bool r = --_activeWindows == 0;
			if (r && _activeCopyRawData == 0)
			{
				DoClose();
			}
			return r;
		}

		internal ByteArrayWindow Read(long pos, int size)
		{
			if (Length < pos + size)
			{
				size = (int)(Length - pos);
			}

			var buf = new byte[size];
			NB.ReadFully(_fd, pos, buf, 0, size);
			return new ByteArrayWindow(this, pos, buf);
		}

		internal ByteWindow MemoryMappedByteWindow(long pos, int size)
		{
			if (Length < pos + size)
			{
				size = (int)(Length - pos);
			}

			Stream map;

			try
			{
				_fdMap = MemoryMappedFile.Create(File.FullName, MapProtection.PageReadOnly);
				map = _fdMap.MapView(MapAccess.FileMapRead, pos, size); // was: map = _fd.map(MapMode.READ_ONLY, pos, size);
			}
			catch (IOException)
			{
				// The most likely reason this failed is the process has run out
				// of virtual memory. We need to discard quickly, and try to
				// force the GC to finalize and release any existing mappings.
				//
				GC.Collect();
				GC.WaitForPendingFinalizers();
				map = _fdMap.MapView(MapAccess.FileMapRead, pos, size);
			}

			return new ByteBufferWindow(this, pos, map);
		}

		internal void CopyRawData<T>(PackedObjectLoader loader, T @out, byte[] buf, WindowCursor cursor)
			where T : Stream
		{
			long objectOffset = loader.ObjectOffset;
			long dataOffset = loader.DataOffset;
			var cnt = (int)(FindEndOffset(objectOffset) - dataOffset);

			if (LoadPackIndex().HasCRC32Support)
			{
				var crc = new Crc32();
				var headerCnt = (int)(dataOffset - objectOffset);
				while (headerCnt > 0)
				{
					int toRead = Math.Min(headerCnt, buf.Length);
					ReadFully(objectOffset, buf, 0, toRead, cursor);
					crc.Update(buf, 0, toRead);
					headerCnt -= toRead;
				}
				var crcOut = new CheckedOutputStream(@out, crc);
				CopyToStream(dataOffset, buf, cnt, crcOut, cursor);
				long computed = crc.Value;
				ObjectId id = FindObjectForOffset(objectOffset);
				long expected = LoadPackIndex().FindCRC32(id);
				if (computed != expected)
				{
					throw new CorruptObjectException("object at " + dataOffset + " in " + File.FullName + " has bad zlib stream");
				}
			}
			else
			{
				try
				{
					cursor.InflateVerify(this, dataOffset);
				}
				catch (Exception fe) // [henon] was DataFormatException
				{
					throw new CorruptObjectException("object at " + dataOffset + " in " + File.FullName + " has bad zlib stream", fe);
				}

				CopyToStream(dataOffset, buf, cnt, @out, cursor);
			}
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		private PackIndex LoadPackIndex()
		{
			if (_loadedIdx == null)
			{
				if (_invalid)
				{
					throw new PackInvalidException(File.FullName);
				}

				try
				{
					PackIndex idx = PackIndex.Open(_idxFile);

					if (_packChecksum == null)
					{
						_packChecksum = idx.packChecksum;
					}
					else if (_packChecksum.SequenceEqual(idx.packChecksum))
					{
						throw new PackMismatchException("Pack checksum mismatch");
					}

					_loadedIdx = idx;
				}
				catch (IOException)
				{
					_invalid = true;
					throw;
				}
			}

			return _loadedIdx;
		}

		private void ReadFully(long position, byte[] dstbuf, int dstoff, int cnt, WindowCursor curs)
		{
			if (curs.Copy(this, position, dstbuf, dstoff, cnt) != cnt)
			{
				throw new EndOfStreamException();
			}
		}

		private void CopyToStream(long position, byte[] buffer, long count, Stream stream, WindowCursor windowCursor)
		{
			while (count > 0)
			{
				var toRead = (int)Math.Min(count, buffer.Length);
				ReadFully(position, buffer, 0, toRead, windowCursor);
				position += toRead;
				count -= toRead;
				stream.Write(buffer, 0, toRead);
			}
		}

		private void DoOpen()
		{
			try
			{
				if (_invalid)
				{
					throw new PackInvalidException(File.FullName);
				}

				_fd = new FileStream(File.FullName, System.IO.FileMode.Open, FileAccess.Read);
				Length = File.Length;
				OnOpenPack();
			}
			catch (Exception)
			{
				OpenFail();
				throw;
			}
		}

		private void OpenFail()
		{
			_activeWindows = 0;
			_activeCopyRawData = 0;
			_invalid = true;
			DoClose();
		}

		private void DoClose()
		{
			if (_fd == null) return;

			try
			{
				_fd.Close();
			}
			catch (IOException)
			{
				// Ignore a close event. We had it open only for reading.
				// There should not be errors related to network buffers
				// not flushed, etc.
			}

			_fd = null;
		}

		private void OnOpenPack()
		{
			PackIndex idx = LoadPackIndex();
			var buf = new byte[20];

			NB.ReadFully(_fd, 0, buf, 0, 12);
			if (RawParseUtils.match(buf, 0, Constants.PACK_SIGNATURE) != 4)
			{
				throw new IOException("Not a PACK file.");
			}

			long vers = NB.decodeUInt32(buf, 4);
			long packCnt = NB.decodeUInt32(buf, 8);
			if (vers != 2 && vers != 3)
			{
				throw new IOException("Unsupported pack version " + vers + ".");
			}

			if (packCnt != idx.ObjectCount)
			{
				throw new PackMismatchException("Pack object count mismatch:"
												+ " pack " + packCnt
												+ " index " + idx.ObjectCount
												+ ": " + File.FullName);
			}

			NB.ReadFully(_fd, Length - 20, buf, 0, 20);

			if (!buf.SequenceEqual(_packChecksum))
			{
				throw new PackMismatchException("Pack checksum mismatch:"
												+ " pack " + ObjectId.FromRaw(buf)
												+ " index " + ObjectId.FromRaw(idx.packChecksum)
												+ ": " + File.FullName);
			}
		}

		private PackedObjectLoader Reader(WindowCursor curs, long objOffset)
		{
			long pos = objOffset;
			int p = 0;
			byte[] ib = curs.TempId; // Reader.ReadBytes(ObjectId.ObjectIdLength);
			ReadFully(pos, ib, 0, 20, curs);
			int c = ib[p++] & 0xff;
			int typeCode = (c >> 4) & 7;
			long dataSize = c & 15;
			int shift = 4;
			while ((c & 0x80) != 0)
			{
				c = ib[p++] & 0xff;
				dataSize += (c & 0x7f) << shift;
				shift += 7;
			}
			pos += p;

			switch (typeCode)
			{
				case Constants.OBJ_COMMIT:
				case Constants.OBJ_TREE:
				case Constants.OBJ_BLOB:
				case Constants.OBJ_TAG:
					return new WholePackedObjectLoader(this, pos, objOffset, typeCode, (int)dataSize);

				case Constants.OBJ_OFS_DELTA:
					ReadFully(pos, ib, 0, 20, curs);
					p = 0;
					c = ib[p++] & 0xff;
					long ofs = c & 127;
					while ((c & 0x128) != 0)
					{
						ofs += 1;
						c = ib[p++] & 0xff;
						ofs <<= 7;
						ofs += (c & 127);
					}
					return new DeltaOfsPackedObjectLoader(this, pos + p, objOffset, (int)dataSize, objOffset - ofs);

				case Constants.OBJ_REF_DELTA:
					ReadFully(pos, ib, 0, 20, curs);
					return new DeltaRefPackedObjectLoader(this, pos + ib.Length, objOffset, (int)dataSize, ObjectId.FromRaw(ib));

				default:
					throw new IOException("Unknown object type " + typeCode + ".");
			}
		}

		private long FindEndOffset(long startOffset)
		{
			long maxOffset = Length - 20;
			return GetReverseIdx().FindNextOffset(startOffset, maxOffset);
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		private PackReverseIndex GetReverseIdx()
		{
			if (_reverseIndex == null)
			{
				_reverseIndex = new PackReverseIndex(LoadPackIndex());
			}

			return _reverseIndex;
		}
	}
}