/*
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



// Note: this file originates from jgit's NB.java



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace GitSharp.Util
{
    /// <summary>
    /// Conversion utilities for network byte order handling.
    /// </summary>
    public static class NB // [henon] need public for testsuite
    {

        /**
         * Compare a 32 bit unsigned integer stored in a 32 bit signed integer.
         * <p>
         * This function performs an unsigned compare operation, even though Java
         * does not natively support unsigned integer values. Negative numbers are
         * treated as larger than positive ones.
         * 
         * @param a
         *            the first value to compare.
         * @param b
         *            the second value to compare.
         * @return < 0 if a < b; 0 if a == b; > 0 if a > b.
         */
        public static int compareUInt32(int a, int b)
        {
            int cmp = (int)(((uint)a >> 1) - ((uint)b >> 1));
            if (cmp != 0)
                return cmp;
            return (a & 1) - (b & 1);
        }

        public static int CompareUInt32(int a, int b)
        {
            return compareUInt32(a, b);
        }

        /**
         * Convert sequence of 2 bytes (network byte order) into unsigned value.
         *
         * @param intbuf
         *            buffer to acquire the 2 bytes of data from.
         * @param offset
         *            position within the buffer to begin reading from. This
         *            position and the next byte after it (for a total of 2 bytes)
         *            will be read.
         * @return unsigned integer value that matches the 16 bits read.
         */
        public static int decodeUInt16(byte[] intbuf, int offset)
        {
            int r = (intbuf[offset] & 0xff) << 8;
            return r | (intbuf[offset + 1] & 0xff);
        }


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
        public static long decodeUInt32(byte[] intbuf, int offset)
        {
            int low = (intbuf[offset + 1] & 0xff) << 8;
            low |= (intbuf[offset + 2] & 0xff);
            low <<= 8;

            low |= (intbuf[offset + 3] & 0xff);
            return ((long)(intbuf[offset] & 0xff)) << 24 | low;
        }


        public static long DecodeUInt32(byte[] intbuf, int offset)
        {
            return decodeUInt32(intbuf, offset);
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
        public static int decodeInt32(byte[] intbuf, int offset)
        {
            int r = intbuf[offset] << 8;

            r |= intbuf[offset + 1] & 0xff;
            r <<= 8;

            r |= intbuf[offset + 2] & 0xff;

            return (r << 8) | (intbuf[offset + 3] & 0xff);
        }

        public static int DecodeInt32(byte[] intbuf, int offset)
        {
            return decodeInt32(intbuf, offset);
        }

        /**
         * Read an entire local file into memory as a byte array.
         *
         * @param path
         *            location of the file to read.
         * @return complete contents of the requested local file.
         * @throws FileNotFoundException
         *             the file does not exist.
         * @throws IOException
         *             the file exists, but its contents cannot be read.
         */
        public static byte[] ReadFully(FileInfo path)
        {
            return ReadFully(path, int.MaxValue);
        }

        /**
         * Read an entire local file into memory as a byte array.
         *
         * @param path
         *            location of the file to read.
         * @param max
         *            maximum number of bytes to read, if the file is larger than
         *            this limit an IOException is thrown.
         * @return complete contents of the requested local file.
         * @throws FileNotFoundException
         *             the file does not exist.
         * @throws IOException
         *             the file exists, but its contents cannot be read.
         */
        public static byte[] ReadFully(FileInfo path, int max)
        {
            using (var @in = new FileStream(path.FullName, System.IO.FileMode.Open, FileAccess.Read))
            {
                long sz = @in.Length;
                if (sz > max)
                    throw new IOException("File is too large: " + path);
                byte[] buf = new byte[(int)sz];
                ReadFully(@in, buf, 0, buf.Length);
                return buf;
            }
        }


        /**
         * Read the entire byte array into memory, or throw an exception.
         * 
         * @param fd
         *            input stream to read the data from.
         * @param dst
         *            buffer that must be fully populated, [off, off+len).
         * @param off
         *            position within the buffer to start writing to.
         * @param len
         *            number of bytes that must be read.
         * @throws EOFException
         *             the stream ended before dst was fully populated.
         * @throws IOException
         *             there was an error reading from the stream.
         */
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
         * Read the entire byte array into memory, or throw an exception.
         *
         * @param fd
         *            file to read the data from.
         * @param pos
         *            position to read from the file at.
         * @param dst
         *            buffer that must be fully populated, [off, off+len).
         * @param off
         *            position within the buffer to start writing to.
         * @param len
         *            number of bytes that must be read.
         * @throws EOFException
         *             the stream ended before dst was fully populated.
         * @throws IOException
         *             there was an error reading from the stream.
         */
        public static void ReadFully(Stream fd, long pos, byte[] dst, int off, int len)
        {
            while (len > 0)
            {
                fd.Position = pos;
                int r = fd.Read(dst, off, len);
                if (r <= 0)
                    throw new EndOfStreamException("Short read of block.");
                pos += r;
                off += r;
                len -= r;
            }
        }


        /**
         * Skip an entire region of an input stream.
         * <p>
         * The input stream's position is moved forward by the number of requested
         * bytes, discarding them from the input. This method does not return until
         * the exact number of bytes requested has been skipped.
         *
         * @param fd
         *            the stream to skip bytes from.
         * @param toSkip
         *            total number of bytes to be discarded. Must be >= 0.
         * @throws EOFException
         *             the stream ended before the requested number of bytes were
         *             skipped.
         * @throws IOException
         *             there was an error reading from the stream.
         */
        public static void skipFully(Stream fd, long toSkip)
        {
            while (toSkip > 0)
            {
                long r = fd.Seek(toSkip, SeekOrigin.Current);
                if (r <= 0)
                    throw new EndOfStreamException("Short skip of block");
                toSkip -= r;
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
        public static long decodeUInt64(byte[] intbuf, int offset)
        {
            return DecodeUInt64(intbuf, offset);
        }

        public static long DecodeUInt64(byte[] intbuf, int offset)
        {
            return (DecodeUInt32(intbuf, offset) << 32)
                   | DecodeUInt32(intbuf, offset + 4);
        }

        // [henon] constants for DecimalToBase
        const int base10 = 10;
        static readonly char[] cHexa = new char[] { 'A', 'B', 'C', 'D', 'E', 'F' };
        static readonly int[] iHexaNumeric = new int[] { 10, 11, 12, 13, 14, 15 };
        static readonly int[] iHexaIndices = new int[] { 0, 1, 2, 3, 4, 5 };
        const int asciiDiff = 48;

        /// <summary>
        /// This function takes two arguments; the integer value to be converted and the base value (2, 8, or 16) 
        /// to which the number is converted to.
        /// </summary>
        /// <param name="iDec">the decimal</param>
        /// <param name="numbase">the base of the output</param>
        /// <returns>a string representation of the base number</returns>
        public static string DecimalToBase(int iDec, int numbase) // [henon] needed to output octal numbers
        {
            string strBin = "";
            int[] result = new int[32];
            int MaxBit = 32;
            for (; iDec > 0; iDec /= numbase)
            {
                int rem = iDec % numbase;
                result[--MaxBit] = rem;
            }
            for (int i = 0; i < result.Length; i++)
                if ((int)result.GetValue(i) >= base10)
                    strBin += cHexa[(int)result.GetValue(i) % base10];
                else
                    strBin += result.GetValue(i);
            strBin = strBin.TrimStart(new char[] { '0' });
            return strBin;
        }

        /// <summary>
        /// This function takes two arguments; a string value representing the binary, octal, or hexadecimal 
        /// value and the corresponding integer base value respective to the first argument. For instance, 
        /// if you pass the first argument value "1101", then the second argument should take the value "2".
        /// </summary>
        /// <param name="sBase">the string in base sBase notation</param>
        /// <param name="numbase">the base to convert from</param>
        /// <returns>decimal</returns>
        public static int BaseToDecimal(string sBase, int numbase)
        {
            int dec = 0;
            int b;
            int iProduct = 1;
            string sHexa = "";
            if (numbase > base10)
                for (int i = 0; i < cHexa.Length; i++)
                    sHexa += cHexa.GetValue(i).ToString();
            for (int i = sBase.Length - 1; i >= 0; i--, iProduct *= numbase)
            {
                string sValue = sBase[i].ToString();
                if (sValue.IndexOfAny(cHexa) >= 0)
                    b = iHexaNumeric[sHexa.IndexOf(sBase[i])];
                else
                    b = (int)sBase[i] - asciiDiff;
                dec += (b * iProduct);
            }
            return dec;
        }

        /**
         * Write a 16 bit integer as a sequence of 2 bytes (network byte order).
         *
         * @param intbuf
         *            buffer to write the 2 bytes of data into.
         * @param offset
         *            position within the buffer to begin writing to. This position
         *            and the next byte after it (for a total of 2 bytes) will be
         *            replaced.
         * @param v
         *            the value to write.
         */
        public static void encodeInt16(byte[] intbuf, int offset, int v)
        {
            intbuf[offset + 1] = (byte)v;
            v >>= 8; // >>>

            intbuf[offset] = (byte)v;
        }

        /**
         * Write a 32 bit integer as a sequence of 4 bytes (network byte order).
         * 
         * @param intbuf
         *            buffer to write the 4 bytes of data into.
         * @param offset
         *            position within the buffer to begin writing to. This position
         *            and the next 3 bytes after it (for a total of 4 bytes) will be
         *            replaced.
         * @param v
         *            the value to write.
         */
        public static void encodeInt32(byte[] intbuf, int offset, int v)
        {
            intbuf[offset + 3] = (byte)v;
            v >>= 8;

            intbuf[offset + 2] = (byte)v;
            v >>= 8;

            intbuf[offset + 1] = (byte)v;
            v >>= 8;

            intbuf[offset] = (byte)v;
        }

        /**
         * Write a 64 bit integer as a sequence of 8 bytes (network byte order).
         *
         * @param intbuf
         *            buffer to write the 48bytes of data into.
         * @param offset
         *            position within the buffer to begin writing to. This position
         *            and the next 7 bytes after it (for a total of 8 bytes) will be
         *            replaced.
         * @param v
         *            the value to write.
         */
        public static void encodeInt64(byte[] intbuf, int offset, long v)
        {
            intbuf[offset + 7] = (byte)v;
            v >>= 8;

            intbuf[offset + 6] = (byte)v;
            v >>= 8;

            intbuf[offset + 5] = (byte)v;
            v >>= 8;

            intbuf[offset + 4] = (byte)v;
            v >>= 8;

            intbuf[offset + 3] = (byte)v;
            v >>= 8;

            intbuf[offset + 2] = (byte)v;
            v >>= 8;

            intbuf[offset + 1] = (byte)v;
            v >>= 8;

            intbuf[offset] = (byte)v;
        }


    }
}
