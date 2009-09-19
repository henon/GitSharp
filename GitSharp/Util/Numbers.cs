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
using System.Globalization;
using System.IO;
using System.Net;

namespace GitSharp.Util
{
	/// <summary>
	/// Conversion utilities for network byte order handling.
	/// </summary>
	public static class NB // [henon] need public for testsuite
	{
		/// <summary>
		/// Compare a 32 bit unsigned integer stored in a 32 bit signed integer.
		/// <para />
		/// This function performs an unsigned compare operation, even though Java
		/// does not natively support unsigned integer values. Negative numbers are
		/// treated as larger than positive ones.
		/// </summary>
		/// <param name="a">the first value to compare.</param>
		/// <param name="b">the second value to compare.</param>
		/// <returns>return &lt; 0 if a &lt; b; 0 if a == b; &gt; 0 if a &gt; b.</returns>
		public static int CompareUInt32(int a, int b)
		{
			var cmp = (int)(((uint)a >> 1) - ((uint)b >> 1));
			if (cmp != 0)
				return cmp;
			return (a & 1) - (b & 1);
		}

		/// <summary>
		/// Convert sequence of 2 bytes (network byte order) into unsigned value.
		/// </summary>
		/// <param name="intbuf">
		/// Buffer to acquire the 2 bytes of data from.
		/// </param>
		/// <param name="offset">
		/// Position within the buffer to begin reading from. This
		/// position and the next byte After it (for a total of 2 bytes)
		/// will be read.
		/// </param>
		/// <returns>
		/// Unsigned integer value that matches the 16 bits Read.
		/// </returns>
		public static int decodeUInt16(byte[] intbuf, int offset)
		{
			int r = (intbuf[offset] & 0xff) << 8;
			return r | (intbuf[offset + 1] & 0xff);
		}

		/// <summary>
		/// Convert sequence of 4 bytes (network byte order) into unsigned value.
		/// </summary>
		/// <param name="intbuf">buffer to acquire the 4 bytes of data from.</param>
		/// <param name="offset">
		/// position within the buffer to begin reading from. This
		/// position and the next 3 bytes After it (for a total of 4
		/// bytes) will be read.
		/// </param>
		/// <returns>
		/// Unsigned integer value that matches the 32 bits Read.
		/// </returns>
		public static long decodeUInt32(byte[] intbuf, int offset)
		{
			uint low = (intbuf[offset + 1] & (uint)0xff) << 8;
			low |= (intbuf[offset + 2] & (uint)0xff);
			low <<= 8;

			low |= (intbuf[offset + 3] & (uint)0xff);
			return ((long)(intbuf[offset] & 0xff)) << 24 | low;
		}

		/// <summary>
		/// Convert sequence of 4 bytes (network byte order) into unsigned value.
		/// </summary>
		/// <param name="intbuf">buffer to acquire the 4 bytes of data from.</param>
		/// <param name="offset">
		/// position within the buffer to begin reading from. This
		/// position and the next 3 bytes After it (for a total of 4
		/// bytes) will be read.
		/// </param>
		/// <returns>
		/// Unsigned integer value that matches the 32 bits Read.
		/// </returns>
		public static long DecodeUInt32(byte[] intbuf, int offset)
		{
			return decodeUInt32(intbuf, offset);
		}

		/// <summary>
		/// Convert sequence of 4 bytes (network byte order) into signed value.
		/// </summary>
		/// <param name="intbuf">Buffer to acquire the 4 bytes of data from.</param>
		/// <param name="offset">
		/// position within the buffer to begin reading from. This
		/// position and the next 3 bytes After it (for a total of 4
		/// bytes) will be read.
		/// </param>
		/// <returns>
		/// Signed integer value that matches the 32 bits Read.
		/// </returns>
		public static int DecodeInt32(byte[] intbuf, int offset)
		{
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(intbuf, offset));
		}

		/// <summary>
		/// Read an entire local file into memory as a byte array.
		/// </summary>
		/// <param name="path">Location of the file to read.</param>
		/// <returns>Complete contents of the requested local file.</returns>
		/// <exception cref="IOException">
		/// The file exists, but its contents cannot be read.
		/// </exception>
		public static byte[] ReadFully(FileInfo path)
		{
			return ReadFully(path, int.MaxValue);
		}

		/// <summary>
		/// Read an entire local file into memory as a byte array.
		/// </summary>
		/// <param name="path">Location of the file to read.</param>
		/// <param name="max">
		/// Maximum number of bytes to Read, if the file is larger than
		/// this limit an IOException is thrown.
		/// </param>
		/// <returns>
		/// Complete contents of the requested local file.
		/// </returns>
		/// <exception cref="FileNotFoundException">
		/// The file exists, but its contents cannot be Read.
		/// </exception>
		/// <exception cref="IOException"></exception>
		public static byte[] ReadFully(FileInfo path, int max)
		{
			using (var @in = new FileStream(path.FullName, System.IO.FileMode.Open, FileAccess.Read))
			{
				long sz = @in.Length;
				if (sz > max)
					throw new IOException("File is too large: " + path);
				var buf = new byte[(int)sz];
				ReadFully(@in, buf, 0, buf.Length);
				return buf;
			}
		}

		/// <summary>
		/// Read the entire byte array into memory, or throw an exception.
		/// </summary>
		/// <param name="fd">Input stream to read the data from.</param>
		/// <param name="dst">buffer that must be fully populated</param>
		/// <param name="off">position within the buffer to start writing to.</param>
		/// <param name="len">number of bytes that must be read.</param>
		/// <exception cref="EndOfStreamException">
		/// The stream ended before <paramref name="dst"/> was fully populated.
		/// </exception>
		/// <exception cref="IOException">
		/// There was an error reading from the stream.
		/// </exception>
		public static void ReadFully(Stream fd, byte[] dst, int off, int len)
		{
			while (len > 0)
			{
				int r = fd.Read(dst, off, len);
				if (r <= 0)
					throw new EndOfStreamException("Short Read of block.");
				off += r;
				len -= r;
			}
		}

		/// <summary>
		/// Read the entire byte array into memory, or throw an exception.
		/// </summary>
		/// <param name="fd">Stream to read the data from.</param>
		/// <param name="pos">Position to read from the file at.</param>
		/// <param name="dst">Buffer that must be fully populated, [off, off+len].</param>
		/// <param name="off">position within the buffer to start writing to.</param>
		/// <param name="len">number of bytes that must be read.</param>
		/// <exception cref="EndOfStreamException">
		/// The <paramref name="stream"/> ended before the requested number of 
		/// bytes were read.
		/// </exception>
		/// <exception cref="NotSupportedException">
		/// The <paramref name="stream"/> does not supports seeking.
		/// </exception>
		/// <exception cref="IOException">
		/// There was an error reading from the stream.
		/// </exception>
		public static void ReadFully(Stream fd, long pos, byte[] dst, int off, int len)
		{
			while (len > 0)
			{
				fd.Position = pos;
				int r = fd.Read(dst, off, len);
				if (r <= 0)
					throw new EndOfStreamException("Short Read of block.");
				pos += r;
				off += r;
				len -= r;
			}
		}


		/// <summary>
		/// Skip an entire region of an input stream.
		/// <para />
		/// The input stream's position is moved forward by the number of requested
		/// bytes, discarding them from the input. This method does not return until
		/// the exact number of bytes requested has been skipped.
		/// </summary>
		/// <param name="fd">The stream to skip bytes from.</param>
		/// <param name="toSkip">
		/// Total number of bytes to be discarded. Must be >= 0.
		/// </param>
		/// <exception cref="EndOfStreamException">
		/// The stream ended before the requested number of bytes were
		/// skipped.
		/// </exception>
		/// <exception cref="IOException">
		/// There was an error reading from the stream.
		/// </exception>
		public static void skipFully(Stream fd, long toSkip)
		{
			while (toSkip > 0)
			{
				var r = fd.Seek(toSkip, SeekOrigin.Current);
				if (r <= 0)
					throw new EndOfStreamException("Short skip of block");
				toSkip -= r;
			}
		}

		/// <summary>
		/// Convert sequence of 8 bytes (network byte order) into unsigned value.
		/// </summary>
		/// <param name="intbuf">buffer to acquire the 8 bytes of data from.</param>
		/// <param name="offset">
		/// Position within the buffer to begin reading from. This
		/// position and the next 7 bytes After it (for a total of 8
		/// bytes) will be read.
		/// </param>
		/// <returns>
		/// Unsigned integer value that matches the 64 bits read.
		/// </returns>
		public static long DecodeUInt64(byte[] intbuf, int offset)
		{
			return (DecodeUInt32(intbuf, offset) << 32) | DecodeUInt32(intbuf, offset + 4);
		}

		/// <summary>
		/// This function takes two arguments; the integer value to be 
		/// converted and the base value (2, 8, or 16)  to which the number 
		/// is converted to.
		/// </summary>
		/// <param name="iDec">the decimal</param>
		/// <param name="numbase">the base of the output</param>
		/// <returns>a string representation of the base number</returns>
		public static string DecimalToBase(int iDec, int numbase) // [henon] needed to output octal numbers
		{
			return Convert.ToString(iDec, numbase);
		}

		/// <summary>
		/// This function takes two arguments; a string value representing the binary, octal, or hexadecimal 
		/// value and the corresponding integer base value respective to the first argument. For instance, 
		/// if you pass the first argument value "1101", then the second argument should take the value "2".
		/// </summary>
		/// <param name="sBase">the string in base sBase notation</param>
		/// <param name="numBase">the base to convert from</param>
		/// <returns>decimal</returns>
		public static int BaseToDecimal(string sBase, int numBase)
		{
			long value;
			if (!long.TryParse(sBase, out value))
			{
				throw new ArgumentException("sBase");
			}

			return Convert.ToInt32(Convert.ToString(value, CultureInfo.InvariantCulture), numBase);
		}

		/**
		 * Write a 16 bit integer as a sequence of 2 bytes (network byte order).
		 *
		 * @param intbuf
		 *            buffer to write the 2 bytes of data into.
		 * @param offset
		 *            position within the buffer to begin writing to. This position
		 *            and the next byte After it (for a total of 2 bytes) will be
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
		 *            and the next 3 bytes After it (for a total of 4 bytes) will be
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
		 *            and the next 7 bytes After it (for a total of 8 bytes) will be
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

		/// <summary>
		/// Converts an unsigned byte (.NET default when reading files, for instance) 
		/// to a signed byte
		/// </summary>
		/// <param name="b">The value to be converted.</param>
		/// <returns></returns>
		public static sbyte ConvertUnsignedByteToSigned(byte b)
		{
			// Convert to the equivalent binary string, then to the equivalent signed value.
			return Convert.ToSByte(Convert.ToString(b, 2), 2);
		}
	}
}