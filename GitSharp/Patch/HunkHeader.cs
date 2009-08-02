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

using System.IO;
using System.Text;
using GitSharp.Diff;
using GitSharp.Util;
using GitSharp.Patch;

namespace GitSharp.Patch
{
    /** Hunk header describing the layout of a single block of lines */
    public class HunkHeader
    {
	    /** Details about an old image of the file. */
	    public abstract class OldImage
        {
		    /** First line number the hunk starts on in this file. */
		    public int startLine;

		    /** Total number of lines this hunk covers in this file. */
		    public int lineCount;

		    /** Number of lines deleted by the post-image from this file. */
		    public int nDeleted;

		    /** Number of lines added by the post-image not in this file. */
		    public int nAdded;

		    /** @return first line number the hunk starts on in this file. */
		    public int getStartLine()
            {
			    return startLine;
		    }

		    /** @return total number of lines this hunk covers in this file. */
		    public int getLineCount()
            {
			    return lineCount;
		    }

		    /** @return number of lines deleted by the post-image from this file. */
		    public int getLinesDeleted()
            {
			    return nDeleted;
		    }

		    /** @return number of lines added by the post-image not in this file. */
		    public int getLinesAdded()
            {
			    return nAdded;
		    }

		    /** @return object id of the pre-image file. */
		    public abstract AbbreviatedObjectId getId();
	    }

        public readonly FileHeader file;

	    /** Offset within {@link #file}.buf to the "@@ -" line. */
        public readonly int startOffset;

	    /** Position 1 past the end of this hunk within {@link #file}'s buf. */
	    public int endOffset;

	    private readonly OldImage old;

	    /** First line number in the post-image file where the hunk starts */
        public int newStartLine;

	    /** Total number of post-image lines this hunk covers (context + inserted) */
        public int newLineCount;

	    /** Total number of lines of context appearing in this hunk */
        public int nContext;

        private class OldImageInstance : OldImage
        {
            private FileHeader fh;

            public OldImageInstance(FileHeader fh)
            {
                this.fh = fh;
            }

            public override AbbreviatedObjectId getId()
            {
                return fh.getOldId();
            }
        }

        public HunkHeader(FileHeader fh, int offset)
            :this(fh, offset, new OldImageInstance(fh))
        {}

	    public HunkHeader(FileHeader fh, int offset, OldImage oi)
        {
		    file = fh;
		    startOffset = offset;
		    old = oi;
	    }

	    /** @return header for the file this hunk applies to */
	    public FileHeader getFileHeader()
        {
		    return file;
	    }

	    /** @return the byte array holding this hunk's patch script. */
	    public byte[] getBuffer()
        {
		    return file.buf;
	    }

	    /** @return offset the start of this hunk in {@link #getBuffer()}. */
	    public int getStartOffset()
        {
		    return startOffset;
	    }

	    /** @return offset one past the end of the hunk in {@link #getBuffer()}. */
	    public int getEndOffset()
        {
		    return endOffset;
	    }

	    /** @return information about the old image mentioned in this hunk. */
	    public OldImage getOldImage()
        {
		    return old;
	    }

	    /** @return first line number in the post-image file where the hunk starts */
	    public int getNewStartLine()
        {
		    return newStartLine;
	    }

	    /** @return Total number of post-image lines this hunk covers */
	    public int getNewLineCount()
        {
		    return newLineCount;
	    }

	    /** @return total number of lines of context appearing in this hunk */
	    public int getLinesContext()
        {
		    return nContext;
	    }

	    /** @return a list describing the content edits performed within the hunk. */
	    public EditList toEditList()
        {
		    EditList r = new EditList();
		    byte[] buf = file.buf;
		    int c = RawParseUtils.nextLF(buf, startOffset);
		    int oLine = old.startLine;
		    int nLine = newStartLine;
		    Edit inEdit = null;

            bool break_scan = false;
		    for (; c < endOffset; c = RawParseUtils.nextLF(buf, c)) {
			    switch (buf[c]) {
			    case (byte)' ':
			    case (byte)'\n':
				    inEdit = null;
				    oLine++;
				    nLine++;
				    continue;

			    case (byte)'-':
				    if (inEdit == null) {
					    inEdit = new Edit(oLine - 1, nLine - 1);
					    r.Add(inEdit);
				    }
				    oLine++;
				    inEdit.extendA();
				    continue;

			    case (byte)'+':
				    if (inEdit == null) {
					    inEdit = new Edit(oLine - 1, nLine - 1);
					    r.Add(inEdit);
				    }
				    nLine++;
				    inEdit.extendB();
				    continue;

			    case (byte)'\\': // Matches "\ No newline at end of file"
				    continue;

			    default:
				    break_scan = true;
                    break;
			    }
                if (break_scan)
                    break;
		    }
		    return r;
	    }

	    public void parseHeader()
        {
		    // Parse "@@ -236,9 +236,9 @@ protected boolean"
		    //
		    byte[] buf = file.buf;
		    MutableInteger ptr = new MutableInteger();
		    ptr.value = RawParseUtils.nextLF(buf, startOffset, (byte)' ');
		    old.startLine = -RawParseUtils.parseBase10(buf, ptr.value, ptr);
		    if (buf[ptr.value] == ',')
			    old.lineCount = RawParseUtils.parseBase10(buf, ptr.value + 1, ptr);
		    else
			    old.lineCount = 1;

		    newStartLine = RawParseUtils.parseBase10(buf, ptr.value + 1, ptr);
		    if (buf[ptr.value] == ',')
			    newLineCount = RawParseUtils.parseBase10(buf, ptr.value + 1, ptr);
		    else
			    newLineCount = 1;
	    }

	    public int parseBody(Patch script, int end) {
		    byte[] buf = file.buf;
		    int c = RawParseUtils.nextLF(buf, startOffset), last = c;

		    old.nDeleted = 0;
		    old.nAdded = 0;

            bool break_scan = false;
		    for (; c < end; last = c, c = RawParseUtils.nextLF(buf, c))
            {
			    switch (buf[c])
                {
			    case (byte)' ':
			    case (byte)'\n':
				    nContext++;
				    continue;

			    case (byte)'-':
				    old.nDeleted++;
				    continue;

			    case (byte)'+':
				    old.nAdded++;
				    continue;

			    case (byte)'\\': // Matches "\ No newline at end of file"
				    continue;

			    default:
                    break_scan = true;
				    break;
			    }
                if (break_scan)
                    break;
		    }

		    if (last < end && nContext + old.nDeleted - 1 == old.lineCount
				    && nContext + old.nAdded == newLineCount
				    && RawParseUtils.match(buf, last, Patch.SIG_FOOTER) >= 0)
            {
			    // This is an extremely common occurrence of "corruption".
			    // Users add footers with their signatures after this mark,
			    // and git diff adds the git executable version number.
			    // Let it slide; the hunk otherwise looked sound.
			    //
			    old.nDeleted--;
			    return last;
		    }

		    if (nContext + old.nDeleted < old.lineCount)
            {
			    int missingCount = old.lineCount - (nContext + old.nDeleted);
			    script.error(buf, startOffset, "Truncated hunk, at least "
					    + missingCount + " old lines is missing");

		    }
            else if (nContext + old.nAdded < newLineCount)
            {
			    int missingCount = newLineCount - (nContext + old.nAdded);
			    script.error(buf, startOffset, "Truncated hunk, at least "
					    + missingCount + " new lines is missing");

		    }
            else if (nContext + old.nDeleted > old.lineCount
				    || nContext + old.nAdded > newLineCount)
            {
			    string oldcnt = old.lineCount + ":" + newLineCount;
			    string newcnt = (nContext + old.nDeleted) + ":"
					    + (nContext + old.nAdded);
			    script.warn(buf, startOffset, "Hunk header " + oldcnt
					    + " does not match body line count of " + newcnt);
		    }

		    return c;
	    }

	    public void extractFileLines(TemporaryBuffer[] outStream)
        {
		    byte[] buf = file.buf;
		    int ptr = startOffset;
		    int eol = RawParseUtils.nextLF(buf, ptr);
		    if (endOffset <= eol)
			    return;

		    // Treat the hunk header as though it were from the ancestor,
		    // as it may have a function header appearing after it which
		    // was copied out of the ancestor file.
		    //
		    outStream[0].write(buf, ptr, eol - ptr);

            bool break_scan = false;
		    for (ptr = eol; ptr < endOffset; ptr = eol)
            {
			    eol = RawParseUtils.nextLF(buf, ptr);
			    switch (buf[ptr]) {
			    case (byte)' ':
			    case (byte)'\n':
			    case (byte)'\\':
				    outStream[0].write(buf, ptr, eol - ptr);
				    outStream[1].write(buf, ptr, eol - ptr);
				    break;
			    case (byte)'-':
				    outStream[0].write(buf, ptr, eol - ptr);
				    break;
			    case (byte)'+':
				    outStream[1].write(buf, ptr, eol - ptr);
				    break;
			    default:
				    break_scan = true;
                    break;
			    }
                if (break_scan)
                    break;
		    }
	    }

	    public void extractFileLines(StringBuilder sb, string[] text, int[] offsets)
        {
		    byte[] buf = file.buf;
		    int ptr = startOffset;
		    int eol = RawParseUtils.nextLF(buf, ptr);
		    if (endOffset <= eol)
			    return;
		    copyLine(sb, text, offsets, 0);

            bool break_scan = false;
		    for (ptr = eol; ptr < endOffset; ptr = eol)
            {
			    eol = RawParseUtils.nextLF(buf, ptr);
			    switch (buf[ptr]) {
			    case (byte)' ':
			    case (byte)'\n':
			    case (byte)'\\':
				    copyLine(sb, text, offsets, 0);
				    skipLine(text, offsets, 1);
				    break;
			    case (byte)'-':
				    copyLine(sb, text, offsets, 0);
				    break;
			    case (byte)'+':
				    copyLine(sb, text, offsets, 1);
				    break;
			    default:
                    break_scan = true;
				    break;
			    }
                if (break_scan)
                    break;
		    }
	    }

	    public void copyLine(StringBuilder sb, string[] text, int[] offsets, int fileIdx)
        {
		    string s = text[fileIdx];
		    int start = offsets[fileIdx];
		    int end = s.IndexOf('\n', start);
		    if (end < 0)
			    end = s.Length;
		    else
			    end++;
		    sb.Append(s, start, end - start);
		    offsets[fileIdx] = end;
	    }

	    public void skipLine(string[] text, int[] offsets, int fileIdx)
        {
		    string s = text[fileIdx];
		    int end = s.IndexOf('\n', offsets[fileIdx]);
		    offsets[fileIdx] = end < 0 ? s.Length : end + 1;
	    }
    }
}