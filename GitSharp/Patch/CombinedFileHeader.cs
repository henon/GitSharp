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

using System.Collections.Generic;
using System.Text;
using GitSharp.Util;

namespace GitSharp.Patch
{
    /**
     * A file in the Git "diff --cc" or "diff --combined" format.
     * <p>
     * A combined diff shows an n-way comparison between two or more ancestors and
     * the final revision. Its primary function is to perform code reviews on a
     * merge which introduces changes not in any ancestor.
     */
    public class CombinedFileHeader : FileHeader
    {
        private static readonly byte[] MODE = Constants.encodeASCII("mode ");

        private AbbreviatedObjectId[] oldIds;

        private FileMode[] oldModes;

        public CombinedFileHeader(byte[] b, int offset)
            : base(b, offset)
        { }

        /** @return number of ancestor revisions mentioned in this diff. */
        public override int getParentCount()
        {
            return oldIds.Length;
        }

        /** @return get the file mode of the first parent. */
        public override FileMode getOldMode()
        {
            return getOldMode(0);
        }

        /**
         * Get the file mode of the nth ancestor
         *
         * @param nthParent
         *            the ancestor to get the mode of
         * @return the mode of the requested ancestor.
         */
        public FileMode getOldMode(int nthParent)
        {
            return oldModes[nthParent];
        }

        /** @return get the object id of the first parent. */
        public override AbbreviatedObjectId getOldId()
        {
            return getOldId(0);
        }

        /**
         * Get the ObjectId of the nth ancestor
         *
         * @param nthParent
         *            the ancestor to get the object id of
         * @return the id of the requested ancestor.
         */
        public AbbreviatedObjectId getOldId(int nthParent)
        {
            return oldIds[nthParent];
        }

        public override string getScriptText(Encoding ocs, Encoding ncs)
        {
            Encoding[] cs = new Encoding[getParentCount() + 1];
            for (int i = 0; i < cs.Length; i++)
                cs[i] = ocs;
            cs[getParentCount()] = ncs;
            return getScriptText(cs);
        }
        
        public override int parseGitHeaders(int ptr, int end)
        {
            while (ptr < end)
            {
                int eol = RawParseUtils.nextLF(buf, ptr);
                if (isHunkHdr(buf, ptr, end) >= 1)
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
                else if (RawParseUtils.match(buf, ptr, INDEX) >= 0)
                {
                    parseIndexLine(ptr + INDEX.Length, eol);

                }
                else if (RawParseUtils.match(buf, ptr, MODE) >= 0)
                {
                    parseModeLine(ptr + MODE.Length, eol);

                }
                else if (RawParseUtils.match(buf, ptr, NEW_FILE_MODE) >= 0)
                {
                    parseNewFileMode(ptr, eol);

                }
                else if (RawParseUtils.match(buf, ptr, DELETED_FILE_MODE) >= 0)
                {
                    parseDeletedFileMode(ptr + DELETED_FILE_MODE.Length, eol);

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

        public override void parseIndexLine(int ptr, int eol)
        {
            // "index $asha1,$bsha1..$csha1"
            //
            List<AbbreviatedObjectId> ids = new List<AbbreviatedObjectId>();
            while (ptr < eol)
            {
                int comma = RawParseUtils.nextLF(buf, ptr, (byte)',');
                if (eol <= comma)
                    break;
                ids.Add(AbbreviatedObjectId.FromString(buf, ptr, comma - 1));
                ptr = comma;
            }

            oldIds = new AbbreviatedObjectId[ids.Count + 1];
            ids.CopyTo(oldIds);
            int dot2 = RawParseUtils.nextLF(buf, ptr, (byte)'.');
            oldIds[oldIds.Length - 1] = AbbreviatedObjectId.FromString(buf, ptr, dot2 - 1);
            newId = AbbreviatedObjectId.FromString(buf, dot2 + 1, eol - 1);
            oldModes = new FileMode[oldIds.Length];
        }

        public override void parseNewFileMode(int ptr, int eol)
        {
            for (int i = 0; i < oldModes.Length; i++)
                oldModes[i] = FileMode.Missing;
            base.parseNewFileMode(ptr, eol);
        }

        public override HunkHeader newHunkHeader(int offset)
        {
            return new CombinedHunkHeader(this, offset);
        }

        private void parseModeLine(int ptr, int eol)
        {
            // "mode $amode,$bmode..$cmode"
            //
            int n = 0;
            while (ptr < eol)
            {
                int comma = RawParseUtils.nextLF(buf, ptr, (byte)',');
                if (eol <= comma)
                    break;
                oldModes[n++] = parseFileMode(ptr, comma);
                ptr = comma;
            }
            int dot2 = RawParseUtils.nextLF(buf, ptr, (byte)'.');
            oldModes[n] = parseFileMode(ptr, dot2);
            newMode = parseFileMode(dot2 + 1, eol);
        }

        private void parseDeletedFileMode(int ptr, int eol)
        {
            // "deleted file mode $amode,$bmode"
            //
            changeType = ChangeType.DELETE;
            int n = 0;
            while (ptr < eol)
            {
                int comma = RawParseUtils.nextLF(buf, ptr, (byte)',');
                if (eol <= comma)
                    break;
                oldModes[n++] = parseFileMode(ptr, comma);
                ptr = comma;
            }
            oldModes[n] = parseFileMode(ptr, eol);
            newMode = FileMode.Missing;
        }
    }
}