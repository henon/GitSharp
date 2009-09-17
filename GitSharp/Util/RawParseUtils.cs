/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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

namespace GitSharp.Util
{
	internal static class RawParseUtils
	{
		private static readonly byte[] Digits16 = Gen16();
		private static readonly byte[] FooterLineKeyChars = GenerateFooterLineKeyChars();
		private static readonly byte[] Base10Byte = { (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9' };
		private const byte MinusByte = (byte)'-';
		private const byte PlusByte = (byte)'+';
		private const byte ZeroByte = (byte)'0';

		private static byte[] Gen16()
		{
			var ret = new byte['f' + 1];

			for (char i = '0'; i <= '9'; i++)
			{
				ret[i] = (byte)(i - '0');
			}

			for (char i = 'a'; i <= 'f'; i++)
			{
				ret[i] = (byte)((i - 'a') + 10);
			}

			for (char i = 'A'; i <= 'F'; i++)
			{
				ret[i] = (byte)((i - 'A') + 10);
			}

			return ret;
		}

		private static byte[] GenerateFooterLineKeyChars()
		{
			var footerLineKeyChars = new byte[(byte)'z' + 1];
			footerLineKeyChars[MinusByte] = 1;

			for (char i = '0'; i <= '9'; i++)
			{
				footerLineKeyChars[i] = 1;
			}

			for (char i = 'A'; i <= 'Z'; i++)
			{
				footerLineKeyChars[i] = 1;
			}

			for (char i = 'a'; i <= 'z'; i++)
			{
				footerLineKeyChars[i] = 1;
			}

			return footerLineKeyChars;
		}

		/// <summary>
		/// Determine if b[ptr] matches src.
		/// </summary>
		/// <param name="b">the buffer to scan.</param>
		/// <param name="ptr">first position within b, this should match src[0].</param>
		/// <param name="src">the buffer to test for equality with b.</param>
		/// <returns>ptr + src.Length if b[ptr..src.Length] == src; else -1.</returns>
		public static int match(byte[] b, int ptr, byte[] src)
		{
			if (ptr + src.Length > b.Length)
			{
				return -1;
			}

			for (int i = 0; i < src.Length; i++, ptr++)
			{
				if (b[ptr] != src[i])
				{
					return -1;
				}
			}

			return ptr;
		}

		/// <summary>
		/// Format a base 10 numeric into a temporary buffer.
		/// <para />
		/// Formatting is performed backwards. The method starts at offset
		/// <code>o-1</code> and ends at <code>o-1-digits</code>, where
		/// <code>digits</code> is the number of positions necessary to store the
		/// base 10 value.
		/// <para />
		/// The argument and return values from this method make it easy to chain
		/// writing, for example:
		///
		/// <code>
		/// byte[] tmp = new byte[64];
		/// int ptr = tmp.Length;
		/// tmp[--ptr] = '\n';
		/// ptr = RawParseUtils.formatBase10(tmp, ptr, 32);
		/// tmp[--ptr] = ' ';
		/// ptr = RawParseUtils.formatBase10(tmp, ptr, 18);
		/// tmp[--ptr] = 0;
		/// string str = new string(tmp, ptr, tmp.Length - ptr);
		/// </code>
		/// </summary>
		/// <param name="b">buffer to write into.</param>
		/// <param name="o">
		/// One offset past the location where writing will begin; writing
		/// proceeds towards lower index values.
		/// </param>
		/// <param name="value">the value to store.</param>
		/// <returns>
		/// the new offset value <code>o</code>. This is the position of
		/// the last byte written. Additional writing should start at one
		/// position earlier.
		/// </returns>
		public static int formatBase10(byte[] b, int o, int value)
		{
			if (value == 0)
			{
				b[--o] = ZeroByte;
				return o;
			}

			bool isneg = value < 0;

			while (value != 0)
			{
				b[--o] = Base10Byte[value % 10];
				value /= 10;
			}

			if (isneg)
			{
				b[--o] = MinusByte;
			}

			return o;
		}

		/// <summary>
		/// Parse a base 10 numeric from a sequence of ASCII digits into an int.
		/// <para />
		/// Digit sequences can begin with an optional run of spaces before the
		/// sequence, and may start with a '+' or a '-' to indicate sign position.
		/// Any other characters will cause the method to stop and return the current
		/// result to the caller.
		/// </summary>
		/// <param name="b">Buffer to scan.</param>
		/// <param name="ptr">Position within buffer to start parsing digits at.</param>
		/// <param name="ptrResult">
		/// Optional location to return the new ptr value through. If null
		/// the ptr value will be discarded.
		/// </param>
		/// <returns>
		/// the value at this location; 0 if the location is not a valid numeric.
		/// </returns>
		public static int ParseBase10(byte[] b, int ptr, MutableInteger ptrResult)
		{
			int r = 0;
			int sign = 0;
			try
			{
				int sz = b.Length;
				while (ptr < sz && char.IsWhiteSpace(Convert.ToChar(b[ptr])))
				{
					ptr++;
				}

				if (ptr >= sz)
				{
					return 0;
				}

				switch (b[ptr])
				{
					case (MinusByte):
						sign = -1;
						ptr++;
						break;

					case (PlusByte):
						ptr++;
						break;
				}

				while (ptr < sz)
				{
					char d = Convert.ToChar(b[ptr]);
					if (!char.IsDigit(d)) break;
					r = r * 10 + ((byte)d - ZeroByte);
					ptr++;
				}
			}
			catch (IndexOutOfRangeException)
			{
				// Not a valid digit.
			}

			if (ptrResult != null)
			{
				ptrResult.value = ptr;
			}

			return sign < 0 ? -r : r;
		}

		/// <summary>
		/// Parse a base 10 numeric from a sequence of ASCII digits into a long.
		/// <para />
		/// Digit sequences can begin with an optional run of spaces before the
		/// sequence, and may start with a '+' or a '-' to indicate sign position.
		/// Any other characters will cause the method to stop and return the current
		/// result to the caller.
		/// </summary>
		/// <param name="b">Buffer to scan.</param>
		/// <param name="ptr">
		/// Position within buffer to start parsing digits at.
		/// </param>
		/// <param name="ptrResult">
		/// Optional location to return the new ptr value through. If null
		/// the ptr value will be discarded.
		/// </param>
		/// <returns>
		/// The value at this location; 0 if the location is not a valid
		/// numeric.
		/// </returns>
		private static long ParseLongBase10(byte[] b, int ptr, MutableInteger ptrResult)
		{
			long r = 0;
			int sign = 0;
			try
			{
				int sz = b.Length;
				while (ptr < sz && b[ptr] == ' ')
				{
					ptr++;
				}

				if (ptr >= sz)
				{
					return 0;
				}

				switch (b[ptr])
				{
					case (MinusByte):
						sign = -1;
						ptr++;
						break;

					case (PlusByte):
						ptr++;
						break;
				}

				while (ptr < sz)
				{
					byte d = b[ptr];
					if (!char.IsDigit(Convert.ToChar(d))) break;
					r = r * 10 + (d - ZeroByte);
					ptr++;
				}
			}
			catch (IndexOutOfRangeException)
			{
				// Not a valid digit.
			}

			if (ptrResult != null)
			{
				ptrResult.value = ptr;
			}

			return sign < 0 ? -r : r;
		}

		///	<summary>
		/// Parse 4 character base 16 (hex) formatted string to unsigned integer.
		/// <para />
		/// The number is read in network byte order, that is, most significant
		/// nibble first.
		/// </summary>
		/// <param name="bs">
		/// buffer to parse digits from; positions <code>[p, p+4]</code> will
		/// be parsed.
		/// </param>
		/// <param name="p">First position within the buffer to parse.</param>
		///	<returns>The integer value.</returns>
		/// <exception cref="IndexOutOfRangeException">
		/// If the string is not hex formatted.
		/// </exception>
		public static int ParseHexInt16(byte[] bs, int p)
		{
			int r = Digits16[bs[p]] << 4;

			r |= Digits16[bs[p + 1]];
			r <<= 4;

			r |= Digits16[bs[p + 2]];
			r <<= 4;

			r |= Digits16[bs[p + 3]];
			if (r < 0)
			{
				throw new IndexOutOfRangeException();
			}

			return r;
		}

		///	<summary>
		/// Parse 8 character base 16 (hex) formatted string to unsigned integer.
		///	<para />
		///	The number is read in network byte order, that is, most significant
		///	nibble first.
		///	</summary>
		///	<param name="bs">
		/// Buffer to parse digits from; positions <code>[p, p+8]</code> will
		/// be parsed.
		/// </param>
		///	<param name="p">First position within the buffer to parse.</param>
		///	<returns> the integer value.</returns>
		///	<exception cref="IndexOutOfRangeException">
		///	if the string is not hex formatted.
		/// </exception>
		public static int ParseHexInt32(byte[] bs, int p)
		{
			int r = Digits16[bs[p]] << 4;

			r |= Digits16[bs[p + 1]];
			r <<= 4;

			r |= Digits16[bs[p + 2]];
			r <<= 4;

			r |= Digits16[bs[p + 3]];
			r <<= 4;

			r |= Digits16[bs[p + 4]];
			r <<= 4;

			r |= Digits16[bs[p + 5]];
			r <<= 4;

			r |= Digits16[bs[p + 6]];

			int last = Digits16[bs[p + 7]];
			if (r < 0 || last < 0)
			{
				throw new IndexOutOfRangeException();
			}

			return (r << 4) | last;
		}

		///	<summary>
		/// Parse a single hex digit to its numeric value (0-15).
		///	</summary>
		///	<param name="digit">Hex character to parse.</param>
		///	<returns>Numeric value, in the range 0-15.</returns>
		///	<exception cref="IndexOutOfRangeException">
		///	If the input digit is not a valid hex digit.
		/// </exception>
		public static int ParseHexInt4(byte digit)
		{
			byte r = Digits16[digit];

			if (r < 0)
			{
				throw new IndexOutOfRangeException();
			}

			return r;
		}

		/// <summary>
		/// Parse a Git style timezone string.
		///	<para />
		///	The sequence "-0315" will be parsed as the numeric value -195, as the
		///	lower two positions count minutes, not 100ths of an hour.
		///	</summary>
		///	<param name="b">Buffer to scan.</param>
		///	<param name="ptr">
		///	Position within buffer to start parsing digits at. </param>
		///	<returns> the timezone at this location, expressed in minutes. </returns>
		public static int ParseTimeZoneOffset(byte[] b, int ptr)
		{
			int v = ParseBase10(b, ptr, null);
			int tzMins = v % 100;
			int tzHours = v / 100;
			return tzHours * 60 + tzMins;
		}

		/// <summary>
		/// Locate the first position after a given character.
		/// </summary>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">
		/// position within buffer to start looking for <paramref name="chrA"/> at.
		/// </param>
		/// <param name="chrA">character to find.</param>
		/// <returns>New position just after <paramref name="chrA"/>.</returns>
		public static int next(byte[] b, int ptr, char chrA)
		{
			int sz = b.Length;

			while (ptr < sz)
			{
				if (b[ptr++] == chrA)
				{
					return ptr;
				}
			}

			return ptr;
		}

		/// <summary>
		/// Locate the first position after LF.
		/// </summary>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">
		/// position within buffer to start looking for LF at.
		/// </param>
		/// <returns>New position just after LF.</returns>
		public static int nextLF(byte[] b, int ptr)
		{
			return next(b, ptr, '\n');
		}

		///	<summary>
		/// Locate the first position after either the given character or LF.
		///	<para />
		///	This method stops on the first match it finds from either chrA or '\n'.
		///	</summary>
		///	<param name="b">Buffer to scan.</param>
		///	<param name="ptr">
		///	Position within buffer to start looking for chrA or LF at.
		/// </param>
		///	<param name="chrA"><see cref="char"/> to find.</param>
		///	<returns>
		/// New position just after the first chrA or LF to be found.
		/// </returns>
		public static int nextLF(byte[] b, int ptr, char chrA)
		{
			int sz = b.Length;

			while (ptr < sz)
			{
				var c = b[ptr++];
				if (c == chrA || c == '\n')
				{
					return ptr;
				}
			}

			return ptr;
		}

		/// <summary>
		/// Locate the first position before a given character.
		/// </summary>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">
		/// Position within buffer to start looking for chrA at.
		/// </param>
		/// <param name="chrA"><see cref="char"/> to find.</param>
		/// <returns>
		/// New position just before chrA, -1 for not found
		/// </returns>
		public static int prev(byte[] b, int ptr, char chrA)
		{
			if (ptr == b.Length)
			{
				--ptr;
			}

			while (ptr >= 0)
			{
				if (b[ptr--] == chrA)
				{
					return ptr;
				}
			}

			return ptr;
		}

		/// <summary>
		/// Locate the first position before the previous LF.
		/// <para />
		/// This method stops on the first '\n' it finds.
		/// </summary>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">
		/// Position within buffer to start looking for LF at.
		/// </param>
		/// <returns>
		/// New position just before LF, -1 for not found
		/// </returns>
		public static int prevLF(byte[] b, int ptr)
		{
			return prev(b, ptr, '\n');
		}

		/// <summary>
		/// Locate the previous position before either the given character or LF.
		///	<para />
		///	This method stops on the first match it finds from either chrA or '\n'.
		///	</summary>
		///	<param name="b">Buffer to scan.</param>
		///	<param name="ptr">
		///	Position within buffer to start looking for chrA or LF at.
		/// </param>
		///	<param name="chrA"><see cref="char" to find.</param>
		///	<returns>
		/// New position just before the first chrA or LF to be found, -1 for
		///	not found.
		/// </returns>
		public static int prevLF(byte[] b, int ptr, char chrA)
		{
			if (ptr == b.Length)
			{
				--ptr;
			}

			while (ptr >= 0)
			{
				byte c = b[ptr--];
				if (c == chrA || c == '\n')
				{
					return ptr;
				}
			}

			return ptr;
		}

		/// <summary>
		/// Index the region between <code>[ptr, end]</code> to find line starts.
		///	<para />
		///	The returned list is 1 indexed. Index 0 contains
		///	<seealso cref="Int32.MinValue"/> to pad the list out.
		///	<para />
		///	Using a 1 indexed list means that line numbers can be directly accessed
		///	from the list, so <code>list.get(1)</code> (aka get line 1) returns
		///	<paramref name="ptr"/>.
		///	<para />
		///	The last element (index <code>map.size()-1</code>) always contains
		///	<paramref name="end"/>.
		///	</summary>
		///	<param name="buf">buffer to scan.</param>
		///	<param name="ptr">
		///	Position within the buffer corresponding to the first byte of
		///	line 1.
		/// </param>
		///	<param name="end">
		///	1 past the end of the content within <paramref name="buf"/>.
		/// </param>
		///	<returns>
		/// A line map indexing the start position of each line.
		/// </returns>
		public static IntList lineMap(byte[] buf, int ptr, int end)
		{
			// Experimentally derived from multiple source repositories
			// the average number of bytes/line is 36. Its a rough guess
			// to initially size our map close to the target.
			//
			var map = new IntList((end - ptr) / 36);
			map.fillTo(1, int.MinValue);
			for (; ptr < end; ptr = nextLF(buf, ptr))
			{
				map.add(ptr);
			}
			map.add(end);
			return map;
		}

		///	<summary>
		/// Locate the "author " header line data.
		///	</summary>
		///	<param name="b">Buffer to scan.</param>
		///	<param name="ptr">
		/// Position in buffer to start the scan at. Most callers should
		/// pass 0 to ensure the scan starts from the beginning of the
		/// commit buffer and does not accidentally look at message body.
		/// </param>
		/// <returns>
		/// Position just after the space in "author ", so the first
		/// character of the author's name. If no author header can be
		/// located -1 is returned.
		/// </returns>
		public static int author(byte[] b, int ptr)
		{
			int sz = b.Length;
			if (ptr == 0)
			{
				ptr += 46; // skip the "tree ..." line.
			}

			while (ptr < sz && b[ptr] == (byte)'p')
			{
				ptr += 48; // skip this parent.
			}

			return match(b, ptr, ObjectChecker.author);
		}

		///	<summary> * Locate the "committer " header line data.
		///	</summary>
		///	<param name="b">Buffer to scan.</param>
		///	<param name="ptr">
		/// Position in buffer to start the scan at. Most callers should
		/// pass 0 to ensure the scan starts from the beginning of the
		/// commit buffer and does not accidentally look at message body.
		/// </param>
		/// <returns>
		/// Position just after the space in "committer ", so the first
		/// character of the committer's name. If no committer header can be
		/// located -1 is returned.
		/// </returns>
		public static int committer(byte[] b, int ptr)
		{
			int sz = b.Length;
			if (ptr == 0)
			{
				ptr += 46; // skip the "tree ..." line.
			}

			while (ptr < sz && b[ptr] == (byte)'p')
			{
				ptr += 48; // skip this parent.
			}

			if (ptr < sz && b[ptr] == (byte)'a')
			{
				ptr = nextLF(b, ptr);
			}

			return match(b, ptr, ObjectChecker.committer);
		}

		///	<summary> * Locate the "tagger " header line data.
		///	</summary>
		///	<param name="b">Buffer to scan.</param>
		///	<param name="ptr">
		/// Position in buffer to start the scan at. Most callers should
		/// pass 0 to ensure the scan starts from the beginning of the tag
		/// buffer and does not accidentally look at message body.
		/// </param>
		///	<returns>
		/// Position just after the space in "tagger ", so the first
		/// character of the tagger's name. If no tagger header can be
		/// located -1 is returned.
		/// </returns>
		public static int tagger(byte[] b, int ptr)
		{
			int sz = b.Length;
			if (ptr == 0)
			{
				ptr += 48; // skip the "object ..." line.
			}

			while (ptr < sz)
			{
				if (b[ptr] == (byte)'\n') return -1;

				int m = match(b, ptr, ObjectChecker.tagger);
				if (m >= 0)
				{
					return m;
				}

				ptr = nextLF(b, ptr);
			}
			return -1;
		}

		///	<summary>
		/// Locate the "encoding " header line.
		/// </summary>
		///	<param name="b">Buffer to scan.</param>
		/// <param name="ptr">
		/// Position in buffer to start the scan at. Most callers should
		/// pass 0 to ensure the scan starts from the beginning of the
		/// buffer and does not accidentally look at the message body.
		/// </param>
		/// <returns>
		/// Position just after the space in "encoding ", so the first
		/// character of the encoding's name. If no encoding header can be
		/// located -1 is returned (and UTF-8 should be assumed).
		/// </returns>
		public static int encoding(byte[] b, int ptr)
		{
			int sz = b.Length;
			while (ptr < sz)
			{
				if (b[ptr] == '\n')
				{
					return -1;
				}

				if (b[ptr] == 'e') break;

				ptr = nextLF(b, ptr);
			}
			return match(b, ptr, ObjectChecker.encoding);
		}

		/// <summary>
		/// Parse the "encoding " header into a character set reference.
		/// <para />
		/// Locates the "encoding " header (if present) by first calling
		/// <seealso cref="encoding(byte[], int)"/> and then returns the proper character set
		/// to apply to this buffer to evaluate its contents as character data.
		/// <para />
		/// If no encoding header is present, <seealso cref="Constants#CHARSET"/> is assumed.
		/// </summary>
		/// <param name="b">Buffer to scan.</param>
		/// <returns>The <see cref="Encoding"/> representation. Never null.</returns>
		public static Encoding parseEncoding(byte[] b)
		{
			int enc = encoding(b, 0);
			if (enc < 0) return Constants.CHARSET;

			int lf = nextLF(b, enc);
			string encodingName = decode(Constants.CHARSET, b, enc, lf - 1).ToUpperInvariant();

			if (string.IsNullOrEmpty(encodingName)) return Constants.CHARSET;

			if (encodingName.Contains("_"))
			{
				encodingName = encodingName.Replace("_", "-");
			}

			return Encoding.GetEncoding(encodingName);
		}

		///	<summary>
		/// Parse a name line (e.g. author, committer, tagger) into a PersonIdent.
		///	<para />
		///	When passing in a value for <paramref name="nameB"/> callers should use the
		///	return value of <seealso cref="author(byte[], int)"/> or
		///	<seealso cref="committer(byte[], int)"/>, as these methods provide 
		/// the proper position within the buffer.
		///	</summary>
		///	<param name="raw">The buffer to parse character data from.</param>
		///	<param name="nameB">
		/// First position of the identity information. This should be the
		/// first position after the space which delimits the header field
		/// name (e.g. "author" or "committer") from the rest of the
		/// identity line.
		/// </param>
		///	<returns> the parsed identity. Never null. </returns>
		public static PersonIdent parsePersonIdent(byte[] raw, int nameB)
		{
			Encoding cs = parseEncoding(raw);
			int emailB = nextLF(raw, nameB, '<');
			int emailE = nextLF(raw, emailB, '>');

			string name = decode(cs, raw, nameB, emailB - 2);
			string email = decode(cs, raw, emailB, emailE - 1);

			var ptrout = new MutableInteger();
			long when = ParseLongBase10(raw, emailE + 1, ptrout);
			int tz = ParseTimeZoneOffset(raw, ptrout.value);

			return new PersonIdent(name, email, when, tz);
		}

		/// <summary>
		/// Parse a name data (e.g. as within a reflog) into a PersonIdent.
		/// <para />
		/// When passing in a value for <paramref name="nameB"/> callers should use the
		/// return value of <see cref="author(byte[], int)"/> or
		/// <see cref="committer(byte[], int)"/>, as these methods provide the proper
		/// position within the buffer.
		/// </summary>
		/// <param name="raw">The buffer to parse character data from.</param>
		/// <param name="nameB">
		/// First position of the identity information. This should be the
		/// first position After the space which delimits the header field
		/// name (e.g. "author" or "committer") from the rest of the
		/// identity line.
		/// </param>
		/// <returns>The parsed identity. Never null.</returns>
		public static PersonIdent parsePersonIdentOnly(byte[] raw, int nameB)
		{
			int stop = nextLF(raw, nameB);
			int emailB = nextLF(raw, nameB, '<');
			int emailE = nextLF(raw, emailB, '>');

			string email = emailE < stop ? decode(raw, emailB, emailE - 1) : "invalid";
			string name = emailB < stop ? decode(raw, nameB, emailB - 2) : decode(raw, nameB, stop);

			var ptrout = new MutableInteger();

			long when;
			int tz;

			if (emailE < stop)
			{
				when = ParseLongBase10(raw, emailE + 1, ptrout);
				tz = ParseTimeZoneOffset(raw, ptrout.value);
			}
			else
			{
				when = 0;
				tz = 0;
			}

			return new PersonIdent(name, email, when * 1000L, tz);
		}

		///	<summary>
		/// Locate the end of a footer line key string.
		/// <para />
		/// If the region at {@code raw[ptr]} matches {@code ^[A-Za-z0-9-]+:} (e.g.
		/// "Signed-off-by: A. U. Thor\n") then this method returns the position of
		/// the first ':'.
		/// <para />
		/// If the region at {@code raw[ptr]} does not match {@code ^[A-Za-z0-9-]+:}
		/// then this method returns -1.
		/// </summary>
		/// <param name="raw">Buffer to scan.</param>
		/// <param name="ptr">
		/// First position within raw to consider as a footer line key.
		/// </param>
		///	<returns>
		/// Position of the ':' which terminates the footer line key if this
		/// is otherwise a valid footer line key; otherwise -1.
		/// </returns>
		public static int endOfFooterLineKey(byte[] raw, int ptr)
		{
			try
			{
				while (true)
				{
					byte c = raw[ptr];
					if (FooterLineKeyChars[c] == 0)
					{
						if (c == (byte)':')
						{
							return ptr;
						}
						return -1;
					}
					ptr++;
				}
			}
			catch (IndexOutOfRangeException)
			{
				return -1;
			}
		}

		///	<summary>
		/// Decode a buffer under UTF-8, if possible.
		///	<para />
		///	If the byte stream cannot be decoded that way, the platform default is tried
		///	and if that too fails, the fail-safe ISO-8859-1 encoding is tried.
		///	</summary>
		///	<param name="buffer">Vuffer to pull raw bytes from.</param>
		///	<returns>
		/// A string representation of the range <code>[start,end]</code>,
		///	 after decoding the region through the specified character set.
		/// </returns>
		public static string decode(byte[] buffer)
		{
			return decode(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Decode a buffer under UTF-8, if possible.
		/// <para />
		/// If the byte stream cannot be decoded that way, the platform default is
		/// tried and if that too fails, the fail-safe ISO-8859-1 encoding is tried.
		/// </summary>
		/// <param name="buffer">Buffer to pull raw bytes from.</param>
		/// <param name="start">Start position in buffer.</param>
		/// <param name="end">
		/// One position past the last location within the buffer to take
		/// data from.
		/// </param>
		/// <returns>
		/// A string representation of the range <code>[start,end)</code>,
		/// after decoding the region through the specified character set.
		/// </returns>
		public static string decode(byte[] buffer, int start, int end)
		{
			return decode(Constants.CHARSET, buffer, start, end);
		}

		///	<summary>
		/// Decode a buffer under the specified character set if possible.
		/// <para />
		/// If the byte stream cannot be decoded that way, the platform default is tried
		/// and if that too fails, the fail-safe ISO-8859-1 encoding is tried.
		/// </summary>
		/// <param name="cs">Character set to use when decoding the buffer.</param>
		/// <param name="buffer">Buffer to pull raw bytes from.</param>
		/// <returns>
		/// A string representation of the range <code>[start,end)</code>,
		/// after decoding the region through the specified character set.
		/// </returns>
		public static string decode(Encoding cs, byte[] buffer)
		{
			return decode(cs, buffer, 0, buffer.Length);
		}

		///	<summary>
		/// Decode a region of the buffer under the specified character set if possible.
		/// <para />
		///	If the char stream cannot be decoded that way, the platform default is tried
		/// and if that too fails, the fail-safe ISO-8859-1 encoding is tried.
		/// </summary>
		/// <param name="cs">Character set to use when decoding the buffer.</param>
		///	<param name="buffer">Buffer to pull raw bytes from.</param>
		///	<param name="start">First position within the buffer to take data from.</param>
		///	<param name="end">
		/// One position past the last location within the buffer to take
		/// data from.
		/// </param>
		///	<returns>
		/// A string representation of the range <code>[start,end]</code>,
		/// after decoding the region through the specified character set.
		/// </returns>
		public static string decode(Encoding cs, byte[] buffer, int start, int end)
		{
			try
			{
				return decodeNoFallback(cs, buffer, start, end);
			}
			catch (DecoderFallbackException)
			{
				// Fall back to an ISO-8859-1 style encoding. At least all of
				// the bytes will be present in the output.
				//
				return extractBinaryString(buffer, start, end);
			}
		}

		///	<summary>
		/// Decode a region of the buffer under the specified character set if
		/// possible.
		/// <para />
		/// If the byte stream cannot be decoded that way, the platform default is
		/// tried and if that too fails, an exception is thrown.
		/// </summary>
		/// <param name="cs">Character set to use when decoding the buffer.</param>
		/// <param name="buffer">Buffer to pull raw bytes from.</param>
		/// <param name="start">First position within the buffer to take data from.</param>
		/// <param name="end">
		/// One position past the last location within the buffer to take
		/// data from.
		/// </param>
		/// <returns>
		/// A string representation of the range <code>[start,end]</code>,
		/// after decoding the region through the specified character set.
		/// </returns>
		///	<exception cref="CharacterCodingException">
		/// The input is not in any of the tested character sets.
		/// </exception>
		public static string decodeNoFallback(Encoding cs, byte[] buffer, int start, int end)
		{
			var b = new byte[end - start];
			for (int i = 0; i < end - start; i++)
			{
				b[i] = buffer[start + i];
			}

			try
			{
				return cs != null ? cs.GetString(b) : Constants.CHARSET.GetString(b);
			}
			catch (DecoderFallbackException)
			{
			}

			throw new DecoderFallbackException("decoding failed with encoding: " + cs.HeaderName);
		}

		///	<summary>
		/// Decode a region of the buffer under the ISO-8859-1 encoding.
		/// <para />
		/// Each byte is treated as a single character in the 8859-1 character
		///	encoding, performing a raw binary->char conversion.
		/// </summary>
		/// <param name="buffer">Buffer to pull raw bytes from.</param>
		/// <param name="start">
		/// First position within the buffer to take data from.
		/// </param>
		/// <param name="end">
		/// One position past the last location within the buffer to take
		/// data from.
		/// </param>
		/// <returns>
		/// A string representation of the range <code>[start,end]</code>.
		/// </returns>
		public static string extractBinaryString(byte[] buffer, int start, int end)
		{
			var r = new StringBuilder(end - start);
			for (int i = start; i < end; i++)
			{
				r.Append((char)(buffer[i] & 0xff));
			}
			return r.ToString();
		}

		///	<summary>
		/// Locate the position of the commit message body.
		///	</summary>
		///	<param name="b">Buffer to scan.</param>
		///	<param name="ptr">
		/// Position in buffer to start the scan at. Most callers should
		/// pass 0 to ensure the scan starts from the beginning of the
		/// commit buffer.
		/// </param>
		///	<returns>Position of the user's message buffer.</returns>
		public static int commitMessage(byte[] b, int ptr)
		{
			int sz = b.Length;
			if (ptr == 0)
			{
				ptr += 46; // skip the "tree ..." line.
			}

			while (ptr < sz && b[ptr] == (byte)'p')
			{
				ptr += 48; // skip this parent.
			}

			// Skip any remaining header lines, ignoring what their actual
			// header line type is. This is identical to the logic for a tag.
			//
			return tagMessage(b, ptr);
		}

		///	<summary>
		/// Locate the position of the tag message body.
		/// </summary>
		/// <param name="b">Buffer to scan.</param>
		/// <param name="ptr">
		/// Position in buffer to start the scan at. Most callers should
		/// pass 0 to ensure the scan starts from the beginning of the tag
		/// buffer.
		/// </param>
		/// <returns>Position of the user's message buffer.</returns>
		public static int tagMessage(byte[] b, int ptr)
		{
			int sz = b.Length;
			if (ptr == 0)
			{
				ptr += 48; // skip the "object ..." line.
			}

			while (ptr < sz && b[ptr] != (byte)'\n')
			{
				ptr = nextLF(b, ptr);
			}

			if (ptr < sz && b[ptr] == (byte)'\n')
			{
				return ptr + 1;
			}

			return -1;
		}

		/// <summary>
		/// Locate the end of a paragraph.
		/// <para />
		/// A paragraph is ended by two consecutive LF bytes.
		/// </summary>
		/// <param name="b">Buffer to scan.</param>
		/// <param name="start">
		/// Position in buffer to start the scan at. Most callers will
		/// want to pass the first position of the commit message (as
		/// found by <seealso cref="commitMessage(byte[], int)"/>.
		/// </param>
		/// <returns>
		/// Position of the LF at the end of the paragraph;
		/// <code>b.length</code> if no paragraph end could be located.
		/// </returns>
		public static int endOfParagraph(byte[] b, int start)
		{
			int ptr = start;
			int sz = b.Length;

			while (ptr < sz && b[ptr] != (byte)'\n')
			{
				ptr = nextLF(b, ptr);
			}

			while (0 < ptr && start < ptr && b[ptr - 1] == (byte)'\n')
			{
				ptr--;
			}

			return ptr;
		}
	}
}