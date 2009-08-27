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
using System.Text;
using GitSharp.Diff;
using GitSharp.Util;

namespace GitSharp.Patch
{
    /// <summary>
	/// Patch header describing an action for a single file path.
    /// </summary>
    [Serializable]
    public class FileHeader
    {
	    /** Magical file name used for file adds or deletes. */
	    public static readonly string DEV_NULL = "/dev/null";

        private static readonly byte[] OLD_MODE = Constants.encodeASCII("old mode ");

        private static readonly byte[] NEW_MODE = Constants.encodeASCII("new mode ");

        public static readonly byte[] DELETED_FILE_MODE = Constants.encodeASCII("deleted file mode ");

        public static readonly byte[] NEW_FILE_MODE = Constants.encodeASCII("new file mode ");

        private static readonly byte[] COPY_FROM = Constants.encodeASCII("copy from ");

        private static readonly byte[] COPY_TO = Constants.encodeASCII("copy to ");

        private static readonly byte[] RENAME_OLD = Constants.encodeASCII("rename old ");

        private static readonly byte[] RENAME_NEW = Constants.encodeASCII("rename new ");

        private static readonly byte[] RENAME_FROM = Constants.encodeASCII("rename from ");

        private static readonly byte[] RENAME_TO = Constants.encodeASCII("rename to ");

        private static readonly byte[] SIMILARITY_INDEX = Constants.encodeASCII("similarity index ");

        private static readonly byte[] DISSIMILARITY_INDEX = Constants.encodeASCII("dissimilarity index ");

        public static readonly byte[] INDEX = Constants.encodeASCII("index ");

        public static readonly byte[] OLD_NAME = Constants.encodeASCII("--- ");

        public static readonly byte[] NEW_NAME = Constants.encodeASCII("+++ ");

	    /** General type of change a single file-level patch describes. */
	    public enum ChangeType {
		    /** Add a new file to the project */
		    ADD,

		    /** Modify an existing file in the project (content and/or mode) */
		    MODIFY,

		    /** Delete an existing file from the project */
		    DELETE,

		    /** Rename an existing file to a new location */
		    RENAME,

		    /** Copy an existing file to a new location, keeping the original */
		    COPY
	    }

	    /** Type of patch used by this file. */
	    public enum PatchType {
		    /** A traditional unified diff style patch of a text file. */
		    UNIFIED,

		    /** An empty patch with a message "Binary files ... differ" */
		    BINARY,

		    /** A Git binary patch, holding pre and post image deltas */
		    GIT_BINARY
	    }

	    /** Buffer holding the patch data for this file. */
	    public readonly byte[] buf;

	    /** Offset within {@link #buf} to the "diff ..." line. */
	    public readonly int startOffset;

	    /** Position 1 past the end of this file within {@link #buf}. */
	    public int endOffset;

	    /** File name of the old (pre-image). */
	    private String oldName;

	    /** File name of the new (post-image). */
	    private String newName;

	    /** Old mode of the file, if described by the patch, else null. */
	    private FileMode oldMode;

	    /** New mode of the file, if described by the patch, else null. */
	    protected FileMode newMode;

	    /** General type of change indicated by the patch. */
	    protected ChangeType changeType;

	    /** Similarity score if {@link #changeType} is a copy or rename. */
	    private int score;

	    /** ObjectId listed on the index line for the old (pre-image) */
	    private AbbreviatedObjectId oldId;

	    /** ObjectId listed on the index line for the new (post-image) */
	    protected AbbreviatedObjectId newId;

	    /** Type of patch used to modify this file */
        public PatchType patchType;

	    /** The hunks of this file */
	    private List<HunkHeader> hunks;

	    /** If {@link #patchType} is {@link PatchType#GIT_BINARY}, the new image */
	    public BinaryHunk forwardBinaryHunk;

	    /** If {@link #patchType} is {@link PatchType#GIT_BINARY}, the old image */
	    public BinaryHunk reverseBinaryHunk;

	    public FileHeader(byte[] b, int offset)
        {
		    buf = b;
		    startOffset = offset;
		    changeType = ChangeType.MODIFY; // unless otherwise designated
		    patchType = PatchType.UNIFIED;
	    }

	    public virtual int getParentCount()
        {
		    return 1;
	    }

	    /** @return the byte array holding this file's patch script. */
	    public byte[] getBuffer()
        {
		    return buf;
	    }

	    /** @return offset the start of this file's script in {@link #getBuffer()}. */
	    public int getStartOffset()
        {
		    return startOffset;
	    }

	    /** @return offset one past the end of the file script. */
	    public int getEndOffset()
        {
		    return endOffset;
	    }

	    /**
	     * Convert the patch script for this file into a string.
	     * <p>
	     * The default character encoding ({@link Constants#CHARSET}) is assumed for
	     * both the old and new files.
	     *
	     * @return the patch script, as a Unicode string.
	     */
	    public String getScriptText()
        {
		    return getScriptText(null, null);
	    }

        /// <summary>
        ///Convert the patch script for this file into a string.
        /// </summary>
        /// <param name="oldCharset">hint character set to decode the old lines with.</param>
        /// <param name="newCharset">hint character set to decode the new lines with.</param>
        /// <returns>the patch script, as a Unicode string.</returns>
	    public virtual String getScriptText(Encoding oldCharset, Encoding newCharset)
        {
		    return getScriptText(new Encoding[] { oldCharset, newCharset });
	    }

        /// <summary>
        /// Convert the patch script for this file into a string.
        /// </summary>
        /// <param name="charsetGuess">
        /// optional array to suggest the character set to use when
        /// decoding each file's line. If supplied the array must have a
        /// length of <code>{@link #getParentCount()} + 1</code>
        /// representing the old revision character sets and the new
        /// revision character set.
        /// </param>
        /// <returns>the patch script, as a Unicode string.</returns>
	    public virtual String getScriptText(Encoding[] charsetGuess)
        {
		    if (getHunks().Count == 0)
            {
			    // If we have no hunks then we can safely assume the entire
			    // patch is a binary style patch, or a meta-data only style
			    // patch. Either way the encoding of the headers should be
			    // strictly 7-bit US-ASCII and the body is either 7-bit ASCII
			    // (due to the base 85 encoding used for a BinaryHunk) or is
			    // arbitrary noise we have chosen to ignore and not understand
			    // (e.g. the message "Binary files ... differ").
			    //
			    return RawParseUtils.extractBinaryString(buf, startOffset, endOffset);
		    }

		    if (charsetGuess != null && charsetGuess.Length != getParentCount() + 1)
			    throw new ArgumentException("Expected "
					    + (getParentCount() + 1) + " character encoding guesses");

		    if (trySimpleConversion(charsetGuess))
            {
			    Encoding cs = charsetGuess != null ? charsetGuess[0] : null;
			    if (cs == null)
				    cs = Constants.CHARSET;

			    try
                {
				    return RawParseUtils.decodeNoFallback(cs, buf, startOffset, endOffset);
			    }
                catch (EncoderFallbackException)
                {
				    // Try the much slower, more-memory intensive version which
				    // can handle a character set conversion patch.
			    }
		    }

		    StringBuilder r = new StringBuilder(endOffset - startOffset);

		    // Always treat the headers as US-ASCII; Git file names are encoded
		    // in a C style escape if any character has the high-bit set.
		    //
		    int hdrEnd = getHunks()[0].StartOffset;
		    for (int ptr = startOffset; ptr < hdrEnd;) {
			    int eol = Math.Min(hdrEnd, RawParseUtils.nextLF(buf, ptr));
			    r.Append(RawParseUtils.extractBinaryString(buf, ptr, eol));
			    ptr = eol;
		    }

		    String[] files = extractFileLines(charsetGuess);
		    int[] offsets = new int[files.Length];
		    foreach (HunkHeader h in getHunks())
			    h.extractFileLines(r, files, offsets);
		    return r.ToString();
	    }

        private static bool trySimpleConversion(Encoding[] charsetGuess)
        {
		    if (charsetGuess == null)
			    return true;
		    for (int i = 1; i < charsetGuess.Length; i++) {
			    if (charsetGuess[i] != charsetGuess[0])
				    return false;
		    }
		    return true;
	    }

	    private String[] extractFileLines(Encoding[] csGuess)
        {
		    TemporaryBuffer[] tmp = new TemporaryBuffer[getParentCount() + 1];
		    try
            {
			    for (int i = 0; i < tmp.Length; i++)
				    tmp[i] = new TemporaryBuffer();
			    foreach (HunkHeader h in getHunks())
				    h.extractFileLines(tmp);

			    String[] r = new String[tmp.Length];
			    for (int i = 0; i < tmp.Length; i++) {
				    Encoding cs = csGuess != null ? csGuess[i] : null;
				    if (cs == null)
					    cs = Constants.CHARSET;
				    r[i] = RawParseUtils.decode(cs, tmp[i].ToArray());
			    }
			    return r;
		    }
            catch (IOException ioe)
            {
			    throw new Exception("Cannot convert script to text", ioe);
		    }
            finally
            {
			    foreach (TemporaryBuffer b in tmp)
                {
				    if (b != null)
					    b.destroy();
			    }
		    }
	    }

	    /**
	     * Get the old name associated with this file.
	     * <p>
	     * The meaning of the old name can differ depending on the semantic meaning
	     * of this patch:
	     * <ul>
	     * <li><i>file add</i>: always <code>/dev/null</code></li>
	     * <li><i>file modify</i>: always {@link #getNewName()}</li>
	     * <li><i>file delete</i>: always the file being deleted</li>
	     * <li><i>file copy</i>: source file the copy originates from</li>
	     * <li><i>file rename</i>: source file the rename originates from</li>
	     * </ul>
	     *
	     * @return old name for this file.
	     */
	    public String getOldName() {
		    return oldName;
	    }

	    /**
	     * Get the new name associated with this file.
	     * <p>
	     * The meaning of the new name can differ depending on the semantic meaning
	     * of this patch:
	     * <ul>
	     * <li><i>file add</i>: always the file being created</li>
	     * <li><i>file modify</i>: always {@link #getOldName()}</li>
	     * <li><i>file delete</i>: always <code>/dev/null</code></li>
	     * <li><i>file copy</i>: destination file the copy ends up at</li>
	     * <li><i>file rename</i>: destination file the rename ends up at/li>
	     * </ul>
	     *
	     * @return new name for this file.
	     */
	    public String getNewName()
        {
		    return newName;
	    }

	    /** @return the old file mode, if described in the patch */
	    public virtual FileMode getOldMode()
        {
		    return oldMode;
	    }

	    /** @return the new file mode, if described in the patch */
	    public FileMode getNewMode()
        {
		    return newMode;
	    }

	    /** @return the type of change this patch makes on {@link #getNewName()} */
	    public ChangeType getChangeType()
        {
		    return changeType;
	    }

	    /**
	     * @return similarity score between {@link #getOldName()} and
	     *         {@link #getNewName()} if {@link #getChangeType()} is
	     *         {@link ChangeType#COPY} or {@link ChangeType#RENAME}.
	     */
	    public int getScore()
        {
		    return score;
	    }

	    /**
	     * Get the old object id from the <code>index</code>.
	     *
	     * @return the object id; null if there is no index line
	     */
	    public virtual AbbreviatedObjectId getOldId()
        {
		    return oldId;
	    }

	    /**
	     * Get the new object id from the <code>index</code>.
	     *
	     * @return the object id; null if there is no index line
	     */
	    public AbbreviatedObjectId getNewId()
        {
		    return newId;
	    }

	    /** @return style of patch used to modify this file */
	    public PatchType getPatchType()
        {
		    return patchType;
	    }

	    /** @return true if this patch modifies metadata about a file */
	    public bool hasMetaDataChanges()
        {
		    return changeType != ChangeType.MODIFY || newMode != oldMode;
	    }

	    /** @return hunks altering this file; in order of appearance in patch */
	    public List<HunkHeader> getHunks()
        {
		    if (hunks == null)
			    return new List<HunkHeader>();
		    return hunks;
	    }

        public void addHunk(HunkHeader h)
        {
		    if (h.File != this)
		    {
		    	throw new ArgumentException("Hunk belongs to another file");
		    }
		    if (hunks == null)
		    {
		    	hunks = new List<HunkHeader>();
		    }
		    hunks.Add(h);
	    }

	    public virtual HunkHeader newHunkHeader(int offset)
        {
		    return new HunkHeader(this, offset);
	    }

	    /** @return if a {@link PatchType#GIT_BINARY}, the new-image delta/literal */
	    public BinaryHunk getForwardBinaryHunk()
        {
		    return forwardBinaryHunk;
	    }

	    /** @return if a {@link PatchType#GIT_BINARY}, the old-image delta/literal */
	    public BinaryHunk getReverseBinaryHunk()
        {
		    return reverseBinaryHunk;
	    }

	    /// <summary>
		/// Returns a list describing the content edits performed on this file.
	    /// </summary>
	    /// <returns></returns>
	    public EditList ToEditList()
        {
		    EditList r = new EditList();
			hunks.ForEach(hunk => r.AddRange(hunk.ToEditList()));
		    return r;
	    }

	    /**
	     * Parse a "diff --git" or "diff --cc" line.
	     *
	     * @param ptr
	     *            first character after the "diff --git " or "diff --cc " part.
	     * @param end
	     *            one past the last position to parse.
	     * @return first character after the LF at the end of the line; -1 on error.
	     */
	    public int parseGitFileName(int ptr, int end)
        {
		    int eol = RawParseUtils.nextLF(buf, ptr);
		    int bol = ptr;
		    if (eol >= end)
            {
			    return -1;
		    }

		    // buffer[ptr..eol] looks like "a/foo b/foo\n". After the first
		    // A regex to match this is "^[^/]+/(.*?) [^/+]+/\1\n$". There
		    // is only one way to split the line such that text to the left
		    // of the space matches the text to the right, excluding the part
		    // before the first slash.
		    //

		    int aStart = RawParseUtils.nextLF(buf, ptr, (byte)'/');
		    if (aStart >= eol)
			    return eol;

		    while (ptr < eol) {
			    int sp = RawParseUtils.nextLF(buf, ptr, (byte)' ');
			    if (sp >= eol) {
				    // We can't split the header, it isn't valid.
				    // This may be OK if this is a rename patch.
				    //
				    return eol;
			    }
			    int bStart = RawParseUtils.nextLF(buf, sp, (byte)'/');
			    if (bStart >= eol)
				    return eol;

			    // If buffer[aStart..sp - 1] = buffer[bStart..eol - 1]
			    // we have a valid split.
			    //
			    if (eq(aStart, sp - 1, bStart, eol - 1)) {
				    if (buf[bol] == '"') {
					    // We're a double quoted name. The region better end
					    // in a double quote too, and we need to decode the
					    // characters before reading the name.
					    //
					    if (buf[sp - 2] != '"') {
						    return eol;
					    }
					    oldName = QuotedString.GitPathStyle.GIT_PATH.dequote(buf, bol, sp - 1);
					    oldName = p1(oldName);
				    } else {
					    oldName = RawParseUtils.decode(Constants.CHARSET, buf, aStart, sp - 1);
				    }
				    newName = oldName;
				    return eol;
			    }

			    // This split wasn't correct. Move past the space and try
			    // another split as the space must be part of the file name.
			    //
			    ptr = sp;
		    }

		    return eol;
	    }

	    public virtual int parseGitHeaders(int ptr, int end)
        {
		    while (ptr < end)
            {
			    int eol = RawParseUtils.nextLF(buf, ptr);
			    if (isHunkHdr(buf, ptr, eol) >= 1)
                {
				    // First hunk header; break out and parse them later.
				    break;

                }
                else if (RawParseUtils.match(buf, ptr, OLD_NAME) >= 0)
                {
				    parseOldName(ptr, eol);

                }
                else if (RawParseUtils.match(buf, ptr, NEW_NAME) >= 0)
                {
				    parseNewName(ptr, eol);

                }
                else if (RawParseUtils.match(buf, ptr, OLD_MODE) >= 0)
                {
				    oldMode = parseFileMode(ptr + OLD_MODE.Length, eol);

                }
                else if (RawParseUtils.match(buf, ptr, NEW_MODE) >= 0)
                {
				    newMode = parseFileMode(ptr + NEW_MODE.Length, eol);

                }
                else if (RawParseUtils.match(buf, ptr, DELETED_FILE_MODE) >= 0)
                {
				    oldMode = parseFileMode(ptr + DELETED_FILE_MODE.Length, eol);
				    newMode = FileMode.Missing;
				    changeType = ChangeType.DELETE;

                }
                else if (RawParseUtils.match(buf, ptr, NEW_FILE_MODE) >= 0)
                {
				    parseNewFileMode(ptr, eol);

                }
                else if (RawParseUtils.match(buf, ptr, COPY_FROM) >= 0)
                {
				    oldName = parseName(oldName, ptr + COPY_FROM.Length, eol);
				    changeType = ChangeType.COPY;

                }
                else if (RawParseUtils.match(buf, ptr, COPY_TO) >= 0)
                {
				    newName = parseName(newName, ptr + COPY_TO.Length, eol);
				    changeType = ChangeType.COPY;

                }
                else if (RawParseUtils.match(buf, ptr, RENAME_OLD) >= 0)
                {
				    oldName = parseName(oldName, ptr + RENAME_OLD.Length, eol);
				    changeType = ChangeType.RENAME;

                }
                else if (RawParseUtils.match(buf, ptr, RENAME_NEW) >= 0)
                {
				    newName = parseName(newName, ptr + RENAME_NEW.Length, eol);
				    changeType = ChangeType.RENAME;

                }
                else if (RawParseUtils.match(buf, ptr, RENAME_FROM) >= 0)
                {
				    oldName = parseName(oldName, ptr + RENAME_FROM.Length, eol);
				    changeType = ChangeType.RENAME;

                }
                else if (RawParseUtils.match(buf, ptr, RENAME_TO) >= 0)
                {
				    newName = parseName(newName, ptr + RENAME_TO.Length, eol);
				    changeType = ChangeType.RENAME;

                }
                else if (RawParseUtils.match(buf, ptr, SIMILARITY_INDEX) >= 0)
                {
                    score = RawParseUtils.parseBase10(buf, ptr + SIMILARITY_INDEX.Length, null);

                }
                else if (RawParseUtils.match(buf, ptr, DISSIMILARITY_INDEX) >= 0)
                {
                    score = RawParseUtils.parseBase10(buf, ptr + DISSIMILARITY_INDEX.Length, null);

                }
                else if (RawParseUtils.match(buf, ptr, INDEX) >= 0)
                {
				    parseIndexLine(ptr + INDEX.Length, eol);

			    }
                else
                {
				    // Probably an empty patch (stat dirty).
				    break;
			    }

			    ptr = eol;
		    }
		    return ptr;
	    }

	    public void parseOldName(int ptr, int eol)
        {
		    oldName = p1(parseName(oldName, ptr + OLD_NAME.Length, eol));
		    if (oldName == DEV_NULL)
			    changeType = ChangeType.ADD;
	    }

        public void parseNewName(int ptr, int eol)
        {
		    newName = p1(parseName(newName, ptr + NEW_NAME.Length, eol));
		    if (newName == DEV_NULL)
			    changeType = ChangeType.DELETE;
	    }

        public virtual void parseNewFileMode(int ptr, int eol)
        {
		    oldMode = FileMode.Missing;
		    newMode = parseFileMode(ptr + NEW_FILE_MODE.Length, eol);
		    changeType = ChangeType.ADD;
	    }

	    public int parseTraditionalHeaders(int ptr, int end)
        {
		    while (ptr < end)
            {
                int eol = RawParseUtils.nextLF(buf, ptr);
			    if (isHunkHdr(buf, ptr, eol) >= 1)
                {
				    // First hunk header; break out and parse them later.
				    break;
                }

                if (RawParseUtils.match(buf, ptr, OLD_NAME) >= 0)
                {
				    parseOldName(ptr, eol);
                }
                else if (RawParseUtils.match(buf, ptr, NEW_NAME) >= 0)
                {
				    parseNewName(ptr, eol);
			    }
                else
                {
				    // Possibly an empty patch.
				    break;
			    }

			    ptr = eol;
		    }

		    return ptr;
	    }

	    private String parseName(String expect, int ptr, int end)
        {
		    if (ptr == end)
			    return expect;

		    String r;
		    if (buf[ptr] == '"')
            {
			    // New style GNU diff format
			    //
			    r = QuotedString.GitPathStyle.GIT_PATH.dequote(buf, ptr, end - 1);
		    }
            else
            {
			    // Older style GNU diff format, an optional tab ends the name.
			    //
			    int tab = end;
			    while (ptr < tab && buf[tab - 1] != '\t')
				    tab--;
			    if (ptr == tab)
				    tab = end;
                r = RawParseUtils.decode(Constants.CHARSET, buf, ptr, tab - 1);
		    }

		    if (r.Equals(DEV_NULL))
			    r = DEV_NULL;
		    return r;
	    }

	    private static String p1(String r)
        {
		    int s = r.IndexOf('/');
		    return s > 0 ? r.Substring(s + 1) : r;
	    }

	    public FileMode parseFileMode(int ptr, int end)
        {
		    int tmp = 0;
		    while (ptr < end - 1) {
			    tmp <<= 3;
			    tmp += buf[ptr++] - '0';
		    }
		    return FileMode.FromBits(tmp);
	    }

        public virtual void parseIndexLine(int ptr, int end)
        {
		    // "index $asha1..$bsha1[ $mode]" where $asha1 and $bsha1
		    // can be unique abbreviations
		    //
            int dot2 = RawParseUtils.nextLF(buf, ptr, (byte)'.');
            int mode = RawParseUtils.nextLF(buf, dot2, (byte)' ');

		    oldId = AbbreviatedObjectId.FromString(buf, ptr, dot2 - 1);
		    newId = AbbreviatedObjectId.FromString(buf, dot2 + 1, mode - 1);

		    if (mode < end)
			    newMode = oldMode = parseFileMode(mode, end);
	    }

	    private bool eq(int aPtr, int aEnd, int bPtr, int bEnd)
        {
		    if (aEnd - aPtr != bEnd - bPtr)
            {
			    return false;
		    }
		    while (aPtr < aEnd)
            {
			    if (buf[aPtr++] != buf[bPtr++])
				    return false;
		    }
		    return true;
	    }

	    /**
	     * Determine if this is a patch hunk header.
	     *
	     * @param buf
	     *            the buffer to scan
	     * @param start
	     *            first position in the buffer to evaluate
	     * @param end
	     *            last position to consider; usually the end of the buffer (
	     *            <code>buf.length</code>) or the first position on the next
	     *            line. This is only used to avoid very long runs of '@' from
	     *            killing the scan loop.
	     * @return the number of "ancestor revisions" in the hunk header. A
	     *         traditional two-way diff ("@@ -...") returns 1; a combined diff
	     *         for a 3 way-merge returns 3. If this is not a hunk header, 0 is
	     *         returned instead.
	     */
	    public static int isHunkHdr(byte[] buf, int start, int end)
        {
		    int ptr = start;
		    while ((ptr < end) && (buf[ptr] == '@'))
			    ptr++;
		    if ((ptr - start) < 2)
			    return 0;
		    if ((ptr == end) || (buf[ptr++] != ' '))
			    return 0;
		    if ((ptr == end) || (buf[ptr++] != '-'))
			    return 0;
		    return (ptr - 3) - start;
	    }
    }
}