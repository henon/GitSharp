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
using System.Linq;
using GitSharp.Exceptions;
using GitSharp.Util;

namespace GitSharp.DirectoryCache
{
	/**
	 * Support for the Git dircache (aka index file).
	 * <p>
	 * The index file keeps track of which objects are currently checked out in the
	 * working directory, and the last modified time of those working files. Changes
	 * in the working directory can be detected by comparing the modification times
	 * to the cached modification time within the index file.
	 * <p>
	 * Index files are also used during merges, where the merge happens within the
	 * index file first, and the working directory is updated as a post-merge step.
	 * Conflicts are stored in the index file to allow tool (and human) based
	 * resolutions to be easily performed.
	 */
	public class DirCache
	{
		private static readonly byte[] SigDirc = { (byte)'D', (byte)'I', (byte)'R', (byte)'C' };
		private static readonly DirCacheEntry[] NoEntries = { };
		private const int ExtTree = 0x54524545 /* 'TREE' */;
		private const int InfoLen = DirCacheEntry.INFO_LEN;

		public static Comparison<DirCacheEntry> ENT_CMP = (o1, o2) =>
		{
			int cr = cmp(o1, o2);
			if (cr != 0)
			{
				return cr;
			}
			return o1.getStage() - o2.getStage();
		};

		public static int cmp(DirCacheEntry a, DirCacheEntry b)
		{
			return cmp(a.Path, a.Path.Length, b);
		}

		public static int cmp(byte[] aPath, int aLen, DirCacheEntry b)
		{
			return cmp(aPath, aLen, b.Path, b.Path.Length);
		}

		public static int cmp(byte[] aPath, int aLen, byte[] bPath, int bLen)
		{
			for (int cPos = 0; cPos < aLen && cPos < bLen; cPos++)
			{
				int cmp = (aPath[cPos] & 0xff) - (bPath[cPos] & 0xff);
				if (cmp != 0)
				{
					return cmp;
				}
			}

			return aLen - bLen;
		}

		/**
		 * Create a new empty index which is never stored on disk.
		 *
		 * @return an empty cache which has no backing store file. The cache may not
		 *         be Read or written, but it may be queried and updated (in
		 *         memory).
		 */
		public static DirCache newInCore()
		{
			return new DirCache(null);
		}

		/**
		 * Create a new in-core index representation and Read an index from disk.
		 * <p>
		 * The new index will be Read before it is returned to the caller. Read
		 * failures are reported as exceptions and therefore prevent the method from
		 * returning a partially populated index.
		 *
		 * @param indexLocation
		 *            location of the index file on disk.
		 * @return a cache representing the contents of the specified index file (if
		 *         it exists) or an empty cache if the file does not exist.
		 * @
		 *             the index file is present but could not be Read.
		 * @throws CorruptObjectException
		 *             the index file is using a format or extension that this
		 *             library does not support.
		 */
		public static DirCache read(FileInfo indexLocation)
		{
			DirCache c = new DirCache(indexLocation);
			c.read();
			return c;
		}

		/**
		 * Create a new in-core index representation and Read an index from disk.
		 * <p>
		 * The new index will be Read before it is returned to the caller. Read
		 * failures are reported as exceptions and therefore prevent the method from
		 * returning a partially populated index.
		 *
		 * @param db
		 *            repository the caller wants to Read the default index of.
		 * @return a cache representing the contents of the specified index file (if
		 *         it exists) or an empty cache if the file does not exist.
		 * @
		 *             the index file is present but could not be Read.
		 * @throws CorruptObjectException
		 *             the index file is using a format or extension that this
		 *             library does not support.
		 */
		public static DirCache read(Repository db)
		{
			return read(new FileInfo(db.Directory + "/index"));
		}

		/**
		 * Create a new in-core index representation, lock it, and Read from disk.
		 * <p>
		 * The new index will be locked and then Read before it is returned to the
		 * caller. Read failures are reported as exceptions and therefore prevent
		 * the method from returning a partially populated index.  On Read failure,
		 * the lock is released.
		 *
		 * @param indexLocation
		 *            location of the index file on disk.
		 * @return a cache representing the contents of the specified index file (if
		 *         it exists) or an empty cache if the file does not exist.
		 * @
		 *             the index file is present but could not be Read, or the lock
		 *             could not be obtained.
		 * @throws CorruptObjectException
		 *             the index file is using a format or extension that this
		 *             library does not support.
		 */
		public static DirCache Lock(FileInfo indexLocation)
		{
			var c = new DirCache(indexLocation);
			if (!c.Lock())
			{
				throw new IOException("Cannot lock " + indexLocation);
			}

			try
			{
				c.read();
			}
			catch (Exception e)
			{
				c.unlock();
				throw e;
			}

			return c;
		}

		/**
		 * Create a new in-core index representation, lock it, and Read from disk.
		 * <p>
		 * The new index will be locked and then Read before it is returned to the
		 * caller. Read failures are reported as exceptions and therefore prevent
		 * the method from returning a partially populated index.
		 *
		 * @param db
		 *            repository the caller wants to Read the default index of.
		 * @return a cache representing the contents of the specified index file (if
		 *         it exists) or an empty cache if the file does not exist.
		 * @
		 *             the index file is present but could not be Read, or the lock
		 *             could not be obtained.
		 * @throws CorruptObjectException
		 *             the index file is using a format or extension that this
		 *             library does not support.
		 */
		public static DirCache Lock(Repository db)
		{
			return Lock(new FileInfo(db.Directory + "/index"));
		}

		/** Location of the current version of the index file. */
		private readonly FileInfo _liveFile;

		/** Modification time of the file at the last Read/write we did. */
		private DateTime _lastModified;

		/** Individual file index entries, sorted by path name. */
		private DirCacheEntry[] _sortedEntries;

		/** Number of positions within {@link #_sortedEntries} that are valid. */
		private int _entryCnt;

		/** Cache tree for this index; null if the cache tree is not available. */
		private DirCacheTree _cacheTree;

		/** Our active lock (if we hold it); null if we don't have it locked. */
		private LockFile _myLock;

		/**
		 * Create a new in-core index representation.
		 * <p>
		 * The new index will be empty. Callers may wish to Read from the on disk
		 * file first with {@link #Read()}.
		 *
		 * @param indexLocation
		 *            location of the index file on disk.
		 */
		public DirCache(FileInfo indexLocation)
		{
			_liveFile = indexLocation;
			clear();
		}

		/**
		 * Create a new builder to update this cache.
		 * <p>
		 * Callers should add all entries to the builder, then use
		 * {@link DirCacheBuilder#finish()} to update this instance.
		 *
		 * @return a new builder instance for this cache.
		 */
		public DirCacheBuilder builder()
		{
			return new DirCacheBuilder(this, _entryCnt + 16);
		}

		/**
		 * Create a new editor to recreate this cache.
		 * <p>
		 * Callers should add commands to the editor, then use
		 * {@link DirCacheEditor#finish()} to update this instance.
		 *
		 * @return a new builder instance for this cache.
		 */
		public DirCacheEditor editor()
		{
			return new DirCacheEditor(this, _entryCnt + 16);
		}

		public void replace(DirCacheEntry[] e, int cnt)
		{
			_sortedEntries = e;
			_entryCnt = cnt;
			_cacheTree = null;
		}

		/**
		 * Read the index from disk, if it has changed on disk.
		 * <p>
		 * This method tries to avoid loading the index if it has not changed since
		 * the last time we consulted it. A missing index file will be treated as
		 * though it were present but had no file entries in it.
		 *
		 * @
		 *             the index file is present but could not be Read. This
		 *             DirCache instance may not be populated correctly.
		 * @throws CorruptObjectException
		 *             the index file is using a format or extension that this
		 *             library does not support.
		 */
		public void read()
		{
			if (_liveFile == null)
				throw new IOException("DirCache does not have a backing file");
			if (!_liveFile.Exists)
				clear();
			else if (_liveFile.LastAccessTime != _lastModified)
			{
				try
				{
					var inStream = new FileStream(_liveFile.FullName, System.IO.FileMode.Open, FileAccess.Read);
					try
					{
						clear();
						ReadFrom(inStream);
					}
					finally
					{
						try
						{
							inStream.Close();
						}
						catch (IOException)
						{
							// Ignore any close failures.
						}
					}
				}
				catch (FileNotFoundException)
				{
					// Someone must have deleted it between our exists test
					// and actually opening the path. That's fine, its empty.
					//
					clear();
				}
			}
		}

		/// <summary>
		/// Empty this index, removing all entries.
		/// </summary>
		public void clear()
		{
			_lastModified = DateTime.MinValue;
			_sortedEntries = NoEntries;
			_entryCnt = 0;
			_cacheTree = null;
		}

		private void ReadFrom(Stream inStream)
		{
			var @in = new StreamReader(inStream);
			MessageDigest md = Constants.newMessageDigest();

			// Read the index header and verify we understand it.
			//
			var hdr = new byte[20];
			NB.ReadFully(inStream, hdr, 0, 12);
			md.Update(hdr, 0, 12);
			if (!IsDIRC(hdr))
			{
				throw new CorruptObjectException("Not a DIRC file.");
			}

			int ver = NB.decodeInt32(hdr, 4);
			if (ver != 2)
			{
				throw new CorruptObjectException("Unknown DIRC version " + ver);
			}

			_entryCnt = NB.decodeInt32(hdr, 8);
			if (_entryCnt < 0)
			{
				throw new CorruptObjectException("DIRC has too many entries.");
			}

			// Load the individual file entries.
			//
			var infos = new byte[InfoLen * _entryCnt];
			_sortedEntries = new DirCacheEntry[_entryCnt];
			for (int i = 0; i < _entryCnt; i++)
			{
				_sortedEntries[i] = new DirCacheEntry(infos, i * InfoLen, inStream, md);
			}
			_lastModified = _liveFile.LastAccessTime;

			// After the file entries are index extensions, and then a footer.
			//
			while (true)
			{
				var pos = inStream.Position;
				NB.ReadFully(inStream, hdr, 0, 20);
				if (@in.Read() < 0)
				{
					// No extensions present; the file ended where we expected.
					//
					break;
				}
				inStream.Seek(pos, SeekOrigin.Begin);

				switch (NB.decodeInt32(hdr, 0))
				{
					case ExtTree:
						var raw = new byte[NB.decodeInt32(hdr, 4)];
						md.Update(hdr, 0, 8);
						NB.skipFully(inStream, 8);
						NB.ReadFully(inStream, raw, 0, raw.Length);
						md.Update(raw, 0, raw.Length);
						_cacheTree = new DirCacheTree(raw, new MutableInteger(), null);
						break;

					default:
						if (hdr[0] >= (byte)'A' && hdr[0] <= (byte)'Z')
						{
							// The extension is optional and is here only as
							// a performance optimization. Since we do not
							// understand it, we can safely skip past it.
							//
							NB.skipFully(inStream, NB.decodeUInt32(hdr, 4));
						}
						else
						{
							// The extension is not an optimization and is
							// _required_ to understand this index format.
							// Since we did not trap it above we must abort.
							//
							throw new CorruptObjectException("DIRC extension '"
									+ Constants.CHARSET.GetString(hdr.Take(4).ToArray())
									+ "' not supported by this version.");
						}

						break;
				}
			}

			byte[] exp = md.Digest();
			if (!exp.SequenceEqual(hdr))
			{
				throw new CorruptObjectException("DIRC checksum mismatch");
			}
		}

		private static bool IsDIRC(byte[] header)
		{
			if (header.Length < SigDirc.Length)
			{
				return false;
			}

			for (int i = 0; i < SigDirc.Length; i++)
			{
				if (header[i] != SigDirc[i]) return false;
			}

			return true;
		}

		/**
		 * Try to establish an update lock on the cache file.
		 *
		 * @return true if the lock is now held by the caller; false if it is held
		 *         by someone else.
		 * @
		 *             the output file could not be created. The caller does not
		 *             hold the lock.
		 */
		public bool Lock()
		{
			if (_liveFile == null)
			{
				throw new IOException("DirCache does not have a backing file");
			}
			
			var tmp = new LockFile(_liveFile);
			if (tmp.Lock())
			{
				tmp.NeedStatInformation = true;
				_myLock = tmp;
				return true;
			}

			return false;
		}

		/**
		 * Write the entry records from memory to disk.
		 * <p>
		 * The cache must be locked first by calling {@link #lock()} and receiving
		 * true as the return value. Applications are encouraged to lock the index,
		 * then invoke {@link #Read()} to ensure the in-memory data is current,
		 * prior to updating the in-memory entries.
		 * <p>
		 * Once written the lock is closed and must be either committed with
		 * {@link #commit()} or rolled back with {@link #unlock()}.
		 *
		 * @
		 *             the output file could not be created. The caller no longer
		 *             holds the lock.
		 */
		public void write()
		{
			LockFile tmp = _myLock;
			RequireLocked(tmp);
			try
			{
				WriteTo(tmp.GetOutputStream());
			}
			catch (Exception)
			{
				tmp.Unlock();
				throw;
			}
		}

		private void WriteTo(Stream os)
		{
			MessageDigest foot = Constants.newMessageDigest();
			var dos = new DigestOutputStream(os, foot);

			// Write the header.
			//
			var tmp = new byte[128];
			Array.Copy(SigDirc, 0, tmp, 0, SigDirc.Length);
			NB.encodeInt32(tmp, 4, /* version */2);
			NB.encodeInt32(tmp, 8, _entryCnt);
			dos.Write(tmp, 0, 12);

			// Write the individual file entries.
			//
			if (_lastModified == DateTime.MinValue)
			{
				// Write a new index, as no entries require smudging.
				//
				for (int i = 0; i < _entryCnt; i++)
				{
					_sortedEntries[i].write(dos);
				}
			}
			else
			{
				var smudge_s = (int)(_lastModified.ToGitInternalTime());
				var smudge_ns = (int)(_lastModified.Millisecond * 1000000); // [henon] <--- this could be done with much more precision in C# since DateTime has 100 nanosec ticks
				for (int i = 0; i < _entryCnt; i++)
				{
					DirCacheEntry e = _sortedEntries[i];
					if (e.mightBeRacilyClean(smudge_s, smudge_ns))
						e.smudgeRacilyClean();
					e.write(dos);
				}
			}

			if (_cacheTree != null)
			{
				var bb = new TemporaryBuffer();
				_cacheTree.write(tmp, bb);
				bb.close();

				NB.encodeInt32(tmp, 0, ExtTree);
				NB.encodeInt32(tmp, 4, (int)bb.Length);
				dos.Write(tmp, 0, 8);
				bb.writeTo(dos, null);
			}
			var hash = foot.Digest();
			os.Write(hash, 0, hash.Length);
			os.Close();
		}

		/**
		 * Commit this change and release the lock.
		 * <p>
		 * If this method fails (returns false) the lock is still released.
		 *
		 * @return true if the commit was successful and the file contains the new
		 *         data; false if the commit failed and the file remains with the
		 *         old data.
		 * @throws InvalidOperationException
		 *             the lock is not held.
		 */
		public bool commit()
		{
			LockFile tmp = _myLock;
			RequireLocked(tmp);
			_myLock = null;
			if (!tmp.Commit())
				return false;
			_lastModified = tmp.CommitLastModified;
			return true;
		}

		private void RequireLocked(LockFile tmp)
		{
			if (_liveFile == null)
			{
				throw new InvalidOperationException("DirCache is not locked");
			}
			if (tmp == null)
			{
				throw new InvalidOperationException("DirCache " + _liveFile.FullName + " not locked.");
			}
		}

		/**
		 * Unlock this file and abort this change.
		 * <p>
		 * The temporary file (if created) is deleted before returning.
		 */
		public void unlock()
		{
			LockFile tmp = _myLock;
			if (tmp != null)
			{
				_myLock = null;
				tmp.Unlock();
			}
		}

		/**
		 * Locate the position a path's entry is at in the index.
		 * <p>
		 * If there is at least one entry in the index for this path the position of
		 * the lowest stage is returned. Subsequent stages can be identified by
		 * testing consecutive entries until the path differs.
		 * <p>
		 * If no path matches the entry -(position+1) is returned, where position is
		 * the location it would have gone within the index.
		 *
		 * @param path
		 *            the path to search for.
		 * @return if >= 0 then the return value is the position of the entry in the
		 *         index; pass to {@link #getEntry(int)} to obtain the entry
		 *         information. If < 0 the entry does not exist in the index.
		 */
		public int findEntry(String path)
		{
			if (_entryCnt == 0) return -1;
			byte[] p = Constants.encode(path);
			return findEntry(p, p.Length);
		}

		public int findEntry(byte[] p, int pLen)
		{
			int low = 0;
			int high = _entryCnt;
			do
			{
				var mid = (int)(((uint)(low + high)) >> 1);
				int cmp = DirCache.cmp(p, pLen, _sortedEntries[mid]);
				if (cmp < 0)
					high = mid;
				else if (cmp == 0)
				{
					while (mid > 0 && DirCache.cmp(p, pLen, _sortedEntries[mid - 1]) == 0)
					{
						mid--;
					}
					return mid;
				}
				else
					low = mid + 1;
			} while (low < high);
			return -(low + 1);
		}

		/**
		 * Determine the next index position past all entries with the same name.
		 * <p>
		 * As index entries are sorted by path name, then stage number, this method
		 * advances the supplied position to the first position in the index whose
		 * path name does not match the path name of the supplied position's entry.
		 *
		 * @param position
		 *            entry position of the path that should be skipped.
		 * @return position of the next entry whose path is After the input.
		 */
		public int nextEntry(int position)
		{
			DirCacheEntry last = _sortedEntries[position];
			int nextIdx = position + 1;
			while (nextIdx < _entryCnt)
			{
				DirCacheEntry next = _sortedEntries[nextIdx];
				if (cmp(last, next) != 0) break;
				last = next;
				nextIdx++;
			}
			return nextIdx;
		}

		public int nextEntry(byte[] p, int pLen, int nextIdx)
		{
			while (nextIdx < _entryCnt)
			{
				DirCacheEntry next = _sortedEntries[nextIdx];
				if (!DirCacheTree.peq(p, next.Path, pLen)) break;
				nextIdx++;
			}
			return nextIdx;
		}

		/**
		 * Total number of file entries stored in the index.
		 * <p>
		 * This count includes unmerged stages for a file entry if the file is
		 * currently conflicted in a merge. This means the total number of entries
		 * in the index may be up to 3 times larger than the number of files in the
		 * working directory.
		 * <p>
		 * Note that this value counts only <i>files</i>.
		 *
		 * @return number of entries available.
		 * @see #getEntry(int)
		 */
		public int getEntryCount()
		{
			return _entryCnt;
		}

		/**
		 * Get a specific entry.
		 *
		 * @param i
		 *            position of the entry to get.
		 * @return the entry at position <code>i</code>.
		 */
		public DirCacheEntry getEntry(int i)
		{
			return _sortedEntries[i];
		}

		/**
		 * Get a specific entry.
		 *
		 * @param path
		 *            the path to search for.
		 * @return the entry at position <code>i</code>.
		 */
		public DirCacheEntry getEntry(String path)
		{
			int i = findEntry(path);
			return i < 0 ? null : _sortedEntries[i];
		}

		/**
		 * Recursively get all entries within a subtree.
		 *
		 * @param path
		 *            the subtree path to get all entries within.
		 * @return all entries recursively contained within the subtree.
		 */
		public DirCacheEntry[] getEntriesWithin(string path)
		{
			if (!path.EndsWith("/"))
			{
				path += "/";
			}

			byte[] p = Constants.encode(path);
			int pLen = p.Length;

			int eIdx = findEntry(p, pLen);
			if (eIdx < 0)
			{
				eIdx = -(eIdx + 1);
			}
			int lastIdx = nextEntry(p, pLen, eIdx);
			var r = new DirCacheEntry[lastIdx - eIdx];
			Array.Copy(_sortedEntries, eIdx, r, 0, r.Length);
			return r;
		}

		public void toArray(int i, DirCacheEntry[] dst, int off, int cnt)
		{
			Array.Copy(_sortedEntries, i, dst, off, cnt);
		}

		/**
		 * Obtain (or build) the current cache tree structure.
		 * <p>
		 * This method can optionally recreate the cache tree, without flushing the
		 * tree objects themselves to disk.
		 *
		 * @param build
		 *            if true and the cache tree is not present in the index it will
		 *            be generated and returned to the caller.
		 * @return the cache tree; null if there is no current cache tree available
		 *         and <code>build</code> was false.
		 */
		public DirCacheTree getCacheTree(bool build)
		{
			if (build)
			{
				if (_cacheTree == null)
					_cacheTree = new DirCacheTree();
				_cacheTree.validate(_sortedEntries, _entryCnt, 0, 0);
			}
			return _cacheTree;
		}

		/**
		 * Write all index trees to the object store, returning the root tree.
		 *
		 * @param ow
		 *            the writer to use when serializing to the store.
		 * @return identity for the root tree.
		 * @throws UnmergedPathException
		 *             one or more paths contain higher-order stages (stage > 0),
		 *             which cannot be stored in a tree object.
		 * @throws  IOException
		 *             an unexpected error occurred writing to the object store.
		 */
		public ObjectId writeTree(ObjectWriter ow)
		{
			return getCacheTree(true).writeTree(_sortedEntries, 0, 0, ow);
		}
	}
}