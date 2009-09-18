/*
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
using GitSharp.Exceptions;
using GitSharp.Util;

namespace GitSharp
{
	public enum ObjectType
	{
		/// <summary>
		/// An unknown or invalid object type code.
		/// </summary>
		Bad = -1,

		/// <summary>
		/// In-pack object type: extended types.
		/// <para />
		/// This header code is reserved for future expansion. It is currently
		/// undefined/unsupported.
		/// </summary>
		Extension = 0,

		/// <summary>
		/// In-pack object type: commit.
		/// <para />
		/// Indicates the associated object is a commit.
		/// <para />
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// <seealso cref="Constants.TYPE_COMMIT"/>
		/// </summary>
		Commit = 1,

		/// <summary>
		/// In-pack object type: tree.
		/// <para />
		/// Indicates the associated object is a tree.
		/// <para />
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// </summary>
		/// <seealso cref="Constants.TYPE_BLOB"/>
		Tree = 2,

		/// <summary>
		/// In-pack object type: blob.
		/// <para />
		/// Indicates the associated object is a blob.
		/// <para />
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// </summary>
		/// <seealso cref="Constants.TYPE_BLOB"/>
		Blob = 3,

		/// <summary>
		/// In-pack object type: annotated tag.
		/// <para />
		/// Indicates the associated object is an annotated tag.
		/// <para />
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// </summary>
		/// <seealso cref="Constants.TYPE_TAG"/>
		Tag = 4,

		/// <summary>
		/// In-pack object type: reserved for future use.
		/// </summary>
		ObjectType5 = 5,

		/// <summary>
		/// In-pack object type: offset delta
		/// <para />
		/// Objects stored with this type actually have a different type which must
		/// be obtained from their delta base object. Delta objects store only the
		/// changes needed to apply to the base object in order to recover the
		/// original object.
		/// <para />
		/// An offset delta uses a negative offset from the start of this object to
		/// refer to its delta base. The base object must exist in this packfile
		/// (even in the case of a thin pack).
		/// <para />
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// </summary>
		OffsetDelta = 6,

		/// <summary>
		/// In-pack object type: reference delta
		/// <para />
		/// Objects stored with this type actually have a different type which must
		/// be obtained from their delta base object. Delta objects store only the
		/// changes needed to apply to the base object in order to recover the
		/// original object.
		/// <para />
		/// A reference delta uses a full object id (hash) to reference the delta
		/// base. The base object is allowed to be omitted from the packfile, but
		/// only in the case of a thin pack being transferred over the network.
		/// <para />
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// </summary>
		ReferenceDelta = 7,

		DeltaBase = 254,

		Unknown = 255
	}

	public static class ObjectTypeExtensions
	{
		/// <summary>
		/// Convert an OBJ_* type constant to a TYPE_* type constant.
		/// </summary>
		/// <param name="typeCode">
		/// typeCode the type code, from a pack representation.
		/// </param>
		/// <returns>The canonical string name of this type.</returns>
		public static string ObjectTypeToString(this ObjectType typeCode)
		{
			switch (typeCode)
			{
				case ObjectType.Commit:
					return Constants.TYPE_COMMIT;

				case ObjectType.Tree:
					return Constants.TYPE_TREE;

				case ObjectType.Blob:
					return Constants.TYPE_BLOB;

				case ObjectType.Tag:
					return Constants.TYPE_TAG;

				default:
					throw new ArgumentException("Bad object type: " + typeCode);
			}
		}

		/// <summary>
		/// Convert an OBJ_* type constant to an ASCII encoded string constant.
		/// <para />
		/// The ASCII encoded string is often the canonical representation of
		/// the type within a loose object header, or within a tag header.
		/// </summary>
		/// <param name="typeCode">
		/// typeCode the type code, from a pack representation.
		/// </param>
		/// <returns>
		/// The canonical ASCII encoded name of this type.
		/// </returns>
		public static byte[] EncodedTypeString(this ObjectType typeCode)
		{
			switch (typeCode)
			{
				case ObjectType.Commit:
					return Constants.EncodedTypeCommit;

				case ObjectType.Tree:
					return Constants.EncodedTypeTree;

				case ObjectType.Blob:
					return Constants.EncodedTypeBlob;

				case ObjectType.Tag:
					return Constants.EncodedTypeTag;

				default:
					throw new ArgumentException("Bad object type: " + typeCode);
			}
		}

		/// <summary>
		/// Parse an encoded type string into a type constant.
		/// </summary>
		/// <param name="id">
		/// <see cref="ObjectId" /> this type string came from; may be null if 
		/// that is not known at the time the Parse is occurring.
		/// </param>
		/// <param name="typeString">string version of the type code.</param>
		/// <param name="endMark">
		/// Character immediately following the type string. Usually ' '
		/// (space) or '\n' (line feed).
		/// </param>
		/// <param name="offset">
		/// Position within <paramref name="typeString"/> where the Parse
		/// should start. Updated with the new position (just past
		/// <paramref name="endMark"/> when the Parse is successful).
		/// </param>
		/// <returns>
		/// A type code constant (one of <see cref="ObjectType"/>.
		/// </returns>
		/// <exception cref="CorruptObjectException"></exception>
		public static ObjectType DecodeTypeString(AnyObjectId id, byte[] typeString, byte endMark, MutableInteger offset)
		{
			try
			{
				int position = offset.value;
				switch (typeString[position])
				{
					case (byte)'b':
						if (typeString[position + 1] != (byte)'l'
							|| typeString[position + 2] != (byte)'o'
							|| typeString[position + 3] != (byte)'b'
							|| typeString[position + 4] != endMark)
						{
							throw new CorruptObjectException(id, "invalid type");
						}
						offset.value = position + 5;
						return ObjectType.Blob;

					case (byte)'c':
						if (typeString[position + 1] != (byte)'o'
								|| typeString[position + 2] != (byte)'m'
								|| typeString[position + 3] != (byte)'m'
								|| typeString[position + 4] != (byte)'i'
								|| typeString[position + 5] != (byte)'t'
								|| typeString[position + 6] != endMark)
						{
							throw new CorruptObjectException(id, "invalid type");
						}
						offset.value = position + 7;
						return ObjectType.Commit;

					case (byte)'t':
						switch (typeString[position + 1])
						{
							case (byte)'a':
								if (typeString[position + 2] != (byte)'g'
									|| typeString[position + 3] != endMark)
								{
									throw new CorruptObjectException(id, "invalid type");
								}
								offset.value = position + 4;
								return ObjectType.Tag;

							case (byte)'r':
								if (typeString[position + 2] != (byte)'e'
										|| typeString[position + 3] != (byte)'e'
										|| typeString[position + 4] != endMark)
								{
									throw new CorruptObjectException(id, "invalid type");
								}
								offset.value = position + 5;
								return ObjectType.Tree;

							default:
								throw new CorruptObjectException(id, "invalid type");
						}

					default:
						throw new CorruptObjectException(id, "invalid type");
				}
			}
			catch (IndexOutOfRangeException)
			{
				throw new CorruptObjectException(id, "invalid type");
			}
		}

		/// <summary>
		/// Returns an instance of <see cref="ObjectType"/> based on an integer flag.
		/// </summary>
		/// <param name="flag"></param>
		/// <returns></returns>
		public static ObjectType FromFlag(int flag)
		{
			return (ObjectType)Enum.ToObject(typeof(ObjectType), flag >> 4 & 7);
		}

		/// <summary>
		/// Returns an instance of <see cref="ObjectType"/> based on an integer value.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static ObjectType FromInteger(int value)
		{
			return (ObjectType)Enum.ToObject(typeof(ObjectType), value);
		}
	}
}