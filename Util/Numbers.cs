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
        /**
         * Convert sequence of 4 bytes (network byte order) into unsigned value.
         * 
         * @param intbuf
         *            buffer to acquire the 4 bytes of data from.
         * @param offset
         *            position within the buffer to begin reading from. This
         *            position and the next 3 bytes after it (for a total of 4
         *            bytes) will be read.
         * @return unsigned integer value that matches the 32 bits read.
         */
        public static long DecodeUInt32(byte[] intbuf, int offset)
        {
            long low = (intbuf[offset + 1] & 0xff);
            low <<= 8;

            low |= (byte)(intbuf[offset + 2] & 0xff);
            low <<= 8;

            low |= (byte)(intbuf[offset + 3] & 0xff);

            return ((long)(intbuf[offset] & 0xff) << 24) | low;
        }

        /**
         * Convert sequence of 4 bytes (network byte order) into signed value.
         * 
         * @param intbuf
         *            buffer to acquire the 4 bytes of data from.
         * @param offset
         *            position within the buffer to begin reading from. This
         *            position and the next 3 bytes after it (for a total of 4
         *            bytes) will be read.
         * @return signed integer value that matches the 32 bits read.
         */
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

        /**
         * Convert sequence of 8 bytes (network byte order) into unsigned value.
         * 
         * @param intbuf
         *            buffer to acquire the 8 bytes of data from.
         * @param offset
         *            position within the buffer to begin reading from. This
         *            position and the next 7 bytes after it (for a total of 8
         *            bytes) will be read.
         * @return unsigned integer value that matches the 64 bits read.
         */
        public static long DecodeUInt64(byte[] intbuf, int offset)
        {
            return (DecodeUInt32(intbuf, offset) << 32)
                   | DecodeUInt32(intbuf, offset + 4);
        }
    }
}
