/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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

using GitSharp;
using System.Text;
using System;
using System.Text.RegularExpressions;

namespace GitSharp.Util
{
    /** Utility functions related to quoted string handling. */
    public abstract class QuotedString
    {
	    /**
	     * Quoting style used by the Bourne shell.
	     * <p>
	     * Quotes are unconditionally inserted during {@link #quote(String)}. This
	     * protects shell meta-characters like <code>$</code> or <code>~</code> from
	     * being recognized as special.
	     */
	    public static readonly BourneStyle BOURNE = new BourneStyle();

	    /** Bourne style, but permits <code>~user</code> at the start of the string. */
	    public static readonly BourneUserPathStyle BOURNE_USER_PATH = new BourneUserPathStyle();

	    /**
	     * Quote an input string by the quoting rules.
	     * <p>
	     * If the input string does not require any quoting, the same String
	     * reference is returned to the caller.
	     * <p>
	     * Otherwise a quoted string is returned, including the opening and closing
	     * quotation marks at the start and end of the string. If the style does not
	     * permit raw Unicode characters then the string will first be encoded in
	     * UTF-8, with unprintable sequences possibly escaped by the rules.
	     *
	     * @param in
	     *            any non-null Unicode string.
	     * @return a quoted string. See above for details.
	     */
	    public abstract String quote(String in_str);

	    /**
	     * Clean a previously quoted input, decoding the result via UTF-8.
	     * <p>
	     * This method must match quote such that:
	     *
	     * <pre>
	     * a.equals(dequote(quote(a)));
	     * </pre>
	     *
	     * is true for any <code>a</code>.
	     *
	     * @param in
	     *            a Unicode string to remove quoting from.
	     * @return the cleaned string.
	     * @see #dequote(byte[], int, int)
	     */
	    public String dequote(String in_str)
        {
		    byte[] b = Constants.encode(in_str);
		    return dequote(b, 0, b.Length);
	    }

	    /**
	     * Decode a previously quoted input, scanning a UTF-8 encoded buffer.
	     * <p>
	     * This method must match quote such that:
	     *
	     * <pre>
	     * a.equals(dequote(Constants.encode(quote(a))));
	     * </pre>
	     *
	     * is true for any <code>a</code>.
	     * <p>
	     * This method removes any opening/closing quotation marks added by
	     * {@link #quote(String)}.
	     *
	     * @param in
	     *            the input buffer to parse.
	     * @param offset
	     *            first position within <code>in</code> to scan.
	     * @param end
	     *            one position past in <code>in</code> to scan.
	     * @return the cleaned string.
	     */
	    public abstract String dequote(byte[] in_str, int offset, int end);

	    /**
	     * Quoting style used by the Bourne shell.
	     * <p>
	     * Quotes are unconditionally inserted during {@link #quote(String)}. This
	     * protects shell meta-characters like <code>$</code> or <code>~</code> from
	     * being recognized as special.
	     */
	    public class BourneStyle : QuotedString
        {
		    public override String quote(String in_str)
            {
			    StringBuilder r = new StringBuilder();
			    r.Append('\'');
			    int start = 0, i = 0;
			    for (; i < in_str.Length; i++) {
				    switch (in_str[i]) {
				    case '\'':
				    case '!':
					    r.Append(in_str, start, i - start);
					    r.Append('\'');
					    r.Append('\\');
					    r.Append(in_str[i]);
					    r.Append('\'');
					    start = i + 1;
					    break;
				    }
			    }

                r.Append(in_str, start, i - start);
			    r.Append('\'');
			    return r.ToString();
		    }

		    public override String dequote(byte[] in_str, int ip, int ie)
            {
			    bool inquote = false;
			    byte[] r = new byte[ie - ip];
			    int rPtr = 0;
			    while (ip < ie)
                {
				    byte b = in_str[ip++];
				    switch (b)
                    {
				    case (byte)'\'':
					    inquote = !inquote;
					    continue;
				    case (byte)'\\':
					    if (inquote || ip == ie)
						    r[rPtr++] = b; // literal within a quote
					    else
						    r[rPtr++] = in_str[ip++];
					    continue;
				    default:
					    r[rPtr++] = b;
					    continue;
				    }
			    }
			    return RawParseUtils.decode(Constants.CHARSET, r, 0, rPtr);
		    }
	    }

	    /** Bourne style, but permits <code>~user</code> at the start of the string. */
        public sealed class BourneUserPathStyle : BourneStyle
        {
		    public override String quote(String in_str)
            {
                if (new Regex("^~[A-Za-z0-9_-]+$").IsMatch(in_str))
                {
				    // If the string is just "~user" we can assume they
				    // mean "~user/".
				    //
				    return in_str + "/";
			    }

			    if (new Regex("^~[A-Za-z0-9_-]*/.*$").IsMatch(in_str))
                {
				    // If the string is of "~/path" or "~user/path"
				    // we must not escape ~/ or ~user/ from the shell.
				    //
				    int i = in_str.IndexOf('/') + 1;
				    if (i == in_str.Length)
					    return in_str;
				    return in_str.Substring(0, i) + base.quote(in_str.Substring(i));
			    }

			    return base.quote(in_str);
		    }
	    }

	    /** Quoting style that obeys the rules Git applies to file names */
	    public sealed class GitPathStyle : QuotedString
        {
		    private static readonly byte[] quote_m;
		    
            static GitPathStyle()
            {
                quote_m = new byte[128];
                for (int i = 0; i < quote_m.Length; i++)
                {
                    quote_m[i] = byte.MaxValue;
                }

			    for (int i = '0'; i <= '9'; i++)
                    quote_m[i] = 0;
			    for (int i = 'a'; i <= 'z'; i++)
                    quote_m[i] = 0;
			    for (int i = 'A'; i <= 'Z'; i++)
                    quote_m[i] = 0;
                quote_m[' '] = 0;
                quote_m['+'] = 0;
                quote_m[','] = 0;
                quote_m['-'] = 0;
                quote_m['.'] = 0;
                quote_m['/'] = 0;
                quote_m['='] = 0;
                quote_m['_'] = 0;
                quote_m['^'] = 0;

                quote_m['\u0007'] = (byte)'a';
                quote_m['\b'] = (byte)'b';
                quote_m['\f'] = (byte)'f';
                quote_m['\n'] = (byte)'n';
                quote_m['\r'] = (byte)'r';
                quote_m['\t'] = (byte)'t';
                quote_m['\u000B'] = (byte)'v';
                quote_m['\\'] = (byte)'\\';
                quote_m['"'] = (byte)'"';
		    }

		    public override String quote(String instr)
            {
			    if (instr.Length == 0)
				    return "\"\"";
			    bool reuse = true;
			    byte[] in_str = Constants.encode(instr);
			    StringBuilder r = new StringBuilder(2 + in_str.Length);
			    r.Append('"');
			    for (int i = 0; i < in_str.Length; i++) {
				    int c = in_str[i] & 0xff;
                    if (c < quote_m.Length)
                    {
                        byte style = quote_m[c];
					    if (style == 0) {
						    r.Append((char) c);
						    continue;
					    }
					    if (style > 0) {
						    reuse = false;
						    r.Append('\\');
						    r.Append((char) style);
						    continue;
					    }
				    }

				    reuse = false;
				    r.Append('\\');
				    r.Append((char) (((c >> 6) & 03) + '0'));
				    r.Append((char) (((c >> 3) & 07) + '0'));
				    r.Append((char) (((c >> 0) & 07) + '0'));
			    }
			    if (reuse)
				    return instr;
			    r.Append('"');
			    return r.ToString();
		    }

		    public override String dequote(byte[] in_str, int inPtr, int inEnd)
            {
			    if (2 <= inEnd - inPtr && in_str[inPtr] == '"' && in_str[inEnd - 1] == '"')
				    return dq(in_str, inPtr + 1, inEnd - 1);
			    return RawParseUtils.decode(Constants.CHARSET, in_str, inPtr, inEnd);
		    }

		    private static String dq(byte[] in_str, int inPtr, int inEnd)
            {
			    byte[] r = new byte[inEnd - inPtr];
			    int rPtr = 0;
			    while (inPtr < inEnd)
                {
				    byte b = in_str[inPtr++];
				    if (b != '\\') {
					    r[rPtr++] = b;
					    continue;
				    }

				    if (inPtr == inEnd) {
					    // Lone trailing backslash. Treat it as a literal.
					    //
					    r[rPtr++] = (byte)'\\';
					    break;
				    }

				    switch (in_str[inPtr++]) {
				    case (byte)'a':
					    r[rPtr++] = 0x07 /* \a = BEL */;
					    continue;
				    case (byte)'b':
					    r[rPtr++] = (byte)'\b';
					    continue;
				    case (byte)'f':
					    r[rPtr++] = (byte)'\f';
					    continue;
				    case (byte)'n':
					    r[rPtr++] = (byte)'\n';
					    continue;
				    case (byte)'r':
					    r[rPtr++] = (byte)'\r';
					    continue;
				    case (byte)'t':
					    r[rPtr++] = (byte)'\t';
					    continue;
				    case (byte)'v':
					    r[rPtr++] = 0x0B/* \v = VT */;
					    continue;

				    case (byte)'\\':
				    case (byte)'"':
					    r[rPtr++] = in_str[inPtr - 1];
					    continue;

				    case (byte)'0':
				    case (byte)'1':
				    case (byte)'2':
				    case (byte)'3': {
					    int cp = in_str[inPtr - 1] - '0';
					    while (inPtr < inEnd) {
						    byte c = in_str[inPtr];
						    if ('0' <= c && c <= '7') {
							    cp <<= 3;
							    cp |= c - '0';
							    inPtr++;
						    } else {
							    break;
						    }
					    }
					    r[rPtr++] = (byte) cp;
					    continue;
				    }

				    default:
					    // Any other code is taken literally.
					    //
					    r[rPtr++] = (byte)'\\';
					    r[rPtr++] = in_str[inPtr - 1];
					    continue;
				    }
			    }

			    return RawParseUtils.decode(Constants.CHARSET, r, 0, rPtr);
		    }

		    private GitPathStyle() {
			    // Singleton
		    }

            /** Quoting style that obeys the rules Git applies to file names */
            public static readonly GitPathStyle GIT_PATH = new GitPathStyle();
	    }
    }
}