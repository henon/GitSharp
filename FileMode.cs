/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Gitty.Core
{
    [Complete]
    public class FileMode
    {

        public static readonly FileMode Tree = new FileMode(0040000, ObjectType.Tree, modeBits => (modeBits & 0170000) == 0040000);

        public static readonly FileMode Symlink = new FileMode(0120000, ObjectType.Blob, modeBits => (modeBits & 0170000) == 0120000);

        public static readonly FileMode RegularFile = new FileMode(0100644, ObjectType.Blob, modeBits => (modeBits & 0170000) == 0100000 && (modeBits & 0111) == 0);

        public static readonly FileMode ExecutableFile = new FileMode(0100755, ObjectType.Blob, modeBits => (modeBits & 0170000) == 0100000 && (modeBits & 0111) != 0);

        public static readonly FileMode GitLink = new FileMode(0160000, ObjectType.Commit, modeBits => (modeBits & 0170000) == 0160000);

        public static readonly FileMode Missing = new FileMode(0000000, ObjectType.Bad, modeBits => modeBits == 0);

        private byte[] _octalBytes;

        private FileMode(int mode, ObjectType type, EqualsDelegate equals)
        {
            if (equals == null)
                throw new ArgumentNullException("equals");

            this.Equals = equals;

            this.Bits = mode;
            this.ObjectType = type;

            if (mode != 0)
            {
                byte[] tmp = new byte[10];
                int p = tmp.Length;

                while (mode != 0)
                {
                    tmp[--p] = (byte)('0' + (mode & 07));
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
                _octalBytes = new byte[] { (byte)'0' };
            }
        }

        public delegate bool EqualsDelegate(int bits);
        public new EqualsDelegate Equals { get; private set; }

        public int Bits { get; private set; }
        public ObjectType ObjectType { get; private set; }


        public static FileMode FromBits(int bits)
        {
            switch (bits & 0170000)
            {
                case 0000000:
                    if (bits == 0)
                        return Missing;
                    break;
                case 0040000:
                    return Tree;
                case 0100000:
                    if ((bits & 0111) != 0)
                        return ExecutableFile;
                    return RegularFile;
                case 0120000:
                    return Symlink;
                case 0160000:
                    return GitLink;
            }

            return new FileMode(bits, ObjectType.Bad,
                delegate(int a) { return bits == a; }
            );
        }

        public void CopyTo(StreamWriter writer)
        {
            writer.Write(_octalBytes);
        }
    }
}
