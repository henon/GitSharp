using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Gitty.Exceptions;
using Gitty.Util;
using ICSharpCode.SharpZipLib.Checksums;

namespace Gitty.Lib
{
	public class PackFile : IEnumerable<PackIndex.MutableEntry>
	{
		public sealed class Constants
		{
			public static readonly string PackSignature = "PACK";
		}
		private WindowedFile pack;

		private PackIndex idx;

		private PackReverseIndex reverseIdx;

		/**
		 * Construct a reader for an existing, pre-indexed packfile.
		 * 
		 * @param parentRepo
		 *            Git repository holding this pack file
		 * @param idxFile
		 *            path of the <code>.idx</code> file listing the contents.
		 * @param packFile
		 *            path of the <code>.pack</code> file holding the data.
		 * @throws IOException
		 *             the index file cannot be accessed at this time.
		 */
		public PackFile(Repository parentRepo, FileInfo idxFile, FileInfo packFile)
		{
			pack = new WindowedFile(packFile);
			pack.OnOpen = delegate()
			{
				ReadPackHeader();
			};

			try
			{
				idx = PackIndex.Open(idxFile);
			}
			catch (IOException ioe)
			{
				throw ioe;
			}
		}

		PackedObjectLoader ResolveBase(WindowCursor curs, long ofs)
		{
			return Reader(curs, ofs);
		}

		/**
		 * Determine if an object is contained within the pack file.
		 * <p>
		 * For performance reasons only the index file is searched; the main pack
		 * content is ignored entirely.
		 * </p>
		 * 
		 * @param id
		 *            the object to look for. Must not be null.
		 * @return true if the object is in this pack; false otherwise.
		 */
		public bool HasObject(AnyObjectId id)
		{
			return idx.HasObject(id);
		}

		/**
		 * Get an object from this pack.
		 * 
		 * @param id
		 *            the object to obtain from the pack. Must not be null.
		 * @return the object loader for the requested object if it is contained in
		 *         this pack; null if the object was not found.
		 * @throws IOException
		 *             the pack file or the index could not be read.
		 */
		public PackedObjectLoader Get(AnyObjectId id)
		{
			return Get(new WindowCursor(), id);
		}

		/**
		 * Get an object from this pack.
		 * 
		 * @param curs
		 *            temporary working space associated with the calling thread.
		 * @param id
		 *            the object to obtain from the pack. Must not be null.
		 * @return the object loader for the requested object if it is contained in
		 *         this pack; null if the object was not found.
		 * @throws IOException
		 *             the pack file or the index could not be read.
		 */
		public PackedObjectLoader Get(WindowCursor curs, AnyObjectId id)
		{
			long offset = idx.FindOffset(id);
			if (offset == -1)
				return null;
			PackedObjectLoader objReader = Reader(curs, offset);
			objReader.Id = id.ToObjectId();
			return objReader;
		}

		/**
		 * Close the resources utilized by this repository
		 */
		public void Close()
		{
			UnpackedObjectCache.Purge(pack);
			pack.Close();
		}



		/**
		 * Obtain the total number of objects available in this pack. This method
		 * relies on pack index, giving number of effectively available objects.
		 * 
		 * @return number of objects in index of this pack, likewise in this pack
		 */
		long GetObjectCount()
		{
			return idx.ObjectCount;
		}

		/**
		 * Search for object id with the specified start offset in associated pack
		 * (reverse) index.
		 *
		 * @param offset
		 *            start offset of object to find
		 * @return object id for this offset, or null if no object was found
		 */
		ObjectId FindObjectForOffset(long offset)
		{
			return GetReverseIdx().FindObject(offset);
		}

		public UnpackedObjectCache.Entry ReadCache(long position)
		{
			return UnpackedObjectCache.Get(pack, position);
		}

		public void SaveCache(long position, byte[] data, ObjectType type)
		{
			UnpackedObjectCache.Store(pack, position, data, type);
		}

		public byte[] Decompress(long position, int totalSize, WindowCursor curs)
		{
			byte[] dstbuf = new byte[totalSize];
			pack.ReadCompressed(position, dstbuf, curs);
			return dstbuf;
		}

		public void CopyRawData(PackedObjectLoader loader, Stream stream, byte[] buf)
		{
			long objectOffset = loader.objectOffset;
			long dataOffset = loader.DataOffset;
			int cnt = (int)(FindEndOffset(objectOffset) - dataOffset);
			WindowCursor curs = loader.curs;

			if (idx.HasCRC32Support())
			{
				Crc32 crc = new Crc32();
				int headerCnt = (int)(dataOffset - objectOffset);
				while (headerCnt > 0)
				{
					int toRead = Math.Min(headerCnt, buf.Length);
					int read = pack.Read(objectOffset, buf, 0, toRead, curs);
					if (read != toRead)
						throw new EndOfStreamException();
					crc.Update(buf, 0, read);
					headerCnt -= toRead;
				}
#warning I had to comment this out to get it to compile. We need to figure out what we can use instead of a CheckedOutputStream
				/* CheckedOutputStream crcOut = new CheckedOutputStream(stream, crc);
				pack.CopyToStream(dataOffset, buf, cnt, crcOut, curs); */
				long computed = crc.Value;

				ObjectId id;
				if (loader.HasComputedId)
					id = loader.Id;
				else
					id = FindObjectForOffset(objectOffset);
				long expected = idx.FindCRC32(id);
				if (computed != expected)
					throw new CorruptObjectException(id,
							"Possible data corruption - CRC32 of raw pack data (object offset "
									+ objectOffset
									+ ") mismatch CRC32 from pack index");
			}
			else
			{
				pack.CopyToStream(dataOffset, buf, cnt, stream, curs);

				// read to verify against Adler32 zlib checksum
				//loader.CachedBytes;
			}
		}

		public bool SupportsFastCopyRawData()
		{
			return idx.HasCRC32Support();
		}


		private void ReadPackHeader()
		{
			WindowCursor curs = new WindowCursor();
			long position = 0;
			byte[] sig = new byte[Constants.PackSignature.Length];
			byte[] intbuf = new byte[4];
			long vers;

			if (pack.Read(position, sig, curs) != Constants.PackSignature.Length)
				throw new IOException("Not a PACK file.");
			for (int k = 0; k < Constants.PackSignature.Length; k++)
			{
				if (sig[k] != Constants.PackSignature[k])
					throw new IOException("Not a PACK file.");
			}
			position += Constants.PackSignature.Length;

			pack.ReadFully(position, intbuf, curs);
			vers = NB.decodeUInt32(intbuf, 0);
			if (vers != 2 && vers != 3)
				throw new IOException("Unsupported pack version " + vers + ".");
			position += 4;

			pack.ReadFully(position, intbuf, curs);
			long objectCnt = NB.decodeUInt32(intbuf, 0);
			if (idx.ObjectCount != objectCnt)
				throw new IOException("Pack index"
						+ " object count mismatch; expected " + objectCnt
						+ " found " + idx.ObjectCount + ": " + pack.Name);
		}

		private PackedObjectLoader Reader(WindowCursor curs, long objOffset)
		{
			long pos = objOffset;
			int p = 0;
			byte[] ib = curs.tempId;
			pack.ReadFully(pos, ib, curs);
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

			switch ((ObjectType)typeCode)
			{
				case ObjectType.Commit:
				case ObjectType.Tree:
				case ObjectType.Blob:
				case ObjectType.Tag:
					return new WholePackedObjectLoader(curs, this, pos, objOffset, (ObjectType)typeCode, (int)dataSize);
				case ObjectType.OFSDelta:
					pack.ReadFully(pos, ib, curs);
					p = 0;
					c = ib[p++] & 0xff;
					long ofs = c & 127;
					while ((c & 128) != 0)
					{
						ofs += 1;
						c = ib[p++] & 0xff;
						ofs <<= 7;
						ofs += (c & 127);
					}
					return new DeltaOfsPackedObjectLoader(curs, this, pos + p, objOffset, (int)dataSize, objOffset - ofs);
				case ObjectType.RefDelta:
					pack.ReadFully(pos, ib, curs);
					return new DeltaRefPackedObjectLoader(curs, this, pos + ib.Length, objOffset, (int)dataSize, ObjectId.FromRaw(ib));

				default:
					throw new IOException("Unknown object type " + typeCode + ".");
			}
		}

		private long FindEndOffset(long startOffset)
		{
			long maxOffset = pack.Length - ObjectId.Constants.ObjectIdLength;
			return GetReverseIdx().findNextOffset(startOffset, maxOffset);
		}

		private PackReverseIndex GetReverseIdx()
		{
			if (reverseIdx == null)
				reverseIdx = new PackReverseIndex(idx);
			return reverseIdx;
		}

		#region IEnumerable<MutableEntry> Members

		public IEnumerator<PackIndex.MutableEntry> GetEnumerator()
		{
			return idx.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return idx.GetEnumerator();
		}

		#endregion

	}
}
