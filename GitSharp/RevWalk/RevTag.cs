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

using GitSharp.Util;
using GitSharp.Exceptions;
using System.Text;
namespace GitSharp.RevWalk
{


    /** An annotated tag. */
    public class RevTag : RevObject
    {
        private RevObject @object;

        private byte[] buffer;

        private string name;

        /**
         * Create a new tag reference.
         * 
         * @param id
         *            @object name for the tag.
         */
        internal RevTag(AnyObjectId id)
            : base(id)
        {
        }


        internal override void parse(RevWalk walk)
        {
            ObjectLoader ldr = walk.db.openObject(walk.curs, this);
            if (ldr == null)
                throw new MissingObjectException(this, Constants.TYPE_TAG);
            byte[] data = ldr.CachedBytes;
            if (Constants.OBJ_TAG != ldr.Type)
                throw new IncorrectObjectTypeException(this, Constants.TYPE_TAG);
            parseCanonical(walk, data);
        }

        public void parseCanonical(RevWalk walk, byte[] rawTag)
        {
            MutableInteger pos = new MutableInteger();
            int oType;

            pos.value = 53; // "@object $sha1\ntype "
            oType = Constants.decodeTypeString(this, rawTag, (byte)'\n', pos);
            walk.idBuffer.FromString(rawTag, 7);
            @object = walk.lookupAny(walk.idBuffer, oType);

            int p = pos.value += 4; // "tag "
            int nameEnd = RawParseUtils.nextLF(rawTag, p) - 1;
            name = RawParseUtils.decode(Constants.CHARSET, rawTag, p, nameEnd);
            buffer = rawTag;
            flags |= PARSED;
        }

        public override int getType()
        {
            return Constants.OBJ_TAG;
        }

        /**
         * Parse the tagger identity from the raw buffer.
         * <p>
         * This method parses and returns the content of the tagger line, After
         * taking the tag's character set into account and decoding the tagger
         * name and email address. This method is fairly expensive and produces a
         * new PersonIdent instance on each invocation. Callers should invoke this
         * method only if they are certain they will be outputting the result, and
         * should cache the return value for as long as necessary to use all
         * information from it.
         *
         * @return identity of the tagger (name, email) and the time the tag
         *         was made by the tagger; null if no tagger line was found.
         */
        public PersonIdent getTaggerIdent()
        {
            byte[] raw = buffer;
            int nameB = RawParseUtils.tagger(raw, 0);
            if (nameB < 0)
                return null;
            return RawParseUtils.parsePersonIdent(raw, nameB);
        }

        /**
         * Parse the complete tag message and decode it to a string.
         * <p>
         * This method parses and returns the message portion of the tag buffer,
         * After taking the tag's character set into account and decoding the buffer
         * using that character set. This method is a fairly expensive operation and
         * produces a new string on each invocation.
         *
         * @return decoded tag message as a string. Never null.
         */
        public string getFullMessage()
        {
            byte[] raw = buffer;
            int msgB = RawParseUtils.tagMessage(raw, 0);
            if (msgB < 0)
                return "";
            Encoding enc = RawParseUtils.parseEncoding(raw);
            return RawParseUtils.decode(enc, raw, msgB, raw.Length);
        }

        /**
         * Parse the tag message and return the first "line" of it.
         * <p>
         * The first line is everything up to the first pair of LFs. This is the
         * "oneline" format, suitable for output in a single line display.
         * <p>
         * This method parses and returns the message portion of the tag buffer,
         * After taking the tag's character set into account and decoding the buffer
         * using that character set. This method is a fairly expensive operation and
         * produces a new string on each invocation.
         *
         * @return decoded tag message as a string. Never null. The returned string
         *         does not contain any LFs, even if the first paragraph spanned
         *         multiple lines. Embedded LFs are converted to spaces.
         */
        public string getShortMessage()
        {
            byte[] raw = buffer;
            int msgB = RawParseUtils.tagMessage(raw, 0);
            if (msgB < 0)
                return "";

            Encoding enc = RawParseUtils.parseEncoding(raw);
            int msgE = RawParseUtils.endOfParagraph(raw, msgB);
            string str = RawParseUtils.decode(enc, raw, msgB, msgE);
            if (RevCommit.hasLF(raw, msgB, msgE))
                str = str.Replace('\n', ' ');
            return str;
        }

        /**
         * Parse this tag buffer for display.
         * 
         * @param walk
         *            revision walker owning this reference.
         * @return parsed tag.
         */
        public Tag asTag(RevWalk walk)
        {
            return new Tag(walk.db, this, name, buffer);
        }

        /**
         * Get a reference to the @object this tag was placed on.
         * 
         * @return @object this tag refers to.
         */
        public RevObject getObject()
        {
            return @object;
        }

        /**
         * Get the name of this tag, from the tag header.
         * 
         * @return name of the tag, according to the tag header.
         */
        public string getName()
        {
            return name;
        }

        public override void dispose()
        {
            flags &= ~PARSED;
            buffer = null;
        }
    }
}