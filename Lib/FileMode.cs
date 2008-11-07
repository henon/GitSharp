using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Gitty.Lib
{
    [Complete]
    public class FileMode
    {

        public static readonly FileMode Tree = new FileMode(0040000, ObjectType.Tree,
            delegate(int modeBits) { return (modeBits & 0170000) == 0040000; }
        );

        public static readonly FileMode Symlink = new FileMode(0120000, ObjectType.Blob,
            delegate(int modeBits) { return (modeBits & 0170000) == 0120000; }
        );

        public static readonly FileMode RegularFile = new FileMode(0100644, ObjectType.Blob,
            delegate(int modeBits) { return (modeBits & 0170000) == 0100000 && (modeBits & 0111) == 0; }
        );

        public static readonly FileMode ExecutableFile = new FileMode(0100755, ObjectType.Blob,
            delegate(int modeBits) { return (modeBits & 0170000) == 0100000 && (modeBits & 0111) != 0; }
        );

        public static readonly FileMode GitLink = new FileMode(0160000, ObjectType.Commit,
            delegate(int modeBits) { return (modeBits & 0170000) == 0160000; }
        );

        public static readonly FileMode Missing = new FileMode(0000000, ObjectType.Bad,
            delegate(int modeBits) { return modeBits == 0; }
        );

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
