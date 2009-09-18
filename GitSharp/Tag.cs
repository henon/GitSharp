/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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
using System.Text;
using GitSharp.Exceptions;

namespace GitSharp
{
    public class Tag
    {
        public Repository Repository { get; internal set; }

		private PersonIdent _author;
		private string _message;
		private string _tagType;
        private byte[] _raw;

        /// <summary>
		/// Construct a new, yet unnamed Tag.
        /// </summary>
        /// <param name="db"></param>
        public Tag(Repository db)
        {
            Repository = db;
        }

        /// <summary>
        /// Construct a Tag representing an existing with a known name referencing an known object.
		/// This could be either a simple or annotated tag.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="id">Target id.</param>
        /// <param name="refName">Tag name</param>
		/// <param name="raw">Data of an annotated tag.</param>
        public Tag(Repository db, ObjectId id, string refName, byte[] raw)
        {
            Repository = db;
            if (raw != null)
            {
                TagId = id;
                Id = ObjectId.FromString(raw, 7);
            }
            else
                Id = id;
            if (refName != null && refName.StartsWith("refs/tags/"))
                refName = refName.Substring(10);
            TagName = refName;
            this._raw = raw;
        }

        /// <summary>
		/// Gets/Sets the tagger of a annotated tag.
        /// </summary>
        public PersonIdent Author
        {
            get
            {
                decode();
                return _author;
            }
            set { _author = value; }
        }

        /// <summary>
		/// Gets/Sets the comment of an annotated tag.
        /// </summary>
        public string Message
        {
            get
            {
                decode();
                return _message;
            }
            set { _message = value; }
        }

        private void decode()
        {
            // FIXME: handle I/O errors
            if (_raw == null) return;

            using (var br = new StreamReader(new MemoryStream(_raw)))
            {
                string n = br.ReadLine();
                if (n == null || !n.StartsWith("object "))
                {
                    throw new CorruptObjectException(TagId, "no object");
                }
                Id = ObjectId.FromString(n.Substring(7));

                n = br.ReadLine();
                if (n == null || !n.StartsWith("type "))
                {
                    throw new CorruptObjectException(TagId, "no type");
                }

                TagType = n.Substring("type ".Length);
                n = br.ReadLine();
                if (n == null || !n.StartsWith("tag "))
                {
                    throw new CorruptObjectException(TagId, "no tag name");
                }

                TagName = n.Substring("tag ".Length);
                n = br.ReadLine();

                // We should see a "tagger" header here, but some repos have tags
                // without it.
                if (n == null)
                {
                	throw new CorruptObjectException(TagId, "no tagger header");
                }

                if (n.Length > 0)
                {
                	if (n.StartsWith("tagger "))
                	{
                		Tagger = new PersonIdent(n.Substring("tagger ".Length));
                	}
                	else
                	{
                		throw new CorruptObjectException(TagId, "no tagger/bad header");
                	}
                }

                // Message should start with an empty line, but
                var tempMessage = new StringBuilder();
                var readBuf = new char[2048];
                int readLen;

                while ((readLen = br.Read(readBuf, 0, readBuf.Length)) > 0)
                {
                    //readIndex += readLen;
                    tempMessage.Append(readBuf, 0, readLen);
                }

                _message = tempMessage.ToString();
                if (_message.StartsWith("\n"))
                {
                	_message = _message.Substring(1);
                }
            }

            _raw = null;
        }

        /// <summary>
        /// Store a tag.
		/// If author, message or type is set make the tag an annotated tag.
        /// </summary>
        public void Save()  //renamed from Tag
        {
            if (TagId != null)
            {
            	throw new InvalidOperationException("exists " + TagId);
            }

            ObjectId id;

            if (_author != null || _message != null || _tagType != null)
            {
                ObjectId tagid = new ObjectWriter(Repository).WriteTag(this);
                TagId = tagid;
                id = tagid;
            }
            else
            {
                id = Id;
            }

            RefUpdate ru = Repository.UpdateRef(Constants.RefsTags + TagName);
            ru.NewObjectId = id;
            ru.SetRefLogMessage("tagged " + TagName, false);
            if (ru.ForceUpdate() == RefUpdate.RefUpdateResult.LockFailure)
            {
            	throw new ObjectWritingException("Unable to lock tag " + TagName);
            }
        }

        public override string ToString()
        {
            return "tag[" + TagName + TagType + Id + " " + Author + "]";
        }

        public ObjectId TagId { get; set; }

        /// <summary>
		/// Gets/Sets the creator of this tag.
        /// </summary>
        public PersonIdent Tagger
        {
            get { return Author; }
            set { Author = value; }
        }

        /// <summary>
		/// Gets/Sets the tag target type
        /// </summary>
        public string TagType
        {
            get
            {
                decode();
                return _tagType;
            }
            set { _tagType = value; }
        }

        public string TagName { get; set; }

        /// <summary>
		/// Gets/Sets the SHA'1 of the object this tag refers to.
        /// </summary>
        public ObjectId Id { get; set; }
    }
}
