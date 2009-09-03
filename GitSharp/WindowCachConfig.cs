/*
 * Copyright (C) 2009, Google Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp
{
/** Configuration parameters for {@link WindowCache}. */
public class WindowCacheConfig {
	/** 1024 (number of bytes in one kibibyte/kilobyte) */
	public static  int KB = 1024;

	/** 1024 {@link #KB} (number of bytes in one mebibyte/megabyte) */
	public static  int MB = 1024 * KB;

	private int packedGitOpenFiles;

	private int packedGitLimit;

	private int packedGitWindowSize;

	private bool packedGitMMAP;

	private int deltaBaseCacheLimit;

	/** Create a default configuration. */
	public WindowCacheConfig() {
		packedGitOpenFiles = 128;
		packedGitLimit = 10 * MB;
		packedGitWindowSize = 8 * KB;
		packedGitMMAP = false;
		deltaBaseCacheLimit = 10 * MB;
	}

	/**
	 * @return maximum number of streams to open at a time. Open packs count
	 *         against the process limits. <b>Default is 128.</b>
	 */
	public int getPackedGitOpenFiles() {
		return packedGitOpenFiles;
	}

	/**
	 * @param fdLimit
	 *            maximum number of streams to open at a time. Open packs count
	 *            against the process limits
	 */
	public void setPackedGitOpenFiles( int fdLimit) {
		packedGitOpenFiles = fdLimit;
	}

	/**
	 * @return maximum number bytes of heap memory to dedicate to caching pack
	 *         file data. <b>Default is 10 MB.</b>
	 */
	public int getPackedGitLimit() {
		return packedGitLimit;
	}

	/**
	 * @param newLimit
	 *            maximum number bytes of heap memory to dedicate to caching
	 *            pack file data.
	 */
	public void setPackedGitLimit( int newLimit) {
		packedGitLimit = newLimit;
	}

	/**
	 * @return size in bytes of a single window mapped or read in from the pack
	 *         file. <b>Default is 8 KB.</b>
	 */
	public int getPackedGitWindowSize() {
		return packedGitWindowSize;
	}

	/**
	 * @param newSize
	 *            size in bytes of a single window read in from the pack file.
	 */
	public void setPackedGitWindowSize( int newSize) {
		packedGitWindowSize = newSize;
	}

	/**
	 * @return true enables use of Java NIO virtual memory mapping for windows;
	 *         false reads entire window into a byte[] with standard read calls.
	 *         <b>Default false.</b>
	 */
	public bool isPackedGitMMAP() {
		return packedGitMMAP;
	}

	/**
	 * @param usemmap
	 *            true enables use of Java NIO virtual memory mapping for
	 *            windows; false reads entire window into a byte[] with standard
	 *            read calls.
	 */
	public void setPackedGitMMAP( bool usemmap) {
		packedGitMMAP = usemmap;
	}

	/**
	 * @return maximum number of bytes to cache in {@link UnpackedObjectCache}
	 *         for inflated, recently accessed objects, without delta chains.
	 *         <b>Default 10 MB.</b>
	 */
	public int getDeltaBaseCacheLimit() {
		return deltaBaseCacheLimit;
	}

	/**
	 * @param newLimit
	 *            maximum number of bytes to cache in
	 *            {@link UnpackedObjectCache} for inflated, recently accessed
	 *            objects, without delta chains.
	 */
	public void setDeltaBaseCacheLimit( int newLimit) {
		deltaBaseCacheLimit = newLimit;
	}

	/**
	 * Update properties by setting fields from the configuration.
	 * <p>
	 * If a property is not defined in the configuration, then it is left
	 * unmodified.
	 *
	 * @param rc configuration to read properties from.
	 */
	public void fromConfig( RepositoryConfig rc) {
		setPackedGitOpenFiles(rc.getInt("core", null, "packedgitopenfiles", getPackedGitOpenFiles()));
        setPackedGitLimit(rc.getInt("core", null, "packedgitlimit", getPackedGitLimit()));
        setPackedGitWindowSize(rc.getInt("core", null, "packedgitwindowsize", getPackedGitWindowSize()));
		setPackedGitMMAP(rc.getBoolean("core", null, "packedgitmmap", isPackedGitMMAP()));
        setDeltaBaseCacheLimit(rc.getInt("core", null, "deltabasecachelimit", getDeltaBaseCacheLimit()));
	}
}

}
