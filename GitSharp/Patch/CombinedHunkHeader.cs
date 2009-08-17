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
using GitSharp;
using GitSharp.Util;

namespace GitSharp.Patch
{
    /** Hunk header for a hunk appearing in a "diff --cc" style patch. */
    public class CombinedHunkHeader : HunkHeader
    {
        private class CombinedOldImageInstance : CombinedOldImage
        {
            private CombinedFileHeader fh;
            private int imagePos;

            public CombinedOldImageInstance(CombinedFileHeader fh, int imagePos)
            {
                this.fh = fh;
                this.imagePos = imagePos;
            }

            public override AbbreviatedObjectId getId()
            {
			    return fh.getOldId(imagePos);
			}
        }

	    private abstract class CombinedOldImage : OldImage
        {
		    public int nContext;
	    }

	    private CombinedOldImageInstance[] old;

	    public CombinedHunkHeader(CombinedFileHeader fh, int offset)
            :base(fh, offset, null)
        {
		    old = new CombinedOldImageInstance[fh.getParentCount()];
		    for (int i = 0; i < old.Length; i++)
            {
			    int imagePos = i;
			    old[i] = new CombinedOldImageInstance(fh, imagePos);
		    }
	    }

	    public new CombinedFileHeader getFileHeader()
        {
		    return (CombinedFileHeader) base.getFileHeader();
	    }

	    public override OldImage getOldImage()
        {
		    return getOldImage(0);
	    }

	    /**
	     * Get the OldImage data related to the nth ancestor
	     *
	     * @param nthParent
	     *            the ancestor to get the old image data of
	     * @return image data of the requested ancestor.
	     */
	    public OldImage getOldImage(int nthParent)
        {
		    return old[nthParent];
	    }

	    public override void parseHeader()
        {
		    // Parse "@@@ -55,12 -163,13 +163,15 @@@ protected boolean"
		    //
		    byte[] buf = file.buf;
		    MutableInteger ptr = new MutableInteger();
		    ptr.value = RawParseUtils.nextLF(buf, startOffset, (byte)' ');

		    for (int n = 0; n < old.Length; n++) {
			    old[n].startLine = -RawParseUtils.parseBase10(buf, ptr.value, ptr);
			    if (buf[ptr.value] == ',')
				    old[n].lineCount = RawParseUtils.parseBase10(buf, ptr.value + 1, ptr);
			    else
				    old[n].lineCount = 1;
		    }

		    newStartLine = RawParseUtils.parseBase10(buf, ptr.value + 1, ptr);
		    if (buf[ptr.value] == ',')
			    newLineCount = RawParseUtils.parseBase10(buf, ptr.value + 1, ptr);
		    else
			    newLineCount = 1;
	    }

	    public override int parseBody(Patch script, int end)
        {
		    byte[] buf = file.buf;
		    int c = RawParseUtils.nextLF(buf, startOffset);

		    foreach(CombinedOldImageInstance o in old)
            {
			    o.nDeleted = 0;
			    o.nAdded = 0;
			    o.nContext = 0;
		    }
		    nContext = 0;
		    int nAdded = 0;

		    for (int eol; c < end; c = eol)
            {
			    eol = RawParseUtils.nextLF(buf, c);

			    if (eol - c < old.Length + 1) {
				    // Line isn't long enough to mention the state of each
				    // ancestor. It must be the end of the hunk.
				    break;
			    }

                bool break_scan = false;
			    switch (buf[c]) {
			    case (byte)' ':
			    case (byte)'-':
			    case (byte)'+':
				    break;

			    default:
				    // Line can't possibly be part of this hunk; the first
				    // ancestor information isn't recognizable.
				    //
                    break_scan = true;
				    break;
			    }
                if (break_scan)
                    break;

			    int localcontext = 0;
			    for (int ancestor = 0; ancestor < old.Length; ancestor++)
                {
				    switch (buf[c + ancestor])
                    {
				    case (byte)' ':
					    localcontext++;
					    old[ancestor].nContext++;
					    continue;

				    case (byte)'-':
					    old[ancestor].nDeleted++;
					    continue;

				    case (byte)'+':
					    old[ancestor].nAdded++;
					    nAdded++;
					    continue;

				    default:
					    break_scan = true;
                        break;
				    }
                    if (break_scan)
                        break;
			    }
                if (break_scan)
                    break;

			    if (localcontext == old.Length)
				    nContext++;
		    }

		    for (int ancestor = 0; ancestor < old.Length; ancestor++) {
			    CombinedOldImage o = old[ancestor];
			    int cmp = o.nContext + o.nDeleted;
			    if (cmp < o.lineCount) {
				    int missingCnt = o.lineCount - cmp;
				    script.error(buf, startOffset, "Truncated hunk, at least "
						    + missingCnt + " lines is missing for ancestor "
						    + (ancestor + 1));
			    }
		    }

		    if (nContext + nAdded < newLineCount) {
			    int missingCount = newLineCount - (nContext + nAdded);
			    script.error(buf, startOffset, "Truncated hunk, at least "
					    + missingCount + " new lines is missing");
		    }

		    return c;
	    }

	    public void extractFileLines(Stream[] outStream)
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
		    outStream[0].Write(buf, ptr, eol - ptr);

		    //SCAN: 
            for (ptr = eol; ptr < endOffset; ptr = eol) {
			    eol = RawParseUtils.nextLF(buf, ptr);

			    if (eol - ptr < old.Length + 1) {
				    // Line isn't long enough to mention the state of each
				    // ancestor. It must be the end of the hunk.
				    break;
			    }

                bool break_scan = false;
			    switch (buf[ptr])
                {
			    case (byte)' ':
			    case (byte)'-':
			    case (byte)'+':
				    break;

			    default:
				    // Line can't possibly be part of this hunk; the first
				    // ancestor information isn't recognizable.
				    //
                    break_scan = true;
				    break;
			    }
                if (break_scan)
                    break;

			    int delcnt = 0;
			    for (int ancestor = 0; ancestor < old.Length; ancestor++)
                {
				    switch (buf[ptr + ancestor])
                    {
				    case (byte)'-':
					    delcnt++;
					    outStream[ancestor].Write(buf, ptr, eol - ptr);
					    continue;

				    case (byte)' ':
					    outStream[ancestor].Write(buf, ptr, eol - ptr);
					    continue;

				    case (byte)'+':
					    continue;

				    default:
                        break_scan = true;
					    break;
				    }

                    if (break_scan)
                        break;
			    }

                if (break_scan)
                        break;

			    if (delcnt < old.Length) {
				    // This line appears in the new file if it wasn't deleted
				    // relative to all ancestors.
				    //
				    outStream[old.Length].Write(buf, ptr, eol - ptr);
			    }
		    }
	    }

	    public override void extractFileLines(StringBuilder sb, string[] text, int[] offsets)
        {
		    byte[] buf = file.buf;
		    int ptr = startOffset;
		    int eol = RawParseUtils.nextLF(buf, ptr);
		    if (endOffset <= eol)
			    return;
		    copyLine(sb, text, offsets, 0);
		    for (ptr = eol; ptr < endOffset; ptr = eol)
            {
			    eol = RawParseUtils.nextLF(buf, ptr);

			    if (eol - ptr < old.Length + 1)
                {
				    // Line isn't long enough to mention the state of each
				    // ancestor. It must be the end of the hunk.
				    break;
			    }

                bool break_scan = false;
			    switch (buf[ptr])
                {
			    case (byte)' ':
			    case (byte)'-':
			    case (byte)'+':
				    break;

			    default:
				    // Line can't possibly be part of this hunk; the first
				    // ancestor information isn't recognizable.
				    //
                    break_scan = true;
				    break;
			    }
                if (break_scan)
                    break;

			    bool copied = false;
			    for (int ancestor = 0; ancestor < old.Length; ancestor++)
                {
				    switch (buf[ptr + ancestor])
                    {
				    case (byte)' ':
				    case (byte)'-':
					    if (copied)
						    skipLine(text, offsets, ancestor);
					    else
                        {
						    copyLine(sb, text, offsets, ancestor);
						    copied = true;
					    }
					    continue;

				    case (byte)'+':
					    continue;

				    default:
					    break_scan = true;
                        break;
				    }
                    if (break_scan)
                        break;
			    }
                if (break_scan)
                    break;

			    if (!copied) {
				    // If none of the ancestors caused the copy then this line
				    // must be new across the board, so it only appears in the
				    // text of the new file.
				    //
				    copyLine(sb, text, offsets, old.Length);
			    }
		    }
	    }
    }
}