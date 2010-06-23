/*
 * Copyright (C) 2009, Christian Halstrick <christian.halstrick@sap.com>
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
 * - Neither the name of the Eclipse Foundation, Inc. nor the
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
using System.Text;

namespace GitSharp.Tests.GitSharp.Core.Diff
{
    public class DiffTestDataGenerator {
    /*
	 * Generate sequence of characters in ascending order. The first character
	 * is a space. All subsequent characters have an ASCII code one greater then
	 * the ASCII code of the preceding character. On exception: the character
	 * following which follows '~' is again a ' '.
	 *
	 * @param len
	 *            length of the String to be returned
	 * @return the sequence of characters as String
	 */
        public static string generateSequence(int len) {
            return generateSequence(len, 0, 0);
        }

    /*
	 * Generate sequence of characters similar to the one returned by
	 * {@link #generateSequence(int)}. But this time in each chunk of
	 * <skipPeriod> characters the last <skipLength> characters are left out. By
	 * calling this method twice with two different prime skipPeriod values and
	 * short skipLength values you create test data which is similar to what
	 * programmers do to their source code - huge files with only few
	 * insertions/deletions/changes.
	 *
	 * @param len
	 *            length of the String to be returned
	 * @param skipPeriod
	 * @param skipLength
	 * @return the sequence of characters as String
	 */
        public static string generateSequence(int len, int skipPeriod,
                                              int skipLength) {
            StringBuilder text = new StringBuilder(len);
            int skipStart = skipPeriod - skipLength;
            int skippedChars = 0;
            for (int i = 0; i - skippedChars < len; ++i) {
                if (skipPeriod == 0 || i % skipPeriod < skipStart) {
                    text.Append((char) (32 + i % 95));
                } else {
                    skippedChars++;
                }
            }
            return text.ToString();
                                              }
    }
}