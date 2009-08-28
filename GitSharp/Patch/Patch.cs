/*
 * Copyright (C) 2008, Google Inc.
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
using System.Collections.Generic;
using System.IO;
using GitSharp.Util;

namespace GitSharp.Patch
{
    /// <summary>
	/// A parsed collection of <seealso cref="FileHeader"/>s from a unified diff patch file.
    /// </summary>
    [Serializable]
    public class Patch
    {
	    private static readonly byte[] DIFF_GIT = Constants.encodeASCII("diff --git ");

	    private static readonly byte[] DIFF_CC = Constants.encodeASCII("diff --cc ");

	    private static readonly byte[] DIFF_COMBINED = Constants.encodeASCII("diff --combined ");

	    private static readonly byte[][] BIN_HEADERS = new byte[][] {
			    Constants.encodeASCII("Binary files "), Constants.encodeASCII("Files "), };

	    private static readonly byte[] BIN_TRAILER = Constants.encodeASCII(" differ\n");

	    private static readonly byte[] GIT_BINARY = Constants.encodeASCII("GIT binary patch\n");

	    public static readonly byte[] SIG_FOOTER = Constants.encodeASCII("-- \n");

	    /** The files, in the order they were parsed out of the input. */
	    private readonly List<FileHeader> files;

	    /** Formatting errors, if any were identified. */
	    private readonly List<FormatError> errors;

	    /** Create an empty patch. */
	    public Patch()
        {
		    files = new List<FileHeader>();
		    errors = new List<FormatError>(0);
	    }

	    /**
	     * Add a single file to this patch.
	     * <p>
	     * Typically files should be added by parsing the text through one of this
	     * class's parse methods.
	     *
	     * @param fh
	     *            the header of the file.
	     */
	    public void addFile(FileHeader fh)
        {
		    files.Add(fh);
	    }

	    /** @return list of files described in the patch, in occurrence order. */
	    public List<FileHeader> getFiles()
        {
		    return files;
	    }

	    /**
	     * Add a formatting error to this patch script.
	     *
	     * @param err
	     *            the error description.
	     */
	    public void addError(FormatError err)
        {
		    errors.Add(err);
	    }

	    /** @return collection of formatting errors, if any. */
	    public List<FormatError> getErrors()
        {
		    return errors;
	    }

	    /**
	     * Parse a patch received from an InputStream.
	     * <p>
	     * Multiple parse calls on the same instance will concatenate the patch
	     * data, but each parse input must start with a valid file header (don't
	     * split a single file across parse calls).
	     *
	     * @param is
	     *            the stream to read the patch data from. The stream is read
	     *            until EOF is reached.
	     * @throws IOException
	     *             there was an error reading from the input stream.
	     */
	    public void parse(Stream iStream)
        {
		    byte[] buf = readFully(iStream);
		    parse(buf, 0, buf.Length);
	    }

	    private static byte[] readFully(Stream iStream)
        {
		    TemporaryBuffer b = new TemporaryBuffer();
		    try {
			    b.copy(iStream);
			    b.close();
			    return b.ToArray();
		    } finally {
			    b.destroy();
		    }
	    }

	    /**
	     * Parse a patch stored in a byte[].
	     * <p>
	     * Multiple parse calls on the same instance will concatenate the patch
	     * data, but each parse input must start with a valid file header (don't
	     * split a single file across parse calls).
	     *
	     * @param buf
	     *            the buffer to parse.
	     * @param ptr
	     *            starting position to parse from.
	     * @param end
	     *            1 past the last position to end parsing. The total length to
	     *            be parsed is <code>end - ptr</code>.
	     */
	    public void parse(byte[] buf, int ptr, int end)
        {
		    while (ptr < end)
		    {
		    	ptr = parseFile(buf, ptr, end);
		    }
	    }

	    private int parseFile(byte[] buf, int c, int end)
        {
		    while (c < end)
            {
			    if (FileHeader.isHunkHdr(buf, c, end) >= 1)
                {
				    // If we find a disconnected hunk header we might
				    // have missed a file header previously. The hunk
				    // isn't valid without knowing where it comes from.
				    //
				    error(buf, c, "Hunk disconnected from file");
				    c = RawParseUtils.nextLF(buf, c);
				    continue;
			    }

			    // Valid git style patch?
			    //
			    if (RawParseUtils.match(buf, c, DIFF_GIT) >= 0)
			    {
			    	return parseDiffGit(buf, c, end);
			    }
			    if (RawParseUtils.match(buf, c, DIFF_CC) >= 0)
			    {
			    	return parseDiffCombined(DIFF_CC, buf, c, end);
			    }
			    if (RawParseUtils.match(buf, c, DIFF_COMBINED) >= 0)
			    {
			    	return parseDiffCombined(DIFF_COMBINED, buf, c, end);
			    }

			    // Junk between files? Leading junk? Traditional
			    // (non-git generated) patch?
			    //
			    int n = RawParseUtils.nextLF(buf, c);
			    if (n >= end) {
				    // Patches cannot be only one line long. This must be
				    // trailing junk that we should ignore.
				    //
				    return end;
			    }

			    if (n - c < 6) {
				    // A valid header must be at least 6 bytes on the
				    // first line, e.g. "--- a/b\n".
				    //
				    c = n;
				    continue;
			    }

			    if (RawParseUtils.match(buf, c, FileHeader.OLD_NAME) >= 0 &&
                    RawParseUtils.match(buf, n, FileHeader.NEW_NAME) >= 0)
                {
				    // Probably a traditional patch. Ensure we have at least
				    // a "@@ -0,0" smelling line next. We only check the "@@ -".
				    //
				    int f = RawParseUtils.nextLF(buf, n);
				    if (f >= end)
					    return end;
                    if (FileHeader.isHunkHdr(buf, f, end) == 1)
					    return parseTraditionalPatch(buf, c, end);
			    }

			    c = n;
		    }
		    return c;
	    }

	    private int parseDiffGit(byte[] buf, int start, int end)
        {
		    FileHeader fh = new FileHeader(buf, start);
		    int ptr = fh.parseGitFileName(start + DIFF_GIT.Length, end);
		    if (ptr < 0)
			    return skipFile(buf, start);

		    ptr = fh.parseGitHeaders(ptr, end);
		    ptr = parseHunks(fh, ptr, end);
		    fh.endOffset = ptr;
		    addFile(fh);
		    return ptr;
	    }

	    private int parseDiffCombined(ICollection<byte> hdr, byte[] buf, int start, int end)
        {
		    var fh = new CombinedFileHeader(buf, start);
		    int ptr = fh.parseGitFileName(start + hdr.Count, end);
		    if (ptr < 0)
			    return skipFile(buf, start);

		    ptr = fh.parseGitHeaders(ptr, end);
		    ptr = parseHunks(fh, ptr, end);
		    fh.endOffset = ptr;
		    addFile(fh);
		    return ptr;
	    }

	    private int parseTraditionalPatch(byte[] buf, int start, int end)
        {
		    FileHeader fh = new FileHeader(buf, start);
		    int ptr = fh.parseTraditionalHeaders(start, end);
		    ptr = parseHunks(fh, ptr, end);
		    fh.endOffset = ptr;
		    addFile(fh);
		    return ptr;
	    }

	    private static int skipFile(byte[] buf, int ptr)
        {
		    ptr = RawParseUtils.nextLF(buf, ptr);
            if (RawParseUtils.match(buf, ptr, FileHeader.OLD_NAME) >= 0)
			    ptr = RawParseUtils.nextLF(buf, ptr);
		    return ptr;
	    }

	    private int parseHunks(FileHeader fh, int c, int end)
        {
		    byte[] buf = fh.buf;
		    while (c < end)
            {
			    // If we see a file header at this point, we have all of the
			    // hunks for our current file. We should stop and report back
			    // with this position so it can be parsed again later.
			    //
			    if (RawParseUtils.match(buf, c, DIFF_GIT) >= 0)
				    break;
			    if (RawParseUtils.match(buf, c, DIFF_CC) >= 0)
				    break;
			    if (RawParseUtils.match(buf, c, DIFF_COMBINED) >= 0)
				    break;
			    if (RawParseUtils.match(buf, c, FileHeader.OLD_NAME) >= 0)
				    break;
                if (RawParseUtils.match(buf, c, FileHeader.NEW_NAME) >= 0)
				    break;

                if (FileHeader.isHunkHdr(buf, c, end) == fh.getParentCount())
                {
				    HunkHeader h = fh.newHunkHeader(c);
				    h.parseHeader();
				    c = h.parseBody(this, end);
				    h.EndOffset = c;
				    fh.addHunk(h);
				    if (c < end) {
					    switch (buf[c]) {
					    case (byte)'@':
					    case (byte)'d':
					    case (byte)'\n':
						    break;
					    default:
						    if (RawParseUtils.match(buf, c, SIG_FOOTER) < 0)
							    warn(buf, c, "Unexpected hunk trailer");
                            break;
					    }
				    }
				    continue;
			    }

			    int eol = RawParseUtils.nextLF(buf, c);
			    if (fh.getHunks().isEmpty() && RawParseUtils.match(buf, c, GIT_BINARY) >= 0) {
				    fh.patchType = FileHeader.PatchType.GIT_BINARY;
				    return parseGitBinary(fh, eol, end);
			    }

			    if (fh.getHunks().isEmpty() && BIN_TRAILER.Length < eol - c
					    && RawParseUtils.match(buf, eol - BIN_TRAILER.Length, BIN_TRAILER) >= 0
					    && matchAny(buf, c, BIN_HEADERS)) {
				    // The patch is a binary file diff, with no deltas.
				    //
				    fh.patchType = FileHeader.PatchType.BINARY;
				    return eol;
			    }

			    // Skip this line and move to the next. Its probably garbage
			    // after the last hunk of a file.
			    //
			    c = eol;
		    }

		    if (fh.getHunks().isEmpty()
				    && fh.getPatchType() == FileHeader.PatchType.UNIFIED
				    && !fh.hasMetaDataChanges())
            {
			    // Hmm, an empty patch? If there is no metadata here we
			    // really have a binary patch that we didn't notice above.
			    //
			    fh.patchType = FileHeader.PatchType.BINARY;
		    }

		    return c;
	    }

	    private int parseGitBinary(FileHeader fh, int c, int end) {
		    BinaryHunk postImage = new BinaryHunk(fh, c);
		    int nEnd = postImage.parseHunk(c, end);
		    if (nEnd < 0) {
			    // Not a binary hunk.
			    //
			    error(fh.buf, c, "Missing forward-image in GIT binary patch");
			    return c;
		    }
		    c = nEnd;
		    postImage.endOffset = c;
		    fh.forwardBinaryHunk = postImage;

		    BinaryHunk preImage = new BinaryHunk(fh, c);
		    int oEnd = preImage.parseHunk(c, end);
		    if (oEnd >= 0) {
			    c = oEnd;
			    preImage.endOffset = c;
			    fh.reverseBinaryHunk = preImage;
		    }

		    return c;
	    }

	    public void warn(byte[] buf, int ptr, string msg)
        {
		    addError(new FormatError(buf, ptr, FormatError.Severity.WARNING, msg));
	    }

	    public void error(byte[] buf, int ptr, string msg)
        {
		    addError(new FormatError(buf, ptr, FormatError.Severity.ERROR, msg));
	    }

	    private static bool matchAny(byte[] buf, int c, byte[][] srcs)
        {
		    foreach (byte[] s in srcs)
            {
                if (RawParseUtils.match(buf, c, s) >= 0)
				    return true;
		    }
		    return false;
	    }
    }
}