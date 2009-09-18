/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 * 
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
using System.Text;
using GitSharp.Exceptions;
using GitSharp.Util;

namespace GitSharp
{
	/// <summary>
	/// Verifies that an object is formatted correctly.
	/// <para />
	/// Verifications made by this class only check that the fields of an object are
	/// formatted correctly. The ObjectId checksum of the object is not verified, and
	/// connectivity links between objects are also not verified. Its assumed that
	/// the caller can provide both of these validations on its own.
	/// <para />
	/// Instances of this class are not thread safe, but they may be reused to
	/// perform multiple object validations.
	/// </summary>
	public class ObjectChecker
	{
		// Header "tree "
		internal static readonly byte[] tree = Encoding.ASCII.GetBytes("tree ");
		//public static readonly char[] tree = "tree ".ToCharArray();

		// Header "parent "
		internal static readonly byte[] parent = Encoding.ASCII.GetBytes("parent ");
		//public static readonly char[] parent = "parent ".ToCharArray();

		// Header "author "
		internal static readonly byte[] author = Encoding.ASCII.GetBytes("author ");
		//public static readonly char[] author = "author ".ToCharArray();

		// Header "committer "
		internal static readonly byte[] committer = Encoding.ASCII.GetBytes("committer ");
		//public static readonly char[] committer = "committer ".ToCharArray();

		// Header "encoding "
		internal static readonly byte[] encoding = Encoding.ASCII.GetBytes("encoding ");
		//public static readonly char[] encoding = "encoding ".ToCharArray();

		// Header "object "
		internal static readonly byte[] @object = Encoding.ASCII.GetBytes("object ");
		//public static readonly char[] @object = "object ".ToCharArray();

		// Header "type "
		internal static readonly byte[] type = Encoding.ASCII.GetBytes("type ");
		//public static readonly char[] type = "type ".ToCharArray();

		// Header "tag "
		internal static readonly byte[] tag = Encoding.ASCII.GetBytes("tag ");
		//public static readonly char[] tag = "tag ".ToCharArray();

		// Header "tagger "
		internal static readonly byte[] tagger = Encoding.ASCII.GetBytes("tagger ");
		//public static readonly char[] tagger = "tagger ".ToCharArray();

		private readonly MutableObjectId _tempId = new MutableObjectId();
		private readonly MutableInteger _ptrout = new MutableInteger();

		/// <summary>
		/// Check an object for parsing errors.
		/// </summary>
		/// <param name="objType">
		/// Type of the object. Must be a valid object type code in
		/// <see cref="Constants"/>.</param>
		/// <param name="raw">
		/// The raw data which comprises the object. This should be in the
		/// canonical format (that is the format used to generate the
		/// <see cref="ObjectId"/> of the object). The array is never modified.
		/// </param>
		/// <exception cref="CorruptObjectException">If any error is identified.</exception>
		public void check(ObjectType objType, char[] raw)
		{
			switch (objType)
			{
				case ObjectType.Commit:
					checkCommit(raw);
					break;

				case ObjectType.Tag:
					checkTag(raw);
					break;

				case ObjectType.Tree:
					checkTree(raw);
					break;

				case ObjectType.Blob:
					checkBlob(raw);
					break;

				default:
					throw new CorruptObjectException("Invalid object type: " + objType);
			}
		}

		/// <summary>
		/// Check an object for parsing errors.
		/// </summary>
		/// <param name="objType">
		/// Type of the object. Must be a valid object type code in
		/// <see cref="Constants"/>.</param>
		/// <param name="raw">
		/// The raw data which comprises the object. This should be in the
		/// canonical format (that is the format used to generate the
		/// <see cref="ObjectId"/> of the object). The array is never modified.
		/// </param>
		/// <exception cref="CorruptObjectException">If any error is identified.</exception>
		public void check(ObjectType objType, byte[] raw)
		{
			switch (objType)
			{
				case ObjectType.Commit:
					checkCommit(raw);
					break;

				case ObjectType.Tag:
					checkTag(raw);
					break;

				case ObjectType.Tree:
					checkTree(raw);
					break;

				case ObjectType.Blob:
					checkBlob(raw);
					break;

				default:
					throw new CorruptObjectException("Invalid object type: " + objType);
			}
		}

		private int Id(byte[] raw, int ptr)
		{
			try
			{
				_tempId.FromString(raw, ptr);
				return ptr + Constants.OBJECT_ID_STRING_LENGTH;
			}
			catch (ArgumentException)
			{
				return -1;
			}
		}

		private int PersonIdent(byte[] raw, int ptr)
		{
			int emailB = RawParseUtils.nextLF(raw, ptr, '<');
			if (emailB == ptr || raw[emailB - 1] != '<') return -1;

			int emailE = RawParseUtils.nextLF(raw, emailB, '>');
			if (emailE == emailB || raw[emailE - 1] != '>') return -1;
			if (emailE == raw.Length || raw[emailE] != ' ') return -1;

			RawParseUtils.ParseBase10(raw, emailE + 1, _ptrout); // when
			ptr = _ptrout.value;
			if (emailE + 1 == ptr) return -1;
			if (ptr == raw.Length || raw[ptr] != ' ') return -1;

			RawParseUtils.ParseBase10(raw, ptr + 1, _ptrout); // tz offset
			if (ptr + 1 == _ptrout.value) return -1;

			return _ptrout.value;
		}

		/// <summary>
		/// Check a commit for errors.
		/// </summary>
		/// <param name="raw">The commit data. The array is never modified.</param>
		/// <exception cref="CorruptObjectException">If any error was detected.</exception>
		public void checkCommit(char[] raw)
		{
			checkCommit(Constants.CHARSET.GetBytes(raw));
		}

		/// <summary>
		/// Check a commit for errors.
		/// </summary>
		/// <param name="raw">The commit data. The array is never modified.</param>
		/// <exception cref="CorruptObjectException">If any error was detected.</exception>
		public void checkCommit(byte[] raw)
		{
			int ptr = 0;

			if ((ptr = RawParseUtils.match(raw, ptr, tree)) < 0)
			{
				throw new CorruptObjectException("no tree header");
			}

			if ((ptr = Id(raw, ptr)) < 0 || raw[ptr++] != '\n')
			{
				throw new CorruptObjectException("invalid tree");
			}

			while (RawParseUtils.match(raw, ptr, parent) >= 0)
			{
				ptr += parent.Length;
				if ((ptr = Id(raw, ptr)) < 0 || raw[ptr++] != '\n')
					throw new CorruptObjectException("invalid parent");
			}

			if ((ptr = RawParseUtils.match(raw, ptr, author)) < 0)
			{
				throw new CorruptObjectException("no author");
			}

			if ((ptr = PersonIdent(raw, ptr)) < 0 || raw[ptr++] != '\n')
			{
				throw new CorruptObjectException("invalid author");
			}

			if ((ptr = RawParseUtils.match(raw, ptr, committer)) < 0)
			{
				throw new CorruptObjectException("no committer");
			}

			if ((ptr = PersonIdent(raw, ptr)) < 0 || raw[ptr++] != '\n')
			{
				throw new CorruptObjectException("invalid committer");
			}
		}

		/// <summary>
		/// Check an annotated tag for errors.
		/// </summary>
		/// <param name="raw">The tag data. The array is never modified.</param>
		/// <exception cref="CorruptObjectException">If any error was detected.</exception>
		public void checkTag(char[] raw)
		{
			checkTag(Constants.CHARSET.GetBytes(raw));
		}

		/// <summary>
		/// Check an annotated tag for errors.
		/// </summary>
		/// <param name="raw">The tag data. The array is never modified.</param>
		/// <exception cref="CorruptObjectException">If any error was detected.</exception>
		public void checkTag(byte[] raw)
		{
			int ptr = 0;

			if ((ptr = RawParseUtils.match(raw, ptr, @object)) < 0)
			{
				throw new CorruptObjectException("no object header");
			}

			if ((ptr = Id(raw, ptr)) < 0 || raw[ptr++] != '\n')
			{
				throw new CorruptObjectException("invalid object");
			}

			if ((ptr = RawParseUtils.match(raw, ptr, type)) < 0)
			{
				throw new CorruptObjectException("no type header");
			}

			ptr = RawParseUtils.nextLF(raw, ptr);
			if ((ptr = RawParseUtils.match(raw, ptr, tag)) < 0)
			{
				throw new CorruptObjectException("no tag header");
			}

			ptr = RawParseUtils.nextLF(raw, ptr);
			if ((ptr = RawParseUtils.match(raw, ptr, tagger)) < 0)
			{
				throw new CorruptObjectException("no tagger header");
			}

			if ((ptr = PersonIdent(raw, ptr)) < 0 || raw[ptr++] != '\n')
			{
				throw new CorruptObjectException("invalid tagger");
			}
		}

		/// <summary>
		/// Check a canonical formatted tree for errors.
		/// </summary>
		/// <param name="raw">The raw tree data. The array is never modified.</param>
		/// <exception cref="CorruptObjectException">If any error was detected.</exception>
		public void checkTree(char[] raw)
		{
			checkTree(Constants.CHARSET.GetBytes(raw));
		}

		/// <summary>
		/// Check a canonical formatted tree for errors.
		/// </summary>
		/// <param name="raw">The raw tree data. The array is never modified.</param>
		/// <exception cref="CorruptObjectException">If any error was detected.</exception>
		public void checkTree(byte[] raw)
		{
			int sz = raw.Length;
			int ptr = 0;
			int lastNameB = 0, lastNameE = 0, lastMode = 0;

			while (ptr < sz)
			{
				int thisMode = 0;
				while (true)
				{
					if (ptr == sz)
					{
						throw new CorruptObjectException("truncated in mode");
					}

					var c = (char)raw[ptr++];
					if (' ' == c) break;
					if (c < '0' || c > '7')
					{
						throw new CorruptObjectException("invalid mode character");
					}

					if (thisMode == 0 && c == '0')
					{
						throw new CorruptObjectException("mode starts with '0'");
					}

					thisMode <<= 3;
					thisMode += ((byte)c - (byte)'0');
				}

				if (FileMode.FromBits(thisMode).ObjectType == ObjectType.Bad)
				{
					throw new CorruptObjectException("invalid mode " + NB.DecimalToBase(thisMode, 8));
				}

				int thisNameB = ptr;
				while (true)
				{
					if (ptr == sz)
					{
						throw new CorruptObjectException("truncated in name");
					}

					var c = (char)raw[ptr++];
					if (c == '\0') break;
					if (c == '/')
					{
						throw new CorruptObjectException("name contains '/'");
					}
				}

				if (thisNameB + 1 == ptr)
				{
					throw new CorruptObjectException("zero length name");
				}

				if (raw[thisNameB] == '.')
				{
					int nameLen = (ptr - 1) - thisNameB;
					if (nameLen == 1)
					{
						throw new CorruptObjectException("invalid name '.'");
					}

					if (nameLen == 2 && raw[thisNameB + 1] == '.')
					{
						throw new CorruptObjectException("invalid name '..'");
					}
				}

				if (DuplicateName(raw, thisNameB, ptr - 1))
				{
					throw new CorruptObjectException("duplicate entry names");
				}

				if (lastNameB != 0)
				{
					int cmp = PathCompare(raw, lastNameB, lastNameE, lastMode, thisNameB, ptr - 1, thisMode);
					if (cmp > 0)
					{
						throw new CorruptObjectException("incorrectly sorted");
					}
				}

				lastNameB = thisNameB;
				lastNameE = ptr - 1;
				lastMode = thisMode;

				ptr += Constants.OBJECT_ID_LENGTH;
				if (ptr > sz)
				{
					throw new CorruptObjectException("truncated in object id");
				}
			}
		}

		/// <summary>
		/// Check a blob for errors.
		/// </summary>
		/// <param name="raw">The blob data. The array is never modified.</param>
		/// <exception cref="CorruptObjectException">If any error was detected.</exception>
		public void checkBlob(char[] raw)
		{
			// We can always assume the blob is valid.
		}

		/// <summary>
		/// Check a blob for errors.
		/// </summary>
		/// <param name="raw">The blob data. The array is never modified.</param>
		/// <exception cref="CorruptObjectException">If any error was detected.</exception>
		public void checkBlob(byte[] raw)
		{
			// We can always assume the blob is valid.
		}

		private static int LastPathChar(int mode)
		{
			return FileMode.Tree.Equals(mode) ? '/' : '\0';
		}

		private static int PathCompare(byte[] raw, int aPos, int aEnd, int aMode, int bPos, int bEnd, int bMode)
		{
			while (aPos < aEnd && bPos < bEnd)
			{
				int cmp = (((byte)raw[aPos++]) & 0xff) - (((byte)raw[bPos++]) & 0xff);
				if (cmp != 0)
				{
					return cmp;
				}
			}

			if (aPos < aEnd)
			{
				return (((byte)raw[aPos]) & 0xff) - LastPathChar(bMode);
			}

			if (bPos < bEnd)
			{
				return LastPathChar(aMode) - (((byte)raw[bPos]) & 0xff);
			}

			return 0;
		}

		private static bool DuplicateName(byte[] raw, int thisNamePos, int thisNameEnd)
		{
			int sz = raw.Length;
			int nextPtr = thisNameEnd + 1 + Constants.OBJECT_ID_LENGTH;

			while (true)
			{
				int nextMode = 0;
				while (true)
				{
					if (nextPtr >= sz) return false;
					var c = (char)raw[nextPtr++];
					if (' ' == c) break;
					nextMode <<= 3;
					nextMode += ((byte)c - (byte)'0');
				}

				int nextNamePos = nextPtr;
				while (true)
				{
					if (nextPtr == sz) return false;
					var c = (char)raw[nextPtr++];
					if (c == '\0') break;
				}

				if (nextNamePos + 1 == nextPtr) return false;

				int cmp = PathCompare(raw, thisNamePos, thisNameEnd, FileMode.Tree.Bits, nextNamePos, nextPtr - 1, nextMode);
				if (cmp < 0) return false;
				if (cmp == 0) return true;

				nextPtr += Constants.OBJECT_ID_LENGTH;
			}
		}
	}
}
