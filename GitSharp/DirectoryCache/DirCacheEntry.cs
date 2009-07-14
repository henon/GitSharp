/*
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GitSharp.Util;

namespace GitSharp.DirectoryCache
{
/**
 * A single file (or stage of a file) in a {@link DirCache}.
 * <p>
 * An entry represents exactly one stage of a file. If a file path is unmerged
 * then multiple DirCacheEntry instances may appear for the same path name.
 */
public class DirCacheEntry {
	private static  byte[] nullpad = new byte[8];

	/** The standard (fully merged) stage for an entry. */
	public static  int STAGE_0 = 0;

	/** The base tree revision for an entry. */
	public static  int STAGE_1 = 1;

	/** The first tree revision (usually called "ours"). */
	public static  int STAGE_2 = 2;

	/** The second tree revision (usually called "theirs"). */
	public static  int STAGE_3 = 3;

	// private static  int P_CTIME = 0;

	// private static  int P_CTIME_NSEC = 4;

	private static  int P_MTIME = 8;

	// private static  int P_MTIME_NSEC = 12;

	// private static  int P_DEV = 16;

	// private static  int P_INO = 20;

	private static  int P_MODE = 24;

	// private static  int P_UID = 28;

	// private static  int P_GID = 32;

	private static  int P_SIZE = 36;

	private static  int P_OBJECTID = 40;

	private static  int P_FLAGS = 60;

	/** Mask applied to data in {@link #P_FLAGS} to get the name length. */
	private static  int NAME_MASK = 0xfff;

	public static  int INFO_LEN = 62;

	private static  int ASSUME_VALID = 0x80;

	/** (Possibly shared) header information storage. */
	private  byte[] info;

	/** First location within {@link #info} where our header starts. */
	private  int infoOffset;

	/** Our encoded path name, from the root of the repository. */
	 public byte[] path;

	public DirCacheEntry( byte[] sharedInfo,  int infoAt, Stream @in,  MessageDigest md) {
		info = sharedInfo;
		infoOffset = infoAt;

		NB.ReadFully(@in, info, infoOffset, INFO_LEN);
		md.update(info, infoOffset, INFO_LEN);

		int pathLen = NB.decodeUInt16(info, infoOffset + P_FLAGS) & NAME_MASK;
		int skipped = 0;
		if (pathLen < NAME_MASK) {
			path = new byte[pathLen];
			NB.readFully(@in, path, 0, pathLen);
			md.update(path, 0, pathLen);
		} else {
			 ByteArrayOutputStream tmp = new ByteArrayOutputStream();
			{
				 byte[] buf = new byte[NAME_MASK];
				NB.readFully(@in, buf, 0, NAME_MASK);
				tmp.write(buf);
			}
			for (;;) {
				 int c = @in.read();
				if (c < 0)
					throw new EndOfStreamException("Short read of block.");
				if (c == 0)
					break;
				tmp.write(c);
			}
			path = tmp.toByteArray();
			pathLen = path.length;
			skipped = 1; // we already skipped 1 '\0' above to break the loop.
			md.update(path, 0, pathLen);
			md.update((byte) 0);
		}

		// Index records are padded out to the next 8 byte alignment
		// for historical reasons related to how C Git read the files.
		//
		 int actLen = INFO_LEN + pathLen;
		 int expLen = (actLen + 8) & ~7;
		 int padLen = expLen - actLen - skipped;
		if (padLen > 0) {
			NB.skipFully(@in, padLen);
			md.update(nullpad, 0, padLen);
		}
	}

	/**
	 * Create an empty entry at stage 0.
	 *
	 * @param newPath
	 *            name of the cache entry.
	 */
	public DirCacheEntry( String newPath) {
		this(Constants.encode(newPath));
	}

	/**
	 * Create an empty entry at the specified stage.
	 *
	 * @param newPath
	 *            name of the cache entry.
	 * @param stage
	 *            the stage index of the new entry.
	 */
	public DirCacheEntry( String newPath,  int stage) {
		this(Constants.encode(newPath), stage);
	}

	/**
	 * Create an empty entry at stage 0.
	 *
	 * @param newPath
	 *            name of the cache entry, in the standard encoding.
	 */
	public DirCacheEntry( byte[] newPath) {
		this(newPath, STAGE_0);
	}

	/**
	 * Create an empty entry at the specified stage.
	 *
	 * @param newPath
	 *            name of the cache entry, in the standard encoding.
	 * @param stage
	 *            the stage index of the new entry.
	 */
	public DirCacheEntry( byte[] newPath,  int stage) {
		info = new byte[INFO_LEN];
		infoOffset = 0;
		path = newPath;

		int flags = ((stage & 0x3) << 12);
		if (path.length < NAME_MASK)
			flags |= path.length;
		else
			flags |= NAME_MASK;
		NB.encodeInt16(info, infoOffset + P_FLAGS, flags);
	}

    public void write(Stream os)
    {
		 int pathLen = path.length;
		os.write(info, infoOffset, INFO_LEN);
		os.write(path, 0, pathLen);

		// Index records are padded out to the next 8 byte alignment
		// for historical reasons related to how C Git read the files.
		//
		 int actLen = INFO_LEN + pathLen;
		 int expLen = (actLen + 8) & ~7;
		if (actLen != expLen)
			os.Write(nullpad, 0, expLen - actLen);
	}

	/**
	 * Is it possible for this entry to be accidentally assumed clean?
	 * <p>
	 * The "racy git" problem happens when a work file can be updated faster
	 * than the filesystem records file modification timestamps. It is possible
	 * for an application to edit a work file, update the index, then edit it
	 * again before the filesystem will give the work file a new modification
	 * timestamp. This method tests to see if file was written out at the same
	 * time as the index.
	 *
	 * @param smudge_s
	 *            seconds component of the index's last modified time.
	 * @param smudge_ns
	 *            nanoseconds component of the index's last modified time.
	 * @return true if extra careful checks should be used.
	 */
    public bool mightBeRacilyClean(int smudge_s, int smudge_ns)
    {
		// If the index has a modification time then it came from disk
		// and was not generated from scratch in memory. In such cases
		// the entry is 'racily clean' if the entry's cached modification
		// time is equal to or later than the index modification time. In
		// such cases the work file is too close to the index to tell if
		// it is clean or not based on the modification time alone.
		//
		 int @base = infoOffset + P_MTIME;
		 int mtime = NB.decodeInt32(info, @base);
		if (smudge_s < mtime)
			return true;
		if (smudge_s == mtime)
			return smudge_ns <= NB.decodeInt32(info, @base + 4) / 1000000;
		return false;
	}

	/**
	 * Force this entry to no longer match its working tree file.
	 * <p>
	 * This avoids the "racy git" problem by making this index entry no longer
	 * match the file in the working directory. Later git will be forced to
	 * compare the file content to ensure the file matches the working tree.
	 */
     public void smudgeRacilyClean()
     {
		// We don't use the same approach as C Git to smudge the entry,
		// as we cannot compare the working tree file to our SHA-1 and
		// thus cannot use the "size to 0" trick without accidentally
		// thinking a zero length file is clean.
		//
		// Instead we force the mtime to the largest possible value, so
		// it is certainly after the index's own modification time and
		// on a future read will cause mightBeRacilyClean to say "yes!".
		// It is also unlikely to match with the working tree file.
		//
		// I'll see you again before Jan 19, 2038, 03:14:07 AM GMT.
		//
		 int @base = infoOffset + P_MTIME;
		Arrays.fill(info, base, base + 8, (byte) 127);
	}

     public byte[] idBuffer()
     {
		return info;
	}

	 public int idOffset() {
		return infoOffset + P_OBJECTID;
	}

	/**
	 * Is this entry always thought to be unmodified?
	 * <p>
	 * Most entries in the index do not have this flag set. Users may however
	 * set them on if the file system stat() costs are too high on this working
	 * directory, such as on NFS or SMB volumes.
	 *
	 * @return true if we must assume the entry is unmodified.
	 */
	public bool isAssumeValid() {
		return (info[infoOffset + P_FLAGS] & ASSUME_VALID) != 0;
	}

	/**
	 * Set the assume valid flag for this entry,
	 *
	 * @param assume
	 *            true to ignore apparent modifications; false to look at last
	 *            modified to detect file modifications.
	 */
	public void setAssumeValid( bool assume) {
		if (assume)
			info[infoOffset + P_FLAGS] |= ASSUME_VALID;
		else
			info[infoOffset + P_FLAGS] &= ~ASSUME_VALID;
	}

	/**
	 * Get the stage of this entry.
	 * <p>
	 * Entries have one of 4 possible stages: 0-3.
	 *
	 * @return the stage of this entry.
	 */
	public int getStage() {
		return (int)((uint)(info[infoOffset + P_FLAGS]) >> 4) & 0x3;
	}

	/**
	 * Obtain the raw {@link FileMode} bits for this entry.
	 *
	 * @return mode bits for the entry.
	 * @see FileMode#fromBits(int)
	 */
	public int getRawMode() {
		return NB.decodeInt32(info, infoOffset + P_MODE);
	}

	/**
	 * Obtain the {@link FileMode} for this entry.
	 *
	 * @return the file mode singleton for this entry.
	 */
	public FileMode getFileMode() {
		return FileMode.fromBits(getRawMode());
	}

	/**
	 * Set the file mode for this entry.
	 *
	 * @param mode
	 *            the new mode constant.
	 */
	public void setFileMode( FileMode mode) {
		NB.encodeInt32(info, infoOffset + P_MODE, mode.Bits);
	}

	/**
	 * Get the cached last modification date of this file, in milliseconds.
	 * <p>
	 * One of the indicators that the file has been modified by an application
	 * changing the working tree is if the last modification time for the file
	 * differs from the time stored in this entry.
	 *
	 * @return last modification time of this file, in milliseconds since the
	 *         Java epoch (midnight Jan 1, 1970 UTC).
	 */
	public long getLastModified() {
		return decodeTS(P_MTIME);
	}

	/**
	 * Set the cached last modification date of this file, using milliseconds.
	 *
	 * @param when
	 *            new cached modification date of the file, in milliseconds.
	 */
	public void setLastModified( long when) {
		encodeTS(P_MTIME, when);
	}

	/**
	 * Get the cached size (in bytes) of this file.
	 * <p>
	 * One of the indicators that the file has been modified by an application
	 * changing the working tree is if the size of the file (in bytes) differs
	 * from the size stored in this entry.
	 * <p>
	 * Note that this is the length of the file in the working directory, which
	 * may differ from the size of the decompressed blob if work tree filters
	 * are being used, such as LF<->CRLF conversion.
	 *
	 * @return cached size of the working directory file, in bytes.
	 */
	public int getLength() {
		return NB.decodeInt32(info, infoOffset + P_SIZE);
	}

	/**
	 * Set the cached size (in bytes) of this file.
	 *
	 * @param sz
	 *            new cached size of the file, as bytes.
	 */
	public void setLength( int sz) {
		NB.encodeInt32(info, infoOffset + P_SIZE, sz);
	}

	/**
	 * Obtain the ObjectId for the entry.
	 * <p>
	 * Using this method to compare ObjectId values between entries is
	 * inefficient as it causes memory allocation.
	 *
	 * @return object identifier for the entry.
	 */
	public ObjectId getObjectId() {
		return ObjectId.fromRaw(idBuffer(), idOffset());
	}

	/**
	 * Set the ObjectId for the entry.
	 *
	 * @param id
	 *            new object identifier for the entry. May be
	 *            {@link ObjectId#zeroId()} to remove the current identifier.
	 */
	public void setObjectId( AnyObjectId id) {
		id.copyRawTo(idBuffer(), idOffset());
	}

	/**
	 * Set the ObjectId for the entry from the raw binary representation.
	 *
	 * @param bs
	 *            the raw byte buffer to read from. At least 20 bytes after p
	 *            must be available within this byte array.
	 * @param p
	 *            position to read the first byte of data from.
	 */
	public void setObjectIdFromRaw( byte[] bs,  int p) {
		 int n = Constants.OBJECT_ID_LENGTH;
		Array.Copy(bs, p, idBuffer(), idOffset(), n);
	}

	/**
	 * Get the entry's complete path.
	 * <p>
	 * This method is not very efficient and is primarily meant for debugging
	 * and  output generation. Applications should try to avoid calling it,
	 * and if invoked do so only once per interesting entry, where the name is
	 * absolutely required for correct function.
	 *
	 * @return complete path of the entry, from the root of the repository. If
	 *         the entry is in a subtree there will be at least one '/' in the
	 *         returned string.
	 */
	public String getPathString() {
		return Constants.CHARSET.decode(ByteBuffer.wrap(path)).toString();
	}

	/**
	 * Copy the ObjectId and other meta fields from an existing entry.
	 * <p>
	 * This method copies everything except the path from one entry to another,
	 * supporting renaming.
	 *
	 * @param src
	 *            the entry to copy ObjectId and meta fields from.
	 */
	public void copyMetaData( DirCacheEntry src) {
		 int pLen = NB.decodeUInt16(info, infoOffset + P_FLAGS) & NAME_MASK;
		System.arraycopy(src.info, src.infoOffset, info, infoOffset, INFO_LEN);
		NB.encodeInt16(info, infoOffset + P_FLAGS, pLen
				| NB.decodeUInt16(info, infoOffset + P_FLAGS) & ~NAME_MASK);
	}

	private long decodeTS( int pIdx) {
		 int @base = infoOffset + pIdx;
		 int sec = NB.decodeInt32(info, @base);
		 int ms = NB.decodeInt32(info, @base + 4) / 1000000;
		return 1000L * sec + ms;
	}

	private void encodeTS( int pIdx,  long when) {
		 int @base = infoOffset + pIdx;
		NB.encodeInt32(info, @base, (int) (when / 1000));
		NB.encodeInt32(info, @base + 4, ((int) (when % 1000)) * 1000000);
	}
}}
