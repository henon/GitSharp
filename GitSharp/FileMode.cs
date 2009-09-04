/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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
using System.IO;

namespace GitSharp
{
	[Serializable]
	public class FileMode
	{
		// [henon] c# does not support octal literals, so every number starting with 0 in java code had to be converted to decimal!
		// Here are the octal literals used by jgit and their decimal counterparts:
		// decimal ... octal
		// 33188 ... 0100644
		// 33261 ... 0100755
		// 61440 ... 0170000
		// 16384 ... 0040000
		// 32768 ... 0100000
		// 40960 ... 0120000
		// 57344 ... 0160000
		// 73 ... 0111

		/**
         * Mask to apply to a file mode to obtain its type bits.
         *
         * @see #TYPE_TREE
         * @see #TYPE_SYMLINK
         * @see #TYPE_FILE
         * @see #TYPE_GITLINK
         * @see #TYPE_MISSING
         */

		/** Bit pattern for {@link #TYPE_MASK} matching {@link #REGULAR_FILE}. */

		#region Delegates

		public delegate bool EqualsDelegate(int bits);

		#endregion

		public const int OCTAL_0100644 = 33188;
		public const int OCTAL_0100755 = 33261;
		public const int OCTAL_0111 = 73;

		public const int TYPE_FILE = 32768;
		public const int TYPE_GITLINK = 57344;

		/** Bit pattern for {@link #TYPE_MASK} matching {@link #GITLINK}. */
		public const int TYPE_MASK = 61440;

		/** Bit pattern for {@link #TYPE_MASK} matching {@link #MISSING}. */
		public const int TYPE_MISSING = 0;
		public const int TYPE_SYMLINK = 40960;
		public const int TYPE_TREE = 16384;

		[field: NonSerialized]
		public static readonly FileMode ExecutableFile = 
			new FileMode(OCTAL_0100755, ObjectType.Blob,
				modeBits => (modeBits & TYPE_MASK) == TYPE_FILE && (modeBits & OCTAL_0111) != 0);

		[field: NonSerialized]
		public static readonly FileMode GitLink = 
			new FileMode(TYPE_GITLINK, ObjectType.Commit,
				modeBits => (modeBits & TYPE_MASK) == TYPE_GITLINK);

		[field: NonSerialized]
		public static readonly FileMode Missing = 
			new FileMode(0, ObjectType.Bad, modeBits => modeBits == 0);

		[field: NonSerialized]
		public static readonly FileMode RegularFile = 
			new FileMode(OCTAL_0100644, ObjectType.Blob,
                modeBits => (modeBits & TYPE_MASK) == TYPE_FILE && (modeBits & OCTAL_0111) == 0);

		[field: NonSerialized]
		public static readonly FileMode Symlink = 
			new FileMode(TYPE_SYMLINK, ObjectType.Blob,
				modeBits => (modeBits & TYPE_MASK) == TYPE_SYMLINK);

		[field: NonSerialized]
		public static readonly FileMode Tree = 
			new FileMode(TYPE_TREE, ObjectType.Tree,
                modeBits => (modeBits & TYPE_MASK) == TYPE_TREE);
		
		public static FileMode FromBits(int bits)
		{
			switch (bits & TYPE_MASK) // octal 0170000
			{
				case 0:
					if (bits == 0)
					{
						return Missing;
					}
					break;

				case TYPE_TREE: // octal 0040000
					return Tree;

				case TYPE_FILE: // octal 0100000
					return (bits & OCTAL_0111) != 0 ? ExecutableFile : RegularFile;

				case TYPE_SYMLINK: // octal 0120000
					return Symlink;

				case TYPE_GITLINK: // octal 0160000
					return GitLink;
			}

			return new FileMode(bits, ObjectType.Bad, a => bits == a);
		}

		private readonly byte[] _octalBytes;

		private FileMode(int mode, ObjectType type, EqualsDelegate equals)
		{
			if (equals == null)
			{
				throw new ArgumentNullException("equals");
			}

			Equals = equals;

			Bits = mode;
			ObjectType = type;

			if (mode != 0)
			{
				var tmp = new byte[10];
				int p = tmp.Length;

				while (mode != 0)
				{
					tmp[--p] = (byte) ((byte) '0' + (mode & 07));
					mode >>= 3;
				}

				_octalBytes = new byte[tmp.Length - p];
				for (int k = 0; k < _octalBytes.Length; k++)
				{
					_octalBytes[k] = tmp[p + k];
				}
			}
			else
			{
				_octalBytes = new byte[] {(byte) '0'};
			}
		}

		public new EqualsDelegate Equals { get; private set; }

		public int Bits { get; private set; }
		public ObjectType ObjectType { get; private set; }

		public void CopyTo(Stream stream)
		{
			new BinaryWriter(stream).Write(_octalBytes);
		}

		/// <summary>
		/// Returns the number of bytes written by <see cref="CopyTo(Stream)"/>
		/// </summary>
		/// <returns></returns>
		public int copyToLength()
		{
			return _octalBytes.Length;
		}
	}
}