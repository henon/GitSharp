using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Gitty.Extensions;

namespace Gitty.Util
{
    internal class NB
    {
        public static long decodeUInt32(byte[] intbuf, int offset)
        {
            long low = (intbuf[offset + 1] & 0xff);
            low <<= 8;

            low |= (byte)(intbuf[offset + 2] & 0xff);
            low <<= 8;

            low |= (byte)(intbuf[offset + 3] & 0xff);

            return ((long)(intbuf[offset] & 0xff) << 24) | low;
        }

        public static int DecodeInt32(byte[] intbuf, int offset)
        {
            int r = intbuf[offset] << 8;

            r |= intbuf[offset + 1] & 0xff;
            r <<= 8;

            r |= intbuf[offset + 2] & 0xff;
            return (r << 8) | (intbuf[offset + 3] & 0xff);
        }

        internal static int CompareUInt32(int a, int b)
        {
            int cmp = a.UnsignedRightShift(1) - b.UnsignedRightShift(1);

            if (cmp != 0)
                return cmp;
            return (a & 1) - (b & 1);
        }



        public static void ReadFully(Stream fd, byte[] dst, int off, int len)
        {
            while (len > 0)
            {
                int r = fd.Read(dst, off, len);
                if (r <= 0)
                    throw new EndOfStreamException("Short read of block.");
                off += r;
                len -= r;
            }
        }
    }
}
